#include <gtest/gtest.h>

#include <cmath>
#include <vector>

#include "MonsterSpawner.h"

//---------------------------------------------------------------------------------------
// monster_spawn 마커의 수량/리스폰 규칙 검증.
//
// 맵도 네비메시도 없이 돌린다 — 스포너는 "살아 있는가"와 "세워라"를 콜백으로만 받으므로,
// 여기서는 가짜 월드를 세워 스폰 타이밍만 고정한다. 리스폰은 눈으로 확인하려면 분 단위로
// 기다려야 하는 종류의 로직이라, 이 테스트가 없으면 회귀를 알아채기 어렵다.
//---------------------------------------------------------------------------------------

namespace
{
	using Marker = gamedata::MapSpawnPointsMonsterSpawn;

	// 스폰 요청을 받아 액터 id 를 발급하고, 죽이거나 지울 수 있는 최소한의 월드.
	class FakeWorld
	{
	public:
		struct Spawned
		{
			int actor_id = 0;
			int marker_id = 0;
			double x = 0.0;
			double y = 0.0;
			double z = 0.0;
		};

		MonsterSpawner::SpawnFn Spawn()
		{
			return [this](const Marker& marker, double x, double y, double z)
			{
				if (fail_next_ > 0)
				{
					--fail_next_;
					return -1;
				}

				const int actor_id = next_actor_id_++;
				spawned_.push_back({ actor_id, marker.id, x, y, z });
				alive_.push_back(actor_id);
				return actor_id;
			};
		}

		MonsterSpawner::IsAliveFn IsAlive()
		{
			return [this](int actor_id)
			{
				for (int id : alive_)
					if (id == actor_id)
						return true;
				return false;
			};
		}

		// 살아 있는 개체 중 count 마리를 죽인다(먼저 스폰된 순).
		void Kill(int count)
		{
			for (int i = 0; i < count && !alive_.empty(); ++i)
				alive_.erase(alive_.begin());
		}

		// 다음 count 번의 스폰 요청을 실패시킨다(navmesh 밖 지점 등).
		void FailNextSpawns(int count) { fail_next_ = count; }

		const std::vector<Spawned>& Spawned_() const { return spawned_; }
		size_t AliveCount() const { return alive_.size(); }
		size_t TotalSpawned() const { return spawned_.size(); }

	private:
		std::vector<Spawned> spawned_;
		std::vector<int> alive_;
		int next_actor_id_ = 1;
		int fail_next_ = 0;
	};

	Marker MakeMarker(int id, int count, int respawn_seconds,
		int spawn_delay = 0, double radius = 0.0)
	{
		Marker marker;
		marker.id = id;
		marker.monster_id = 1;
		marker.count = count;
		marker.spawn_interval = respawn_seconds;
		marker.spawn_delay = spawn_delay;
		marker.radius = radius;
		marker.position.x = 10.0;
		marker.position.y = 0.0;
		marker.position.z = -5.0;
		return marker;
	}

	// 마커 벡터는 Build 이후 재할당되면 안 된다(스포너가 원소 주소를 들고 있다).
	// 실제 데이터는 ResourceLoader 가 소유해 고정돼 있고, 여기서는 미리 다 채운 뒤 Build 한다.
	gamedata::Map MakeMapData(std::vector<Marker> markers)
	{
		gamedata::Map map;
		map.id = 1;
		map.spawn_points.monster_spawn = std::move(markers);
		return map;
	}

	// dt 초씩 seconds 만큼 시간을 흘린다(맵 틱과 같은 방식).
	int Advance(MonsterSpawner& spawner, FakeWorld& world, float seconds, float dt = 0.1f)
	{
		int spawned = 0;
		for (float elapsed = 0.0f; elapsed < seconds; elapsed += dt)
			spawned += spawner.Update(dt, world.IsAlive(), world.Spawn());
		return spawned;
	}
}

