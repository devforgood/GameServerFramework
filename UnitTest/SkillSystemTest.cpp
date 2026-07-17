#include "pch.h"
#include <gtest/gtest.h>
#include <cmath>
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
#include "Player.h"
#include "Skill.h"
#include "SkillSet.h"
#include "SkillRegistry.h"
#include "Vector3.h"
#include "syncnet_generated.h"

namespace
{
	// 테스트 실행 위치에 따라 GameData 경로가 달라지므로 후보들을 순회한다.
	// Map::Init 이 "GameData/..." 상대 경로로 navmesh 를 읽으므로, GameData 를 포함한
	// 디렉터리를 찾아 작업 디렉터리로 고정한다.
	std::string ResolveGameDataRoot()
	{
		const std::vector<std::string> candidates = { ".", "../UnitTest", "../../UnitTest" };
		for (const auto& p : candidates)
		{
			if (std::filesystem::exists(p + "/GameData/skill.json"))
				return p;
		}
		return "";
	}

	// 엔진 코드의 LOG 매크로는 "net" 로거를 역참조하므로 테스트 하네스에 등록해 둔다.
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
// 스킬 파이프라인 검증: 정의/상태 분리(SkillRegistry/SkillSet), TryCast 검증 게이트,
// 페이즈 상태 머신(Active→Cooldown→Ready), 효과 조합(damage/input_lock/teleport),
// 데미지 단일 경로(combat::ApplyDamage, 킬 크레딧), 데이터 전용 스킬 추가.
// 실제 Map(navmesh + grid) 위에서 네트워크 메시지 없이(CastContext) 시전한다.
//---------------------------------------------------------------------------------------

class SkillSystemTest : public ::testing::Test
{
protected:
	std::unique_ptr<World> world_;
	std::unique_ptr<Map> map_;
	std::vector<std::shared_ptr<Player>> players_; // 캐릭터 수명 유지용
	double spawnX_ = 0, spawnY_ = 0, spawnZ_ = 0;  // 클라 좌표계 스폰 지점

	void SetUp() override
	{
		EnsureNetLogger();

		std::string root = ResolveGameDataRoot();
		ASSERT_FALSE(root.empty()) << "GameData/skill.json 을 알려진 경로에서 찾지 못했습니다.";
		std::filesystem::current_path(root);

		ASSERT_TRUE(ResourceLoader::Instance().LoadResources("GameData/")) << "LoadResources 실패";
		// 리소스를 다시 로드하면 이전 gamedata 포인터가 무효화되므로 공유 정의 캐시를 비운다.
		SkillRegistry::Instance().Clear();

		// navmesh 바이너리가 배치되어 있고 플레이어 스폰이 있는 맵 중 id 최소 맵을 쓴다.
		const gamedata::Map* mapData = nullptr;
		for (const auto& [id, m] : ResourceLoader::Instance().GetMaps())
		{
			if (m == nullptr || m->navmesh_path.empty())
				continue;
			if (!std::filesystem::exists("GameData/" + m->navmesh_path))
				continue;
			if (m->spawn_points.player_spawn.empty())
				continue;
			if (mapData == nullptr || m->id < mapData->id)
				mapData = m;
		}
		ASSERT_NE(mapData, nullptr) << "navmesh 가 배치된 맵이 없습니다.";

		world_ = std::make_unique<World>();
		map_ = std::make_unique<Map>(world_.get());
		ASSERT_TRUE(map_->Init("crowd", mapData)) << "Map::Init 실패";

		const auto& spawn = mapData->spawn_points.player_spawn[0].position;
		spawnX_ = spawn.x;
		spawnY_ = spawn.y;
		spawnZ_ = spawn.z;
	}

