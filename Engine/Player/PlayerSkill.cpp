#include "PlayerSkill.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "GameData/ResourceLoader.h"

void PlayerSkill::Load(std::any data)
{
	const auto& load_data = std::any_cast<const PlayerLoadData&>(data);

	characterId_ = static_cast<int>(load_data.player.id);

	skills_.Clear();
	for (const auto& vo : load_data.skills)
		skills_.AddPersisted(vo.skill_id, vo);
}

void PlayerSkill::Save(std::any data)
{
	auto* save_data = std::any_cast<PlayerSaveData*>(data);

	if (skills_.HasPendingChanges())
		skills_.Flush(save_data->skills.emplace());
}

bool PlayerSkill::LearnSkill(int skill_id, int level)
{
	if (skill_id <= 0 || level <= 0)
		return false;

	// 데이터에 없는 스킬은 배울 수 없다.
	if (ResourceLoader::Instance().GetSkill(skill_id) == nullptr)
		return false;

	if (PlayerSkillVO* vo = skills_.Modify(skill_id))
	{
		if (level > vo->level)
		{
			vo->level = level;
			markDirty();
		}
		return false;
	}

	PlayerSkillVO fresh{};
	fresh.character_id = characterId_;
	fresh.skill_id = skill_id;
	fresh.level = level;
	if (!skills_.Add(skill_id, fresh))
		return false;

	markDirty();
	return true;
}

int PlayerSkill::GetLevel(int skill_id) const
{
	const PlayerSkillVO* vo = skills_.Find(skill_id);
	return vo != nullptr ? vo->level : 0;
}
