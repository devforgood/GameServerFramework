#include "NormalAttackSkill.h"
#include "Actor.h"
#include "World.h"
#include "Vector3.h"
#include "LogHelper.h"


int NormalAttackSkill::cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset)
{
	if (!actor || !msg)
	{
		LOG.error("Invalid parameters: actor or msg is null.");
		return -1; // Invalid parameters
	}

	if (is_casting_)
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