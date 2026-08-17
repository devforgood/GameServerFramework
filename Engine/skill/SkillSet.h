#pragma once
#include <unordered_map>
#include <vector>
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
	// ResourceLoader 의 전체 스킬 테이블을 등록한다.
	// 플레이어 캐릭터에는 쓰지 않는다 — 보유 여부와 무관하게 모든 스킬을 쓸 수 있게 된다.
	// 몬스터/NPC 처럼 "가진 것"이 데이터로 고정된 액터나 테스트에서만 쓴다.
	void InitFromResources();

	// 플레이어가 실제로 배운 스킬만 등록한다(player_skill 테이블 → PlayerSkill 컴포넌트).
	// 기존 등록을 비우고 다시 채운다.
	void InitFromOwned(const std::vector<int>& skillIds);

	// skill.json 에서 starter=true 인 스킬 id 목록(신규 캐릭터 기본 지급용).
	static std::vector<int> CollectStarterSkillIds();

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
