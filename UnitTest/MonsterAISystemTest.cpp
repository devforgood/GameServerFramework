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
#include "Character.h"
#include "Monster.h"
#include "MonsterAIProfile.h"
#include "MonsterAISystem.h"
#include "Player.h"
#include "SkillRegistry.h"
#include "Vector3.h"
#include "syncnet_generated.h"

//---------------------------------------------------------------------------------------
// ECS 백엔드가 지켜야 하는 성질.
//
// MonsterBTTest 는 "세 백엔드가 같은 결정을 내리는가"를 본다. 여기서는 ECS 백엔드가
// 그 결정을 어떤 구조로 내리는지를 고정한다 — 결정은 표에서 나오고, 상태는 개체별이고,
// 사고는 스케줄에 따라 걸러진다. 이 세 가지가 성능의 근거이자 회귀하기 쉬운 지점이다.
//---------------------------------------------------------------------------------------

namespace
{
	void EnsureNetLoggerForAiTest()
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

class MonsterAISystemTest : public ::testing::Test
{
protected:
	std::unique_ptr<World> world_;
	std::unique_ptr<Map> map_;
	std::vector<std::shared_ptr<Player>> players_;
	Monster::BTBackend previousBackend_ = Monster::BTBackend::Ecs;
	double spawnX_ = 0, spawnY_ = 0, spawnZ_ = 0;

	static constexpr float kTickDt = 1.0f / 30.0f;

	void SetUp() override
	{
		EnsureNetLoggerForAiTest();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "skill.json"))
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
		ASSERT_NE(mapData, nullptr) << "navmesh 가 배치된 맵이 없습니다.";

		world_ = std::make_unique<World>();
		map_ = std::make_unique<Map>(world_.get());
		ASSERT_TRUE(map_->Init("waypoint", mapData)) << "Map::Init 실패";

		const auto& spawn = mapData->spawn_points.player_spawn[0].position;
		spawnX_ = spawn.x;
		spawnY_ = spawn.y;
		spawnZ_ = spawn.z;

		previousBackend_ = Monster::btBackend_;
		Monster::btBackend_ = Monster::BTBackend::Ecs;
	}

	void TearDown() override
	{
		Monster::btBackend_ = previousBackend_;
	}

	std::shared_ptr<Character> SpawnCharacter(float offsetX = 0.0f, float offsetZ = 0.0f)
	{
		auto player = std::make_shared<Player>();
		players_.push_back(player);

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

	// monster.json 의 그 종류로 스폰한다 — 성향은 종류가 정하므로, 실제 서버가 밟는
	// 경로(스폰 → SetDataId)를 그대로 태워야 성향이 슬롯에 들어간다.
	std::shared_ptr<Monster> SpawnMonsterOfKind(int dataId, float offsetX, float offsetZ)
	{
		auto monster = SpawnMonster(offsetX, offsetZ);
		if (monster != nullptr)
			monster->SetDataId(dataId);
		return monster;
	}

	// 지정한 성향을 가진 monster.json 의 첫 종류 id. 데이터가 바뀌어도 테스트가 따라간다.
	static int FindMonsterKindWithProfile(monsterai::AIProfile profile)
	{
		int found = -1;
		for (const auto& [id, data] : ResourceLoader::Instance().GetMonsterDatas())
		{
			if (data == nullptr || monsterai::ParseProfile(data->ai) != profile)
				continue;
			if (found < 0 || id < found)
				found = static_cast<int>(id);
		}
		return found;
	}

	void Tick(int count)
	{
		for (int i = 0; i < count; ++i)
			map_->UpdateActors(kTickDt);
	}
};

// 결정표는 트리를 컴파일한 것이다. 트리 그림(MonsterAISystem.h)의 우선순위와 어긋나면
// 몬스터가 조용히 엉뚱한 행동을 한다 — 조합 8가지를 통째로 고정한다.
TEST(MonsterDecisionTableTest, MatchesTreePriority)
{
	using namespace monsterai;

	EXPECT_EQ(kDecisionTable[0], Action::Dead);                                 // 체력 없음
	EXPECT_EQ(kDecisionTable[kAlive], Action::Patrol);                          // 적 없음
	EXPECT_EQ(kDecisionTable[kAlive | kHasTarget], Action::Chase);              // 적 있음, 사거리 밖
	EXPECT_EQ(kDecisionTable[kAlive | kHasTarget | kInAttackRange], Action::Attack);

	// 죽었으면 나머지 조건은 보지 않는다(생존 조건이 트리의 첫 관문이다).
	EXPECT_EQ(kDecisionTable[kHasTarget], Action::Dead);
	EXPECT_EQ(kDecisionTable[kInAttackRange], Action::Dead);
	EXPECT_EQ(kDecisionTable[kHasTarget | kInAttackRange], Action::Dead);
}

