#include "MonsterSpawner.h"

#include <algorithm>
#include <cmath>

#include "LogHelper.h"
#include "RandomUtil.h"

namespace
{
	constexpr double kPi = 3.14159265358979323846;

	// 월드가 없는 환경(단위 테스트/벤치마크)에서 쓰는 난수원.
	// combat::RollDamage / PlayerLoot 와 같은 폴백 방식이다.
	RandomUtil& FallbackRandom()
	{
		static RandomUtil fallback;
		return fallback;
	}
}

int MonsterSpawner::ResolveCount(const Marker& marker)
{
	// count 필드가 없던 시절의 데이터는 0 으로 읽힌다. 마커 하나 = 한 마리가 그때의 의미다.
	return marker.count > 0 ? marker.count : 1;
}

float MonsterSpawner::ResolveRespawnSeconds(const Marker& marker)
{
	return marker.spawn_interval > 0 ? static_cast<float>(marker.spawn_interval) : 0.0f;
}

float MonsterSpawner::ResolveSpawnDelay(const Marker& marker)
{
	return marker.spawn_delay > 0 ? static_cast<float>(marker.spawn_delay) : 0.0f;
}

void MonsterSpawner::Build(const gamedata::Map* map_data, RandomUtil* random)
{
	groups_.clear();
	random_ = random;

	if (map_data == nullptr)
		return;

	groups_.reserve(map_data->spawn_points.monster_spawn.size());
	for (const Marker& marker : map_data->spawn_points.monster_spawn)
	{
		Group group;
		group.marker = &marker;
		group.desired = ResolveCount(marker);
		group.respawn_seconds = ResolveRespawnSeconds(marker);
		group.delay_remain = ResolveSpawnDelay(marker);
		group.alive.reserve(group.desired);
		groups_.push_back(std::move(group));
	}
}

int MonsterSpawner::SpawnInitial(const SpawnFn& spawn)
{
	int spawned = 0;
	for (Group& group : groups_)
	{
		// 지연이 걸린 마커는 여기서 세우지 않는다 — Update 가 시간을 흘려보낸 뒤 채운다.
		if (group.delay_remain > 0.0f)
			continue;

		spawned += Fill(group, spawn);
	}
	return spawned;
}

int MonsterSpawner::Update(float delta_seconds, const IsAliveFn& is_alive, const SpawnFn& spawn)
{
	int spawned = 0;

	for (Group& group : groups_)
	{
		// 죽었거나 월드에서 사라진 개체를 걷어낸다.
		// 사망(체력 0) 시점에 바로 빠지므로, 시체가 치워지는 2초를 기다리지 않고
		// 리스폰 시계가 돌기 시작한다.
		if (!group.alive.empty() && is_alive)
		{
			group.alive.erase(
				std::remove_if(group.alive.begin(), group.alive.end(),
					[&is_alive](int actor_id) { return !is_alive(actor_id); }),
				group.alive.end());
		}

		if (group.delay_remain > 0.0f)
		{
			group.delay_remain -= delta_seconds;
			if (group.delay_remain > 0.0f)
				continue;
			group.delay_remain = 0.0f;
		}

		const int missing = group.desired - static_cast<int>(group.alive.size());
		if (missing <= 0)
		{
			// 정원이 찼으면 시계를 되돌린다. 다음 사망은 온전한 주기를 기다린다.
			group.respawn_acc = 0.0f;
			continue;
		}

		// 최초 채움(지연이 끝난 직후)은 주기를 기다리지 않는다.
		if (group.initial_done)
		{
			if (group.respawn_seconds <= 0.0f)
				continue;   // 리스폰을 끄는 마커

			group.respawn_acc += delta_seconds;
			if (group.respawn_acc < group.respawn_seconds)
				continue;

			group.respawn_acc = 0.0f;
		}

		spawned += Fill(group, spawn);
	}

	return spawned;
}

int MonsterSpawner::Fill(Group& group, const SpawnFn& spawn)
{
	group.initial_done = true;

	if (!spawn)
		return 0;

	int spawned = 0;
	const int missing = group.desired - static_cast<int>(group.alive.size());
	for (int i = 0; i < missing; ++i)
	{
		double x = 0.0, y = 0.0, z = 0.0;
		ScatterPosition(group, x, y, z);

		const int actor_id = spawn(*group.marker, x, y, z);
		if (actor_id < 0)
		{
			// 흩뿌린 지점이 navmesh 밖일 수 있다. 이번 틱은 넘기고 다음 주기에 다시 시도한다.
			LOG.warn("monster_spawn {} 스폰 실패: pos({:.1f}, {:.1f}, {:.1f})",
				group.marker->id, x, y, z);
			continue;
		}

		group.alive.push_back(actor_id);
		++spawned;
	}

	return spawned;
}

void MonsterSpawner::ScatterPosition(const Group& group, double& out_x, double& out_y, double& out_z)
{
	const auto& position = group.marker->position;
	out_x = position.x;
	out_y = position.y;
	out_z = position.z;

	const double radius = group.marker->radius;
	if (radius <= 0.0)
		return;

	RandomUtil& random = random_ != nullptr ? *random_ : FallbackRandom();

	// 거리에 sqrt 를 씌워야 원 안에 고르게 퍼진다. 그냥 [0, radius) 를 뽑으면
	// 중심에 몰려서, 반경을 넓혀도 몬스터가 한 덩어리로 붙어 선다.
	const double angle = random.GetRandomDouble(0.0, 2.0 * kPi);
	const double distance = radius * std::sqrt(random.GetRandomDouble(0.0, 1.0));

	out_x += std::cos(angle) * distance;
	out_z += std::sin(angle) * distance;
}

int MonsterSpawner::AliveCount() const
{
	int count = 0;
	for (const Group& group : groups_)
		count += static_cast<int>(group.alive.size());
	return count;
}

int MonsterSpawner::DesiredCount() const
{
	int count = 0;
	for (const Group& group : groups_)
		count += group.desired;
	return count;
}
