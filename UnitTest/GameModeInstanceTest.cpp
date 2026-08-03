#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include <memory>
#include <string>
#include "spdlog/spdlog.h"
#include "spdlog/sinks/stdout_sinks.h"

#include "GameData/ResourceLoader.h"
#include "gamedata.h"
#include "World.h"
#include "Map.h"
#include "GameMode.h"
#include "Character.h"
#include "Player.h"
#include "PlayerLevel.h"
#include "Vector3.h"
#include "syncnet_generated.h"

//---------------------------------------------------------------------------------------
// 인스턴스 모드(raid 등 field 가 아닌 모드)의 라이프사이클.
//
// field 맵은 기동 시 한 번 만들어 모두가 공유하지만, 인스턴스 맵은 플레이어가 게이트로
// 들어가는 순간 새로 만들어지고 진행이 끝나면 파괴된다. 여기서 고정하는 것은:
//   - 인스턴스 맵은 World::Init 이 만들지 않는다(입장 전에는 존재하지 않는다)
//   - ChangeMap 이 인스턴스 목적지를 만나면 새 Map 을 만들어 거기로 보낸다
//   - 진행 로직(lua)이 종료를 선언하면 남은 사람을 내보내고 맵이 사라진다
//   - 마지막 사람이 걸어 나가도 맵이 사라진다
//   - 사망/부활/보스 처치가 lua 가 읽는 상태에 실제로 반영된다
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

	// 첫 번째 인스턴스(field 가 아닌) 모드에 속한 맵. 없으면 nullptr.
	const gamedata::Map* FindInstanceMap()
	{
		const gamedata::Map* found = nullptr;
		for (const auto& [id, m] : ResourceLoader::Instance().GetMaps())
		{
			if (m == nullptr)
				continue;
			const gamedata::GameMode* mode = ResourceLoader::Instance().GetGameMode(m->game_mode_id);
			if (mode == nullptr || mode->type == "field")
				continue;
			if (found == nullptr || m->id < found->id)
				found = m;
		}
		return found;
	}

	// mapId 로 들어가는 게이트를 가진 맵과 그 게이트를 찾는다.
	// 게이트의 target_id 는 전역 유일 마커 id 라, 그 마커의 parent 가 목적지 맵이다.
	const gamedata::Map* FindEntranceTo(int mapId, const gamedata::MapGate*& outGate)
	{
		for (const auto& [id, m] : ResourceLoader::Instance().GetMaps())
		{
			if (m == nullptr)
				continue;
			for (const auto& g : m->gates)
			{
				const gamedata::Map* dest = nullptr;
				syncnet::Vec3 unused(0, 0, 0);
				if (!Map::ResolveGateTarget(g.target_id, dest, unused) || dest->id != mapId)
					continue;
				outGate = &g;
				return m;
			}
		}
		return nullptr;
	}
}

class GameModeInstanceTest : public ::testing::Test
{
protected:
	std::unique_ptr<World> world_;
	const gamedata::Map* instanceData_ = nullptr;
	const gamedata::Map* entranceData_ = nullptr;
	const gamedata::MapGate* entranceGate_ = nullptr;

