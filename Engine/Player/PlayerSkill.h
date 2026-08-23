#pragma once
#include "Component.h"
#include "DbChangeTracker.h"
#include "./SQL/generated/vo.h"
#include <vector>

// 플레이어가 배운 스킬 목록.
//
// 실제 시전 상태(쿨다운/페이즈)는 Character 의 SkillSet 이 들고 있다. 여기는
// "무엇을 배웠는가"만 저장한다 — 스킬 보상, 스킬 보유 선행조건, 재접속 복원이 이 값을 본다.
class PlayerSkill : public ComponentBase<PlayerSkill>
{
public:
	virtual void Load(std::any data) override;
	virtual void Save(std::any data) override;

	// 이미 배운 스킬이면 레벨만 올린다(더 낮은 레벨로는 내리지 않는다).
	// 새로 배웠으면 true.
	bool LearnSkill(int skill_id, int level = 1);

	bool Has(int skill_id) const { return skills_.Contains(skill_id); }
	int GetLevel(int skill_id) const;

	void CollectSkillIds(std::vector<int>& out) const { skills_.CollectKeys(out); }
	size_t Count() const { return skills_.Size(); }

private:
	DbCollectionTracker<int, VOPlayerSkill> skills_;
	int characterId_ = 0;
};
