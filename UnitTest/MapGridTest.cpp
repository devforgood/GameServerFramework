#include "pch.h"
#include <gtest/gtest.h>
#include <algorithm>
#include <cmath>
#include <filesystem>
#include <iostream>
#include <memory>
#include <string>
#include "spdlog/spdlog.h"
#include "spdlog/sinks/stdout_sinks.h"

#include "GameData/ResourceLoader.h"
#include "gamedata.h"
#include "World.h"
#include "Map.h"
#include "NavMesh.h"
#include "Character.h"
#include "Monster.h"
#include "Player.h"
#include "syncnet_generated.h"

//---------------------------------------------------------------------------------------
// 공간 분할 그리드는 네비메시의 실제 범위에서 만들어져야 한다.
//
// 예전에는 GridManager(100, 100, 2) 로 고정이라 월드 좌표 ±100 만 담았다. 그 밖의 액터는
// enterCell 이 조용히 무시해서 어떤 셀에도 들어가지 않고, 적 탐지·AoI 구독·스킬 AoE 판정에서
// 전부 빠졌다(크래시가 없어 더 찾기 어려웠다). 큰 맵을 쓰면 바로 드러나는 문제다.
//---------------------------------------------------------------------------------------

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

	// 예전 고정 그리드(±100)가 담지 못하던 크기. 이 밖에 서는 액터가 있어야 검증이 성립한다.
	constexpr float kOldGridHalfExtent = 100.0f;

	// 네비메시가 ±100 을 넘는 맵 중 가장 큰 것. 없으면 nullptr.
	const gamedata::Map* FindLargeMap(float& outHalfExtent)
	{
		const gamedata::Map* best = nullptr;
		outHalfExtent = 0.0f;

		for (const auto& [id, m] : ResourceLoader::Instance().GetMaps())
		{
			if (m == nullptr || m->navmesh_path.empty())
				continue;

			const std::string path = GameDataPath::Resolve() + m->navmesh_path;
			if (!std::filesystem::exists(path))
				continue;

			NavMesh navMesh;
			if (!navMesh.Load(path.c_str()))
				continue;

			float bmin[3], bmax[3];
			if (!navMesh.Bounds(bmin, bmax))
				continue;

			// 원점에서 가장 먼 수평 거리. 이 값이 100 을 넘으면 예전 그리드 밖이다.
			const float halfExtent = std::max(
				std::max(std::abs(bmin[0]), std::abs(bmax[0])),
				std::max(std::abs(bmin[2]), std::abs(bmax[2])));

			if (halfExtent > outHalfExtent)
			{
				outHalfExtent = halfExtent;
				best = m;
			}
		}

		return (best != nullptr && outHalfExtent > kOldGridHalfExtent) ? best : nullptr;
	}
}

class MapGridTest : public ::testing::Test
{
protected:
	void SetUp() override
	{
		EnsureNetLogger();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "Map.json"))
			<< "통합 GameData 폴더를 찾지 못했습니다: " << dataPath;
		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath)) << "LoadResources 실패";
	}
};

// 네비메시 경계는 타일 헤더에서 그대로 읽혀야 한다(그리드 크기의 근거가 되는 값).
TEST_F(MapGridTest, NavMeshReportsLoadedBounds)
{
	const gamedata::Map* mapData = nullptr;
	for (const auto& [id, m] : ResourceLoader::Instance().GetMaps())
	{
		if (m == nullptr || m->navmesh_path.empty())
			continue;
		if (!std::filesystem::exists(GameDataPath::Resolve() + m->navmesh_path))
			continue;
		if (mapData == nullptr || m->id < mapData->id)
			mapData = m;
	}
	ASSERT_NE(mapData, nullptr) << "navmesh 가 배치된 맵이 없습니다.";

	NavMesh navMesh;
	ASSERT_TRUE(navMesh.Load((GameDataPath::Resolve() + mapData->navmesh_path).c_str()));

	float bmin[3], bmax[3];
	ASSERT_TRUE(navMesh.Bounds(bmin, bmax));
	EXPECT_LT(bmin[0], bmax[0]) << "x 경계가 뒤집혔습니다";
	EXPECT_LT(bmin[2], bmax[2]) << "z 경계가 뒤집혔습니다";
}