	void SetUp() override
	{
		EnsureNetLogger();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "Map.json"))
			<< "통합 GameData 폴더를 찾지 못했습니다: " << dataPath;
		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath)) << "LoadResources 실패";

		// 진행 로직 lua 도 같은 폴더에서 읽는다.
		GameMode::InitializeLua(dataPath);

		instanceData_ = FindInstanceMap();
		ASSERT_NE(instanceData_, nullptr) << "인스턴스 모드에 속한 맵이 Map.json 에 없습니다.";
		ASSERT_FALSE(instanceData_->navmesh_path.empty())
			<< "인스턴스 맵 " << instanceData_->id << " 에 navmesh 가 지정되지 않았습니다.";
		ASSERT_TRUE(std::filesystem::exists(dataPath + instanceData_->navmesh_path))
			<< "인스턴스 맵 navmesh 파일이 없습니다: " << instanceData_->navmesh_path;

		entranceData_ = FindEntranceTo(instanceData_->id, entranceGate_);
		ASSERT_NE(entranceData_, nullptr)
			<< "맵 " << instanceData_->id << " 로 들어가는 게이트가 없습니다.";

		world_ = std::make_unique<World>();
		world_->Init("waypoint");
	}

	void TearDown() override
	{
		world_.reset();
	}

	// 입구 맵에 캐릭터를 만들어 세운 플레이어를 돌려준다(세션 없음 — 전송은 무시된다).
	std::shared_ptr<Player> SpawnPlayerAtEntrance()
	{
		Map* entrance = world_->FindMap(entranceData_->id);
		if (entrance == nullptr)
			return nullptr;

		auto player = std::make_shared<Player>();

		// 입구 맵에 player_spawn 이 없을 수 있으므로 인스턴스로 가는 게이트 위치에 세운다.
		syncnet::Vec3 pos(
			static_cast<float>(entranceGate_->position.x),
			static_cast<float>(entranceGate_->position.y),
			static_cast<float>(entranceGate_->position.z));

		if (entrance->OnAddAgent(player, syncnet::GameObjectType_Character, &pos) == nullptr)
			return nullptr;
		entrance->Enter(player);
		return player;
	}

	// 플레이어를 인스턴스에 입장시키고 그 인스턴스 맵을 돌려준다.
	Map* EnterInstance(std::shared_ptr<Player>& player)
	{
		syncnet::Vec3 outPos(0, 0, 0);
		int outAgentId = 0;
		int outMapId = 0;
		if (!world_->ChangeMap(player, entranceGate_->target_id, outMapId, outPos, outAgentId))
			return nullptr;

		auto& character = player->GetCharacter();
		return character != nullptr ? character->GetMap() : nullptr;
	}
};

//---------------------------------------------------------------------------------------
// 생성/파괴
//---------------------------------------------------------------------------------------

// 인스턴스 맵은 기동 시점에 만들어지지 않는다(field 맵만 상시 로드된다).
TEST_F(GameModeInstanceTest, InstanceMapIsNotLoadedAtStartup)
{
	EXPECT_EQ(world_->FindMap(instanceData_->id), nullptr)
		<< "인스턴스 맵 " << instanceData_->id << " 이 상시 맵으로 로드돼 있습니다.";
	EXPECT_EQ(world_->GetInstanceCount(), 0u);
}

// 게이트로 들어가면 인스턴스가 새로 만들어지고 플레이어가 그 맵에 들어간다.
TEST_F(GameModeInstanceTest, EnteringGateCreatesInstance)
{
	auto player = SpawnPlayerAtEntrance();
	ASSERT_NE(player, nullptr);

	Map* instance = EnterInstance(player);
	ASSERT_NE(instance, nullptr) << "인스턴스 입장에 실패했습니다.";

	EXPECT_TRUE(instance->IsInstance());
	EXPECT_EQ(instance->GetMapId(), instanceData_->id);
	EXPECT_EQ(instance->GetPlayerCount(), 1u);
	EXPECT_EQ(world_->GetInstanceCount(), 1u);

	// 인스턴스는 id 로 찾히지 않는다 — 같은 mapId 로 여러 개가 살 수 있기 때문이다.
	EXPECT_EQ(world_->FindMap(instanceData_->id), nullptr);
}

// 두 번 들어가면 인스턴스도 두 개다(파티 매칭이 붙기 전까지는 입장마다 새 사본).
TEST_F(GameModeInstanceTest, EachEntryCreatesItsOwnInstance)
{
	auto a = SpawnPlayerAtEntrance();
	auto b = SpawnPlayerAtEntrance();
	ASSERT_NE(a, nullptr);
	ASSERT_NE(b, nullptr);

	Map* first = EnterInstance(a);
	Map* second = EnterInstance(b);
	ASSERT_NE(first, nullptr);
	ASSERT_NE(second, nullptr);

	EXPECT_NE(first, second);
	EXPECT_NE(first->GetInstanceId(), second->GetInstanceId());
	EXPECT_EQ(world_->GetInstanceCount(), 2u);
}

