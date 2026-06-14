#include "JumpSkill.h"
#include "Actor.h"
#include "World.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "Map.h"

JumpSkill::JumpSkill()
	: actor_(nullptr), skillId_(0), targetPos_(nullptr), isCasting_(false), duration_(0.0f)
{
}

JumpSkill::~JumpSkill()
{
	if (targetPos_)
	{
		delete targetPos_;
		targetPos_ = nullptr;
	}
}

int JumpSkill::cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset)
{
	if (!actor || !msg)
	{
		LOG.error("Invalid parameters: actor or msg is null.");
		return -1; // Invalid parameters
	}

	if (isCasting_)
	{
		LOG.error("Already casting a skill. Cannot cast another one.");
		return -1; // Already casting a skill
	}

	actor_ = actor;
	skillId_ = msg->skillId();
	targetPos_ = new Vector3(msg->pos());
	duration_ = msg->duration() - serverClientTimeOffset; // Adjust duration with server-client time offset

	isCasting_ = true;
	actor_->SetInputLocked(true); // Lock input while casting

	return 0;
}

void JumpSkill::update(float dt)
{
	if (isCasting_)
	{
		duration_ -= dt;
		if (duration_ <= 0)
		{
			end_duration_skill();
			isCasting_ = false;
			actor_->SetInputLocked(false); // Unlock input after casting
			duration_ = 0;
		}
	}
}

int JumpSkill::end_duration_skill()
{
	if (!isCasting_)
	{
		LOG.error("No skill is currently being casted.");
		return -1; // No skill is currently being casted
	}

	if (!actor_ || !targetPos_)
	{
		LOG.error("Invalid parameters: actor or target_pos is null.");
		return -1; // Invalid parameters
	}

	auto agent_id = actor_->GetActorId();
	auto map = actor_->GetMap();
	map->GetNavMap()->teleportAgent(agent_id, targetPos_->pos());

	return 0;
}