#include "pch.h"
#include <gtest/gtest.h>
#include <algorithm>
#include <filesystem>
#include <memory>
#include <string>
#include <vector>
#include "spdlog/spdlog.h"
#include "spdlog/sinks/stdout_sinks.h"

#include "CheatCommands.h"
#include "Character.h"
#include "CombatSystem.h"
#include "GameMode.h"
#include "GameData/ResourceLoader.h"
#include "Map.h"
#include "Monster.h"
#include "Player.h"
#include "PlayerItem.h"
#include "PlayerLevel.h"
#include "PlayerLoadData.h"
#include "PlayerSkill.h"
#include "PlayerWallet.h"
#include "Vector3.h"
#include "World.h"
#include "gamedata.h"
#include "syncnet_generated.h"

//---------------------------------------------------------------------------------------
// 치트 명령 검증.
//
// 치트는 "테스트를 위한 도구" 라서 고장 나면 조용하다 — 아무 일도 일어나지 않는 것과
// 치트가 깨진 것이 화면에서 같아 보인다. 그래서 명령마다 "무엇이 실제로 바뀌었는가" 를
// 월드 상태로 확인한다(응답 문자열만 보면 아무것도 안 하고 성공을 말해도 통과한다).
//
// 실제 Map(navmesh + grid) 위에서 네트워크 없이 돈다 — cheat::Execute 는 GameObject 하나만
// 받기 때문에 세션도 DB 도 필요 없다.
//---------------------------------------------------------------------------------------

namespace
{
	void EnsureNetLogger()
	{
		if (!spdlog::get("net"))
		{
			auto logger = std::make_shared<spdlog::logger>(
				"net", std::make_shared<spdlog::sinks::stdout_sink_mt>());
			logger->set_level(spdlog::level::critical); // 치트는 실행마다 warn 을 남긴다
			spdlog::register_logger(logger);
		}
	}
}

class CheatCommandTest : public ::testing::Test
{
protected:
	std::unique_ptr<World> world_;
	std::unique_ptr<Map> map_;
	std::vector<std::shared_ptr<Player>> players_; // 캐릭터 수명 유지용
	double spawnX_ = 0, spawnY_ = 0, spawnZ_ = 0;  // 클라 좌표계 스폰 지점

	void SetUp() override
	{
		EnsureNetLogger();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "monster.json"))
			<< "통합 GameData 폴더를 찾지 못했습니다: " << dataPath;
		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath)) << "LoadResources 실패";

		// navmesh 가 배치되어 있고 플레이어 스폰이 있는 맵 중 id 최소 맵(SkillSystemTest 와 같은 규칙).
		const gamedata::Map* mapData = nullptr;
		for (const auto& entry : ResourceLoader::Instance().GetMaps())
		{
			const gamedata::Map* candidate = entry.second;
			if (candidate == nullptr || candidate->navmesh_path.empty())
				continue;
			if (!std::filesystem::exists(GameDataPath::Resolve() + candidate->navmesh_path))
				continue;
			if (candidate->spawn_points.player_spawn.empty())
				continue;
			if (mapData == nullptr || candidate->id < mapData->id)
				mapData = candidate;
		}
		ASSERT_NE(mapData, nullptr) << "navmesh 가 배치된 맵이 없습니다.";

		world_ = std::make_unique<World>();
		map_ = std::make_unique<Map>(world_.get());
		ASSERT_TRUE(map_->Init("waypoint", mapData)) << "Map::Init 실패";

		const auto& spawn = mapData->spawn_points.player_spawn[0].position;
		spawnX_ = spawn.x;
		spawnY_ = spawn.y;
		spawnZ_ = spawn.z;
	}

	// 프로덕션 경로로 캐릭터를 스폰하고, 컴포넌트가 동작하도록 로드까지 마친 플레이어를 준다.
	std::shared_ptr<Player> SpawnPlayer()
	{
		auto player = std::make_shared<Player>();
		players_.push_back(player);

		PlayerLoadData data{};
		data.player.id = 4242;
		data.player.name = "Cheater";
		data.player.level = 1;
		player->Load(data); // 인벤토리/지갑/레벨이 자기 characterId 를 알아야 동작한다

		syncnet::Vec3 pos(
			static_cast<float>(spawnX_),
			static_cast<float>(spawnY_),
			static_cast<float>(spawnZ_));
		auto actor = map_->OnAddAgent(player, syncnet::GameObjectType_Character, &pos);
		EXPECT_NE(actor, nullptr);
		return player;
	}

	size_t AliveMonsterCount() const
	{
		size_t alive = 0;
		for (const auto& actor : map_->CollectActors(syncnet::GameObjectType_Monster))
			if (actor != nullptr && !actor->IsDead())
				++alive;
		return alive;
	}

	// 데이터에서 실제로 존재하는 id 를 고른다. 고정 상수로 쓰면 GameData 가 바뀔 때
	// "치트가 깨진 것" 처럼 보이는 실패가 난다.
	static int AnyMonsterId()
	{
		int found = 0;
		for (const auto& entry : ResourceLoader::Instance().GetMonsterDatas())
			if (entry.second != nullptr && (found == 0 || entry.second->id < found))
				found = entry.second->id;
		return found;
	}

	static int AnyItemId()
	{
		int found = 0;
		for (const auto& entry : ResourceLoader::Instance().GetItems())
			if (entry.second != nullptr && (found == 0 || entry.second->id < found))
				found = entry.second->id;
		return found;
	}

	static int AnyPlayerSkillId()
	{
		int found = 0;
		for (const auto& entry : ResourceLoader::Instance().GetSkills())
		{
			const gamedata::Skill* skill = entry.second;
			if (skill == nullptr || skill->monster_only)
				continue;
			if (found == 0 || skill->id < found)
				found = skill->id;
		}
		return found;
	}
};