// 몬스터마다 만들어지는 것은 컴포넌트뿐이다(트리 인스턴스가 없다).
TEST_F(MonsterAISystemTest, RegistersOneSlotPerMonster)
{
	ASSERT_NE(SpawnMonster(1.0f, 0.0f), nullptr);
	ASSERT_NE(SpawnMonster(2.0f, 0.0f), nullptr);
	ASSERT_NE(SpawnMonster(3.0f, 0.0f), nullptr);

	ASSERT_NE(map_->GetAISystem(), nullptr);
	EXPECT_EQ(map_->GetAISystem()->AgentCount(), 3u) << "스폰한 몬스터가 AI 시스템에 등록되지 않았습니다";
}

// 트리를 공유해도 실행 상태는 개체마다 따로다.
// 한 마리는 교전 분기, 한 마리는 사망 분기 — 같은 트리를 돌면서 서로 다른 자리에 있어야 한다.
TEST_F(MonsterAISystemTest, KeepsExecutionStatePerMonster)
{
	auto victim = SpawnCharacter();
	auto engaged = SpawnMonster(1.0f, 0.0f);
	auto dying = SpawnMonster(1.0f, 1.0f);
	ASSERT_NE(victim, nullptr);
	ASSERT_NE(engaged, nullptr);
	ASSERT_NE(dying, nullptr);

	Tick(20);
	ASSERT_EQ(engaged->targetActorId_, victim->GetActorId());

	dying->DecrementHealth(dying->GetHealth());
	Tick(1);

	EXPECT_EQ(dying->GetState(), syncnet::AIState_Dead);
	EXPECT_EQ(engaged->targetActorId_, victim->GetActorId()) << "이웃의 사망이 다른 몬스터의 상태를 건드렸습니다";
	EXPECT_NE(engaged->GetState(), syncnet::AIState_Dead);
}

// 배회 중에는 매 틱 사고하지 않는다. 이것이 ECS 백엔드가 싼 이유이므로 수치로 고정한다.
TEST_F(MonsterAISystemTest, ThinksOnScheduleWhilePatrolling)
{
	ASSERT_NE(SpawnMonster(), nullptr);
	ASSERT_NE(map_->GetAISystem(), nullptr);

	constexpr int kTicks = 100;
	const uint64_t before = map_->GetAISystem()->ThinkCount();
	Tick(kTicks);
	const uint64_t thoughts = map_->GetAISystem()->ThinkCount() - before;

	// 위상(actorId % interval) 때문에 첫 사고 시점이 달라 ±1 회 흔들린다.
	const uint64_t expected = kTicks / monsterai::MonsterAISystem::kIdleThinkInterval;
	EXPECT_GE(thoughts, expected - 1);
	EXPECT_LE(thoughts, expected + 1) << "배회 중인데 매 틱 사고하고 있습니다";
}

// 교전에 들어가면 매 틱 사고한다(공격 주기와 추격 목표 갱신이 늦으면 안 된다).
TEST_F(MonsterAISystemTest, ThinksEveryTickWhileEngaged)
{
	ASSERT_NE(SpawnCharacter(), nullptr);
	auto monster = SpawnMonster(1.0f, 0.0f);
	ASSERT_NE(monster, nullptr);
	ASSERT_NE(map_->GetAISystem(), nullptr);

	Tick(20); // 탐지될 때까지(스케줄에 걸려 즉시는 아니다)
	ASSERT_GE(monster->targetActorId_, 0) << "적을 탐지하지 못했습니다";

	constexpr int kTicks = 20;
	const uint64_t before = map_->GetAISystem()->ThinkCount();
	Tick(kTicks);

	EXPECT_EQ(map_->GetAISystem()->ThinkCount() - before, static_cast<uint64_t>(kTicks));
}

