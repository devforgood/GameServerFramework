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
		if (fx.phase == "pulse")
			continue; // 패시브/오라의 주기 방출. Skill::Tick 이 pulse_interval 마다 실행한다.

		CastResult result = SkillEffectRegistry::Apply(fx, caster, state, *gamedata);
		if (result != CastResult::Success)
			return result;
	}

	return CastResult::Success;
}

void Skill::Tick(Actor* caster, SkillState& state, float dt)
{
	if (caster == nullptr || gamedata == nullptr || gamedata->pulse_interval <= 0.0)
		return;

	const float interval = static_cast<float>(gamedata->pulse_interval);
	state.pulseTimer += dt;

	// 한 틱에 interval 을 여러 번 넘겨도(프레임 지연 등) 밀린 pulse 를 모두 방출한다.
	while (state.pulseTimer >= interval)
	{
		state.pulseTimer -= interval;
		for (const auto& fx : gamedata->effects)
		{
			if (fx.phase != "pulse")
				continue;

			CastResult result = SkillEffectRegistry::Apply(fx, caster, state, *gamedata);
			if (result != CastResult::Success)
				LOG.error("Skill {} pulse effect '{}' failed: {}", gamedata->id, fx.type, static_cast<int>(result));
		}
	}
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
