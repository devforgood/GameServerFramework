#include "SkillRegistry.h"
#include "Skill.h"
#include "SkillFactory.h"

SkillRegistry& SkillRegistry::Instance()
{
	static SkillRegistry instance;
	return instance;
}

Skill* SkillRegistry::Get(int skillId)
{
	std::lock_guard<std::mutex> lock(mutex_);

	auto itr = skills_.find(skillId);
	if (itr != skills_.end())
		return itr->second.get();

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