// ============================================================
// 명령표 / 안내
// ============================================================

// 명령표가 help 와 자동완성의 유일한 원본이다. 표에 있는 모든 명령이 help 에 나와야
// 한다 — 표에 넣고 help 에서 빠지면 클라 자동완성에만 있고 아무도 모르는 명령이 된다.
TEST_F(CheatCommandTest, HelpListsEveryCommandInTheTable)
{
	auto player = SpawnPlayer();

	const cheat::Result help = cheat::Execute(player.get(), "help");
	ASSERT_TRUE(help.handled);

	ASSERT_FALSE(cheat::Commands().empty());
	for (const cheat::CommandInfo& command : cheat::Commands())
	{
		EXPECT_NE(help.reply.find(command.name), std::string::npos)
			<< "help 에 '" << command.name << "' 가 없다";
		EXPECT_NE(command.help[0], '\0') << command.name << " 에 설명이 없다";
	}
}

// 자동완성이 인자까지 채우려면 표의 힌트가 실제 인자와 맞아야 한다.
// 인자를 받는 명령은 힌트가 있어야 하고, 완성 종류는 아는 값이어야 한다.
TEST_F(CheatCommandTest, CommandTableIsWellFormed)
{
	const std::vector<std::string> kinds = { "", "monster", "item", "skill", "map", "quest" };

	for (const cheat::CommandInfo& command : cheat::Commands())
	{
		EXPECT_NE(command.name[0], '\0');
		EXPECT_NE(std::find(kinds.begin(), kinds.end(), std::string(command.complete)), kinds.end())
			<< command.name << " 의 complete 값이 클라가 모르는 종류다: " << command.complete;

		// 완성 대상이 있다는 것은 첫 인자가 id 라는 뜻이다.
		if (command.complete[0] != '\0')
			EXPECT_NE(command.args[0], '\0') << command.name << " 에 인자 힌트가 없다";
	}
}

TEST_F(CheatCommandTest, HelpForOneCommandShowsUsage)
{
	auto player = SpawnPlayer();

	const cheat::Result help = cheat::Execute(player.get(), "/help spawn");
	EXPECT_TRUE(help.handled);
	EXPECT_NE(help.reply.find("spawn"), std::string::npos);
	EXPECT_NE(help.reply.find("monsterId"), std::string::npos);
}

TEST_F(CheatCommandTest, UnknownCommandIsReportedAndBlankLineIsIgnored)
{
	auto player = SpawnPlayer();

	const cheat::Result unknown = cheat::Execute(player.get(), "nosuchcheat");
	EXPECT_FALSE(unknown.handled);
	EXPECT_NE(unknown.reply.find("nosuchcheat"), std::string::npos);

	const cheat::Result blank = cheat::Execute(player.get(), "   ");
	EXPECT_FALSE(blank.handled);
	EXPECT_TRUE(blank.reply.empty());
}

// ============================================================
// 몬스터
// ============================================================