// 마지막 사람이 나가면 빈 인스턴스는 다음 틱에 사라진다.
TEST_F(GameModeInstanceTest, EmptyInstanceIsDestroyed)
{
	auto player = SpawnPlayerAtEntrance();
	ASSERT_NE(player, nullptr);
	ASSERT_NE(EnterInstance(player), nullptr);
	ASSERT_EQ(world_->GetInstanceCount(), 1u);

	// 인스턴스의 출구 게이트로 되돌아 나간다.
	syncnet::Vec3 outPos(0, 0, 0);
	int outAgentId = 0;
	int outMapId = 0;
	ASSERT_TRUE(world_->ChangeMap(player, instanceData_->gates.front().target_id,
		outMapId, outPos, outAgentId));

	world_->update(0.1f);
	EXPECT_EQ(world_->GetInstanceCount(), 0u);
}

//---------------------------------------------------------------------------------------
// 진행 로직 연동
//---------------------------------------------------------------------------------------

// 입장한 인스턴스에는 자기 게임 모드가 붙어 있고 lua 가 로드돼 타이머가 돌아간다.
TEST_F(GameModeInstanceTest, InstanceRunsItsOwnGameMode)
{
	auto player = SpawnPlayerAtEntrance();
	ASSERT_NE(player, nullptr);
	Map* instance = EnterInstance(player);
	ASSERT_NE(instance, nullptr);

	GameMode* mode = instance->GetGameMode();
	ASSERT_NE(mode, nullptr) << "인스턴스에 게임 모드가 붙지 않았습니다.";
	ASSERT_NE(mode->gamedata, nullptr);
	EXPECT_EQ(mode->gamedata->id, instanceData_->game_mode_id);

	// gamemode_raid.lua on_start 가 제한 시간 타이머를 켠다.
	if (mode->gamedata->rules.has_time_limit)
	{
		EXPECT_GT(mode->remaining_time(), 0.0f) << "on_start 가 타이머를 켜지 않았습니다.";

		const float before = mode->remaining_time();
		world_->update(1.0f);
		EXPECT_LT(mode->remaining_time(), before) << "인스턴스 틱이 돌지 않아 타이머가 줄지 않습니다.";
	}
}

// on_start 가 GM_SpawnBoss 를 부르면 boss_spawn 마커에 실제로 액터가 생긴다.
TEST_F(GameModeInstanceTest, BossIsSpawnedFromMarker)
{
	if (instanceData_->spawn_points.boss_spawn.empty())
	{
		std::cout << "[  SKIPPED ] 인스턴스 맵에 boss_spawn 마커가 없습니다." << std::endl;
		return;
	}

	auto player = SpawnPlayerAtEntrance();
	ASSERT_NE(player, nullptr);
	Map* instance = EnterInstance(player);
	ASSERT_NE(instance, nullptr);

	const int bossActorId = instance->GetBossActorId();
	ASSERT_GE(bossActorId, 0) << "on_start 가 보스를 스폰하지 않았습니다.";

	auto boss = instance->FindActor(bossActorId);
	ASSERT_NE(boss, nullptr) << "보스 액터가 맵에 없습니다.";
	EXPECT_FALSE(boss->IsCharacter());

	// 마커 위치(클라 좌표계)를 서버 좌표계로 바꿔 비교한다. navmesh 스냅 오차는 허용.
	const auto& markerPos = instanceData_->spawn_points.boss_spawn.front().position;
	const syncnet::Vec3 marker(
		static_cast<float>(markerPos.x),
		static_cast<float>(markerPos.y),
		static_cast<float>(markerPos.z));
	const Vector3 expected(&marker);

	const Vector3& pos = boss->GetPosition();
	const float dx = pos.x - expected.x;
	const float dz = pos.z - expected.z;
	EXPECT_LT(dx * dx + dz * dz, 4.0f) << "보스가 boss_spawn 마커에서 멀리 떨어져 있습니다.";

	// 보스 체력은 모드 데이터가 정한다.
	const GameMode* mode = instance->GetGameMode();
	ASSERT_NE(mode, nullptr);
	if (mode->gamedata->boss_info.boss_hp > 0)
		EXPECT_EQ(boss->GetHealth(), mode->gamedata->boss_info.boss_hp);
}

