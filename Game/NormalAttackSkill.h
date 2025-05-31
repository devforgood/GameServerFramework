#pragma once
#include "BaseSkill.h"

class NormalAttackSkill : public BaseSkill
{
public:
	virtual int cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset);

};