TEST_F(CheatCommandTest, SpawnPutsMonstersOnTheMap)
{
	auto player = SpawnPlayer();
	const int monsterId = AnyMonsterId();
	ASSERT_NE(monsterId, 0);

	const size_t before = AliveMonsterCount();

	const cheat::Result result = cheat::Execute(player.get(), "/spawn " + std::to_string(monsterId) + " 3");
	ASSERT_TRUE(result.handled);

	ASSERT_EQ(AliveMonsterCount(), before + 3) << result.reply;

	// 종류가 새겨져야 처치 퀘스트가 무엇을 잡았는지 셀 수 있다.
	for (const auto& actor : map_->CollectActors(syncnet::GameObjectType_Monster))
	{
		auto* monster = dynamic_cast<Monster*>(actor.get());
		ASSERT_NE(monster, nullptr);
		EXPECT_EQ(monster->GetDataId(), monsterId);
	}
}

TEST_F(CheatCommandTest, SpawnRejectsUnknownMonsterAndMissingArgument)
{
	auto player = SpawnPlayer();

	const cheat::Result unknown = cheat::Execute(player.get(), "/spawn 999999");
	EXPECT_TRUE(unknown.handled);
	EXPECT_EQ(AliveMonsterCount(), 0u);

	const cheat::Result noArgs = cheat::Execute(player.get(), "/spawn");
	EXPECT_TRUE(noArgs.handled);
	EXPECT_NE(noArgs.reply.find("id"), std::string::npos);
}

// 킬 크레딧이 내 캐릭터로 남아야 정상 처치와 같은 경로(경험치/퀘스트)를 탄다.
TEST_F(CheatCommandTest, KillAllKillsMonstersWithCredit)
{
	auto player = SpawnPlayer();
	const int monsterId = AnyMonsterId();
	ASSERT_NE(monsterId, 0);

	cheat::Execute(player.get(), "/spawn " + std::to_string(monsterId) + " 4");
	ASSERT_EQ(AliveMonsterCount(), 4u);

	const int characterActorId = player->GetCharacter()->GetActorId();

	const cheat::Result result = cheat::Execute(player.get(), "/killall");
	ASSERT_TRUE(result.handled);
	EXPECT_EQ(AliveMonsterCount(), 0u) << result.reply;

	for (const auto& actor : map_->CollectActors(syncnet::GameObjectType_Monster))
		EXPECT_EQ(actor->GetLastAttackerActorId(), characterActorId);
}

// 반경을 주면 그 안만 죽는다. 멀리 있는 무리를 남겨 두고 한 무리만 정리할 때 쓴다.
TEST_F(CheatCommandTest, KillAllWithRadiusSparesDistantMonsters)
{
	auto player = SpawnPlayer();
	const int monsterId = AnyMonsterId();
	ASSERT_NE(monsterId, 0);

	// 반경 1 짜리 원에 4마리(가깝다) + 반경 30 짜리 원에 4마리(멀다).
	cheat::Execute(player.get(), "/spawn " + std::to_string(monsterId) + " 4 1");
	const size_t nearby = AliveMonsterCount();
	cheat::Execute(player.get(), "/spawn " + std::to_string(monsterId) + " 4 30");
	const size_t total = AliveMonsterCount();
	ASSERT_GT(total, nearby) << "먼 쪽 몬스터가 스폰되지 않아 반경 검증을 할 수 없다";

	cheat::Execute(player.get(), "/killall 5");

	EXPECT_EQ(AliveMonsterCount(), total - nearby);
}

// ============================================================
// 체력 / 무적
// ============================================================

TEST_F(CheatCommandTest, HpHealAndDieChangeHealth)
{
	auto player = SpawnPlayer();
	auto character = player->GetCharacter();
	ASSERT_NE(character, nullptr);

	EXPECT_TRUE(cheat::Execute(player.get(), "/hp 1").handled);
	EXPECT_EQ(character->GetHealth(), 1);

	EXPECT_TRUE(cheat::Execute(player.get(), "/heal").handled);
	EXPECT_EQ(character->GetHealth(), character->GetMaxHealth());

	// 최대치를 넘겨 달라고 해도 최대치까지만.
	cheat::Execute(player.get(), "/hp 999999");
	EXPECT_EQ(character->GetHealth(), character->GetMaxHealth());

	EXPECT_TRUE(cheat::Execute(player.get(), "/die").handled);
	EXPECT_TRUE(character->IsDead());
}

