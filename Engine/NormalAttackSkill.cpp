#include "NormalAttackSkill.h"
#include "Actor.h"
#include "World.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "Common.h"
#include "Map.h"

#include <cmath>

// 각도 계산 헬퍼 함수
float calculateAngle(const Vector3& from, const Vector3& to) {
	float dx = to.x - from.x;
	float dy = to.z - from.z; // z축을 y축으로 매핑
	float angle = std::atan2(dy, dx) * 180.0f / 3.14159f;
	return angle < 0 ? angle + 360.0f : angle;
}

// 각도 차이 계산 헬퍼 함수
float calculateAngleDifference(float angle1, float angle2) {
	float diff = std::abs(angle1 - angle2);
	return diff > 180.0f ? 360.0f - diff : diff;
}

int NormalAttackSkill::cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset)
{
	if (!actor || !msg)
	{
		LOG.error("Invalid parameters: actor or msg is null.");
		return -1;
	}

	if (isCasting_)
	{
		LOG.error("Already casting a skill. Cannot cast another one.");
		return -1;
	}

	// 공격 방향 설정
	Vector3 actor_pos = actor->get_position();
	Vector3 target_pos(msg->pos());
	
	// 타겟을 향한 방향 벡터 계산
	Vector3 to_target = (target_pos - actor_pos).normalized();
	to_target.y = 0; // y축 회전만 고려
	to_target = to_target.normalized();
	
	float target_angle = calculateAngle(actor_pos, target_pos);

	// 캐릭터를 목표 지점 방향으로 회전
	actor->rotate_to_target(target_pos, serverClientTimeOffset);

	// 공격 방향 결정 (회전이 완료되지 않았을 경우 타겟 방향으로 강제 설정)
	float attack_direction = actor->get_front_angle_degrees();
	if (calculateAngleDifference(attack_direction, target_angle) > 45.0f) {
		attack_direction = target_angle;
	}

	// 데미지 계산
	double damage = actor->map()->world()->random_util()->GetRandomDouble(gamedata->min_damage(), gamedata->max_damage());

	// AoE 범위 내 대상 검색
	std::vector<IGridActor*> actors_in_range = actor->map()->get_actors_in_range(actor, gamedata->range(), attack_direction, gamedata->angle());

	// 디버깅 로그 (개발 환경에서만)
#ifdef _DEBUG
	LOG.info("=== NormalAttackSkill Debug ===");
	LOG.info("Actor ID: {}, Position: ({}, {}, {})", actor->getAgentID(), actor_pos.x, actor_pos.y, actor_pos.z);
	LOG.info("Target Position: ({}, {}, {})", target_pos.x, target_pos.y, target_pos.z);
	LOG.info("Direction to target: ({}, {}, {}), Target angle: {:.2f} degrees", 
		to_target.x, to_target.y, to_target.z, target_angle);
	LOG.info("Attack Direction: {:.2f} degrees", attack_direction);
	LOG.info("Skill Range: {}, Skill Angle: {}, Damage: {}", gamedata->range(), gamedata->angle(), damage);
	
	// 주변 몬스터 정보 로그 (AoE 범위 내에 있는 것만)
	LOG.info("=== Targets in AoE Range ===");
	for (auto target : actors_in_range) {
		if (target && target != actor) {
			Vector3 target_position(target->getVector2X(), 0, target->getVector2Y());
			float distance = (target_position - actor_pos).length();
			float target_angle = calculateAngle(actor_pos, target_position);
			
			LOG.info("  - Target ID: {}, Position: ({}, {}), Distance: {:.2f}, Angle: {:.2f} degrees", 
				target->getAgentID(), target->getVector2X(), target->getVector2Y(), distance, target_angle);
		}
	}
#endif

	// 공격자가 플레이어 캐릭터라면 킬 판정을 위해 플레이어 ID를 확보(아니면 -1)
	long attacker_player_id = actor->player_id();

	// 데미지 적용
	int hit_count = 0;
	for (auto target : actors_in_range) {
		if (target && target != actor) {
			// 사망 시 킬러를 추적할 수 있도록 마지막 공격자를 기록
			target->setLastAttackerPlayerId(attacker_player_id);
			target->decrementHealth(damage);
			hit_count++;
#ifdef _DEBUG
			LOG.info("Applied {} damage to target ID: {}", damage, target->getAgentID());
#endif
		}
	}

#ifdef _DEBUG
	LOG.info("Hit {} targets, Total damage dealt: {}", hit_count, damage * hit_count);
	LOG.info("=== NormalAttackSkill End ===");
#endif

	return 0;
}

