#include "SkillRegistry.h"
#include "Skill.h"
#include "SkillFactory.h"
#include "Common.h" // ResourceLoader / gamedata::Skill

SkillRegistry& SkillRegistry::Instance()
{
	static SkillRegistry instance;
	return instance;
}

Skill* SkillRegistry::Get(int skillId)
{
	std::lock_guard<std::mutex> lock(mutex_);

	// 캐시된 Skill 은 gamedata 를 ResourceLoader 저장소를 가리키는 포인터로 들고 있다.
	// 리소스를 다시 로드하면 그 저장소가 통째로 교체되므로, 캐시를 그대로 두면 죽은
	// 포인터를 읽는다(SkillSet::Update 의 `gamedata->type == "passive"` 문자열 비교에서
	// 액세스 위반으로 터졌다). Clear() 를 부르는 것에 의존하지 않고 여기서 다시 묶는다.
	//
	// Skill 객체 자체는 버리지 않는다 — 이미 이 포인터를 들고 있는 SkillSet 이 있어서
	// 지우면 그쪽이 댕글링이 된다.
	const gamedata::Skill* current = ResourceLoader::Instance().GetSkill(skillId);

	auto itr = skills_.find(skillId);
	if (itr != skills_.end())
	{
		Skill* cached = itr->second.get();
		if (cached != nullptr && cached->gamedata != current)
			cached->gamedata = current;
		return cached;
	}

	if (current == nullptr)
		return nullptr;

	Skill* skill = SkillFactory::Create(skillId);
	if (skill == nullptr)
		return nullptr;

	skills_[skillId] = std::unique_ptr<Skill>(skill);
	return skill;
}

void SkillRegistry::Clear()
{
	std::lock_guard<std::mutex> lock(mutex_);
	skills_.clear();
}
