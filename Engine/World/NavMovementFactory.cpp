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

	if (!type.empty())
		LOG.warn("NavMovementFactory: unknown movement type '{}', fallback to crowd", type);
	return NavMovementType::Crowd;
}

INavMovement* NavMovementFactory::Create(NavMovementType type, NavMesh* nav)
{
	switch (type)
	{
	case NavMovementType::Waypoint:
		return new WaypointNavMovement(nav);
	case NavMovementType::Crowd:
	default:
		return new CrowdNavMovement(nav);
	}
}

INavMovement* NavMovementFactory::Create(const std::string& type, NavMesh* nav)
{
	return Create(ParseType(type), nav);
}
