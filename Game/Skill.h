#pragma once
#include <vector>
#include "syncnet_generated.h"

enum skillProperties
{
	SKILL_NONE = 0,
	SKILL_JUMP = 1,
};

class Actor; // Forward declaration
class Vector3; // Forward declaration

class Skill
{
private:
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

	int cast_skill(Actor * actor, const syncnet::UseSkill* msg, float serverClientTimeOffset);
	void update(float deltaTime);
	int end_duration_skill();

};

