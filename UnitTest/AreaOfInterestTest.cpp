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

// 회귀 테스트: 갓 생성된 액터의 상태/변경 플래그는 정해진 값이어야 한다.
// 예전에는 Actor::state_ 와 changeFlag_ 가 초기화되지 않아, 아무도 상태를 쓰지 않는
// 캐릭터가 남은 쓰레기 값을 그대로 ActorInfo 에 실어 보냈다. 그 값이 우연히
// Dead/Destroyed 면 다른 클라이언트에는 멀쩡한 플레이어가 죽은 것으로 보인다.
TEST_F(AreaOfInterestTest, NewActorsStartInAKnownState)
{
	std::shared_ptr<Player> player;
	auto character = SpawnCharacter(player, 0.0f, 0.0f);
	ASSERT_NE(character, nullptr);
	EXPECT_EQ(character->GetState(), syncnet::AIState::AIState_Patrol);
	// 스폰 직후에는 전체 스냅샷을 보내야 하므로 All 이 서 있는 것이 정상이다.
	// 검사하는 것은 "정의된 비트만 서 있는가" 다 — 쓰레기 값이면 여기서 걸린다.
	EXPECT_EQ(character->GetChangedFlag() & ~static_cast<long>(GameObjectChangeType::All), 0);

	auto monster = SpawnMonster(1.0f, 0.0f);
	ASSERT_NE(monster, nullptr);
	EXPECT_NE(monster->GetState(), syncnet::AIState::AIState_Destroyed);
}

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

// 회귀 테스트: 부활은 좌표를 직접 쓰는 이동이라 구독이 따라가지 않았다.
//
// 평소 이동은 SyncActorState 가 '네비 위치 != 액터 위치' 를 보고 구독을 옮긴다.
// 그런데 RespawnPlayer 는 스스로 위치를 써 버려서 그 대조가 '변화 없음' 이 됐고,
// 부활한 플레이어의 구독 셀은 죽은 자리에 남았다. 그러면 새 자리의 어떤 액터도
// 받지 못하고 — 자기 자신의 부활조차 못 받아서 클라는 계속 죽은 줄 안다.
// (1,000봇 부하에서 서버는 1,698번 부활시켰는데 봇이 인지한 것은 228번뿐이었다.)
TEST_F(AreaOfInterestTest, RespawnMovesInterestToSpawnPoint)
{
	std::shared_ptr<Player> player;
	auto character = SpawnCharacter(player, 0.0f, 0.0f);
	ASSERT_NE(character, nullptr);

	// 사망/부활은 players_ 에 등록된 플레이어만 대상으로 돈다.
	map_->Enter(player);

	// 스폰 지점 옆의 몬스터. 부활하면 다시 보여야 하는 대상이다.
	auto atSpawn = SpawnMonster(1.0f, 0.0f);
	ASSERT_NE(atSpawn, nullptr);
	ASSERT_TRUE(map_->IsInViewOf(player->GetPlayerId(), atSpawn->GetActorId()));

	// 스폰 지점에서 떨어진 곳으로 이동(평소 이동 경로 — 여기서는 구독이 잘 따라간다).
	// 목적지는 네비메시 위여야 하므로 그 자리에 세운 몬스터의 좌표를 그대로 쓴다.
	auto farAway = SpawnMonster(12.0f, 0.0f);
	ASSERT_NE(farAway, nullptr);

	const Vector3& farPos = farAway->GetPosition();
	Vector3 dest(farPos.x, farPos.y, farPos.z);
	ASSERT_TRUE(map_->GetNavMap()->TeleportAgent(character->GetActorId(), dest.pos()));
	map_->UpdateSystems(1.0f / 30.0f);
	ASSERT_FALSE(map_->IsInViewOf(player->GetPlayerId(), atSpawn->GetActorId()))
		<< "멀리 이동했는데 스폰 지점의 액터가 아직 시야에 있습니다(사전 조건 실패)";

	// 죽고 즉시 부활시킨다.
	character->SetHealth(0);
	map_->UpdatePlayerDeath(0.0f);
	map_->SchedulePlayerRespawn(player->GetPlayerId(), 0.0f);
	map_->UpdatePlayerDeath(0.0f);

	EXPECT_TRUE(map_->IsInViewOf(player->GetPlayerId(), atSpawn->GetActorId()))
		<< "부활했는데 구독이 죽은 자리에 남아 있습니다";
	EXPECT_TRUE(map_->IsInViewOf(player->GetPlayerId(), character->GetActorId()))
		<< "부활한 플레이어가 자기 자신조차 보지 못합니다";
}

//---------------------------------------------------------------------------------------
// 인구에 따른 모드 전환.
//
// 브로드캐스트와 관심영역 중 어느 쪽이 싼지는 인구가 정한다(PERFORMANCE.md 17절 —
// 600명에서는 브로드캐스트가, 800명에서는 관심영역이 이긴다). 그래서 데이터로 한쪽을
// 고정하지 않고 인구를 보고 바꾼다. 여기서는 그 전환 규칙을 고정한다.
//
// 위 테스트들과 달리 SetAoIRadius(강제 켬)를 쓰지 않는다 — 강제 켬은 인구를 무시하므로
// 전환 자체를 볼 수 없다. 대신 맵 데이터의 aoi_radius 를 세운 사본으로 맵을 만든다.
//---------------------------------------------------------------------------------------

