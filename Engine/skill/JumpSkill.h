#pragma once
#include "Skill.h"

// 동작은 skill.json 의 effects 조합으로 정의된다:
//   input_lock(phase=active) → teleport(phase=end)
// 팩토리(code_name) 매칭과 클라이언트 클래스 대응을 위해 남겨둔 얇은 파생 클래스.
class JumpSkill : public Skill
{
};
