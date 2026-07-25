#include "NavMovementFactory.h"
#include "CrowdNavMovement.h"
#include "WaypointNavMovement.h"
#include "LogHelper.h"

NavMovementType NavMovementFactory::ParseType(const std::string& type)
{
	if (type == "waypoint")
		return NavMovementType::Waypoint;
	if (type == "crowd")
		return NavMovementType::Crowd;

	// 기본은 waypoint(경로 추종)다. crowd 는 에이전트 수와 무관하게 정원(MAX_AGENTS)만큼
	// 매 틱 순회하고 회피 계산까지 도는 반면, waypoint 는 활성 에이전트만 전진시킨다
	// (10,000마리 기준 18.4ms vs 0.65ms — Benchmark/PERFORMANCE.md).
	// 군집 회피가 필요한 맵만 데이터에서 "crowd" 로 지정한다.
	if (!type.empty())
		LOG.warn("NavMovementFactory: unknown movement type '{}', fallback to waypoint", type);
	return NavMovementType::Waypoint;
}

INavMovement* NavMovementFactory::Create(NavMovementType type, NavMesh* nav)
{
	switch (type)
	{
	case NavMovementType::Crowd:
		return new CrowdNavMovement(nav);
	case NavMovementType::Waypoint:
	default:
		return new WaypointNavMovement(nav);
	}
}

INavMovement* NavMovementFactory::Create(const std::string& type, NavMesh* nav)
{
	return Create(ParseType(type), nav);
}
