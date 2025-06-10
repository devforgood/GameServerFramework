#pragma once
#include "Skill.h"
#include "Vector3.h"

class NormalAttackSkill : public Skill
{
public:

	virtual int cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset) override;


};