// 보스를 처치하면 lua 의 check_end 가 clear 로 종료시키고, 인스턴스는 정리된다.
TEST_F(GameModeInstanceTest, BossKillEndsInstanceAndEvictsPlayer)
{
	auto player = SpawnPlayerAtEntrance();
	ASSERT_NE(player, nullptr);
	Map* instance = EnterInstance(player);
	ASSERT_NE(instance, nullptr);

	GameMode* mode = instance->GetGameMode();
	ASSERT_NE(mode, nullptr);
	if (mode->gamedata->rules.end_condition != "boss_defeat")
	{
		std::cout << "[  SKIPPED ] 이 모드의 종료 조건이 boss_defeat 가 아닙니다." << std::endl;
		return;
	}

	// 실제 전투 대신 상태만 사망으로 만든다(전투 자체는 SkillSystemTest 가 본다).
	// 여기서는 맵만 틱해 진행 로직을 돌린다 — World::update 를 부르면 같은 틱에
	// 인스턴스가 파괴되어 mode 포인터가 죽는다.
	mode->set_boss_dead(true);
	instance->update(0.1f);

	EXPECT_TRUE(mode->IsEnded());
	EXPECT_EQ(mode->result(), "clear");

	// 종료된 인스턴스는 다음 월드 틱에 사람을 밖으로 내보내고 사라진다.
	world_->update(0.1f);
	EXPECT_EQ(world_->GetInstanceCount(), 0u);

	const gamedata::Map* exitMap = nullptr;
	syncnet::Vec3 exitPos(0, 0, 0);
	ASSERT_TRUE(Map::ResolveGateTarget(instanceData_->gates.front().target_id, exitMap, exitPos));

	auto& character = player->GetCharacter();
	ASSERT_NE(character, nullptr) << "퇴장 후 캐릭터가 사라졌습니다.";
	EXPECT_EQ(character->GetMap()->GetMapId(), exitMap->id);
	EXPECT_FALSE(character->GetMap()->IsInstance());
}

// 제한 시간이 다 되면 fail_timeout 으로 끝나고 인스턴스가 정리된다.
TEST_F(GameModeInstanceTest, TimeoutEndsInstance)
{
	auto player = SpawnPlayerAtEntrance();
	ASSERT_NE(player, nullptr);
	Map* instance = EnterInstance(player);
	ASSERT_NE(instance, nullptr);

	GameMode* mode = instance->GetGameMode();
	ASSERT_NE(mode, nullptr);
	if (!mode->gamedata->rules.has_time_limit)
	{
		std::cout << "[  SKIPPED ] 이 모드에는 제한 시간이 없습니다." << std::endl;
		return;
	}

	// 남은 시간을 한 번에 태운다(맵만 틱해야 mode 가 살아 있는 채로 확인할 수 있다).
	instance->update(mode->remaining_time() + 1.0f);

	EXPECT_TRUE(mode->IsEnded());
	EXPECT_EQ(mode->result(), "fail_timeout");

	world_->update(0.1f);
	EXPECT_EQ(world_->GetInstanceCount(), 0u);
}

//---------------------------------------------------------------------------------------
// 사망 / 부활
//---------------------------------------------------------------------------------------

// 체력이 0 이 되면 사망 상태가 되고 lua 가 읽는 생존 수가 줄어든다.
TEST_F(GameModeInstanceTest, PlayerDeathUpdatesAliveCount)
{
	auto player = SpawnPlayerAtEntrance();
	ASSERT_NE(player, nullptr);
	Map* instance = EnterInstance(player);
	ASSERT_NE(instance, nullptr);

	GameMode* mode = instance->GetGameMode();
	ASSERT_NE(mode, nullptr);
	EXPECT_EQ(mode->alive_player_count(), 1);

	player->GetCharacter()->SetHealth(0);
	instance->update(0.1f); // 전멸로 모드가 끝나므로 월드 틱은 돌리지 않는다(인스턴스가 파괴된다)

	EXPECT_EQ(mode->alive_player_count(), 0);
	EXPECT_EQ(player->GetCharacter()->GetState(), syncnet::AIState::AIState_Dead);
}

