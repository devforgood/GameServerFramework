#include "pch.h"
#include <gtest/gtest.h>
#include <cmath>
#include <filesystem>
#include <memory>
#include <string>
#include "spdlog/spdlog.h"
#include "spdlog/sinks/stdout_sinks.h"

#include "GameData/ResourceLoader.h"
#include "gamedata.h"
#include "World.h"
#include "Map.h"
#include "INavMovement.h"
#include "Vector3.h"
#include "syncnet_generated.h"

namespace
{
	void EnsureNetLogger()
	{
		if (!spdlog::get("net"))
		{
			auto logger = std::make_shared<spdlog::logger>(
				"net", std::make_shared<spdlog::sinks::stdout_sink_mt>());
			logger->set_level(spdlog::level::warn);
			spdlog::register_logger(logger);
		}
	}
}

//---------------------------------------------------------------------------------------
// waypoint 이동 전략(경로 추종) 검증.
//
// 추격은 매 틱 같은 대상으로 SetMoveTarget 을 부르는데, 호출마다 경로를 새로 내면
// 틱 비용이 폭증한다(경로 산출 1회 ≈ 4us, Benchmark/PERFORMANCE.md).
// 그래서 목표가 거의 그대로면 기존 경로를 재사용하는데, 그 최적화가
// "목표가 실제로 움직였을 때 따라가는" 동작을 깨뜨리지 않는지 고정한다.
//---------------------------------------------------------------------------------------

class NavMovementTest : public ::testing::Test
{
protected:
	std::unique_ptr<World> world_;
	std::unique_ptr<Map> map_;
	INavMovement* nav_ = nullptr;
	double spawnX_ = 0, spawnY_ = 0, spawnZ_ = 0;

	static constexpr float kTickDt = 1.0f / 30.0f;
	static constexpr float kAgentSpeed = 3.5f; // 몬스터와 동일

	void SetUp() override
	{
		EnsureNetLogger();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "skill.json"))
			<< "통합 GameData 폴더를 찾지 못했습니다: " << dataPath;
		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath)) << "LoadResources 실패";

		const gamedata::Map* mapData = nullptr;
		for (const auto& [id, m] : ResourceLoader::Instance().GetMaps())
		{
			if (m == nullptr || m->navmesh_path.empty())
				continue;
			if (!std::filesystem::exists(GameDataPath::Resolve() + m->navmesh_path))
				continue;
			if (m->spawn_points.player_spawn.empty())
				continue;
			if (mapData == nullptr || m->id < mapData->id)
				mapData = m;
		}
		ASSERT_NE(mapData, nullptr) << "navmesh 가 배치된 맵이 없습니다.";

		world_ = std::make_unique<World>();
		map_ = std::make_unique<Map>(world_.get());
		ASSERT_TRUE(map_->Init("waypoint", mapData)) << "Map::Init 실패";
		nav_ = map_->GetNavMap();
		ASSERT_NE(nav_, nullptr);

		const auto& spawn = mapData->spawn_points.player_spawn[0].position;
		spawnX_ = spawn.x;
		spawnY_ = spawn.y;
		spawnZ_ = spawn.z;
	}

	// 스폰 지점 기준 오프셋에 에이전트를 추가하고 id 를 반환한다(실패 시 -1).
	int AddAgent(float offsetX, float offsetZ)
	{
		Vector3 pos(
			static_cast<float>(spawnX_) + offsetX,
			static_cast<float>(spawnY_),
			static_cast<float>(spawnZ_) + offsetZ);
		return nav_->AddAgent(pos.pos(), kAgentSpeed);
	}

	static float DistanceXZ(const float* a, const float* b)
	{
		const float dx = a[0] - b[0];
		const float dz = a[2] - b[2];
		return std::sqrt(dx * dx + dz * dz);
	}

	void Tick(int count)
	{
		for (int i = 0; i < count; ++i)
			nav_->Update(kTickDt);
	}
};