// god 은 데미지 단일 경로에서 걸러야 한다. 여기서 막지 못하면 스킬/몬스터 공격 중
// 일부 경로만 막히는 반쪽 무적이 된다.
TEST_F(CheatCommandTest, GodBlocksDamageUntilTurnedOff)
{
	auto player = SpawnPlayer();
	auto character = player->GetCharacter();
	ASSERT_NE(character, nullptr);

	const int monsterId = AnyMonsterId();
	ASSERT_NE(monsterId, 0);
	cheat::Execute(player.get(), "/spawn " + std::to_string(monsterId) + " 1");
	auto monsters = map_->CollectActors(syncnet::GameObjectType_Monster);
	ASSERT_EQ(monsters.size(), 1u);

	EXPECT_TRUE(cheat::Execute(player.get(), "/god on").handled);
	EXPECT_TRUE(character->IsInvincible());

	const int before = character->GetHealth();
	combat::ApplyDamage(monsters[0].get(), character.get(), 50.0);
	EXPECT_EQ(character->GetHealth(), before) << "무적인데 체력이 줄었다";

	// 인자가 없으면 토글이다.
	EXPECT_TRUE(cheat::Execute(player.get(), "/god").handled);
	EXPECT_FALSE(character->IsInvincible());

	combat::ApplyDamage(monsters[0].get(), character.get(), 50.0);
	EXPECT_LT(character->GetHealth(), before);
}

// ============================================================
// 성장 / 재화 / 인벤토리
// ============================================================

TEST_F(CheatCommandTest, LevelSetsLevelAndKeepsExpConsistent)
{
	auto player = SpawnPlayer();
	auto* levels = player->GetComponent<PlayerLevel>();
	ASSERT_NE(levels, nullptr);

	// level.json 에 있는 레벨 중 1 보다 큰 것을 고른다.
	int target = 0;
	for (const auto& entry : ResourceLoader::Instance().GetLevels())
		if (entry.second != nullptr && entry.second->level > 1 && (target == 0 || entry.second->level < target))
			target = entry.second->level;
	ASSERT_NE(target, 0) << "level.json 에 레벨 2 이상이 없다";

	const cheat::Result result = cheat::Execute(player.get(), "/level " + std::to_string(target));
	ASSERT_TRUE(result.handled);
	EXPECT_EQ(levels->GetLevel(), target) << result.reply;

	// 경험치를 맞춰 두지 않으면 다음 경험치 획득에서 레벨이 되돌아간다.
	cheat::Execute(player.get(), "/exp 1");
	EXPECT_GE(levels->GetLevel(), target);

	// 없는 레벨은 거절한다.
	cheat::Execute(player.get(), "/level 9999");
	EXPECT_EQ(levels->GetLevel(), target);
}

TEST_F(CheatCommandTest, GoldAddsAndSpends)
{
	auto player = SpawnPlayer();
	auto* wallet = player->GetComponent<PlayerWallet>();
	ASSERT_NE(wallet, nullptr);

	EXPECT_TRUE(cheat::Execute(player.get(), "/gold 5000").handled);
	EXPECT_EQ(wallet->GetGold(), 5000);

	cheat::Execute(player.get(), "/gold -2000");
	EXPECT_EQ(wallet->GetGold(), 3000);

	// 모자라면 아무것도 하지 않는다.
	cheat::Execute(player.get(), "/gold -999999");
	EXPECT_EQ(wallet->GetGold(), 3000);
}

TEST_F(CheatCommandTest, ItemGrantsAndRejectsUnknownId)
{
	auto player = SpawnPlayer();
	auto* inventory = player->GetComponent<PlayerItem>();
	ASSERT_NE(inventory, nullptr);

	const int itemId = AnyItemId();
	ASSERT_NE(itemId, 0);

	EXPECT_TRUE(cheat::Execute(player.get(), "/item " + std::to_string(itemId) + " 7").handled);
	EXPECT_EQ(inventory->GetCount(itemId), 7);

	cheat::Execute(player.get(), "/item 999999");
	EXPECT_EQ(inventory->DistinctCount(), 1u);
}