// 전멸하면 raid lua 가 fail_wipe 로 종료시킨다.
TEST_F(GameModeInstanceTest, WipeEndsInstance)
{
	auto player = SpawnPlayerAtEntrance();
	ASSERT_NE(player, nullptr);
	Map* instance = EnterInstance(player);
	ASSERT_NE(instance, nullptr);

	GameMode* mode = instance->GetGameMode();
	ASSERT_NE(mode, nullptr);
	if (mode->gamedata->rules.respawn_enabled)
	{
		std::cout << "[  SKIPPED ] 부활이 켜진 모드는 전멸로 끝나지 않습니다." << std::endl;
		return;
	}

	player->GetCharacter()->SetHealth(0);
	instance->update(0.1f);

	EXPECT_TRUE(mode->IsEnded());
	EXPECT_EQ(mode->result(), "fail_wipe");
}

// field 모드는 사망 시 lua 가 부활을 예약하고, respawn_time 이 지나면 되살아난다.
TEST_F(GameModeInstanceTest, FieldModeRespawnsDeadPlayer)
{
	Map* field = world_->GetPrimaryMap();
	ASSERT_NE(field, nullptr);

	GameMode* mode = field->GetGameMode();
	ASSERT_NE(mode, nullptr) << "field 맵에 게임 모드가 붙지 않았습니다.";
	ASSERT_TRUE(mode->gamedata->rules.respawn_enabled) << "field 모드에 부활이 꺼져 있습니다.";

	auto player = std::make_shared<Player>();
	const syncnet::Vec3 spawn = field->GetPlayerSpawnPos();
	ASSERT_NE(field->OnAddAgent(player, syncnet::GameObjectType_Character, &spawn), nullptr);
	field->Enter(player);

	auto& character = player->GetCharacter();
	ASSERT_NE(character, nullptr);

	character->SetHealth(0);
	world_->update(0.1f); // 사망 판정 + on_player_dead -> GM_SchedulePlayerRespawn
	EXPECT_EQ(character->GetState(), syncnet::AIState::AIState_Dead);
	EXPECT_LE(character->GetHealth(), 0);

	// respawn_time 만큼 흘려보낸다.
	world_->update(static_cast<float>(mode->gamedata->rules.respawn_time) + 1.0f);

	EXPECT_GT(character->GetHealth(), 0) << "부활이 예약되지 않았거나 실행되지 않았습니다.";
	EXPECT_NE(character->GetState(), syncnet::AIState::AIState_Dead);
	EXPECT_EQ(mode->alive_player_count(), 1);
}

//---------------------------------------------------------------------------------------
// 보상
//---------------------------------------------------------------------------------------

// 클리어하면 on_end 가 GM_GrantRewards 를 불러 살아있는 참가자에게 경험치가 들어간다.
TEST_F(GameModeInstanceTest, ClearGrantsExpToSurvivors)
{
	auto player = SpawnPlayerAtEntrance();
	ASSERT_NE(player, nullptr);
	Map* instance = EnterInstance(player);
	ASSERT_NE(instance, nullptr);

	GameMode* mode = instance->GetGameMode();
	ASSERT_NE(mode, nullptr);
	if (mode->gamedata->rules.end_condition != "boss_defeat")
	{
		std::cout << "[  SKIPPED ] 이 모드의 종료 조건이 boss_defeat 가 아닙니다." << std::endl;
		return;
	}

	auto* level = player->GetComponent<PlayerLevel>();
	ASSERT_NE(level, nullptr);
	const int before = level->GetExp();

	mode->set_boss_dead(true);
	instance->update(0.1f); // on_end 안에서 GM_GrantRewards 가 돈다

	ASSERT_EQ(mode->result(), "clear");
	EXPECT_GT(level->GetExp(), before) << "클리어 보상 경험치가 들어가지 않았습니다.";
}
