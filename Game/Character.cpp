#include "Character.h"
#include "World.h"
#include "LogHelper.h"
#include "skill.h"

Character::Character(int agent_id, World* world) : Actor(agent_id, world)
{
	// todo : load skills from DB table
	skills_[1] = new Skill();
}

Character::~Character() 
{
	for (auto& skill : skills_)
	{
		delete skill.second;
	}
	skills_.clear();
	LOG.info("Character {} destroyed", player_id_);
}

void Character::use_skill(int skill_id, const syncnet::Vec3* target_pos)
{
	// Implement skill usage logic here
	LOG.info("Character {} using skill {} at position ({}, {}, {})", player_id_, skill_id, target_pos->x(), target_pos->y(), target_pos->z());

	auto itr = skills_.find(skill_id);

	if (itr != skills_.end())
	{
		Skill* skill = itr->second;
		int result = skill->cast_skill(this, skill_id, target_pos);
		if (result == 0)
		{
			LOG.info("Skill {} cast successfully by character {}", skill_id, player_id_);
		}
		else
		{
			LOG.error("Failed to cast skill {} by character {}. Error code: {}", skill_id, player_id_, result);
		}

	}
	else
	{
		LOG.error("Skill {} not found for character {}", skill_id, player_id_);
	}
}