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
#include "Player.h"
#include "SkillRegistry.h"
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
// 몬스터 BT 백엔드 동등성 검증.
//
// 세 백엔드(behaviortree_cpp / 인하우스 / ECS)는 같은 트리 구조·같은 노드 로직을 구현하므로,
// 같은 상황에서 같은 결정을 내려야 한다. 틱 비용 때문에 기본값을 계속 바꿔 왔는데
// (Benchmark/PERFORMANCE.md), 그 교체가 AI 동작을 바꾸지 않았음을 여기서 고정한다.
//
// ECS 백엔드는 배회 중 몇 틱에 한 번만 사고하므로(MonsterAISystem::kIdleThinkInterval)
// 반응이 즉시가 아니다. 아래 시나리오들이 넉넉한 틱 수를 도는 이유다 —
// 원래도 적 탐지는 10틱마다 한 번이라(DetectEnemy 스태거링) 관측 가능한 지연은 같다.
//
// 백엔드는 스폰 시점에 트리를 결정하므로 반드시 몬스터 생성 전에 설정한다.
//---------------------------------------------------------------------------------------

class MonsterBTTest : public ::testing::TestWithParam<Monster::BTBackend>
{
protected:
	std::unique_ptr<World> world_;
	std::unique_ptr<Map> map_;
	std::vector<std::shared_ptr<Player>> players_;
	Monster::BTBackend previousBackend_ = Monster::BTBackend::CodeBase;
	double spawnX_ = 0, spawnY_ = 0, spawnZ_ = 0;

	static constexpr float kTickDt = 1.0f / 30.0f;