	// 프로덕션 경로(OnAddAgent → ActorFactory → grid 등록)로 캐릭터를 스폰한다.
	std::shared_ptr<Character> SpawnCharacter(float offsetX = 0.0f, float offsetZ = 0.0f)
	{
		auto player = std::make_shared<Player>();
		players_.push_back(player);

		syncnet::Vec3 pos(
			static_cast<float>(spawnX_) + offsetX,
			static_cast<float>(spawnY_),
			static_cast<float>(spawnZ_) + offsetZ);
		auto actor = map_->OnAddAgent(player, syncnet::GameObjectType_Character, &pos);
		return std::dynamic_pointer_cast<Character>(actor);
	}
};

// 스킬 정의는 id 당 1개만 생성되어 공유된다(기존: 캐릭터마다 전체 테이블 인스턴스화).
TEST_F(SkillSystemTest, RegistrySharesSkillDefinitions)
{
	Skill* attack = SkillRegistry::Instance().Get(1);
	Skill* jump = SkillRegistry::Instance().Get(2);
	ASSERT_NE(attack, nullptr);
	ASSERT_NE(jump, nullptr);

	EXPECT_EQ(attack, SkillRegistry::Instance().Get(1)); // 같은 인스턴스 공유
	EXPECT_EQ(SkillRegistry::Instance().Get(999), nullptr);

	ASSERT_NE(attack->gamedata, nullptr);
	EXPECT_FALSE(attack->gamedata->effects.empty()); // 효과 조합이 데이터로 정의됨
	EXPECT_GT(attack->gamedata->cooldown, 0.0);
}

// TryCast 검증 게이트: 미보유 스킬 거부, 성공 시 쿨다운 진입, 쿨다운 중 거부, 경과 후 회복.
TEST_F(SkillSystemTest, TryCastValidatesAndRunsCooldown)
{
	auto caster = SpawnCharacter();
	ASSERT_NE(caster, nullptr);
	SkillSet& skills = caster->GetSkillSet();

	CastContext ctx;
	ctx.skillId = 999;
	ctx.targetPos = caster->GetPosition();
	EXPECT_EQ(skills.TryCast(caster.get(), ctx), CastResult::SkillNotFound);

	ctx.skillId = 1;
	EXPECT_EQ(skills.TryCast(caster.get(), ctx), CastResult::Success);
	ASSERT_NE(skills.GetState(1), nullptr);
	EXPECT_EQ(skills.GetState(1)->phase, SkillPhase::Cooldown);

	EXPECT_EQ(skills.TryCast(caster.get(), ctx), CastResult::OnCooldown);

	skills.Update(caster.get(), 0.6f); // cooldown 0.5초 경과
	EXPECT_EQ(skills.GetState(1)->phase, SkillPhase::Ready);
	EXPECT_EQ(skills.TryCast(caster.get(), ctx), CastResult::Success);
}

// 데미지는 combat 단일 경로를 거친다: 체력 감소 + 킬 크레딧(SetLastAttacker) 기록.
TEST_F(SkillSystemTest, DamageGoesThroughCombatPipeline)
{
	auto attacker = SpawnCharacter();
	auto victim = SpawnCharacter(1.0f, 0.0f);
	ASSERT_NE(attacker, nullptr);
	ASSERT_NE(victim, nullptr);

	int healthBefore = victim->GetHealth();

	CastContext ctx;
	ctx.skillId = 1;
	ctx.targetPos = victim->GetPosition();
	ASSERT_EQ(attacker->GetSkillSet().TryCast(attacker.get(), ctx), CastResult::Success);

	EXPECT_LT(victim->GetHealth(), healthBefore);      // 데미지 적용
	EXPECT_GE(victim->GetHealth(), healthBefore - 20); // max_damage 상한
	EXPECT_EQ(victim->GetLastAttackerActorId(), attacker->GetActorId()); // 킬 크레딧
}

// 점프 수명주기: 시전(Active, 입력 잠금) → 지속시간 종료(teleport, 잠금 해제, Cooldown) → Ready.
TEST_F(SkillSystemTest, JumpLifecycleLocksInputAndTeleports)
{
	auto caster = SpawnCharacter();
	auto marker = SpawnCharacter(2.0f, 0.0f); // navmesh 위임이 검증된 좌표를 목적지로 쓴다
	ASSERT_NE(caster, nullptr);
	ASSERT_NE(marker, nullptr);
	Vector3 dest = marker->GetPosition();

	SkillSet& skills = caster->GetSkillSet();
	CastContext ctx;
	ctx.skillId = 2;
	ctx.targetPos = dest;
	ctx.clientDuration = 1.0f;

	ASSERT_EQ(skills.TryCast(caster.get(), ctx), CastResult::Success);
	ASSERT_NE(skills.GetState(2), nullptr);
	EXPECT_EQ(skills.GetState(2)->phase, SkillPhase::Active);
	EXPECT_TRUE(caster->IsInputLocked());

	// 점프 중에는 다른 스킬 시전 불가(입력 잠금).
	CastContext attackCtx;
	attackCtx.skillId = 1;
	attackCtx.targetPos = dest;
	EXPECT_EQ(skills.TryCast(caster.get(), attackCtx), CastResult::InputLocked);

	skills.Update(caster.get(), 0.5f);
	EXPECT_EQ(skills.GetState(2)->phase, SkillPhase::Active);

	skills.Update(caster.get(), 0.6f); // 지속시간 1초 경과 → 착지
	EXPECT_EQ(skills.GetState(2)->phase, SkillPhase::Cooldown);
	EXPECT_FALSE(caster->IsInputLocked());

	const float* navPos = map_->GetNavMap()->GetPos(caster->GetActorId());
	ASSERT_NE(navPos, nullptr);
	float dx = navPos[0] - dest.x;
	float dz = navPos[2] - dest.z;
	EXPECT_LT(std::sqrt(dx * dx + dz * dz), 1.0f) << "텔레포트 위치가 목표 지점과 다릅니다";

	EXPECT_EQ(skills.TryCast(caster.get(), ctx), CastResult::OnCooldown);
	skills.Update(caster.get(), 3.1f); // cooldown 3초 경과
	EXPECT_EQ(skills.GetState(2)->phase, SkillPhase::Ready);
}

// 서버 권위: 클라가 지속시간을 부풀려 보내도 데이터(duration)가 상한이다.
TEST_F(SkillSystemTest, ClientDurationClampedByData)
{
	auto caster = SpawnCharacter();
	ASSERT_NE(caster, nullptr);
	SkillSet& skills = caster->GetSkillSet();

	CastContext ctx;
	ctx.skillId = 2;
	ctx.targetPos = caster->GetPosition();
	ctx.clientDuration = 99.0f;

	ASSERT_EQ(skills.TryCast(caster.get(), ctx), CastResult::Success);
	EXPECT_LE(skills.GetState(2)->activeRemaining, 1.0f);

	skills.Update(caster.get(), 1.1f);
	EXPECT_NE(skills.GetState(2)->phase, SkillPhase::Active);
}

// 새 스킬은 C++ 클래스/팩토리 재생성 없이 데이터(effects 조합)만으로 추가할 수 있다.
TEST_F(SkillSystemTest, DataOnlySkillNeedsNoNewClass)
{
	auto attacker = SpawnCharacter();
	auto victim = SpawnCharacter(1.0f, 0.0f);
	ASSERT_NE(attacker, nullptr);
	ASSERT_NE(victim, nullptr);

	gamedata::SkillEffect fx;
	fx.type = "damage"; // phase 기본값 "" = 시전 즉시

	gamedata::Skill data{};
	data.id = 777;
	data.min_damage = 7;
	data.max_damage = 7;
	data.range = 5;
	data.angle = 360;
	data.cooldown = 0.0;
	data.effects.push_back(fx);

	Skill dataOnly; // 파생 클래스가 아닌 기본 Skill
	dataOnly.gamedata = &data;

	SkillSet& skills = attacker->GetSkillSet();
	skills.AddSkill(777, &dataOnly);

	int healthBefore = victim->GetHealth();
	CastContext ctx;
	ctx.skillId = 777;
	ctx.targetPos = victim->GetPosition();
	EXPECT_EQ(skills.TryCast(attacker.get(), ctx), CastResult::Success);
	EXPECT_EQ(victim->GetHealth(), healthBefore - 7);
}

// 알 수 없는 효과 type 은 시전을 거부하고 쿨다운도 소모하지 않는다.
TEST_F(SkillSystemTest, UnknownEffectRejectsCastWithoutCooldown)
{
	auto caster = SpawnCharacter();
	ASSERT_NE(caster, nullptr);

	gamedata::SkillEffect fx;
	fx.type = "explode";

	gamedata::Skill data{};
	data.id = 778;
	data.cooldown = 10.0;
	data.effects.push_back(fx);

	Skill dataOnly;
	dataOnly.gamedata = &data;

	SkillSet& skills = caster->GetSkillSet();
	skills.AddSkill(778, &dataOnly);

	CastContext ctx;
	ctx.skillId = 778;
	ctx.targetPos = caster->GetPosition();
	EXPECT_EQ(skills.TryCast(caster.get(), ctx), CastResult::UnknownEffect);
	EXPECT_EQ(skills.GetState(778)->phase, SkillPhase::Ready); // 실패 시 상태 불변
}