TEST(MonsterSpawnerTest, SpawnsCountMonstersPerMarker)
{
	gamedata::Map data = MakeMapData({ MakeMarker(10001, 5, 30), MakeMarker(10002, 3, 30) });

	MonsterSpawner spawner;
	spawner.Build(&data, nullptr);
	EXPECT_EQ(spawner.DesiredCount(), 8);

	FakeWorld world;
	EXPECT_EQ(spawner.SpawnInitial(world.Spawn()), 8);
	EXPECT_EQ(spawner.AliveCount(), 8);
	EXPECT_EQ(world.TotalSpawned(), 8u);
}

// count 필드가 없던 시절의 데이터는 0 으로 읽힌다 — 그때의 의미(마커 하나 = 한 마리)를 지킨다.
TEST(MonsterSpawnerTest, MissingCountMeansOneMonster)
{
	gamedata::Map data = MakeMapData({ MakeMarker(10001, 0, 0) });

	MonsterSpawner spawner;
	spawner.Build(&data, nullptr);

	FakeWorld world;
	EXPECT_EQ(spawner.SpawnInitial(world.Spawn()), 1);
	EXPECT_EQ(spawner.DesiredCount(), 1);
}

TEST(MonsterSpawnerTest, RefillsShortageAfterRespawnInterval)
{
	gamedata::Map data = MakeMapData({ MakeMarker(10001, 4, 10) });

	MonsterSpawner spawner;
	spawner.Build(&data, nullptr);

	FakeWorld world;
	ASSERT_EQ(spawner.SpawnInitial(world.Spawn()), 4);

	world.Kill(2);

	// 주기 전에는 채우지 않는다.
	EXPECT_EQ(Advance(spawner, world, 9.0f), 0);
	EXPECT_EQ(spawner.AliveCount(), 2);

	// 주기가 차면 부족분을 한 번에 채운다.
	EXPECT_EQ(Advance(spawner, world, 2.0f), 2);
	EXPECT_EQ(spawner.AliveCount(), 4);
	EXPECT_EQ(world.TotalSpawned(), 6u);
}

// 정원이 찬 동안 시계가 누적되면, 다음 사망 직후 곧바로 리스폰되어 주기가 무의미해진다.
TEST(MonsterSpawnerTest, DoesNotAccumulateTimerWhileFull)
{
	gamedata::Map data = MakeMapData({ MakeMarker(10001, 2, 10) });

	MonsterSpawner spawner;
	spawner.Build(&data, nullptr);

	FakeWorld world;
	ASSERT_EQ(spawner.SpawnInitial(world.Spawn()), 2);

	// 아무도 죽지 않은 채로 주기를 훌쩍 넘긴다.
	ASSERT_EQ(Advance(spawner, world, 60.0f), 0);

	world.Kill(1);
	EXPECT_EQ(Advance(spawner, world, 5.0f), 0) << "정원이 찼던 동안 시계가 누적됐다";
	EXPECT_EQ(Advance(spawner, world, 6.0f), 1);
}

TEST(MonsterSpawnerTest, IntervalZeroDisablesRespawn)
{
	gamedata::Map data = MakeMapData({ MakeMarker(10001, 3, 0) });

	MonsterSpawner spawner;
	spawner.Build(&data, nullptr);

	FakeWorld world;
	ASSERT_EQ(spawner.SpawnInitial(world.Spawn()), 3);

	world.Kill(3);
	EXPECT_EQ(Advance(spawner, world, 120.0f), 0);
	EXPECT_EQ(spawner.AliveCount(), 0);
}

