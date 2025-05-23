#include "Skill.h"
#include "Actor.h"
#include "World.h"
#include "Vector3.h"
#include "LogHelper.h"

Skill::Skill()
	: actor_(nullptr), skill_id_(0), target_pos_(nullptr), is_casting_(false), duration_(0.0f)
{
}

Skill::~Skill()
{
	if(target_pos_)
	{
		delete target_pos_;
		target_pos_ = nullptr;
	}
}

int Skill::cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset)
{
	if(!actor || !msg)
	{
		LOG.error("Invalid parameters: actor or msg is null.");
		return -1; // Invalid parameters
	}

	if(is_casting_)
	{
		LOG.error("Already casting a skill. Cannot cast another one.");
		return -1; // Already casting a skill
	}

	actor_ = actor;
	skill_id_ = msg->skillId();
	target_pos_ = new Vector3(msg->pos());
	duration_ = msg->duration() - serverClientTimeOffset; // Adjust duration with server-client time offset

	is_casting_ = true;
	actor_->set_input_locked(true); // Lock input while casting

	return 0;
}

void Skill::update(float dt)
{
	if(is_casting_)
	{
		duration_ -= dt;
		if(duration_ <= 0)
		{
			end_duration_skill();
			is_casting_ = false;
			actor_->set_input_locked(false); // Unlock input after casting
			duration_ = 0; 
		}
	}
}

int Skill::end_duration_skill()
{
	if(!is_casting_)
	{
		LOG.error("No skill is currently being casted.");
		return -1; // No skill is currently being casted
	}

	if(!actor_ || !target_pos_)
	{
		LOG.error("Invalid parameters: actor or target_pos is null.");
		return -1; // Invalid parameters
	}

	auto agent_id = actor_->agent_id();
	auto world = actor_->world();
	world->map()->teleportAgent(agent_id, target_pos_->pos());

	return 0;
}