// 로드되지 않은 네비메시는 경계를 돌려주지 않는다(Map 이 기본 그리드로 폴백하는 조건).
TEST_F(MapGridTest, UnloadedNavMeshHasNoBounds)
{
	NavMesh navMesh;
	float bmin[3], bmax[3];
	EXPECT_FALSE(navMesh.Bounds(bmin, bmax));
}

// 예전 고정 그리드(±100) 밖에 있는 액터도 셀에 들어가야 한다.
// 셀에 들어가지 못하면 AoI 구독자 목록에 잡히지 않으므로 IsInViewOf 로 확인할 수 있다.
TEST_F(MapGridTest, ActorsBeyondOldFixedGridAreIndexed)
{
	float halfExtent = 0.0f;
	const gamedata::Map* mapData = FindLargeMap(halfExtent);
	if (mapData == nullptr)
	{
		// 이 버전의 gtest 에는 GTEST_SKIP 이 없다. 검증할 대상이 없으면 그냥 통과시킨다.
		std::cout << "[  SKIPPED ] 네비메시가 ±" << kOldGridHalfExtent
			<< " 를 넘는 맵이 없습니다(큰 맵을 추가하면 이 테스트가 활성화됩니다)." << std::endl;
		SUCCEED();
		return;
	}

	World world;
	Map map(&world);
	ASSERT_TRUE(map.Init("waypoint", mapData)) << "Map::Init 실패";
	map.SetAoIRadius(4.0f); // 셀 2 기준 2칸. AoI 를 켜야 셀 구독이 동작한다.

	// 원점에서 멀리 떨어진, 예전 그리드 밖의 지점을 찾는다.
	// 장애물 위일 수 있으므로 몇 군데를 시도한다.
	// (far/near 는 windows.h 매크로라 변수명으로 쓸 수 없다.)
	const float distant = kOldGridHalfExtent + (halfExtent - kOldGridHalfExtent) * 0.5f;
	std::shared_ptr<Player> player;
	std::shared_ptr<Character> character;
	float spawnX = 0.0f, spawnZ = 0.0f;

	for (int i = 0; i < 16 && character == nullptr; ++i)
	{
		// 대각선 방향으로 조금씩 옮겨 가며 네비메시 위의 자리를 찾는다.
		spawnX = distant - i * 3.0f;
		spawnZ = distant - i * 3.0f;

		player = std::make_shared<Player>();
		syncnet::Vec3 pos(spawnX, 0.0f, spawnZ);
		character = std::dynamic_pointer_cast<Character>(
			map.OnAddAgent(player, syncnet::GameObjectType_Character, &pos));
	}
	ASSERT_NE(character, nullptr)
		<< "원점에서 " << distant << " 떨어진 곳에 캐릭터를 스폰하지 못했습니다.";

	// 스냅 결과가 여전히 예전 그리드 밖이어야 검증이 의미가 있다.
	const float actualX = character->GetVecter2X();
	const float actualZ = character->GetVecter2Y();
	ASSERT_GT(std::max(std::abs(actualX), std::abs(actualZ)), kOldGridHalfExtent)
		<< "스폰 위치가 예전 그리드 안으로 스냅되어 검증이 성립하지 않습니다.";

	syncnet::Vec3 monsterPos(spawnX + 1.0f, 0.0f, spawnZ);
	auto monster = std::dynamic_pointer_cast<Monster>(
		map.OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &monsterPos));
	ASSERT_NE(monster, nullptr) << "먼 지점에 몬스터를 스폰하지 못했습니다.";

	EXPECT_TRUE(map.IsInViewOf(player->GetPlayerId(), monster->GetActorId()))
		<< "예전 그리드 밖(" << actualX << ", " << actualZ << ")의 액터가 셀에 들어가지 않았습니다. "
		<< "그리드 크기가 네비메시 범위를 따르는지 확인하세요.";
}
