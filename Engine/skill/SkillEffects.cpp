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

// "aoe_damage": 시전 목표 지점(state.targetPos) 중심 반경(radius) 원형 범위에 min~max 데미지.
// 캐스터 중심 부채꼴인 "damage" 와 달리 '땅에 떨어지는' 광역기(메테오/블리자드/파이어볼 폭발 등)에 쓴다.
// radius 가 없으면 range 를 반경으로 폴백한다. 체력 감소는 combat 단일 경로를 거쳐 동기화된다.
class AoEDamageEffect : public ISkillEffect
{
public:
	virtual CastResult Apply(Actor* caster, SkillState& state, const gamedata::Skill& data) override
	{
		Map* map = caster->GetMap();
		if (map == nullptr)
			return CastResult::CasterInvalid;

		double damage = combat::RollDamage(caster, data.min_damage, data.max_damage);

		float radius = static_cast<float>(data.radius > 0 ? data.radius : data.range);
		const Vector3& center = state.targetPos;
		auto targets = map->get_actors_in_radius(center.get_vecter2_x(), center.get_vecter2_y(), radius);
		int hitCount = combat::ApplyAoEDamage(caster, targets, damage);

		LOG.debug("AoEDamageEffect: skill {} radius {:.1f} hit {} damage {:.1f}",
			data.id, radius, hitCount, damage);

		return CastResult::Success;
	}
};

// "aura_damage": 캐스터의 현재 위치 중심 반경(radius) 원형에 데미지. 오라(pulse)용 —
// 시전 지점 고정인 aoe_damage 와 달리 캐스터를 따라다니며, damage 와 달리 회전/방향 판정이
// 없다. 성화(Holy Fire) 같은 지속 오라가 매 pulse 마다 주변 적을 태우는 데 쓴다.
class AuraDamageEffect : public ISkillEffect
{
public:
	virtual CastResult Apply(Actor* caster, SkillState& state, const gamedata::Skill& data) override
	{
		Map* map = caster->GetMap();
		if (map == nullptr)
			return CastResult::CasterInvalid;

		double damage = combat::RollDamage(caster, data.min_damage, data.max_damage);

		float radius = static_cast<float>(data.radius > 0 ? data.radius : data.range);
		auto targets = map->get_actors_in_radius(caster->GetVecter2X(), caster->GetVecter2Y(), radius);
		combat::ApplyAoEDamage(caster, targets, damage);

		return CastResult::Success;
	}
};