	void SetUp() override
	{
		EnsureNetLogger();

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
		Monster::btBackend_ = GetParam(); // 스폰 전에 백엔드를 고정한다
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

	// 성향은 monster.json 의 종류가 정한다. 서버가 밟는 순서(스폰 → SetDataId)를 그대로 태운다.
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
};

// 근처의 캐릭터를 탐지해 추격/공격까지 진행한다.
// (탐지는 배회 중 10틱마다 1회로 스태거링되므로 여유 있게 틱을 돌린다.)
TEST_P(MonsterBTTest, DetectsAndAttacksNearbyCharacter)
{
	auto victim = SpawnCharacter();
	auto monster = SpawnMonster(1.0f, 0.0f);
	ASSERT_NE(victim, nullptr);
	ASSERT_NE(monster, nullptr);

	const int healthBefore = victim->GetHealth();

	for (int i = 0; i < 20; ++i)
		map_->UpdateActors(kTickDt);

	EXPECT_EQ(monster->targetActorId_, victim->GetActorId()) << "적을 탐지하지 못했습니다";
	EXPECT_LT(victim->GetHealth(), healthBefore) << "탐지 후 공격이 실행되지 않았습니다";
	EXPECT_EQ(victim->GetLastAttackerActorId(), monster->GetActorId());
}

// 주변에 적이 없으면 배회한다(탐지 실패 → ActionPatrol → Patrol 상태 유지).
TEST_P(MonsterBTTest, PatrolsWhenNoEnemyNearby)
{
	auto monster = SpawnMonster();
	ASSERT_NE(monster, nullptr);

	for (int i = 0; i < 20; ++i)
		map_->UpdateActors(kTickDt);

	EXPECT_EQ(monster->targetActorId_, -1);
	EXPECT_EQ(monster->GetState(), syncnet::AIState_Patrol);
}

// 교전 중에는 전체 재탐색 대신 잡은 대상을 유지한다(대상이 유효한 동안 타깃이 흔들리지 않는다).
TEST_P(MonsterBTTest, KeepsTargetWhileItStaysVisible)
{
	auto first = SpawnCharacter();
	auto second = SpawnCharacter(0.0f, 1.0f); // 후보가 둘이어도 잡은 대상을 바꾸지 않는다
	auto monster = SpawnMonster(1.0f, 0.0f);
	ASSERT_NE(first, nullptr);
	ASSERT_NE(second, nullptr);
	ASSERT_NE(monster, nullptr);

	for (int i = 0; i < 20; ++i)
		map_->UpdateActors(kTickDt);

	const int acquired = monster->targetActorId_;
	ASSERT_GE(acquired, 0) << "적을 탐지하지 못했습니다";

	for (int i = 0; i < 20; ++i)
		map_->UpdateActors(kTickDt);

	EXPECT_EQ(monster->targetActorId_, acquired) << "유효한 대상이 있는데 타깃이 바뀌었습니다";
}

// 잡은 대상이 시야 밖으로 벗어나면 놓치고(타깃 해제) 배회로 돌아간다.
TEST_P(MonsterBTTest, DropsTargetWhenItLeavesViewRange)
{
	auto victim = SpawnCharacter();
	auto monster = SpawnMonster(1.0f, 0.0f);
	ASSERT_NE(victim, nullptr);
	ASSERT_NE(monster, nullptr);

	for (int i = 0; i < 20; ++i)
		map_->UpdateActors(kTickDt);
	ASSERT_EQ(monster->targetActorId_, victim->GetActorId());

	// 대상이 시야 반경(10) 밖으로 멀어진다.
	const Vector3& here = victim->GetPosition();
	victim->SetPosition(here.x + 50.0f, here.y, here.z + 50.0f);

	// 재탐색은 스태거링(10틱)에 걸리므로 충분히 돌린다. 근처에 다른 캐릭터가 없으므로
	// 결국 타깃이 없는 상태(배회)로 돌아간다.
	for (int i = 0; i < 30; ++i)
		map_->UpdateActors(kTickDt);

	EXPECT_EQ(monster->targetActorId_, -1) << "시야 밖 대상을 계속 타깃으로 잡고 있습니다";
	EXPECT_EQ(monster->GetState(), syncnet::AIState_Patrol);
}

// 체력이 0 이하가 되면 사망 분기로 넘어간다(생존 조건 실패 → ActionDead).
TEST_P(MonsterBTTest, SwitchesToDeadBranchWhenHealthDepleted)
{
	auto monster = SpawnMonster();
	ASSERT_NE(monster, nullptr);

	map_->UpdateActors(kTickDt);
	EXPECT_NE(monster->GetState(), syncnet::AIState_Dead);

	monster->SetHealth(0);
	map_->UpdateActors(kTickDt);
	EXPECT_EQ(monster->GetState(), syncnet::AIState_Dead);

	// Destroyed 는 Delay(2000ms) 이후라 이 틱에는 아직 아니다(두 백엔드 동일).
	map_->UpdateActors(kTickDt);
	EXPECT_EQ(monster->GetState(), syncnet::AIState_Dead);
}

//---------------------------------------------------------------------------------------
// 성향(monster.json 의 "ai")도 백엔드를 가리지 않는다.
//
// 성향이 트리에 붙는 방식은 백엔드마다 다르다 — BT 둘은 노드(ConditionDetectEnemy 의
// 성향 분기, ActionUpdateCombatPhase)로, ECS 는 패스로 처리한다. 그래도 판정 규칙 자체는
// Monster 와 공유 표(MonsterAIProfile.h) 한 곳에 있으므로, 셋의 결과가 같아야 한다.
//---------------------------------------------------------------------------------------

// 평화로운 성향은 코앞에 적이 있어도 먼저 물지 않는다.
TEST_P(MonsterBTTest, PassiveMonsterDoesNotAttackFirst)
{
	const int passiveKind = FindMonsterKindWithProfile(monsterai::AIProfile::Passive);
	ASSERT_GT(passiveKind, 0) << "monster.json 에 passive 몬스터가 없습니다";

	auto victim = SpawnCharacter();
	auto passive = SpawnMonsterOfKind(passiveKind, 1.0f, 0.0f);
	ASSERT_NE(victim, nullptr);
	ASSERT_NE(passive, nullptr);

	const int healthBefore = victim->GetHealth();
	for (int i = 0; i < 30; ++i)
		map_->UpdateActors(kTickDt);

	// 같은 자리에서 시야 스캔을 직접 돌리면 적이 잡힌다 —
	// 즉 '보이지 않아서' 가만히 있는 것이 아니라 스캔을 돌지 않아서다.
	ASSERT_EQ(map_->DetectEnemy(passive.get()), victim->GetActorId());

	EXPECT_EQ(passive->targetActorId_, -1) << "평화로운 몬스터가 먼저 적을 잡았습니다";
	EXPECT_EQ(passive->GetState(), syncnet::AIState_Patrol);
	EXPECT_EQ(victim->GetHealth(), healthBefore);
}

// 맞으면 문다. 평화로운 성향이 교전에 들어가는 유일한 경로다.
TEST_P(MonsterBTTest, PassiveMonsterRetaliates)
{
	const int passiveKind = FindMonsterKindWithProfile(monsterai::AIProfile::Passive);
	ASSERT_GT(passiveKind, 0);

	auto attacker = SpawnCharacter();
	auto passive = SpawnMonsterOfKind(passiveKind, 1.0f, 0.0f);
	ASSERT_NE(attacker, nullptr);
	ASSERT_NE(passive, nullptr);

	for (int i = 0; i < 30; ++i)
		map_->UpdateActors(kTickDt);
	ASSERT_EQ(passive->targetActorId_, -1);

	// 전투 경로와 같은 순서: 킬 크레딧을 새긴 뒤 체력을 깎는다(combat::ApplyDamage).
	passive->SetLastAttacker(attacker->GetActorId());
	passive->DecrementHealth(1);

	for (int i = 0; i < 3; ++i)
		map_->UpdateActors(kTickDt);

	EXPECT_EQ(passive->targetActorId_, attacker->GetActorId()) << "맞고도 반격하지 않았습니다";
	EXPECT_NE(passive->GetState(), syncnet::AIState_Patrol);
}

// 보스는 체력이 절반 이하가 되면 공격 패턴(스킬 + 사거리)이 바뀐다.
TEST_P(MonsterBTTest, BossSwitchesAttackPatternAtHealthThreshold)
{
	const int bossKind = FindMonsterKindWithProfile(monsterai::AIProfile::Boss);
	ASSERT_GT(bossKind, 0) << "monster.json 에 boss 몬스터가 없습니다";

	auto boss = SpawnMonsterOfKind(bossKind, 1.0f, 0.0f);
	ASSERT_NE(boss, nullptr);

	map_->UpdateActors(kTickDt);
	const int phase1Skill = boss->GetAttackPattern().skillId;
	const int phase1Range = boss->GetAttackPattern().attackRange;
	EXPECT_EQ(boss->GetCombatPhase(), 0) << "만피인데 1페이즈가 아닙니다";

	boss->SetHealth(boss->GetMaxHealth() / 2 + 1);
	map_->UpdateActors(kTickDt);
	EXPECT_EQ(boss->GetAttackPattern().skillId, phase1Skill) << "아직 절반 위인데 패턴이 바뀌었습니다";

	boss->SetHealth(boss->GetMaxHealth() / 2);
	map_->UpdateActors(kTickDt);

	EXPECT_NE(boss->GetAttackPattern().skillId, phase1Skill) << "체력이 절반인데 패턴이 그대로입니다";
	EXPECT_NE(boss->GetAttackPattern().attackRange, phase1Range);

	// 바뀐 패턴의 스킬을 실제로 시전할 수 있어야 한다 — 등록되어 있지 않으면
	// 페이즈만 넘어가고 공격은 SkillNotFound 로 조용히 거부된다.
	EXPECT_TRUE(boss->GetSkillSet().HasSkill(boss->GetAttackPattern().skillId));
}

INSTANTIATE_TEST_CASE_P(
	Backends,
	MonsterBTTest,
	::testing::Values(Monster::BTBackend::Ecs, Monster::BTBackend::CodeBase, Monster::BTBackend::BTCpp),
	[](const ::testing::TestParamInfo<Monster::BTBackend>& info) {
		switch (info.param)
		{
		case Monster::BTBackend::Ecs:      return "Ecs";
		case Monster::BTBackend::CodeBase: return "CodeBase";
		default:                           return "BTCpp";
		}
	});
