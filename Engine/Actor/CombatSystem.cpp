#include "CombatSystem.h"
#include "Actor.h"
#include "Map.h"
#include "World.h"
#include "RandomUtil.h"
#include "IGridActor.h"

namespace combat
{

double RollDamage(Actor* caster, int minDamage, int maxDamage)
{
	// 고정 데미지(min==max)나 뒤집힌 범위는 난수 없이 처리한다.
	// (boost uniform_real_distribution 은 min < max 를 assert 한다)
	if (minDamage >= maxDamage)
		return static_cast<double>(maxDamage >= minDamage ? maxDamage : minDamage);

	if (caster != nullptr && caster->GetMap() != nullptr
		&& caster->GetMap()->world() != nullptr
		&& caster->GetMap()->world()->random_util() != nullptr)
	{
		return caster->GetMap()->world()->random_util()->GetRandomDouble(minDamage, maxDamage);
	}

	// 월드 없이 도는 환경(단위 테스트 등) 폴백.
	static RandomUtil fallback;
	return fallback.GetRandomDouble(minDamage, maxDamage);
}

int ComputeDamage(double rolledDamage, int attackerAttack, int targetDefense)
{
	const double raw = rolledDamage + static_cast<double>(attackerAttack > 0 ? attackerAttack : 0);
	const double defense = static_cast<double>(targetDefense > 0 ? targetDefense : 0);

	const double mitigated = raw * 100.0 / (100.0 + defense);

	const int result = static_cast<int>(mitigated);
	return result > 0 ? result : 1; // 아무리 단단해도 최소 1 은 들어간다
}

void ApplyDamage(Actor* attacker, IGridActor* target, double damage)
{
	if (attacker == nullptr || target == nullptr)
		return;

	const int finalDamage = ComputeDamage(damage, attacker->GetAttack(), target->GetDefense());

	// 사망 시 킬러를 추적할 수 있도록 마지막 공격자를 데미지보다 먼저 기록한다.
	target->SetLastAttacker(attacker->GetActorId());
	target->DecrementHealth(finalDamage);
}

int ApplyAoEDamage(Actor* attacker, const std::vector<IGridActor*>& targets, double damage)
{
	int hitCount = 0;
	for (IGridActor* target : targets)
	{
		if (target == nullptr || target == static_cast<IGridActor*>(attacker))
			continue;

		ApplyDamage(attacker, target, damage);
		++hitCount;
	}
	return hitCount;
}

} // namespace combat
