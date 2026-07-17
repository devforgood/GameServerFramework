#pragma once
#include <string>
#include "CastContext.h"
#include "SkillState.h"

namespace gamedata
{
	struct Skill;
	struct SkillEffect;
}
class Actor;

// 스킬 효과 단위. skill.json 의 effects: [{ "type", "phase" }] 항목 하나가 효과 하나에 대응한다.
// 새 효과는 클래스 하나 구현 + SkillEffectRegistry 등록으로 추가하고,
// 스킬은 데이터에서 효과들을 조합해 만든다(새 스킬마다 C++ 클래스가 필요 없다).
class ISkillEffect
{
public:
	virtual ~ISkillEffect() = default;
	virtual CastResult Apply(Actor* caster, SkillState& state, const gamedata::Skill& data) = 0;
};

// 효과 type 문자열 → 구현 매핑. 알 수 없는 type 이면 UnknownEffect 를 반환한다.
class SkillEffectRegistry
{
public:
	static CastResult Apply(const gamedata::SkillEffect& fx, Actor* caster, SkillState& state, const gamedata::Skill& data);
	static bool Has(const std::string& type);
};
