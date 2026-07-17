#pragma once
#include <unordered_map>
#include "CastContext.h"
#include "SkillState.h"

class Actor;
class Skill;

// 액터가 보유한 스킬 목록과 스킬별 런타임 상태(SkillState).
//
// 시전의 단일 관문: 검증(보유/페이즈/쿨다운/입력잠금) → Skill::Cast → 페이즈 전환을
// TryCast 한 곳에서 처리한다. 검증에 실패하면 상태를 바꾸지 않으므로 호출 측은
// Success 일 때만 브로드캐스트하면 된다(서버 권위).
// CastContext 가 네트워크에 독립적이라 플레이어 핸들러와 AI(BT)가 같은 경로를 쓴다.
class SkillSet
{
public:
	// ResourceLoader 의 전체 스킬 테이블을 등록한다. (todo: DB 보유 스킬 목록으로 대체)
	void InitFromResources();

	// 외부에서 만든 스킬 정의를 직접 등록한다(테스트/특수용). 소유권은 호출 측에 있다.
	void AddSkill(int skillId, Skill* skill);

	CastResult TryCast(Actor* caster, const CastContext& ctx);

	// Active/Cooldown 페이즈 진행. 소유 액터의 Update 에서 매 틱 호출한다.
	void Update(Actor* owner, float dt);

	const SkillState* GetState(int skillId) const;
	bool HasSkill(int skillId) const { return entries_.find(skillId) != entries_.end(); }

private:
	struct Entry
	{
		Skill* skill = nullptr; // SkillRegistry(또는 AddSkill 호출자)가 소유
		SkillState state;
	};

	void StartCooldown(Entry& entry);

	std::unordered_map<int, Entry> entries_;
};
