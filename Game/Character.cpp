#include "Character.h"
#include "World.h"
#include "LogHelper.h"
#include "skill.h"
#include "Vector3.h"

Character::Character(World* world) : Actor(world)
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

void Character::update()
{
	Actor::update();
}

bool Character::init(Vector3& pos)
{
	// Initialize character-specific properties here
	LOG.info("Character {} initialized", player_id_);

	float speed = 4.5f;

	int agent_id = world_->map()->addAgent(Vector3(pos).pos(), speed);
	if (agent_id < 0)
	{
		LOG.error("OnAddAgent error in Map.addAgent()");
		return false;
	}

	if (world_->game_object_map_.find(agent_id) != world_->game_object_map_.end())
	{
		LOG.error("OnAddAgent error already exist in monsters_map_");
		return false;
	}

	this->agent_id_ = agent_id;
	this->x = pos.x();
	this->y = pos.z();
	this->speed = speed;


	return true;
}