class AoIModeSwitchTest : public ::testing::Test
{
protected:
	std::unique_ptr<World> world_;
	std::unique_ptr<Map> map_;
	gamedata::Map mapData_{}; // Map 이 포인터로 들고 있으므로 이 픽스처가 수명을 쥔다
	std::vector<std::shared_ptr<Player>> players_;

	void SetUp() override
	{
		EnsureNetLogger();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "Map.json"));
		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath)) << "LoadResources 실패";

		const gamedata::Map* source = nullptr;
		for (const auto& [id, m] : ResourceLoader::Instance().GetMaps())
		{
			if (m == nullptr || m->navmesh_path.empty())
				continue;
			if (!std::filesystem::exists(GameDataPath::Resolve() + m->navmesh_path))
				continue;
			if (m->spawn_points.player_spawn.empty())
				continue;
			if (source == nullptr || m->id < source->id)
				source = m;
		}
		ASSERT_NE(source, nullptr);

		mapData_ = *source;
		mapData_.aoi_radius = 5; // 이 맵은 관심영역을 쓸 수 있다(켜는 시점은 인구가 정한다)

		world_ = std::make_unique<World>();
		map_ = std::make_unique<Map>(world_.get());
		ASSERT_TRUE(map_->Init("waypoint", &mapData_)) << "Map::Init 실패";
	}

	// 캐릭터 없이 입장만 시킨다 — 전환 규칙은 인구만 본다.
	void EnterPlayers(size_t count)
	{
		for (size_t i = 0; i < count; ++i)
		{
			auto player = std::make_shared<Player>();
			players_.push_back(player);
			map_->Enter(player);
		}
	}

	void LeavePlayers(size_t count)
	{
		for (size_t i = 0; i < count && !players_.empty(); ++i)
		{
			map_->leave(players_.back());
			players_.pop_back();
		}
	}
};

// 사람이 적으면 브로드캐스트, 많아지면 관심영역으로 넘어간다.
TEST_F(AoIModeSwitchTest, TurnsOnWhenCrowded)
{
	EXPECT_FALSE(map_->AoIEnabled()) << "빈 맵에서 관심영역이 켜져 있습니다";

	EnterPlayers(Map::kAoIEnablePlayers - 1);
	EXPECT_FALSE(map_->AoIEnabled()) << "기준 인원 미만인데 켜졌습니다";

	EnterPlayers(1);
	EXPECT_TRUE(map_->AoIEnabled()) << "기준 인원에 닿았는데 켜지지 않았습니다";
}

// 경계에서 오가지 않게 끄는 값이 켜는 값보다 낮다(히스테리시스).
TEST_F(AoIModeSwitchTest, KeepsModeAcrossHysteresisBand)
{
	EnterPlayers(Map::kAoIEnablePlayers);
	ASSERT_TRUE(map_->AoIEnabled());

	// 켜는 값 바로 아래로 내려와도 유지된다.
	LeavePlayers(Map::kAoIEnablePlayers - Map::kAoIDisablePlayers);
	EXPECT_EQ(map_->GetPlayerCount(), Map::kAoIDisablePlayers);
	EXPECT_TRUE(map_->AoIEnabled()) << "히스테리시스 구간에서 모드가 바뀌었습니다";

	// 끄는 값 아래로 내려가면 브로드캐스트로 돌아간다.
	LeavePlayers(1);
	EXPECT_FALSE(map_->AoIEnabled());
}

// 데이터가 반경을 주지 않은 맵은 사람이 아무리 많아도 켜지지 않는다.
TEST_F(AoIModeSwitchTest, StaysOffWhenMapHasNoRadius)
{
	mapData_.aoi_radius = 0;

	EnterPlayers(Map::kAoIEnablePlayers + 10);
	EXPECT_FALSE(map_->AoIEnabled());
}

// 끄는 순간 모든 액터를 변경됨으로 세운다.
// 관심영역에 걸러져 클라가 모르는 액터가 있는데, 브로드캐스트는 '바뀐 것' 만 보내므로
// 가만히 있는 액터는 이 처리가 없으면 영영 전달되지 않는다.
TEST_F(AoIModeSwitchTest, MarksEveryActorChangedWhenTurningOff)
{
	syncnet::Vec3 pos(
		static_cast<float>(mapData_.spawn_points.player_spawn[0].position.x),
		static_cast<float>(mapData_.spawn_points.player_spawn[0].position.y),
		static_cast<float>(mapData_.spawn_points.player_spawn[0].position.z));
	auto monster = std::dynamic_pointer_cast<Monster>(
		map_->OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &pos));
	ASSERT_NE(monster, nullptr);

	EnterPlayers(Map::kAoIEnablePlayers);
	ASSERT_TRUE(map_->AoIEnabled());

	monster->ResetChangedFlag();
	ASSERT_EQ(monster->GetChangedFlag(), static_cast<long>(GameObjectChangeType::None));

	LeavePlayers(Map::kAoIEnablePlayers - Map::kAoIDisablePlayers + 1);
	ASSERT_FALSE(map_->AoIEnabled());

	EXPECT_EQ(monster->GetChangedFlag(), static_cast<long>(GameObjectChangeType::All))
		<< "브로드캐스트로 돌아갈 때 전체 스냅샷이 예약되지 않았습니다";
}