TEST_F(CheatCommandTest, SkillLearnsOneAndAllSkillLearnsEveryPlayerSkill)
{
	auto player = SpawnPlayer();
	auto* skills = player->GetComponent<PlayerSkill>();
	ASSERT_NE(skills, nullptr);

	const int skillId = AnyPlayerSkillId();
	ASSERT_NE(skillId, 0);

	EXPECT_TRUE(cheat::Execute(player.get(), "/skill " + std::to_string(skillId)).handled);
	EXPECT_TRUE(skills->Has(skillId));

	EXPECT_TRUE(cheat::Execute(player.get(), "/allskill").handled);
	for (const auto& entry : ResourceLoader::Instance().GetSkills())
	{
		const gamedata::Skill* data = entry.second;
		if (data == nullptr)
			continue;

		// 몬스터 전용은 배우지 않는다 — 배워 봐야 캐릭터가 쓸 수 없다.
		EXPECT_EQ(skills->Has(data->id), !data->monster_only) << "skill " << data->id;
	}
}

// ============================================================
// 이동 / 조회
// ============================================================

TEST_F(CheatCommandTest, TeleportMovesTheCharacter)
{
	auto player = SpawnPlayer();
	auto character = player->GetCharacter();
	ASSERT_NE(character, nullptr);

	const Vector3 before = character->GetPosition();

	// 인자는 클라 좌표계다(where 가 찍어 주는 값과 같다). 스폰 지점에서 살짝 옮긴다 —
	// navmesh 밖으로 나가면 이동 에이전트가 위치를 스냅해 검증이 흔들린다.
	const std::string command = "/tp " + std::to_string(spawnX_ + 2.0) + " " + std::to_string(spawnZ_ + 2.0);
	const cheat::Result result = cheat::Execute(player.get(), command);
	ASSERT_TRUE(result.handled) << result.reply;

	const Vector3 after = character->GetPosition();
	EXPECT_NE(before.x, after.x) << result.reply;
	EXPECT_NE(before.z, after.z) << result.reply;

	// 인자가 모자라면 아무 데도 가지 않는다.
	const cheat::Result bad = cheat::Execute(player.get(), "/tp 1");
	EXPECT_TRUE(bad.handled);
	EXPECT_EQ(character->GetPosition().x, after.x);
}

TEST_F(CheatCommandTest, WhereReportsMapAndPosition)
{
	auto player = SpawnPlayer();

	const cheat::Result result = cheat::Execute(player.get(), "/where");
	ASSERT_TRUE(result.handled);
	EXPECT_NE(result.reply.find(std::to_string(map_->GetMapId())), std::string::npos) << result.reply;
	EXPECT_NE(result.reply.find("레벨"), std::string::npos) << result.reply;
}

TEST_F(CheatCommandTest, ListFindsDataByKindAndFilter)
{
	auto player = SpawnPlayer();

	const int monsterId = AnyMonsterId();
	ASSERT_NE(monsterId, 0);

	const cheat::Result all = cheat::Execute(player.get(), "/list monster");
	ASSERT_TRUE(all.handled);
	EXPECT_NE(all.reply.find(std::to_string(monsterId)), std::string::npos) << all.reply;

	// 모르는 종류는 사용법을 돌려준다.
	const cheat::Result unknown = cheat::Execute(player.get(), "/list nosuchkind");
	EXPECT_TRUE(unknown.handled);
	EXPECT_NE(unknown.reply.find("monster"), std::string::npos);
}

//---------------------------------------------------------------------------------------
// map 은 맵 하나로는 검증할 수 없다. 이동은 "이전 맵에서 빼고 → 목적지 맵에 캐릭터를 새로
// 만들고 → 클라에 통보" 까지가 한 묶음(World::ForceMove)이라 월드가 있어야 한다.
//---------------------------------------------------------------------------------------
class CheatMapMoveTest : public ::testing::Test
{
protected:
	std::unique_ptr<World> world_;

	void SetUp() override
	{
		EnsureNetLogger();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "Map.json"))
			<< "통합 GameData 폴더를 찾지 못했습니다: " << dataPath;
		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath)) << "LoadResources 실패";

		GameMode::InitializeLua(dataPath); // 맵마다 진행 모드 스크립트를 읽는다

		world_ = std::make_unique<World>();
		world_->Init("waypoint");
	}

	void TearDown() override
	{
		world_.reset();
	}

	// 상시 맵 중 player_spawn 마커가 있는 것(= map 의 기본 도착 지점이 될 수 있는 맵).
	std::vector<Map*> MapsWithSpawn() const
	{
		std::vector<Map*> found;
		for (Map* map : world_->GetMaps())
		{
			const gamedata::Map* data = map != nullptr ? map->GetMapData() : nullptr;
			if (data != nullptr && !data->spawn_points.player_spawn.empty())
				found.push_back(map);
		}
		return found;
	}
};

