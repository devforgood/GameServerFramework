#pragma once

#if !defined(NDEBUG) && !defined(ENABLE_BT_DEBUG)
#define ENABLE_BT_DEBUG 1
#endif

#include <cstdint>
#include <string>
#include <string_view>
#include <vector>

#include "behaviortree_cpp/bt_factory.h"
#include "syncnet_generated.h"

class Monster;

#if defined(ENABLE_BT_DEBUG)

enum class BTDebugNodeType : uint8_t
{
	Control,
	Condition,
	Action,
};

struct BTDebugNodeDefinition
{
	uint16_t node_id = 0;
	int32_t parent_node_id = -1;
	std::string name;
	BTDebugNodeType node_type = BTDebugNodeType::Action;
};

struct BTDebugNodeChange
{
	uint16_t node_id = 0;
	std::string node_name;
	BT::NodeStatus status = BT::NodeStatus::IDLE;
	uint32_t success_count = 0;
	uint32_t failure_count = 0;
	uint32_t running_count = 0;
	std::string reason;
};

struct BTDebugDefinition
{
	std::string tree_id;
	int64_t monster_id = -1;
	std::vector<BTDebugNodeDefinition> nodes;
};

struct BTDebugRuntimeFrame
{
	std::string tree_id;
	int64_t monster_id = -1;
	uint64_t tick = 0;
	syncnet::AIState ai_state = syncnet::AIState_Patrol;
	int64_t target_agent_id = -1;
	std::vector<uint16_t> executed_path;
	std::vector<BTDebugNodeChange> changes;
};

struct BTDebugSyncSnapshot
{
	std::vector<BTDebugDefinition> definitions;
	std::vector<BTDebugRuntimeFrame> frames;

	bool empty() const { return definitions.empty() && frames.empty(); }
};

class BTDebugManager
{
public:
	static BTDebugManager& Instance();

	void BeginTick(Monster* monster);
	void Record(Monster* monster, uint16_t node_id, std::string_view node_name, BT::NodeStatus status, std::string_view reason);
	void EndTick(Monster* monster);
	void PublishTreeDefinition(Monster* monster);

	BTDebugSyncSnapshot ConsumeSnapshot();
	BTDebugDefinition BuildMonsterTreeDefinition(int64_t monster_id) const;

private:
	BTDebugManager() = default;
};

#define BT_DEBUG_BEGIN_TICK(monster) BTDebugManager::Instance().BeginTick(monster)
#define BT_DEBUG_RECORD(monster, node_id, node_name, status, reason) \
	BTDebugManager::Instance().Record(monster, node_id, node_name, status, reason)
#define BT_DEBUG_END_TICK(monster) BTDebugManager::Instance().EndTick(monster)

#else

#define BT_DEBUG_BEGIN_TICK(monster) do {} while (0)
#define BT_DEBUG_RECORD(monster, node_id, node_name, status, reason) do {} while (0)
#define BT_DEBUG_END_TICK(monster) do {} while (0)

#endif
