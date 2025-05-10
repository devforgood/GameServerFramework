#include "Skill.h"
#include "Actor.h"
#include "World.h"
#include "Vector3.h"

int Skill::cast_skill(Actor* actor, int skill_id, const syncnet::Vec3* target_pos)
{
	auto agent_id = actor->agent_id();
	auto world = actor->world();
	world->map()->teleportAgent(agent_id, Vector3(target_pos).pos());


	return 0;
}