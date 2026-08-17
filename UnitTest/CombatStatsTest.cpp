#include "pch.h"
#include <gtest/gtest.h>

#include <filesystem>
#include <memory>
#include <string>
#include <vector>

#include "spdlog/spdlog.h"
#include "spdlog/sinks/stdout_sinks.h"

#include "Character.h"
#include "CombatSystem.h"
#include "GameData/ResourceLoader.h"
#include "INavMovement.h"
#include "Map.h"
#include "Monster.h"
#include "Player.h"
#include "PlayerEventBroker.h"
#include "PlayerItem.h"
#include "PlayerLevel.h"
#include "PlayerSkill.h"
#include "PlayerWallet.h"
#include "EventMessage.h"
#include "SkillRegistry.h"
#include "Vector3.h"
#include "World.h"
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
// 전투 스탯 / 사망·부활 / 드랍 검증.
//
// 예전에는 모든 액터가 체력 100 하나만 가졌고(공격력·방어력 없음), 플레이어는 죽어도
// 아무 일이 없었으며, 몬스터 처치 보상은 고정 경험치 50 뿐이었다.
//---------------------------------------------------------------------------------------

// ── 데미지 공식 (순수 함수) ────────────────────────────────────────────────────────────

TEST(ComputeDamageTest, AddsAttackerAttackToRoll)
{
	// 방어력 0 이면 굴림값 + 공격력이 그대로 들어간다.
	EXPECT_EQ(combat::ComputeDamage(10.0, 5, 0), 15);
	EXPECT_EQ(combat::ComputeDamage(0.0, 20, 0), 20);
}

TEST(ComputeDamageTest, DefenseReducesWithDiminishingReturns)
{
	// 방어력 100 이면 정확히 절반(raw * 100 / (100+100)).
	EXPECT_EQ(combat::ComputeDamage(90.0, 10, 100), 50);

	// 방어력이 오를수록 줄지만 0 이 되지는 않는다.
	const int low = combat::ComputeDamage(50.0, 10, 10);
	const int high = combat::ComputeDamage(50.0, 10, 200);
	EXPECT_GT(low, high);
	EXPECT_GT(high, 0);
}

// 방어력을 빼기로 넣으면 방어력이 공격력을 넘는 순간 전투가 성립하지 않는다.
// 나누기 방식이라 항상 최소 1 은 들어가야 한다.
TEST(ComputeDamageTest, AlwaysDealsAtLeastOne)
{
	EXPECT_EQ(combat::ComputeDamage(1.0, 0, 100000), 1);
	EXPECT_EQ(combat::ComputeDamage(0.0, 0, 0), 1);
}

TEST(ComputeDamageTest, TreatsNegativeStatsAsZero)
{
	EXPECT_EQ(combat::ComputeDamage(10.0, -5, -5), 10);
}

// ── 데이터 기반 스탯 ──────────────────────────────────────────────────────────────────

class CombatStatsTest : public ::testing::Test
{
protected:
	std::unique_ptr<World> world_;
	std::unique_ptr<Map> map_;
	std::vector<std::shared_ptr<Player>> players_;
	double spawnX_ = 0, spawnY_ = 0, spawnZ_ = 0;

