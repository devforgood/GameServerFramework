#pragma once
#include "Skill.h"

class JumpSkill : public Skill
{
private:
	int skillId_;
	std::vector<int> skillProperties_;
	float cooldown_;
	float duration_;
	bool isCasting_;
	Vector3* targetPos_;
	Actor* actor_;

public:
	JumpSkill();
	virtual ~JumpSkill();

	virtual int cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset);
	virtual void update(float deltaTime);
	virtual int end_duration_skill();
};

