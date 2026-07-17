#include "SkillSet.h"
#include "Skill.h"
#include "SkillRegistry.h"
#include "Actor.h"
#include "gamedata.h"
#include "GameData/ResourceLoader.h"
#include "LogHelper.h"

#include <algorithm>

void SkillSet::InitFromResources()
{
	for (const auto& pair : ResourceLoader::Instance().GetSkills())
	{
		int skillId = static_cast<int>(pair.first);
		Skill* skill = SkillRegistry::Instance().Get(skillId);
		if (skill != nullptr)
			AddSkill(skillId, skill);
	}
}

void SkillSet::AddSkill(int skillId, Skill* skill)
{
	Entry entry;
	entry.skill = skill;
	entries_[skillId] = entry;
}

CastResult SkillSet::TryCast(Actor* caster, const CastContext& ctx)
{
	if (caster == nullptr)
		return CastResult::CasterInvalid;

	auto itr = entries_.find(ctx.skillId);
	if (itr == entries_.end() || itr->second.skill == nullptr || itr->second.skill->gamedata == nullptr)
		return CastResult::SkillNotFound;

	Entry& entry = itr->second;
	if (entry.state.phase == SkillPhase::Active)
		return CastResult::AlreadyActive;
	if (entry.state.phase == SkillPhase::Cooldown)
		return CastResult::OnCooldown;
	if (caster->IsInputLocked())
		return CastResult::InputLocked;

	entry.state.castTimeOffset = ctx.serverClientTimeOffset;

	CastResult result = entry.skill->Cast(caster, entry.state, ctx);
	if (result != CastResult::Success)
		return result;

	// Active 지속시간은 데이터(gamedata.duration)가 상한이다(서버 권위).
	// 클라 보고값은 시차 보정 후 상한 안에서만 반영한다.
	const gamedata::Skill* data = entry.skill->gamedata;
	float active = 0.0f;
	if (data->duration > 0)
	{
		active = static_cast<float>(data->duration);
		if (ctx.clientDuration > 0.0f)
		{
			float reported = ctx.clientDuration - ctx.serverClientTimeOffset;
			if (reported > 0.0f)
				active = std::min(active, reported);
		}
	}

	if (active > 0.0f)
	{
		entry.state.phase = SkillPhase::Active;
		entry.state.activeRemaining = active;
	}
	else
	{
		StartCooldown(entry);
	}

	return CastResult::Success;
}

void SkillSet::Update(Actor* owner, float dt)
{
	for (auto& pair : entries_)
	{
		Entry& entry = pair.second;
		switch (entry.state.phase)
		{
		case SkillPhase::Active:
			entry.skill->Tick(owner, entry.state, dt);
			entry.state.activeRemaining -= dt;
			if (entry.state.activeRemaining <= 0.0f)
			{
				entry.state.activeRemaining = 0.0f;
				entry.skill->OnActiveEnd(owner, entry.state);
				StartCooldown(entry);
			}
			break;

		case SkillPhase::Cooldown:
			entry.state.cooldownRemaining -= dt;
			if (entry.state.cooldownRemaining <= 0.0f)
			{
				entry.state.cooldownRemaining = 0.0f;
				entry.state.phase = SkillPhase::Ready;
			}
			break;

		default:
			break;
		}
	}
}

const SkillState* SkillSet::GetState(int skillId) const
{
	auto itr = entries_.find(skillId);
	return itr != entries_.end() ? &itr->second.state : nullptr;
}

void SkillSet::StartCooldown(Entry& entry)
{
	float cooldown = static_cast<float>(entry.skill->gamedata->cooldown);
	if (cooldown > 0.0f)
	{
		entry.state.phase = SkillPhase::Cooldown;
		entry.state.cooldownRemaining = cooldown;
	}
	else
	{
		entry.state.phase = SkillPhase::Ready;
	}
}
