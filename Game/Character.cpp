#include "Character.h"
#include "World.h"
#include "LogHelper.h"
#include "Skill.h"
#include "Vector3.h"
#include "TimeStamp.h"
#include "../GameDataProtobuf/ResourceLoader.h"
#include "SkillFactory.h"

Character::Character(World* world) : Actor(world)
{
	// todo : load skills from DB table
	auto skills = ResourceLoader::Instance().GetSkills();
	for (const auto& skill : skills)
	{
		skills_[skill.first] = SkillFactory::Create(skill.first);
	}

	is_input_locked_ = false;
	game_object_type_ = syncnet::GameObjectType::GameObjectType_Character;
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

void Character::use_skill(const syncnet::UseSkill* msg)
{
	float serverClientTimeOffset = world_->time_stamp_->getServerClientTimeOffset(msg->timestamp());

	// Implement skill usage logic here
	LOG.info("Character {} using skill {} at position ({}, {}, {}) serverClientTimeOffset {} timestamp {}"
		, player_id_, msg->skillId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z(), serverClientTimeOffset, msg->timestamp());

	auto itr = skills_.find(msg->skillId());

	if (itr != skills_.end())
	{
		Skill* skill = itr->second;
		int result = skill->cast_skill(this, msg, serverClientTimeOffset);
		if (result == 0)
		{
			LOG.info("Skill {} cast successfully by character {}", msg->skillId(), player_id_);
		}
		else
		{
			LOG.error("Failed to cast skill {} by character {}. Error code: {}", msg->skillId(), player_id_, result);
		}

	}
	else
	{
		LOG.error("Skill {} not found for character {}", msg->skillId(), player_id_);
	}
}

void Character::update(float dt)
{
	Actor::update(dt);
	for(auto itr = skills_.begin(); itr != skills_.end(); ++itr)
	{
		Skill* skill = itr->second;
		skill->update(dt);
	}
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

	this->set_position(pos.x, pos.y, pos.z);
	this->agent_id_ = agent_id;
	this->speed = speed;


	return true;
}