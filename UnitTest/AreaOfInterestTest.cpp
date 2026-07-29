#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include <memory>
#include <string>
#include <vector>
#include "spdlog/spdlog.h"
#include "spdlog/sinks/stdout_sinks.h"

#include "GameData/ResourceLoader.h"
#include "gamedata.h"
#include "World.h"
#include "Map.h"
#include "INavMovement.h"
#include "Character.h"
#include "Monster.h"
#include "Player.h"
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
// 관심영역(AoI) 구독 검증. 설계는 Engine/Map/AOI_DESIGN.md 참고.
//
// 서버는 맵 전체를 모두에게 보내는 대신, 플레이어 캐릭터 주변 셀을 구독하게 하고
// 그 셀에 속한 액터만 동기화한다. 여기서는 구독 집합이 규칙대로 만들어지고,
// 캐릭터가 움직이면 따라 갱신되는지를 고정한다.
//
// 맵이 작아(약 50유닛) 기본 반경(40)으로는 필터링 효과를 볼 수 없으므로
// 테스트는 SetAoIRadius 로 좁은 반경을 강제한다.
//---------------------------------------------------------------------------------------

class AreaOfInterestTest : public ::testing::Test
{
protected:
	std::unique_ptr<World> world_;
	std::unique_ptr<Map> map_;
	std::vector<std::shared_ptr<Player>> players_;
	double spawnX_ = 0, spawnY_ = 0, spawnZ_ = 0;

	static constexpr float kAoIRadius = 4.0f; // 셀 2 기준 2칸

	void SetUp() override
	{
		EnsureNetLogger();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "Map.json"))
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
		map_->SetAoIRadius(kAoIRadius);

		const auto& spawn = mapData->spawn_points.player_spawn[0].position;
		spawnX_ = spawn.x;
		spawnY_ = spawn.y;
		spawnZ_ = spawn.z;
	}

	std::shared_ptr<Character> SpawnCharacter(std::shared_ptr<Player>& outPlayer, float offsetX, float offsetZ)
	{
		auto player = std::make_shared<Player>();
		players_.push_back(player);
		outPlayer = player;

		syncnet::Vec3 pos(
			static_cast<float>(spawnX_) + offsetX,
			static_cast<float>(spawnY_),
			static_cast<float>(spawnZ_) + offsetZ);
		return std::dynamic_pointer_cast<Character>(
			map_->OnAddAgent(player, syncnet::GameObjectType_Character, &pos));
	}

	std::shared_ptr<Monster> SpawnMonster(float offsetX, float offsetZ)
	{
		syncnet::Vec3 pos(
			static_cast<float>(spawnX_) + offsetX,
			static_cast<float>(spawnY_),
			static_cast<float>(spawnZ_) + offsetZ);
		return std::dynamic_pointer_cast<Monster>(
			map_->OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &pos));
	}
};

// 캐릭터가 생기면 그 주변 셀을 구독한다 — 가까운 액터는 시야 안, 먼 액터는 시야 밖.
TEST_F(AreaOfInterestTest, SubscribesToCellsAroundCharacter)
{
	std::shared_ptr<Player> player;
	auto character = SpawnCharacter(player, 0.0f, 0.0f);
	ASSERT_NE(character, nullptr);

	// near/far 는 windows.h 매크로라 변수명으로 쓸 수 없다.
	auto nearby = SpawnMonster(1.0f, 0.0f);
	auto distant = SpawnMonster(20.0f, 0.0f);
	ASSERT_NE(nearby, nullptr);
	ASSERT_NE(distant, nullptr);

	EXPECT_TRUE(map_->IsInViewOf(player->GetPlayerId(), nearby->GetActorId()))
		<< "바로 옆 액터가 시야에 들어오지 않았습니다";
	EXPECT_FALSE(map_->IsInViewOf(player->GetPlayerId(), distant->GetActorId()))
		<< "반경 밖 액터가 시야에 들어왔습니다";

	// 자기 자신도 자기 시야 안이다(클라가 자기 캐릭터 갱신을 받아야 한다).
	EXPECT_TRUE(map_->IsInViewOf(player->GetPlayerId(), character->GetActorId()));
}

// 다른 플레이어의 시야와 섞이지 않는다.
TEST_F(AreaOfInterestTest, ViewersDoNotShareInterest)
{
	std::shared_ptr<Player> playerA;
	std::shared_ptr<Player> playerB;
	auto characterA = SpawnCharacter(playerA, 0.0f, 0.0f);
	auto characterB = SpawnCharacter(playerB, 20.0f, 0.0f);
	ASSERT_NE(characterA, nullptr);
	ASSERT_NE(characterB, nullptr);

	auto nearA = SpawnMonster(1.0f, 0.0f);
	ASSERT_NE(nearA, nullptr);

	EXPECT_TRUE(map_->IsInViewOf(playerA->GetPlayerId(), nearA->GetActorId()));
	EXPECT_FALSE(map_->IsInViewOf(playerB->GetPlayerId(), nearA->GetActorId()))
		<< "멀리 있는 플레이어에게까지 액터가 보입니다";
}

// 캐릭터가 이동해 셀을 넘으면 구독 범위가 따라 옮겨간다(진입/이탈).
TEST_F(AreaOfInterestTest, MovingCharacterUpdatesInterest)
{
	std::shared_ptr<Player> player;
	auto character = SpawnCharacter(player, 0.0f, 0.0f);
	ASSERT_NE(character, nullptr);

	auto target = SpawnMonster(12.0f, 0.0f);
	ASSERT_NE(target, nullptr);
	ASSERT_FALSE(map_->IsInViewOf(player->GetPlayerId(), target->GetActorId()));

	// 캐릭터를 몬스터 옆으로 옮긴다(네비 에이전트 위치가 진실이므로 텔레포트로 옮긴 뒤 동기화).
	const Vector3& targetPos = target->GetPosition();
	Vector3 dest(targetPos.x, targetPos.y, targetPos.z);
	ASSERT_TRUE(map_->GetNavMap()->TeleportAgent(character->GetActorId(), dest.pos()));

	map_->UpdateSystems(1.0f / 30.0f); // 위치 반영 + 구독 갱신

	EXPECT_TRUE(map_->IsInViewOf(player->GetPlayerId(), target->GetActorId()))
		<< "이동한 캐릭터의 시야에 새 액터가 들어오지 않았습니다";
}