TEST(MonsterSpawnerTest, SpawnDelayHoldsTheFirstSpawn)
{
	gamedata::Map data = MakeMapData({ MakeMarker(10001, 2, 30, /*spawn_delay*/ 5) });

	MonsterSpawner spawner;
	spawner.Build(&data, nullptr);

	FakeWorld world;
	// 지연이 걸린 마커는 맵이 열릴 때 세우지 않는다.
	EXPECT_EQ(spawner.SpawnInitial(world.Spawn()), 0);
	EXPECT_EQ(Advance(spawner, world, 4.0f), 0);

	// 지연이 끝나면 리스폰 주기를 기다리지 않고 곧바로 정원을 채운다.
	EXPECT_EQ(Advance(spawner, world, 2.0f), 2);
	EXPECT_EQ(spawner.AliveCount(), 2);
}

TEST(MonsterSpawnerTest, ScattersWithinRadius)
{
	constexpr double kRadius = 10.0;
	gamedata::Map data = MakeMapData({ MakeMarker(10001, 60, 30, 0, kRadius) });

	MonsterSpawner spawner;
	spawner.Build(&data, nullptr);

	FakeWorld world;
	ASSERT_EQ(spawner.SpawnInitial(world.Spawn()), 60);

	const auto& marker = data.spawn_points.monster_spawn.front();
	int outside_half = 0;
	for (const auto& spawned : world.Spawned_())
	{
		const double dx = spawned.x - marker.position.x;
		const double dz = spawned.z - marker.position.z;
		const double distance = std::sqrt(dx * dx + dz * dz);

		EXPECT_LE(distance, kRadius + 1e-6) << "반경 밖에 스폰됐다";
		EXPECT_DOUBLE_EQ(spawned.y, marker.position.y) << "높이는 마커 값을 유지해야 한다";

		if (distance > kRadius * 0.5)
			++outside_half;
	}

	// 균등 분포라면 넓이 비율상 약 3/4 가 바깥쪽 절반에 놓인다. sqrt 를 빼먹어
	// 중심에 몰리는 회귀를 여기서 잡는다(느슨한 하한만 본다).
	EXPECT_GT(outside_half, 20) << "중심에 몰려 있다 — 반경 분포가 균등하지 않다";
}

TEST(MonsterSpawnerTest, RadiusZeroSpawnsExactlyOnTheMarker)
{
	gamedata::Map data = MakeMapData({ MakeMarker(10001, 3, 30) });

	MonsterSpawner spawner;
	spawner.Build(&data, nullptr);

	FakeWorld world;
	ASSERT_EQ(spawner.SpawnInitial(world.Spawn()), 3);

	const auto& marker = data.spawn_points.monster_spawn.front();
	for (const auto& spawned : world.Spawned_())
	{
		EXPECT_DOUBLE_EQ(spawned.x, marker.position.x);
		EXPECT_DOUBLE_EQ(spawned.z, marker.position.z);
	}
}

// 흩뿌린 지점이 navmesh 밖이면 스폰이 실패한다. 그 자리를 채운 것으로 착각하면
// 마커가 영영 정원을 못 채운다 — 다음 주기에 다시 시도해야 한다.
TEST(MonsterSpawnerTest, RetriesFailedSpawnOnNextInterval)
{
	gamedata::Map data = MakeMapData({ MakeMarker(10001, 3, 10) });

	MonsterSpawner spawner;
	spawner.Build(&data, nullptr);

	FakeWorld world;
	world.FailNextSpawns(3);
	EXPECT_EQ(spawner.SpawnInitial(world.Spawn()), 0);
	EXPECT_EQ(spawner.AliveCount(), 0);

	EXPECT_EQ(Advance(spawner, world, 11.0f), 3);
	EXPECT_EQ(spawner.AliveCount(), 3);
}

TEST(MonsterSpawnerTest, BuildWithoutMapDataIsSafe)
{
	MonsterSpawner spawner;
	spawner.Build(nullptr, nullptr);

	FakeWorld world;
	EXPECT_EQ(spawner.GroupCount(), 0u);
	EXPECT_EQ(spawner.SpawnInitial(world.Spawn()), 0);
	EXPECT_EQ(spawner.Update(1.0f, world.IsAlive(), world.Spawn()), 0);
}
