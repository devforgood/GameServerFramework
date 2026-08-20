#include "WorldView.h"

namespace bot
{
	WorldView::ApplyResult WorldView::Apply(const syncnet::UpdateActorNotify* notify,
		double now, int self_actor_id)
	{
		ApplyResult result;
		if (notify == nullptr)
			return result;

		if (const auto* actors = notify->actors())
		{
			for (const syncnet::ActorInfo* info : *actors)
			{
				if (info == nullptr)
					continue;

				const int actor_id = info->actorId();
				auto found = actors_.find(actor_id);
				const bool is_new = (found == actors_.end());
				if (is_new)
				{
					found = actors_.emplace(actor_id, ActorSnapshot{}).first;
					found->second.actor_id = actor_id;
					++result.entered;
				}
				else
				{
					++result.updated;
				}

				ActorSnapshot& snapshot = found->second;
				const bool was_dead = !is_new && snapshot.IsDead();

				snapshot.type = info->gameObjectType();
				snapshot.input_locked = info->inputLocked();
				snapshot.last_update = now;

				if (const syncnet::Vec3* pos = info->pos())
					snapshot.pos = Vec3(*pos);

				if (const syncnet::ActorState* state = info->state())
					snapshot.state = state->state();

				if (const syncnet::ActorHealth* health = info->health())
				{
					snapshot.health = health->health();
					snapshot.has_health = true;
				}

				const bool is_dead = snapshot.IsDead();
				if (is_dead && !was_dead)
				{
					if (snapshot.type == syncnet::GameObjectType::GameObjectType_Monster)
						++result.monster_deaths;
					else if (self_actor_id != kNoSelf && actor_id == self_actor_id)
						result.self_died = true;
				}
			}
		}

		// 시야 밖으로 나간 액터. 사망이 아니므로 처치 수에는 넣지 않는다.
		if (const auto* removed = notify->removed())
		{
			for (int actor_id : *removed)
			{
				if (actors_.erase(actor_id) > 0)
					++result.removed;
			}
		}

		return result;
	}

	const ActorSnapshot* WorldView::Find(int actor_id) const
	{
		auto found = actors_.find(actor_id);
		return found != actors_.end() ? &found->second : nullptr;
	}

	int WorldView::FindNearestMonster(const Vec3& from, float radius) const
	{
		int nearest_id = 0;
		float nearest_distance_sq = radius * radius;

		for (const auto& entry : actors_)
		{
			const ActorSnapshot& snapshot = entry.second;
			if (snapshot.type != syncnet::GameObjectType::GameObjectType_Monster)
				continue;
			if (snapshot.IsDead())
				continue;

			const float distance_sq = DistanceSqXZ(from, snapshot.pos);
			if (distance_sq > nearest_distance_sq)
				continue;

			// 거리가 같으면 id 가 작은 쪽으로 고정한다. 그래야 컨테이너 순회 순서와
			// 무관하게 같은 입력에서 같은 대상을 고른다(테스트 재현성).
			if (distance_sq < nearest_distance_sq || nearest_id == 0 || entry.first < nearest_id)
			{
				nearest_distance_sq = distance_sq;
				nearest_id = entry.first;
			}
		}

		return nearest_id;
	}
}
