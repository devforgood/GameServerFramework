#pragma once
#include <vector>

class Actor;
class IGridActor;

// 데미지 적용 단일 경로. 플레이어 스킬/몬스터 공격 등 모든 데미지는 이 경로를 거쳐
// 킬 크레딧(SetLastAttacker) → 체력 감소가 항상 같은 순서로 처리되게 한다.
namespace combat
{
	// [minDamage, maxDamage] 균등 분포 데미지. 캐스터가 속한 월드의 RandomUtil 을 쓰고,
	// 월드가 없는 환경(단위 테스트 등)에서는 지역 RandomUtil 로 폴백한다.
	double RollDamage(Actor* caster, int minDamage, int maxDamage);

	// 단일 대상 데미지 적용(킬 크레딧 기록 포함).
	void ApplyDamage(Actor* attacker, IGridActor* target, double damage);

	// AoE 대상 목록에 데미지 적용. 시전자 자신은 제외하며, 적용된 대상 수를 반환한다.
	int ApplyAoEDamage(Actor* attacker, const std::vector<IGridActor*>& targets, double damage);
}