	void SetUp() override
	{
		EnsureNetLogger();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "monster.json"))
			<< "통합 GameData 폴더를 찾지 못했습니다: " << dataPath;
		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath)) << "LoadResources 실패";
		SkillRegistry::Instance().Clear();

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
		ASSERT_NE(mapData, nullptr);

		world_ = std::make_unique<World>();
		map_ = std::make_unique<Map>(world_.get());
		ASSERT_TRUE(map_->Init("waypoint", mapData));

		const auto& spawn = mapData->spawn_points.player_spawn[0].position;
		spawnX_ = spawn.x;
		spawnY_ = spawn.y;
		spawnZ_ = spawn.z;
	}

	std::shared_ptr<Character> SpawnCharacter(float offsetX = 0.0f, float offsetZ = 0.0f)
	{
		auto player = std::make_shared<Player>();
		players_.push_back(player);

		// 사망 판정은 맵의 플레이어 목록을 훑는다. 프로덕션에서는 World::join 이
		// Map::Enter 를 부르므로, 테스트도 같은 상태를 만들어야 한다.
		map_->Enter(player);

		syncnet::Vec3 pos(
			static_cast<float>(spawnX_) + offsetX,
			static_cast<float>(spawnY_),
			static_cast<float>(spawnZ_) + offsetZ);
		return std::dynamic_pointer_cast<Character>(
			map_->OnAddAgent(player, syncnet::GameObjectType_Character, &pos));
	}

	std::shared_ptr<Monster> SpawnMonster(float offsetX = 0.0f, float offsetZ = 0.0f)
	{
		syncnet::Vec3 pos(
			static_cast<float>(spawnX_) + offsetX,
			static_cast<float>(spawnY_),
			static_cast<float>(spawnZ_) + offsetZ);
		return std::dynamic_pointer_cast<Monster>(
			map_->OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &pos));
	}
};

// monster.json 의 hp/attack/defense 가 실제로 액터에 실린다.
// 예전에는 종류만 새기고 스탯은 무시해서 슬라임과 고대 드래곤이 똑같이 체력 100 이었다.
TEST_F(CombatStatsTest, MonsterStatsComeFromData)
{
	auto slime = SpawnMonster(1.0f, 0.0f);
	auto dragon = SpawnMonster(2.0f, 0.0f);
	ASSERT_NE(slime, nullptr);
	ASSERT_NE(dragon, nullptr);

	slime->SetDataId(1);     // Slime: hp 50, attack 5, defense 2
	dragon->SetDataId(1001); // Ancient Dragon: hp 5000, attack 100, defense 50

	EXPECT_EQ(slime->GetMaxHealth(), 50);
	EXPECT_EQ(slime->GetHealth(), 50);
	EXPECT_EQ(slime->GetAttack(), 5);
	EXPECT_EQ(slime->GetDefense(), 2);

	EXPECT_EQ(dragon->GetMaxHealth(), 5000);
	EXPECT_EQ(dragon->GetAttack(), 100);
	EXPECT_EQ(dragon->GetDefense(), 50);

	EXPECT_GT(dragon->GetMaxHealth(), slime->GetMaxHealth());
}

TEST_F(CombatStatsTest, MonsterRewardExpComesFromData)
{
	auto monster = SpawnMonster(1.0f, 0.0f);
	ASSERT_NE(monster, nullptr);

	monster->SetDataId(1);
	EXPECT_EQ(monster->GetRewardExp(), 20);

	monster->SetDataId(1001);
	EXPECT_EQ(monster->GetRewardExp(), 5000);
}

// 캐릭터는 레벨 데이터(level.json)의 스탯으로 스폰된다.
TEST_F(CombatStatsTest, CharacterStatsComeFromLevelData)
{
	auto character = SpawnCharacter();
	ASSERT_NE(character, nullptr);

	const gamedata::Level* lv1 = ResourceLoader::Instance().GetLevel(1);
	ASSERT_NE(lv1, nullptr);

	EXPECT_EQ(character->GetMaxHealth(), lv1->hp);
	EXPECT_EQ(character->GetAttack(), lv1->attack);
	EXPECT_EQ(character->GetDefense(), lv1->defense);
}

// 레벨업하면 스탯이 즉시 오르되, 현재 체력이 가득 차지는 않는다
// (전투 중 레벨업이 완전 회복이 되면 안 된다).
TEST_F(CombatStatsTest, LevelUpRaisesStatsWithoutFullHeal)
{
	auto character = SpawnCharacter();
	ASSERT_NE(character, nullptr);

	auto& player = players_.back();
	auto* level = player->GetComponent<PlayerLevel>();
	ASSERT_NE(level, nullptr);

	character->SetHealth(30);
	const int attackBefore = character->GetAttack();

	level->GainExp(100); // 레벨 2
	ASSERT_EQ(level->GetLevel(), 2);

	const gamedata::Level* lv2 = ResourceLoader::Instance().GetLevel(2);
	ASSERT_NE(lv2, nullptr);
	EXPECT_EQ(character->GetMaxHealth(), lv2->hp);
	EXPECT_GT(character->GetAttack(), attackBefore);
	EXPECT_EQ(character->GetHealth(), 30) << "레벨업이 회복이 되면 안 된다";
}

