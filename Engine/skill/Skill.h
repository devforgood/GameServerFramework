#pragma once
#include "CastContext.h"
#include "SkillState.h"

namespace gamedata
{
	struct Skill; // Forward declaration of gamedata::Skill
}

class Actor;

// 스킬 정의(무상태 flyweight). 시전마다 달라지는 값은 SkillState 로 분리되어
// 캐릭터별 SkillSet 이 보관하므로, 스킬 인스턴스 하나를 모든 캐릭터가 공유한다.
//
// 기본 구현은 gamedata 의 effects 목록(skill.json)을 실행한다. 따라서 새 스킬은
// 데이터(effects 조합)만으로 추가할 수 있고, 데이터로 표현할 수 없는 특수 동작이
// 필요할 때만 파생 클래스에서 훅(Cast/Tick/OnActiveEnd)을 오버라이드한다.
class Skill
{
public:
	const gamedata::Skill* gamedata = nullptr; // Pointer to gamedata for skill information

	virtual ~Skill() = default;

	// 시전 시점 실행. phase 가 "end" 가 아닌 효과들을 적용한다.
	// Success 외를 반환하면 SkillSet 은 페이즈를 바꾸지 않는다(쿨다운 미소모).
	virtual CastResult Cast(Actor* caster, SkillState& state, const CastContext& ctx);

	// Active 페이즈 동안 매 틱 호출.
	virtual void Tick(Actor* caster, SkillState& state, float dt) {}

	// Active 페이즈 종료 시 호출. phase == "end" 효과 적용 + 입력 잠금 해제.
	virtual void OnActiveEnd(Actor* caster, SkillState& state);
};
