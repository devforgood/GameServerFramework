#pragma once
#include "Skill.h"

class JumpSkill : public Skill
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
	JumpSkill();
	virtual ~JumpSkill();

	virtual int cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset);
	virtual void update(float deltaTime);
	virtual int end_duration_skill();
};