// ── 체력 경계 ─────────────────────────────────────────────────────────────────────────

TEST_F(CombatStatsTest, HealthClampsAtZeroAndMax)
{
	auto character = SpawnCharacter();
	ASSERT_NE(character, nullptr);

	// 음수로 내려가지 않는다. 예전에는 계속 내려가서, 사망 후 회복해도
	// 0 에 닿기까지 여러 번 맞아야 했다.
	character->DecrementHealth(999999);
	EXPECT_EQ(character->GetHealth(), 0);
	EXPECT_TRUE(character->IsDead());

	// 최대 체력을 넘지 않는다.
	character->IncrementHealth(999999);
	EXPECT_EQ(character->GetHealth(), character->GetMaxHealth());
	EXPECT_FALSE(character->IsDead());
}

// ── 사망 / 부활 ───────────────────────────────────────────────────────────────────────

// 게임 모드가 없는 필드 맵에서도 사망이 처리되어야 한다.
// 예전에는 사망 판정이 UpdateGameMode 안에 있어서 여기서는 아무 일도 없었다.
TEST_F(CombatStatsTest, PlayerDeathIsHandledWithoutGameMode)
{
	auto character = SpawnCharacter();
	ASSERT_NE(character, nullptr);

	character->SetHealth(0);
	map_->UpdatePlayerDeath(0.1f);

	EXPECT_EQ(character->GetState(), syncnet::AIState_Dead);
	EXPECT_TRUE(character->IsInputLocked()) << "죽은 캐릭터는 조작할 수 없어야 한다";
}

// 부활은 최대 체력으로 되살리고 조작 잠금을 푼다.
TEST_F(CombatStatsTest, RespawnRestoresFullHealthAndUnlocksInput)
{
	auto character = SpawnCharacter();
	ASSERT_NE(character, nullptr);
	const int maxHealth = character->GetMaxHealth();

	character->SetHealth(0);
	map_->UpdatePlayerDeath(0.1f);
	ASSERT_TRUE(character->IsInputLocked());

	// 예약된 부활 시간이 지나도록 충분히 돌린다.
	for (int i = 0; i < 120; ++i)
		map_->UpdatePlayerDeath(0.1f);

	EXPECT_EQ(character->GetHealth(), maxHealth);
	EXPECT_FALSE(character->IsInputLocked());
	EXPECT_NE(character->GetState(), syncnet::AIState_Dead);
}

// ── 스킬 소유 ─────────────────────────────────────────────────────────────────────────

// 캐릭터는 배운 스킬만 쓸 수 있다. 예전에는 생성자가 전체 스킬 테이블을 등록해
// 누구나 모든 스킬을 시전할 수 있었다.
TEST_F(CombatStatsTest, CharacterOnlyOwnsStarterSkillsOnFirstSpawn)
{
	auto character = SpawnCharacter();
	ASSERT_NE(character, nullptr);

	// starter=true 인 스킬(평타/점프)은 지급된다.
	EXPECT_TRUE(character->GetSkillSet().HasSkill(1));
	EXPECT_TRUE(character->GetSkillSet().HasSkill(2));

	// 배우지 않은 고급 스킬은 없다.
	EXPECT_FALSE(character->GetSkillSet().HasSkill(101)); // Fireball
	EXPECT_FALSE(character->GetSkillSet().HasSkill(141)); // Armageddon

	// 몬스터 전용 스킬도 없다.
	EXPECT_FALSE(character->GetSkillSet().HasSkill(Monster::kMeleeSkillId));
}

