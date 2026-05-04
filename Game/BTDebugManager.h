#pragma once

#if !defined(NDEBUG) && !defined(ENABLE_BT_DEBUG)
#define ENABLE_BT_DEBUG 1
#endif

#include <cstdint>
#include <string>
#include <string_view>
#include <vector>

#include "behaviortree_cpp/bt_factory.h"

class Monster;

#if defined(ENABLE_BT_DEBUG)

class BTDebugManager
{
public:
	static BTDebugManager& Instance();

	void BeginTick(Monster* monster);
	void Record(Monster* monster, uint16_t node_id, std::string_view node_name, BT::NodeStatus status, std::string_view reason);
	void EndTick(Monster* monster);

	std::vector<std::string> ConsumeFrames();
	std::string BuildMonsterTreeDefinitionJson(int64_t monster_id) const;

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
