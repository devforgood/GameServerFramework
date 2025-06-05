#pragma once
#include "BaseSkill.h"
#include "Vector3.h"

class NormalAttackSkill : public BaseSkill
{
public:

	virtual int cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset) override;


};