// 피격은 다음 사고 차례를 기다리지 않는다. 기다리면 이미 죽은 몬스터가 살아 움직인다.
TEST_F(MonsterAISystemTest, WakesImmediatelyOnDamage)
{
	auto monster = SpawnMonster(); // 맵에 캐릭터가 없어 계속 배회한다
	ASSERT_NE(monster, nullptr);

	Tick(20);
	ASSERT_EQ(monster->GetState(), syncnet::AIState_Patrol);

	monster->DecrementHealth(monster->GetHealth()); // 체력 0
	Tick(1);

	EXPECT_EQ(monster->GetState(), syncnet::AIState_Dead)
		<< "사망이 다음 사고 차례까지 미뤄졌습니다";
}

// 사망 후 소멸까지의 지연이 시뮬레이션 시간으로 흐른다.
// (벽시계였다면 이 테스트는 2초를 실제로 기다려야 했고, 틱이 느린 기계에서 흔들렸다.)
TEST_F(MonsterAISystemTest, DestroysAfterDelayInSimulationTime)
{
	auto monster = SpawnMonster();
	ASSERT_NE(monster, nullptr);

	Tick(20);
	monster->DecrementHealth(monster->GetHealth());
	Tick(1);
	ASSERT_EQ(monster->GetState(), syncnet::AIState_Dead);

	// 지연이 지나기 전까지는 여전히 Dead 다(경계에서 흔들리지 않게 여유를 둔다).
	const int ticksToDelay = static_cast<int>(
		monsterai::MonsterAISystem::kDestroyDelaySec / kTickDt);
	Tick(ticksToDelay - 5);
	EXPECT_EQ(monster->GetState(), syncnet::AIState_Dead) << "소멸이 너무 이릅니다";

	Tick(10);
	EXPECT_EQ(monster->GetState(), syncnet::AIState_Destroyed) << "소멸 지연이 지났는데 그대로입니다";
}

// 몬스터가 사라지면 슬롯도 함께 사라진다(컴포넌트가 남으면 죽은 포인터를 계속 돈다).
TEST_F(MonsterAISystemTest, ReleasesSlotWhenMonsterIsDestroyed)
{
	auto monster = SpawnMonster();
	ASSERT_NE(monster, nullptr);
	ASSERT_NE(map_->GetAISystem(), nullptr);
	ASSERT_EQ(map_->GetAISystem()->AgentCount(), 1u);

	const int actorId = monster->GetActorId();
	monster.reset();
	map_->OnRemoveAgent(actorId);

	EXPECT_EQ(map_->GetAISystem()->AgentCount(), 0u);
	Tick(3); // 남은 슬롯이 있었다면 여기서 해제된 몬스터를 건드린다.
}

//---------------------------------------------------------------------------------------
// 성향(AI 프로필).
//
// 결정이 표에서 나온다는 성질은 성향에도 그대로 적용된다 — 개체는 프로필/페이즈 번호만
// 들고, 무엇을 어떻게 할지는 공유 표에 있다. 아래 테스트는 그 표와, 표를 읽는 두 지점
// (탐지 패스가 스캔을 건너뛰는가 / 공격 패스가 페이즈의 스킬을 쓰는가)을 고정한다.
//---------------------------------------------------------------------------------------

// 성향 표가 성향의 정의와 어긋나면 몬스터가 조용히 다른 성격이 된다.
TEST(MonsterAIProfileTest, TraitsMatchProfileDefinition)
{
	using namespace monsterai;

	EXPECT_EQ(ParseProfile("passive"), AIProfile::Passive);
	EXPECT_EQ(ParseProfile("boss"), AIProfile::Boss);
	EXPECT_EQ(ParseProfile("aggressive"), AIProfile::Aggressive);
	EXPECT_EQ(ParseProfile("pasive"), AIProfile::Aggressive) << "모르는 이름은 기본 성향이어야 합니다";
	EXPECT_EQ(ParseProfile(""), AIProfile::Aggressive);

	// 평화로운 몬스터는 스스로 적을 찾지 않는다 — 이것이 "먼저 공격하지 않는다"의 구현이다.
	EXPECT_TRUE(TraitsOf(AIProfile::Aggressive).scansForEnemies);
	EXPECT_FALSE(TraitsOf(AIProfile::Passive).scansForEnemies);
	EXPECT_TRUE(TraitsOf(AIProfile::Boss).scansForEnemies);

	// 보스만 페이즈가 여럿이다(그 개체만 페이즈 패스의 버킷에 담긴다).
	EXPECT_EQ(TraitsOf(AIProfile::Aggressive).patternCount, 1);
	EXPECT_EQ(TraitsOf(AIProfile::Passive).patternCount, 1);
	EXPECT_GT(TraitsOf(AIProfile::Boss).patternCount, 1);
}

