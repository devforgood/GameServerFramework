#include "Character.h"
#include "World.h"
#include "LogHelper.h"

Character::Character(int agent_id, World* world) : Actor(agent_id, world)
{
}

void Character::use_skill(int skill_id, const syncnet::Vec3* target_pos)
{
	// Implement skill usage logic here
	LOG.info("Character {} using skill {} at position ({}, {}, {})", player_id_, skill_id, target_pos->x(), target_pos->y(), target_pos->z());
}