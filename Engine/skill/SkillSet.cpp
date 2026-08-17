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
		// 몬스터 전용 스킬은 플레이어(캐릭터) 스킬 목록에서 제외한다.
		// 클라가 UseSkill 로 몬스터 스킬 id 를 보내도 SkillNotFound 로 거부된다.
		if (pair.second != nullptr && pair.second->monster_only)
			continue;

		int skillId = static_cast<int>(pair.first);
		Skill* skill = SkillRegistry::Instance().Get(skillId);
		if (skill != nullptr)
			AddSkill(skillId, skill);
	}
}

void SkillSet::InitFromOwned(const std::vector<int>& skillIds)
{
	entries_.clear();

	for (int skillId : skillIds)
	{
		const gamedata::Skill* data = ResourceLoader::Instance().GetSkill(skillId);
		if (data == nullptr)
		{
			LOG.warn("SkillSet: skill.json 에 id {} 가 없다. 등록하지 않는다.", skillId);
			continue;
		}

		// 배웠더라도 몬스터 전용 스킬은 캐릭터가 쓸 수 없다(데이터 오류 방어).
		if (data->monster_only)
			continue;

		Skill* skill = SkillRegistry::Instance().Get(skillId);
		if (skill != nullptr)
			AddSkill(skillId, skill);
	}
}

std::vector<int> SkillSet::CollectStarterSkillIds()
{
	std::vector<int> result;
	for (const auto& pair : ResourceLoader::Instance().GetSkills())
	{
		const gamedata::Skill* data = pair.second;
		if (data == nullptr || !data->starter || data->monster_only)
			continue;
		result.push_back(static_cast<int>(pair.first));
	}
	return result;
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
	// 패시브는 유저 액션으로 발동하지 않는다(보유만으로 Update 에서 지속 적용된다).
	// 클라가 UseSkill 로 패시브 id 를 보내도 여기서 거부한다.
	if (entry.skill->gamedata->type == "passive")
		return CastResult::SkillNotFound;
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
		entry.state.pulseTimer = 0.0f; // 오라 pulse 는 Active 진입 후 첫 interval 부터 방출한다.
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
		if (entry.skill == nullptr || entry.skill->gamedata == nullptr)
			continue; // 데이터 없이 등록된 스킬(예: 몬스터 melee 미존재)은 건너뛴다.

		// 패시브: 유저 액션/페이즈/쿨다운 없이 보유만으로 매 틱 지속 적용된다.
		// 오라 등 주기 효과는 Skill::Tick 이 pulse_interval 마다 방출한다.
		if (entry.skill->gamedata->type == "passive")
		{
			entry.skill->Tick(owner, entry.state, dt);
			continue;
		}

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
