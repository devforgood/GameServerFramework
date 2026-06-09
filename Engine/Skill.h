#pragma once
#include <vector>
#include "syncnet_generated.h"

enum skillProperties
{
	SKILL_NONE = 0,
	SKILL_JUMP = 1,
};

namespace gamedata
{
	class Skill; // Forward declaration of gamedata::Skill
}

class Actor; // Forward declaration
class Vector3; // Forward declaration

class Skill
{
public:
	const gamedata::Skill* gamedata; // Pointer to gamedata for skill information

protected:
	int skill_id_;
	std::vector<int> skill_properties_;
	float cooldown_;
	float duration_;
	bool is_casting_;
	Vector3* target_pos_;
	Actor* actor_;

public:
	Skill();
	virtual ~Skill();

	virtual int cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset)
	{
		return 0; // Default implementation does nothing
	}
	virtual void update(float deltaTime)
	{
		// Default implementation does nothing
	}

	virtual int end_duration_skill() 
	{
		return 0; // Default implementation does nothing
	}

};

