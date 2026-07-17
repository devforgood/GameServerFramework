#include "Skill.h"
#include "SkillEffects.h"
#include "Actor.h"
#include "gamedata.h"
#include "LogHelper.h"

CastResult Skill::Cast(Actor* caster, SkillState& state, const CastContext& ctx)
{
	if (caster == nullptr || gamedata == nullptr)
		return CastResult::CasterInvalid;

	state.targetPos = ctx.targetPos;

	for (const auto& fx : gamedata->effects)
	{
		if (fx.phase == "end")
			continue; // Active 페이즈 종료 시(OnActiveEnd) 실행된다

		CastResult result = SkillEffectRegistry::Apply(fx, caster, state, *gamedata);
		if (result != CastResult::Success)
			return result;
	}

	return CastResult::Success;
}

void Skill::OnActiveEnd(Actor* caster, SkillState& state)
{
	if (caster == nullptr || gamedata == nullptr)
		return;

	for (const auto& fx : gamedata->effects)
	{
		if (fx.phase != "end")
			continue;

		CastResult result = SkillEffectRegistry::Apply(fx, caster, state, *gamedata);
		if (result != CastResult::Success)
			LOG.error("Skill {} end effect '{}' failed: {}", gamedata->id, fx.type, static_cast<int>(result));
	}

	// input_lock 효과가 잠근 입력은 Active 종료와 함께 반드시 해제한다.
	if (state.inputLocked)
	{
		caster->SetInputLocked(false);
		state.inputLocked = false;
	}
}