// 페이즈 경계. 체력 비율이 내려가면서 패턴이 정확히 한 번씩 바뀌어야 한다.
TEST(MonsterAIProfileTest, BossPhaseFollowsHealthRatio)
{
	using namespace monsterai;

	EXPECT_EQ(PhaseFor(AIProfile::Boss, 1.0f), 0);
	EXPECT_EQ(PhaseFor(AIProfile::Boss, kBossPhase2HealthRatio + 0.01f), 0);
	EXPECT_EQ(PhaseFor(AIProfile::Boss, kBossPhase2HealthRatio), 1) << "절반 이하면 2페이즈여야 합니다";
	EXPECT_EQ(PhaseFor(AIProfile::Boss, 0.0f), 1);

	// 페이즈가 바뀌면 쓰는 스킬과 공격을 거는 거리가 함께 바뀐다(그것이 '패턴'이다).
	EXPECT_NE(PatternOf(AIProfile::Boss, 0).skillId, PatternOf(AIProfile::Boss, 1).skillId);
	EXPECT_NE(PatternOf(AIProfile::Boss, 0).attackRange, PatternOf(AIProfile::Boss, 1).attackRange);

	// 페이즈가 하나뿐인 성향은 체력과 무관하게 늘 같은 패턴이다.
	EXPECT_EQ(PhaseFor(AIProfile::Aggressive, 0.0f), 0);
	EXPECT_EQ(PatternOf(AIProfile::Passive, static_cast<uint8_t>(7)).skillId, kMeleeSkillId)
		<< "범위 밖 페이즈는 잘려야 합니다";
}

// 성향은 종류(monster.json)가 정한다. 종류 id 는 스폰 뒤에 새겨지므로,
// 그때 슬롯에 반영되지 않으면 데이터에 적은 성향이 조용히 무시된다.
TEST_F(MonsterAISystemTest, TakesProfileFromMonsterData)
{
	const int passiveKind = FindMonsterKindWithProfile(monsterai::AIProfile::Passive);
	const int bossKind = FindMonsterKindWithProfile(monsterai::AIProfile::Boss);
	ASSERT_GT(passiveKind, 0) << "monster.json 에 passive 몬스터가 없습니다";
	ASSERT_GT(bossKind, 0) << "monster.json 에 boss 몬스터가 없습니다";

	auto passive = SpawnMonsterOfKind(passiveKind, 1.0f, 0.0f);
	auto boss = SpawnMonsterOfKind(bossKind, 2.0f, 0.0f);
	ASSERT_NE(passive, nullptr);
	ASSERT_NE(boss, nullptr);

	EXPECT_EQ(passive->GetAIProfile(), monsterai::AIProfile::Passive);
	EXPECT_EQ(boss->GetAIProfile(), monsterai::AIProfile::Boss);

	// 보스는 만피로 스폰되므로 1페이즈 패턴을 들고 있어야 한다.
	const monsterai::AttackPattern* pattern = map_->GetAISystem()->CurrentPattern(boss.get());
	ASSERT_NE(pattern, nullptr) << "성향이 AI 슬롯까지 내려가지 않았습니다";
	EXPECT_EQ(pattern->skillId, monsterai::PatternOf(monsterai::AIProfile::Boss, 0).skillId);
}

// 평화로운 몬스터는 코앞에 적이 있어도 먼저 물지 않는다.
TEST_F(MonsterAISystemTest, PassiveMonsterIgnoresNearbyEnemy)
{
	const int passiveKind = FindMonsterKindWithProfile(monsterai::AIProfile::Passive);
	ASSERT_GT(passiveKind, 0);

	auto victim = SpawnCharacter();
	auto passive = SpawnMonsterOfKind(passiveKind, 1.0f, 0.0f);
	ASSERT_NE(victim, nullptr);
	ASSERT_NE(passive, nullptr);

	Tick(30);

	// 같은 자리에서 시야 스캔을 직접 돌리면 적이 잡힌다 —
	// 즉 '보이지 않아서' 가만히 있는 것이 아니라 스캔을 돌지 않아서다.
	ASSERT_EQ(map_->DetectEnemy(passive.get()), victim->GetActorId());

	EXPECT_EQ(passive->targetActorId_, -1) << "평화로운 몬스터가 먼저 적을 잡았습니다";
	EXPECT_EQ(passive->GetState(), syncnet::AIState_Patrol);
}

