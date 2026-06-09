#pragma once
#include "Skill.h"
#include "Vector3.h"

// 각도 계산 헬퍼 함수들
float calculateAngle(const Vector3& from, const Vector3& to);
float calculateAngleDifference(float angle1, float angle2);

class NormalAttackSkill : public Skill
{
public:

	virtual int cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset) override;


};

