#include "BTDebugManager.h"

#if defined(ENABLE_BT_DEBUG)

#include "BTDebugNodeIds.h"
#include "Monster.h"

#include <mutex>
#include <sstream>
#include <unordered_map>

namespace
{
	struct BTNodeDebugState
	{
		uint16_t node_id = 0;
		std::string node_name;
		BT::NodeStatus status = BT::NodeStatus::IDLE;
		uint64_t last_tick = 0;
		uint32_t success_count = 0;
		uint32_t failure_count = 0;
		uint32_t running_count = 0;
		std::string reason;
	};

	struct MonsterBTDebugContext
	{
		int64_t monster_id = -1;
		uint64_t bt_tick = 0;
		syncnet::AIState ai_state = syncnet::AIState_Patrol;
		int64_t target_agent_id = -1;
		std::unordered_map<uint16_t, BTNodeDebugState> nodes;
		std::vector<uint16_t> executed_nodes_this_tick;
		std::vector<BTNodeDebugState> changed_nodes;
		bool dirty = false;
	};

	std::mutex g_mutex;
	std::unordered_map<int64_t, MonsterBTDebugContext> g_contexts;
	std::vector<std::string> g_frames;

	const char* ToString(BT::NodeStatus status)
	{
		switch (status)
		{
		case BT::NodeStatus::IDLE:
			return "IDLE";
		case BT::NodeStatus::RUNNING:
			return "RUNNING";
		case BT::NodeStatus::SUCCESS:
			return "SUCCESS";
		case BT::NodeStatus::FAILURE:
			return "FAILURE";
		case BT::NodeStatus::SKIPPED:
			return "SKIPPED";
		default:
			return "UNKNOWN";
		}
	}

	const char* ToString(syncnet::AIState state)
	{
		switch (state)
		{
		case syncnet::AIState_Patrol:
			return "Patrol";
		case syncnet::AIState_Detect:
			return "Detect";
		case syncnet::AIState_Attack:
			return "Attack";
		case syncnet::AIState_Dead:
			return "Dead";
		case syncnet::AIState_Destroyed:
			return "Destroyed";
		default:
			return "Unknown";
		}
	}

	std::string EscapeJson(std::string_view value)
	{
		std::string escaped;
		escaped.reserve(value.size());

		for (char ch : value)
		{
			switch (ch)
			{
			case '\\':
				escaped += "\\\\";
				break;
			case '"':
				escaped += "\\\"";
				break;
			case '\n':
				escaped += "\\n";
				break;
			case '\r':
				escaped += "\\r";
				break;
			case '\t':
				escaped += "\\t";
				break;
			default:
				escaped += ch;
				break;
			}
		}

		return escaped;
	}

	MonsterBTDebugContext& GetOrCreateContext(Monster* monster)
	{
		auto monster_id = static_cast<int64_t>(monster->agent_id());
		auto& context = g_contexts[monster_id];
		context.monster_id = monster_id;
		return context;
	}

	std::string BuildRuntimeFrameJson(const MonsterBTDebugContext& context)
	{
		std::ostringstream json;
		json << "{";
		json << "\"type\":\"TreeRuntimeFrame\",";
		json << "\"treeId\":\"monster_ai\",";
		json << "\"monsterId\":" << context.monster_id << ",";
		json << "\"tick\":" << context.bt_tick << ",";
		json << "\"aiState\":\"" << ToString(context.ai_state) << "\",";
		json << "\"targetAgentId\":" << context.target_agent_id << ",";

		json << "\"executedPath\":[";
		for (size_t i = 0; i < context.executed_nodes_this_tick.size(); ++i)
		{
			if (i > 0)
				json << ",";
			json << context.executed_nodes_this_tick[i];
		}
		json << "],";

		json << "\"changes\":[";
		for (size_t i = 0; i < context.changed_nodes.size(); ++i)
		{
			const auto& node = context.changed_nodes[i];
			if (i > 0)
				json << ",";
			json << "{";
			json << "\"id\":" << node.node_id << ",";
			json << "\"name\":\"" << EscapeJson(node.node_name) << "\",";
			json << "\"status\":\"" << ToString(node.status) << "\",";
			json << "\"reason\":\"" << EscapeJson(node.reason) << "\",";
			json << "\"successCount\":" << node.success_count << ",";
			json << "\"failureCount\":" << node.failure_count << ",";
			json << "\"runningCount\":" << node.running_count;
			json << "}";
		}
		json << "]";
		json << "}";

		return json.str();
	}
}

BTDebugManager& BTDebugManager::Instance()
{
	static BTDebugManager instance;
	return instance;
}