TEST_F(CombatStatsTest, LearnedSkillBecomesUsable)
{
	auto character = SpawnCharacter();
	ASSERT_NE(character, nullptr);
	auto& player = players_.back();

	ASSERT_FALSE(character->GetSkillSet().HasSkill(101));

	auto* skills = player->GetComponent<PlayerSkill>();
	ASSERT_NE(skills, nullptr);
	skills->LearnSkill(101);
	player->ApplyOwnedSkillsToCharacter();

	EXPECT_TRUE(character->GetSkillSet().HasSkill(101));
}

// ── 드랍 ──────────────────────────────────────────────────────────────────────────────

// 드랍 테이블이 데이터에 있다(monster.json).
TEST_F(CombatStatsTest, MonsterDropTableIsLoaded)
{
	const gamedata::MonsterData* dragon = ResourceLoader::Instance().GetMonsterData(1001);
	ASSERT_NE(dragon, nullptr);

	EXPECT_GT(dragon->gold_min, 0);
	EXPECT_GE(dragon->gold_max, dragon->gold_min);
	ASSERT_FALSE(dragon->drops.empty());

	for (const auto& drop : dragon->drops)
	{
		EXPECT_GT(drop.item_id, 0);
		EXPECT_GT(drop.count, 0);
		EXPECT_GT(drop.chance, 0.0);
		EXPECT_LE(drop.chance, 1.0);
	}
}

// 확률 1.0 인 드랍은 반드시 들어온다. 골드도 함께 지급된다.
TEST_F(CombatStatsTest, KillGrantsGuaranteedDropsAndGold)
{
	auto character = SpawnCharacter();
	ASSERT_NE(character, nullptr);
	auto& player = players_.back();

	auto* inventory = player->GetComponent<PlayerItem>();
	auto* wallet = player->GetComponent<PlayerWallet>();
	ASSERT_NE(inventory, nullptr);
	ASSERT_NE(wallet, nullptr);

	const long long goldBefore = wallet->GetGold();

	// 고대 드래곤(1001): 아이템 3 x5 와 102 x1 이 chance 1.0 이다.
	auto* broker = player->GetComponent<PlayerEventBroker>();
	ASSERT_NE(broker, nullptr);
	broker->publish(EventActorDead{
		character->GetActorId(), 9999, 1001, 1, 5000 });

	EXPECT_EQ(inventory->GetCount(3), 5);
	EXPECT_EQ(inventory->GetCount(102), 1);
	EXPECT_GT(wallet->GetGold(), goldBefore);
}

// 마지막 일격을 넣지 않은 사람은 아이템을 받지 않는다(파티원 수만큼 복사 방지).
// 골드는 크레딧을 나눠 가지므로 받는다.
TEST_F(CombatStatsTest, NonKillerGetsGoldButNoItems)
{
	auto character = SpawnCharacter();
	ASSERT_NE(character, nullptr);
	auto& player = players_.back();

	auto* inventory = player->GetComponent<PlayerItem>();
	auto* wallet = player->GetComponent<PlayerWallet>();
	auto* broker = player->GetComponent<PlayerEventBroker>();
	ASSERT_NE(inventory, nullptr);
	ASSERT_NE(wallet, nullptr);
	ASSERT_NE(broker, nullptr);

	const long long goldBefore = wallet->GetGold();

	// killer_actor_id 가 이 캐릭터가 아니다 = 파티원 몫으로 받은 이벤트.
	const int someoneElse = character->GetActorId() + 12345;
	broker->publish(EventActorDead{ someoneElse, 9999, 1001, 2, 5000 });

	EXPECT_EQ(inventory->GetCount(3), 0) << "킬러가 아닌데 아이템을 받았다";
	EXPECT_EQ(inventory->GetCount(102), 0);
	EXPECT_GT(wallet->GetGold(), goldBefore) << "골드는 크레딧 인원이 나눠 갖는다";
}
