#include "BTDebugManager.h"

#if defined(ENABLE_BT_DEBUG)

#include "BTDebugNodeIds.h"
#include "Monster.h"

#include <mutex>
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
	std::vector<BTDebugDefinition> g_definitions;
	std::vector<BTDebugRuntimeFrame> g_frames;

	BTDebugNodeChange ToNodeChange(const BTNodeDebugState& node)
	{
		BTDebugNodeChange change;
		change.node_id = node.node_id;
		change.node_name = node.node_name;
		change.status = node.status;
		change.success_count = node.success_count;
		change.failure_count = node.failure_count;
		change.running_count = node.running_count;
		change.reason = node.reason;
		return change;
	}

	MonsterBTDebugContext& GetOrCreateContext(Monster* monster)
	{
		auto monster_id = static_cast<int64_t>(monster->GetAgentId());
		auto& context = g_contexts[monster_id];
		context.monster_id = monster_id;
		return context;
	}

	BTDebugRuntimeFrame BuildRuntimeFrame(const MonsterBTDebugContext& context)
	{
		BTDebugRuntimeFrame frame;
		frame.tree_id = "monster_ai";
		frame.monster_id = context.monster_id;
		frame.tick = context.bt_tick;
		frame.ai_state = context.ai_state;
		frame.target_agent_id = context.target_agent_id;
		frame.executed_path = context.executed_nodes_this_tick;
		frame.changes.reserve(context.changed_nodes.size());

		for (const auto& node : context.changed_nodes)
		{
			frame.changes.push_back(ToNodeChange(node));
		}

		return frame;
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
	context.ai_state = monster->GetState();
	context.target_agent_id = monster->targetAgentId_;
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
	context.ai_state = monster->GetState();
	context.target_agent_id = monster->targetAgentId_;

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
	context.ai_state = monster->GetState();
	context.target_agent_id = monster->targetAgentId_;

	if (!context.dirty)
		return;

	g_frames.push_back(BuildRuntimeFrame(context));
}

void BTDebugManager::PublishTreeDefinition(Monster* monster)
{
	if (monster == nullptr)
		return;

	std::lock_guard<std::mutex> lock(g_mutex);
	g_definitions.push_back(BuildMonsterTreeDefinition(monster->GetAgentId()));
}

BTDebugSyncSnapshot BTDebugManager::ConsumeSnapshot()
{
	std::lock_guard<std::mutex> lock(g_mutex);
	BTDebugSyncSnapshot snapshot;
	snapshot.definitions = std::move(g_definitions);
	snapshot.frames = std::move(g_frames);
	g_definitions.clear();
	g_frames.clear();
	return snapshot;
}

BTDebugDefinition BTDebugManager::BuildMonsterTreeDefinition(int64_t monster_id) const
{
	BTDebugDefinition definition;
	definition.tree_id = "monster_ai";
	definition.monster_id = monster_id;
	definition.nodes = {
		{ BTDebugNodeId::ConditionCheckHealth, -1, "ConditionCheckHealth", BTDebugNodeType::Condition },
		{ BTDebugNodeId::ConditionDetectEnemy, BTDebugNodeId::ConditionCheckHealth, "ConditionDetectEnemy", BTDebugNodeType::Condition },
		{ BTDebugNodeId::ConditionAttackRange, BTDebugNodeId::ConditionDetectEnemy, "ConditionAttackRange", BTDebugNodeType::Condition },
		{ BTDebugNodeId::ActionAttack, BTDebugNodeId::ConditionAttackRange, "ActionAttack", BTDebugNodeType::Action },
		{ BTDebugNodeId::ActionChase, BTDebugNodeId::ConditionDetectEnemy, "ActionChase", BTDebugNodeType::Action },
		{ BTDebugNodeId::ActionPatrol, BTDebugNodeId::ConditionDetectEnemy, "ActionPatrol", BTDebugNodeType::Action },
		{ BTDebugNodeId::ActionDead, -1, "ActionDead", BTDebugNodeType::Action },
		{ BTDebugNodeId::ActionDestroyed, BTDebugNodeId::ActionDead, "ActionDestroyed", BTDebugNodeType::Action },
	};
	return definition;
}

#endif
