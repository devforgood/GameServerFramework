#include "SkillEffects.h"
#include "Actor.h"
#include "Map.h"
#include "INavMovement.h"
#include "CombatSystem.h"
#include "gamedata.h"
#include "LogHelper.h"

#include <cmath>
#include <memory>
#include <unordered_map>

namespace
{

// 각도 계산 헬퍼: from→to 방향을 도 단위 각도로(0도=동쪽, 90도=북쪽).
float calculateAngle(const Vector3& from, const Vector3& to)
{
	float dx = to.x - from.x;
	float dy = to.z - from.z; // z축을 y축으로 매핑
	float angle = std::atan2(dy, dx) * 180.0f / 3.14159f;
	return angle < 0 ? angle + 360.0f : angle;
}

float calculateAngleDifference(float angle1, float angle2)
{
	float diff = std::abs(angle1 - angle2);
	return diff > 180.0f ? 360.0f - diff : diff;
}

// "damage": 목표 방향 부채꼴(range/angle) 안의 대상에게 min~max 데미지를 적용한다.
class DamageEffect : public ISkillEffect
{
public:
	virtual CastResult Apply(Actor* caster, SkillState& state, const gamedata::Skill& data) override
	{
		Map* map = caster->GetMap();
		if (map == nullptr)
			return CastResult::CasterInvalid;

		const Vector3 casterPos = caster->GetPosition();
		const Vector3& targetPos = state.targetPos;
		float targetAngle = calculateAngle(casterPos, targetPos);

		// 캐릭터를 목표 지점 방향으로 회전시키되, 회전이 덜 끝났으면 타겟 방향으로 판정한다.
		caster->RotateToTarget(targetPos, state.castTimeOffset);
		float attackDirection = caster->GetFrontAngleDegrees();
		if (calculateAngleDifference(attackDirection, targetAngle) > 45.0f)
			attackDirection = targetAngle;

		double damage = combat::RollDamage(caster, data.min_damage, data.max_damage);

		auto targets = map->get_actors_in_range(
			caster, static_cast<float>(data.range), attackDirection, static_cast<float>(data.angle));
		int hitCount = combat::ApplyAoEDamage(caster, targets, damage);

		LOG.debug("DamageEffect: skill {} dir {:.1f} range {} hit {} damage {:.1f}",
			data.id, attackDirection, data.range, hitCount, damage);

		return CastResult::Success;
	}
};

// "teleport": 시전 목표 지점으로 에이전트를 순간이동시킨다. (점프 착지 등, 보통 phase="end")
class TeleportEffect : public ISkillEffect
{
public:
	virtual CastResult Apply(Actor* caster, SkillState& state, const gamedata::Skill& data) override
	{
		Map* map = caster->GetMap();
		if (map == nullptr || map->GetNavMap() == nullptr)
			return CastResult::CasterInvalid;

		map->GetNavMap()->TeleportAgent(caster->GetActorId(), state.targetPos.pos());
		return CastResult::Success;
	}
};

// "input_lock": 캐스터 입력을 잠근다. Active 페이즈 종료 시 Skill::OnActiveEnd 가 해제한다.
class InputLockEffect : public ISkillEffect
{
public:
	virtual CastResult Apply(Actor* caster, SkillState& state, const gamedata::Skill& data) override
	{
		caster->SetInputLocked(true);
		state.inputLocked = true;
		return CastResult::Success;
	}
};

std::unordered_map<std::string, std::unique_ptr<ISkillEffect>> BuildRegistry()
{
	std::unordered_map<std::string, std::unique_ptr<ISkillEffect>> registry;
	registry.emplace("damage", std::make_unique<DamageEffect>());
	registry.emplace("teleport", std::make_unique<TeleportEffect>());
	registry.emplace("input_lock", std::make_unique<InputLockEffect>());
	return registry;
}

std::unordered_map<std::string, std::unique_ptr<ISkillEffect>>& Registry()
{
	static auto registry = BuildRegistry();
	return registry;
}

} // namespace

CastResult SkillEffectRegistry::Apply(const gamedata::SkillEffect& fx, Actor* caster, SkillState& state, const gamedata::Skill& data)
{
	if (caster == nullptr)
		return CastResult::CasterInvalid;

	auto& registry = Registry();
	auto itr = registry.find(fx.type);
	if (itr == registry.end())
	{
		LOG.error("SkillEffectRegistry: unknown effect type '{}' (skill {})", fx.type, data.id);
		return CastResult::UnknownEffect;
	}

	return itr->second->Apply(caster, state, data);
}

bool SkillEffectRegistry::Has(const std::string& type)
{
	return Registry().count(type) > 0;
}