void BTDebugManager::BeginTick(Monster* monster)
{
	if (monster == nullptr)
		return;

	std::lock_guard<std::mutex> lock(g_mutex);
	auto& context = GetOrCreateContext(monster);
	context.bt_tick++;
	context.ai_state = monster->state();
	context.target_agent_id = monster->target_agent_id_;
	context.executed_nodes_this_tick.clear();
	context.changed_nodes.clear();
	context.dirty = false;
}

void BTDebugManager::Record(Monster* monster, uint16_t node_id, std::string_view node_name, BT::NodeStatus status, std::string_view reason)
{
	if (monster == nullptr)
		return;

	std::lock_guard<std::mutex> lock(g_mutex);
	auto& context = GetOrCreateContext(monster);
	auto& node = context.nodes[node_id];

	const bool changed = node.node_id == 0 ||
		node.status != status ||
		node.reason != reason ||
		node.node_name != node_name;

	node.node_id = node_id;
	node.node_name = std::string(node_name);
	node.status = status;
	node.last_tick = context.bt_tick;
	node.reason = std::string(reason);

	switch (status)
	{
	case BT::NodeStatus::SUCCESS:
		node.success_count++;
		break;
	case BT::NodeStatus::FAILURE:
		node.failure_count++;
		break;
	case BT::NodeStatus::RUNNING:
		node.running_count++;
		break;
	default:
		break;
	}

	context.executed_nodes_this_tick.push_back(node_id);
	context.ai_state = monster->state();
	context.target_agent_id = monster->target_agent_id_;

	if (changed)
	{
		context.changed_nodes.push_back(node);
		context.dirty = true;
	}
}

void BTDebugManager::EndTick(Monster* monster)
{
	if (monster == nullptr)
		return;

	std::lock_guard<std::mutex> lock(g_mutex);
	auto& context = GetOrCreateContext(monster);
	context.ai_state = monster->state();
	context.target_agent_id = monster->target_agent_id_;

	if (!context.dirty)
		return;

	g_frames.push_back(BuildRuntimeFrameJson(context));
}

std::vector<std::string> BTDebugManager::ConsumeFrames()
{
	std::lock_guard<std::mutex> lock(g_mutex);
	auto frames = std::move(g_frames);
	g_frames.clear();
	return frames;
}

std::string BTDebugManager::BuildMonsterTreeDefinitionJson(int64_t monster_id) const
{
	std::ostringstream json;
	json << "{";
	json << "\"type\":\"TreeDefinition\",";
	json << "\"treeId\":\"monster_ai\",";
	json << "\"monsterId\":" << monster_id << ",";
	json << "\"nodes\":[";
	json << "{\"id\":" << BTDebugNodeId::ConditionCheckHealth << ",\"parentId\":null,\"name\":\"ConditionCheckHealth\",\"type\":\"Condition\"},";
	json << "{\"id\":" << BTDebugNodeId::ConditionDetectEnemy << ",\"parentId\":" << BTDebugNodeId::ConditionCheckHealth << ",\"name\":\"ConditionDetectEnemy\",\"type\":\"Condition\"},";
	json << "{\"id\":" << BTDebugNodeId::ConditionAttackRange << ",\"parentId\":" << BTDebugNodeId::ConditionDetectEnemy << ",\"name\":\"ConditionAttackRange\",\"type\":\"Condition\"},";
	json << "{\"id\":" << BTDebugNodeId::ActionAttack << ",\"parentId\":" << BTDebugNodeId::ConditionAttackRange << ",\"name\":\"ActionAttack\",\"type\":\"Action\"},";
	json << "{\"id\":" << BTDebugNodeId::ActionChase << ",\"parentId\":" << BTDebugNodeId::ConditionDetectEnemy << ",\"name\":\"ActionChase\",\"type\":\"Action\"},";
	json << "{\"id\":" << BTDebugNodeId::ActionPatrol << ",\"parentId\":" << BTDebugNodeId::ConditionDetectEnemy << ",\"name\":\"ActionPatrol\",\"type\":\"Action\"},";
	json << "{\"id\":" << BTDebugNodeId::ActionDead << ",\"parentId\":null,\"name\":\"ActionDead\",\"type\":\"Action\"},";
	json << "{\"id\":" << BTDebugNodeId::ActionDestroyed << ",\"parentId\":" << BTDebugNodeId::ActionDead << ",\"name\":\"ActionDestroyed\",\"type\":\"Action\"}";
	json << "]";
	json << "}";
	return json.str();
}

#endif