// 매 틱 목표를 다시 지정해도(추격) 이동이 멈추지 않는다.
// 경로 재사용이 "새 경로를 안 낸다"에 그쳐야지, "가던 길을 멈춘다"가 되면 안 된다.
TEST_F(NavMovementTest, KeepsMovingWhenTargetIsReissuedEveryTick)
{
	const int agent = AddAgent(0.0f, 0.0f);
	const int marker = AddAgent(4.0f, 0.0f); // navmesh 위임이 검증된 좌표를 목표로 쓴다
	ASSERT_GE(agent, 0);
	ASSERT_GE(marker, 0);

	float target[3];
	const float* markerPos = nav_->GetPos(marker);
	ASSERT_NE(markerPos, nullptr);
	target[0] = markerPos[0]; target[1] = markerPos[1]; target[2] = markerPos[2];

	float start[3];
	const float* startPos = nav_->GetPos(agent);
	ASSERT_NE(startPos, nullptr);
	start[0] = startPos[0]; start[1] = startPos[1]; start[2] = startPos[2];

	const float distanceBefore = DistanceXZ(start, target);
	ASSERT_GT(distanceBefore, 2.0f) << "목표가 너무 가까워 이동을 관찰할 수 없습니다";

	// 추격처럼 매 틱 목표를 다시 지정한다. 목표는 재계산 임계(1.5)보다 작게 흔들린다.
	for (int i = 0; i < 30; ++i)
	{
		float jittered[3] = { target[0], target[1], target[2] + ((i % 2) ? 0.2f : -0.2f) };
		nav_->SetMoveTarget(agent, jittered, false);
		nav_->Update(kTickDt);
	}

	const float distanceAfter = DistanceXZ(nav_->GetPos(agent), target);
	EXPECT_LT(distanceAfter, distanceBefore - 1.0f)
		<< "목표를 매 틱 재지정하니 이동이 진행되지 않았습니다(경로 재사용이 이동을 막음)";
}

// 목표가 임계보다 멀리 움직이면 경로를 다시 내서 새 목표를 향한다.
TEST_F(NavMovementTest, RepathsWhenTargetMovesBeyondThreshold)
{
	const int agent = AddAgent(0.0f, 0.0f);
	const int markerA = AddAgent(4.0f, 0.0f);
	const int markerB = AddAgent(-4.0f, 0.0f); // 반대 방향
	ASSERT_GE(agent, 0);
	ASSERT_GE(markerA, 0);
	ASSERT_GE(markerB, 0);

	float targetA[3], targetB[3];
	const float* a = nav_->GetPos(markerA);
	const float* b = nav_->GetPos(markerB);
	ASSERT_NE(a, nullptr);
	ASSERT_NE(b, nullptr);
	targetA[0] = a[0]; targetA[1] = a[1]; targetA[2] = a[2];
	targetB[0] = b[0]; targetB[1] = b[1]; targetB[2] = b[2];

	// 먼저 A 로 이동을 시작한다.
	nav_->SetMoveTarget(agent, targetA, false);
	Tick(10);

	const float distanceToBBefore = DistanceXZ(nav_->GetPos(agent), targetB);

	// 목표를 B 로 바꾼다(임계 1.5 를 훨씬 넘는 이동이라 반드시 재계산돼야 한다).
	nav_->SetMoveTarget(agent, targetB, false);
	Tick(30);

	const float distanceToBAfter = DistanceXZ(nav_->GetPos(agent), targetB);
	EXPECT_LT(distanceToBAfter, distanceToBBefore - 1.0f)
		<< "목표가 멀리 움직였는데 경로를 다시 내지 않았습니다";
}

// 배회(Patrol)로 경로를 받은 뒤 추격 목표가 들어오면 그쪽으로 방향을 바꾼다.
// (Patrol 이 경로 목표를 기록하지 않으면 첫 추격 지시가 재사용으로 무시된다.)
TEST_F(NavMovementTest, SwitchesFromPatrolToChaseTarget)
{
	const int agent = AddAgent(0.0f, 0.0f);
	const int marker = AddAgent(4.0f, 0.0f);
	ASSERT_GE(agent, 0);
	ASSERT_GE(marker, 0);

	float origin[3];
	const float* originPos = nav_->GetPos(agent);
	ASSERT_NE(originPos, nullptr);
	origin[0] = originPos[0]; origin[1] = originPos[1]; origin[2] = originPos[2];

	float dest[3];
	ASSERT_TRUE(nav_->Patrol(agent, origin, 20.0f, dest)) << "배회 목적지 산출 실패";
	Tick(5);

	float target[3];
	const float* markerPos = nav_->GetPos(marker);
	ASSERT_NE(markerPos, nullptr);
	target[0] = markerPos[0]; target[1] = markerPos[1]; target[2] = markerPos[2];

	const float distanceBefore = DistanceXZ(nav_->GetPos(agent), target);
	nav_->SetMoveTarget(agent, target, false);
	Tick(30);

	const float distanceAfter = DistanceXZ(nav_->GetPos(agent), target);
	EXPECT_LT(distanceAfter, distanceBefore) << "배회 중 받은 추격 목표가 무시됐습니다";
}