// 맞으면 문다. 평화로운 몬스터가 교전에 들어가는 유일한 경로다.
TEST_F(MonsterAISystemTest, PassiveMonsterRetaliatesWhenAttacked)
{
	const int passiveKind = FindMonsterKindWithProfile(monsterai::AIProfile::Passive);
	ASSERT_GT(passiveKind, 0);

	auto attacker = SpawnCharacter();
	auto passive = SpawnMonsterOfKind(passiveKind, 1.0f, 0.0f);
	ASSERT_NE(attacker, nullptr);
	ASSERT_NE(passive, nullptr);

	Tick(30);
	ASSERT_EQ(passive->targetActorId_, -1);

	// 전투 경로와 같은 순서: 킬 크레딧을 새긴 뒤 체력을 깎는다(combat::ApplyDamage).
	passive->SetLastAttacker(attacker->GetActorId());
	passive->DecrementHealth(1);
	Tick(1);

	EXPECT_EQ(passive->targetActorId_, attacker->GetActorId()) << "맞고도 반격하지 않았습니다";
	EXPECT_NE(passive->GetState(), syncnet::AIState_Patrol);
}

// 보스는 체력이 절반 아래로 떨어지면 공격 패턴을 바꾼다.
TEST_F(MonsterAISystemTest, BossSwitchesAttackPatternAtHealthThreshold)
{
	const int bossKind = FindMonsterKindWithProfile(monsterai::AIProfile::Boss);
	ASSERT_GT(bossKind, 0);

	auto boss = SpawnMonsterOfKind(bossKind, 1.0f, 0.0f);
	ASSERT_NE(boss, nullptr);
	ASSERT_NE(map_->GetAISystem(), nullptr);

	Tick(1);
	const monsterai::AttackPattern* phase1 = map_->GetAISystem()->CurrentPattern(boss.get());
	ASSERT_NE(phase1, nullptr);
	const int phase1Skill = phase1->skillId;
	const int phase1Range = phase1->attackRange;

	// 절반보다 한 점 높으면 아직 1페이즈다(경계는 '절반 이하'에서 넘어간다).
	boss->SetHealth(boss->GetMaxHealth() / 2 + 1);
	Tick(1);
	EXPECT_EQ(map_->GetAISystem()->CurrentPattern(boss.get())->skillId, phase1Skill);

	boss->SetHealth(boss->GetMaxHealth() / 2);
	Tick(1);

	const monsterai::AttackPattern* phase2 = map_->GetAISystem()->CurrentPattern(boss.get());
	ASSERT_NE(phase2, nullptr);
	EXPECT_NE(phase2->skillId, phase1Skill) << "체력이 절반인데 패턴이 그대로입니다";
	EXPECT_NE(phase2->attackRange, phase1Range);

	// 바뀐 패턴의 스킬을 실제로 시전할 수 있어야 한다 — 등록되어 있지 않으면
	// 페이즈만 넘어가고 공격은 SkillNotFound 로 조용히 거부된다.
	EXPECT_TRUE(boss->GetSkillSet().HasSkill(phase2->skillId));
	EXPECT_TRUE(boss->GetSkillSet().HasSkill(phase1Skill));
}

// 보스가 아닌 몬스터는 체력이 얼마가 되든 패턴이 하나다(페이즈 패스에 담기지도 않는다).
TEST_F(MonsterAISystemTest, NonBossKeepsSinglePattern)
{
	auto monster = SpawnMonster();
	ASSERT_NE(monster, nullptr);

	Tick(1);
	const monsterai::AttackPattern* before = map_->GetAISystem()->CurrentPattern(monster.get());
	ASSERT_NE(before, nullptr);
	const int skillId = before->skillId;

	monster->SetHealth(1);
	Tick(1);

	EXPECT_EQ(map_->GetAISystem()->CurrentPattern(monster.get())->skillId, skillId);
}