TEST_F(CheatMapMoveTest, MapMovesCharacterToAnotherMap)
{
	std::vector<Map*> maps = MapsWithSpawn();
	ASSERT_GE(maps.size(), 2u) << "player_spawn 이 있는 상시 맵이 둘 이상 있어야 검증할 수 있다";

	Map* from = maps[0];
	Map* to = maps[1];

	auto player = std::make_shared<Player>();
	const syncnet::Vec3 spawn = from->GetPlayerSpawnPos();
	ASSERT_NE(from->OnAddAgent(player, syncnet::GameObjectType_Character, &spawn), nullptr);
	from->Enter(player);

	const int oldActorId = player->GetCharacter()->GetActorId();

	const cheat::Result result = cheat::Execute(player.get(), "/map " + std::to_string(to->GetMapId()));
	ASSERT_TRUE(result.handled);

	auto character = player->GetCharacter();
	ASSERT_NE(character, nullptr) << result.reply;
	EXPECT_EQ(character->GetMap()->GetMapId(), to->GetMapId()) << result.reply;

	// 캐릭터는 목적지 맵에서 새로 만들어진다(같은 actor id 가 두 맵에 남아 있으면 안 된다).
	EXPECT_EQ(from->FindActor(oldActorId), nullptr);
	EXPECT_NE(to->FindActor(character->GetActorId()), nullptr);
}

// player_spawn 없이 게이트로만 들어오는 맵도 갈 수 있어야 한다(첫 게이트에 도착).
TEST_F(CheatMapMoveTest, MapArrivesAtGateWhenMapHasNoPlayerSpawn)
{
	std::vector<Map*> maps = MapsWithSpawn();
	ASSERT_FALSE(maps.empty());
	Map* from = maps[0];

	Map* to = nullptr;
	for (Map* map : world_->GetMaps())
	{
		const gamedata::Map* data = map != nullptr ? map->GetMapData() : nullptr;
		if (data != nullptr && data->spawn_points.player_spawn.empty() && !data->gates.empty())
		{
			to = map;
			break;
		}
	}
	ASSERT_NE(to, nullptr) << "player_spawn 없이 게이트만 있는 상시 맵이 데이터에 없다(Dark Forest, Field2 가 그렇다)";

	auto player = std::make_shared<Player>();
	const syncnet::Vec3 spawn = from->GetPlayerSpawnPos();
	ASSERT_NE(from->OnAddAgent(player, syncnet::GameObjectType_Character, &spawn), nullptr);
	from->Enter(player);

	const cheat::Result result = cheat::Execute(player.get(), "/map " + std::to_string(to->GetMapId()));
	ASSERT_NE(player->GetCharacter(), nullptr) << result.reply;
	EXPECT_EQ(player->GetCharacter()->GetMap()->GetMapId(), to->GetMapId()) << result.reply;
}

TEST_F(CheatMapMoveTest, MapRejectsUnknownMapAndCurrentMap)
{
	std::vector<Map*> maps = MapsWithSpawn();
	ASSERT_FALSE(maps.empty());

	Map* from = maps[0];

	auto player = std::make_shared<Player>();
	const syncnet::Vec3 spawn = from->GetPlayerSpawnPos();
	ASSERT_NE(from->OnAddAgent(player, syncnet::GameObjectType_Character, &spawn), nullptr);
	from->Enter(player);

	cheat::Execute(player.get(), "/map 999999");
	EXPECT_EQ(player->GetCharacter()->GetMap()->GetMapId(), from->GetMapId());

	// 지금 있는 맵으로 가라고 하면 캐릭터를 다시 만들지 않는다(할 일이 없다).
	const int actorId = player->GetCharacter()->GetActorId();
	cheat::Execute(player.get(), "/map " + std::to_string(from->GetMapId()));
	EXPECT_EQ(player->GetCharacter()->GetActorId(), actorId);
}

// 캐릭터가 없는 상태(로그인 직후)에도 치트는 죽지 않고 이유를 돌려줘야 한다.
TEST_F(CheatCommandTest, CommandsWithoutCharacterAreRejectedGracefully)
{
	auto player = std::make_shared<Player>();

	for (const char* line : { "/spawn 1", "/killall", "/heal", "/die", "/god", "/tp 1 2", "/where" })
	{
		const cheat::Result result = cheat::Execute(player.get(), line);
		EXPECT_TRUE(result.handled) << line;
		EXPECT_FALSE(result.reply.empty()) << line;
	}
}
