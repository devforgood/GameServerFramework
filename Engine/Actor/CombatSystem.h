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

	// 최종 피해량 계산. 순수 함수라 단위 테스트로 고정한다.
	//
	//   raw   = 스킬 굴림값 + 공격자 공격력
	//   final = raw * 100 / (100 + 방어력)
	//
	// 방어력은 빼기가 아니라 나누기로 넣는다. 빼기는 방어력이 공격력을 넘는 순간
	// 데미지가 0 이 되어 특정 구간에서 전투가 성립하지 않는다. 나누기는 수확 체감만
	// 만들어 항상 조금은 들어간다(최소 1).
	int ComputeDamage(double rolledDamage, int attackerAttack, int targetDefense);

	// 공격력 보정을 넣을지. 오라(패시브 pulse)는 빼고 넣는다 —
	// 시전도 쿨다운도 없이 0.5~2초마다 저절로 터지는데 매번 공격력을 통째로 더하면
	// 손도 안 댄 패시브가 액티브 스킬보다 세진다(레벨 20 공격력 130 × 초당 여러 번).
	// 데이터에 적힌 피해량(min~max)만으로 균형을 잡을 수 있게 한다.
	enum class AttackBonus
	{
		Include,   // 액티브 스킬·평타·몬스터 공격
		Exclude,   // 오라 등 저절로 도는 주기 피해
	};

	// 단일 대상 데미지 적용(킬 크레딧 기록 포함).
	// damage 는 스킬 굴림값이며, 공격자 공격력과 대상 방어력은 이 안에서 반영된다.
	void ApplyDamage(Actor* attacker, IGridActor* target, double damage,
		AttackBonus bonus = AttackBonus::Include);

	// AoE 대상 목록에 데미지 적용. 시전자 자신은 제외하며, 적용된 대상 수를 반환한다.
	int ApplyAoEDamage(Actor* attacker, const std::vector<IGridActor*>& targets, double damage,
		AttackBonus bonus = AttackBonus::Include);
}