// "heal": 캐스터 자신의 체력을 heal 만큼 회복한다. 체력 변경은 Health 플래그로 자동
// 동기화되므로 별도 브로드캐스트가 필요 없다(서버 권위 그대로 유지).
// TODO(개선): 최대 체력(MaxHealth) 개념이 없어 오버힐이 가능하다. MaxHealth 도입 시 clamp 한다.
class HealEffect : public ISkillEffect
{
public:
	virtual CastResult Apply(Actor* caster, SkillState& state, const gamedata::Skill& data) override
	{
		if (data.heal > 0)
			caster->IncrementHealth(data.heal);
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

// "dash": 캐스터를 목표 지점 방향으로 range 만큼(더 가까우면 목표까지) 돌진시킨다(팔라딘 차지).
// 한 번에 사라졌다 나타나는 teleport 와 달리 duration 동안 지면을 따라 전진하므로,
// 여기서는 도착 지점(state.targetPos 보정)과 속도만 정하고 실제 전진은 Skill::Tick(skill_dash::Step)이 한다.
// 도착 지점을 state.targetPos 에 되써 두면 phase="end" 효과(aoe_damage 등)가 착지 지점에 적용된다.
class DashEffect : public ISkillEffect
{
public:
	virtual CastResult Apply(Actor* caster, SkillState& state, const gamedata::Skill& data) override
	{
		Map* map = caster->GetMap();
		if (map == nullptr || map->GetNavMap() == nullptr)
			return CastResult::CasterInvalid;

		const Vector3 from = caster->GetPosition();
		Vector3 delta = state.targetPos - from;
		delta.y = 0.0f;

		float distance = delta.length();
		if (distance < 0.01f)
		{
			state.dashing = false; // 제자리 시전 — 이동 없이 end 효과만 적용된다
			return CastResult::Success;
		}

		const float maxDistance = static_cast<float>(data.range);
		if (maxDistance > 0.0f && distance > maxDistance)
		{
			state.targetPos = from + delta.normalized() * maxDistance;
			distance = maxDistance;
		}

		caster->SetFrontVector(delta.x, 0.0f, delta.z); // 돌진 방향을 바로 바라보게 한다

		// 속도는 '최대 사거리를 duration 안에 주파하는 속도'로 고정한다(거리/duration 이 아니다).
		// 거리에 비례해 속도를 정하면 가까이 찍었을 때 걷기보다 느려져 돌진처럼 보이지 않는다.
		// 가까운 목표는 대신 일찍 도착하고, 도착 즉시 Active 가 끝나 착지 효과가 터진다(skill_dash::Finish).
		const float duration = static_cast<float>(data.duration);
		const float fullDistance = maxDistance > 0.0f ? maxDistance : distance;
		state.dashSpeed = duration > 0.0f ? fullDistance / duration : fullDistance;
		state.dashing = true;

		LOG.debug("DashEffect: skill {} distance {:.1f} speed {:.1f}", data.id, distance, state.dashSpeed);
		return CastResult::Success;
	}
};

// "knockback": 캐스터 중심 radius(없으면 range) 안의 대상을 캐스터 반대 방향으로 knockback 만큼 밀어낸다.
// 밀린 위치는 TeleportAgent 가 네비메시로 스냅하므로 벽 너머로 밀려나지 않는다.
// 중심이 항상 캐스터라 강타/전투 함성처럼 캐스터 주변을 때리는 스킬에 쓴다(시전 지점 광역기와 조합하지 않는다).
class KnockbackEffect : public ISkillEffect
{
public:
	virtual CastResult Apply(Actor* caster, SkillState& state, const gamedata::Skill& data) override
	{
		Map* map = caster->GetMap();
		if (map == nullptr || map->GetNavMap() == nullptr)
			return CastResult::CasterInvalid;
		if (data.knockback <= 0)
			return CastResult::Success;

		INavMovement* nav = map->GetNavMap();
		const float radius = static_cast<float>(data.radius > 0 ? data.radius : data.range);
		const float centerX = caster->GetVecter2X();
		const float centerZ = caster->GetVecter2Y();

		auto targets = map->get_actors_in_radius(centerX, centerZ, radius);
		for (IGridActor* target : targets)
		{
			if (target == nullptr || target == static_cast<IGridActor*>(caster))
				continue;

			const float* pos = nav->GetPos(target->GetActorId());
			if (pos == nullptr)
				continue;

			float dx = pos[0] - centerX;
			float dz = pos[2] - centerZ;
			float length = std::sqrt(dx * dx + dz * dz);
			if (length < 0.001f)
			{
				// 캐스터와 완전히 겹친 대상은 방향을 정할 수 없으므로 캐스터 정면으로 민다.
				const Vector3& front = caster->GetFrontVector();
				dx = front.x;
				dz = front.z;
				length = std::sqrt(dx * dx + dz * dz);
				if (length < 0.001f)
					continue;
			}

			const float scale = static_cast<float>(data.knockback) / length;
			float dest[3] = { pos[0] + dx * scale, pos[1], pos[2] + dz * scale };
			nav->TeleportAgent(target->GetActorId(), dest);
		}

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
	registry.emplace("aoe_damage", std::make_unique<AoEDamageEffect>());
	registry.emplace("aura_damage", std::make_unique<AuraDamageEffect>());
	registry.emplace("heal", std::make_unique<HealEffect>());
	registry.emplace("teleport", std::make_unique<TeleportEffect>());
	registry.emplace("dash", std::make_unique<DashEffect>());
	registry.emplace("knockback", std::make_unique<KnockbackEffect>());
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

namespace skill_dash
{

void Step(Actor* caster, SkillState& state, float dt)
{
	if (!state.dashing || caster == nullptr)
		return;

	Map* map = caster->GetMap();
	if (map == nullptr || map->GetNavMap() == nullptr)
	{
		state.dashing = false;
		return;
	}

	INavMovement* nav = map->GetNavMap();
	// 현재 위치는 네비 에이전트가 권위다(Actor::position_ 는 ECS 동기화로 한 틱 늦을 수 있다).
	const float* pos = nav->GetPos(caster->GetActorId());
	if (pos == nullptr)
	{
		state.dashing = false;
		return;
	}

	Vector3 delta = state.targetPos - Vector3(pos[0], pos[1], pos[2]);
	delta.y = 0.0f;

	const float remaining = delta.length();
	const float step = state.dashSpeed * dt;
	if (remaining <= step || remaining < 0.01f)
	{
		Finish(caster, state);
		return;
	}

	Vector3 next = Vector3(pos[0], pos[1], pos[2]) + delta.normalized() * step;
	nav->TeleportAgent(caster->GetActorId(), next.pos());
}

void Finish(Actor* caster, SkillState& state)
{
	if (!state.dashing || caster == nullptr)
		return;

	state.dashing = false;

	// 도착했으면 남은 Active 를 소진시켜 그 틱에 바로 OnActiveEnd(착지 효과 + 입력 잠금 해제)로 넘어가게 한다.
	// 속도가 고정이라 가까운 목표는 duration 보다 일찍 도착하는데, 그동안 잠긴 채 서 있으면 어색하다.
	// (OnActiveEnd 에서 불릴 때는 이미 dashing==false 라 위 early return 으로 걸러진다.)
	state.activeRemaining = 0.0f;

	Map* map = caster->GetMap();
	if (map == nullptr || map->GetNavMap() == nullptr)
		return;

	map->GetNavMap()->TeleportAgent(caster->GetActorId(), state.targetPos.pos());
}

} // namespace skill_dash
