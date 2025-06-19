#include "NormalAttackSkill.h"
#include "Actor.h"
#include "World.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "GridManager.h"
#include "Common.h"
#include "RandomUtil.h"

#include <cmath>



int NormalAttackSkill::cast_skill(Actor* actor, const syncnet::UseSkill* msg, float serverClientTimeOffset)
{
	if (!actor || !msg)
	{
		LOG.error("Invalid parameters: actor or msg is null.");
		return -1; // Invalid parameters
	}

	if (is_casting_)
	{
		LOG.error("Already casting a skill. Cannot cast another one.");
		return -1; // Already casting a skill
	}


	// 공격 방향 설정 (메시지의 target_position을 기반으로)
	Vector3 actor_pos = actor->get_position();
	Vector3 target_pos(msg->pos());

	// 캐릭터를 목표 지점 방향으로 회전
	actor->rotate_to_target(target_pos, serverClientTimeOffset);

	double damage = actor->world()->random_util()->GetRandomDouble(gamedata->min_damage(), gamedata->max_damage());
	

	// GridManager를 통해 범위 내 대상 검색
	std::vector<IGridActor*> actors_in_range = actor->world()->get_actors_in_range(actor, gamedata->range(), gamedata->angle());

	// 부채꼴 범위 내 대상에게 데미지 적용
	for (auto target : actors_in_range) {
		if (target && target != actor) {
			// 데미지 적용
			target->decrementHealth(damage);
		}
	}

	return 0;
}

