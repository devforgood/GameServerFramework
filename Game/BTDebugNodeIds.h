#pragma once

#include <cstdint>

namespace BTDebugNodeId
{
	constexpr uint16_t ConditionDetectEnemy = 1;
	constexpr uint16_t ActionPatrol = 2;
	constexpr uint16_t ActionChase = 3;
	constexpr uint16_t ConditionAttackRange = 4;
	constexpr uint16_t ActionAttack = 5;
	constexpr uint16_t ConditionCheckHealth = 6;
	constexpr uint16_t ActionDead = 7;
	constexpr uint16_t ActionDestroyed = 8;
}
