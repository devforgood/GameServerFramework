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
// 두 백엔드(behaviortree_cpp / 인하우스)는 같은 트리 구조·같은 노드 로직을 구현하므로,
// 같은 상황에서 같은 결정을 내려야 한다. 틱 비용 때문에 기본값을 인하우스로 바꿨는데
// (Benchmark/PERFORMANCE.md), 그 교체가 AI 동작을 바꾸지 않았음을 여기서 고정한다.
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

	EXPECT_EQ(monster->targetAgentId_, victim->GetActorId()) << "적을 탐지하지 못했습니다";
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

	EXPECT_EQ(monster->targetAgentId_, -1);
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

INSTANTIATE_TEST_CASE_P(
	Backends,
	MonsterBTTest,
	::testing::Values(Monster::BTBackend::CodeBase, Monster::BTBackend::BTCpp),
	[](const ::testing::TestParamInfo<Monster::BTBackend>& info) {
		return info.param == Monster::BTBackend::CodeBase ? "CodeBase" : "BTCpp";
	});
