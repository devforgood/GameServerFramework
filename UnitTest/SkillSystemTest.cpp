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
#include "Monster.h"
#include "Player.h"
#include "Skill.h"
#include "SkillSet.h"
#include "SkillRegistry.h"
#include "Vector3.h"
#include "syncnet_generated.h"

namespace
{
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

		// 리소스는 통합 폴더(Client/Assets/Resources/GameData) 한 곳에서 읽는다.
		// Map::Init 의 navmesh 로드도 같은 리졸버를 쓰므로 작업 디렉터리 고정이 필요 없다.
		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "skill.json"))
			<< "통합 GameData 폴더를 찾지 못했습니다: " << dataPath;

		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath)) << "LoadResources 실패";
		// 리소스를 다시 로드하면 이전 gamedata 포인터가 무효화되므로 공유 정의 캐시를 비운다.
		SkillRegistry::Instance().Clear();

		// navmesh 바이너리가 배치되어 있고 플레이어 스폰이 있는 맵 중 id 최소 맵을 쓴다.
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

	std::shared_ptr<Monster> SpawnMonster(float offsetX = 0.0f, float offsetZ = 0.0f)
	{
		syncnet::Vec3 pos(
			static_cast<float>(spawnX_) + offsetX,
			static_cast<float>(spawnY_),
			static_cast<float>(spawnZ_) + offsetZ);
		auto actor = map_->OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &pos);
		return std::dynamic_pointer_cast<Monster>(actor);
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

// skill.json 의 code_name 없는 엔트리(몬스터 근접, id 3)는 팩토리가 기본 Skill 로 생성한다
// — 새 스킬 = 데이터 한 줄이라는 것을 실데이터로 검증.
TEST_F(SkillSystemTest, FactoryCreatesDataOnlySkillFromJson)
{
	Skill* melee = SkillRegistry::Instance().Get(Monster::kMeleeSkillId);
	ASSERT_NE(melee, nullptr);
	ASSERT_NE(melee->gamedata, nullptr);
	EXPECT_TRUE(melee->gamedata->code_name.empty()); // 파생 클래스 없음
	EXPECT_TRUE(melee->gamedata->monster_only);
	ASSERT_FALSE(melee->gamedata->effects.empty());
	EXPECT_EQ(melee->gamedata->effects[0].type, "damage");
}

// 몬스터 공격이 플레이어와 동일한 스킬 파이프라인(TryCast→DamageEffect→combat)을 탄다.
// 쿨다운도 스킬 데이터로 제어된다.
TEST_F(SkillSystemTest, MonsterAttackGoesThroughSkillPipeline)
{
	auto victim = SpawnCharacter();
	auto monster = SpawnMonster(1.0f, 0.0f);
	ASSERT_NE(victim, nullptr);
	ASSERT_NE(monster, nullptr);

	monster->targetAgentId_ = victim->GetActorId(); // BT DetectEnemy 가 세팅하는 값

	int healthBefore = victim->GetHealth();
	monster->Attack(); // BT Action_Attack 이 호출하는 그 경로
	EXPECT_LT(victim->GetHealth(), healthBefore);
	EXPECT_EQ(victim->GetLastAttackerActorId(), monster->GetActorId()); // 킬 크레딧

	// 쿨다운(1.5초) 동안 연타해도 추가 데미지 없음.
	int healthAfterFirstHit = victim->GetHealth();
	monster->Attack();
	EXPECT_EQ(victim->GetHealth(), healthAfterFirstHit);

	// 쿨다운 경과 후 다시 공격 가능.
	monster->GetSkillSet().Update(monster.get(), 1.6f);
	monster->Attack();
	EXPECT_LT(victim->GetHealth(), healthAfterFirstHit);
}

// monster_only 스킬은 플레이어 스킬 목록에 등록되지 않는다(클라 UseSkill 치팅 차단).
TEST_F(SkillSystemTest, PlayerCannotUseMonsterOnlySkill)
{
	auto character = SpawnCharacter();
	ASSERT_NE(character, nullptr);

	CastContext ctx;
	ctx.skillId = Monster::kMeleeSkillId;
	ctx.targetPos = character->GetPosition();
	EXPECT_EQ(character->GetSkillSet().TryCast(character.get(), ctx), CastResult::SkillNotFound);
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

// aoe_damage: 시전 목표 지점(targetPos) 중심 반경(radius) 원형에 데미지.
// damage(캐스터 중심 부채꼴)와 달리 캐스터에서 떨어진 지점을 노릴 수 있다(메테오/블리자드).
TEST_F(SkillSystemTest, AoEDamageHitsAroundTargetPoint)
{
	auto attacker = SpawnCharacter();
	auto victim = SpawnCharacter(2.0f, 0.0f);
	ASSERT_NE(attacker, nullptr);
	ASSERT_NE(victim, nullptr);

	int before = victim->GetHealth();

	CastContext ctx;
	ctx.skillId = 101; // Fireball: effects=[aoe_damage], radius 4
	ctx.targetPos = victim->GetPosition();
	ASSERT_EQ(attacker->GetSkillSet().TryCast(attacker.get(), ctx), CastResult::Success);

	EXPECT_LT(victim->GetHealth(), before);                              // 목표 지점 대상 피격
	EXPECT_EQ(victim->GetLastAttackerActorId(), attacker->GetActorId()); // combat 단일 경로(킬 크레딧)
	EXPECT_EQ(attacker->GetHealth(), 100);                              // 캐스터는 AoE 에서 제외
}

// heal: 캐스터 자신의 체력을 heal 만큼 회복한다(Health 플래그로 자동 동기화).
TEST_F(SkillSystemTest, HealRestoresCasterHealth)
{
	auto caster = SpawnCharacter();
	ASSERT_NE(caster, nullptr);
	caster->SetHealth(50);

	CastContext ctx;
	ctx.skillId = 112; // Prayer: effects=[heal], heal 40
	ctx.targetPos = caster->GetPosition();
	ASSERT_EQ(caster->GetSkillSet().TryCast(caster.get(), ctx), CastResult::Success);

	EXPECT_EQ(caster->GetHealth(), 90);
}

// dash(팔라딘 차지): 순간이동(teleport)과 달리 Active 동안 지면을 따라 전진하고,
// 도착 지점에서 phase="end" 효과(aoe_damage)가 터진다.
// 속도는 range/duration 로 고정이라(거리 비례가 아니라) 가까운 목표는 duration 보다 일찍 도착하고,
// 도착하는 즉시 Active 가 끝나 착지 데미지와 입력 잠금 해제가 이뤄진다.
TEST_F(SkillSystemTest, ChargeDashesToTargetAndDamagesOnArrival)
{
	auto caster = SpawnCharacter();
	auto victim = SpawnCharacter(3.0f, 0.0f); // navmesh 위임이 검증된 좌표를 돌진 목적지로 쓴다
	ASSERT_NE(caster, nullptr);
	ASSERT_NE(victim, nullptr);

	const Vector3 start = caster->GetPosition();
	const Vector3 dest = victim->GetPosition();
	int healthBefore = victim->GetHealth();

	SkillSet& skills = caster->GetSkillSet();
	CastContext ctx;
	ctx.skillId = 119; // Charge: input_lock + dash(active) + aoe_damage(end), range 12, duration 1
	ctx.targetPos = dest;

	ASSERT_EQ(skills.TryCast(caster.get(), ctx), CastResult::Success);
	EXPECT_EQ(skills.GetState(119)->phase, SkillPhase::Active);
	EXPECT_TRUE(caster->IsInputLocked());
	EXPECT_EQ(victim->GetHealth(), healthBefore); // 데미지는 도착(end)에만 들어간다

	auto distanceXZ = [](const float* pos, const Vector3& to) {
		float dx = pos[0] - to.x;
		float dz = pos[2] - to.z;
		return std::sqrt(dx * dx + dz * dz);
	};

	// 이동 중: 출발 지점에서 멀어졌지만 아직 목적지는 아니다(순간이동이 아니라 전진).
	// 3유닛 거리를 속도 12(range 12 / duration 1)로 달리므로 0.25초면 도착한다.
	skills.Update(caster.get(), 0.1f);
	const float* midPos = map_->GetNavMap()->GetPos(caster->GetActorId());
	ASSERT_NE(midPos, nullptr);
	EXPECT_GT(distanceXZ(midPos, start), 0.5f);
	EXPECT_GT(distanceXZ(midPos, dest), 0.5f);
	EXPECT_EQ(skills.GetState(119)->phase, SkillPhase::Active);
	EXPECT_EQ(victim->GetHealth(), healthBefore); // 아직 도착 전 — 데미지 없음

	// 도착: duration(1초)이 다 되기 전이라도 Active 가 끝나 착지 효과가 적용된다.
	skills.Update(caster.get(), 0.2f);
	EXPECT_EQ(skills.GetState(119)->phase, SkillPhase::Cooldown);
	EXPECT_FALSE(caster->IsInputLocked());

	const float* endPos = map_->GetNavMap()->GetPos(caster->GetActorId());
	ASSERT_NE(endPos, nullptr);
	EXPECT_LT(distanceXZ(endPos, dest), 1.0f) << "돌진이 목적지에 도달하지 못했습니다";
	EXPECT_LT(victim->GetHealth(), healthBefore);
	EXPECT_EQ(victim->GetLastAttackerActorId(), caster->GetActorId()); // combat 단일 경로
}

// dash 의 이동 거리는 데이터(range)가 상한이다 — 클라가 맵 반대편을 찍어도 range 까지만 간다.
TEST_F(SkillSystemTest, DashClampsDistanceToRange)
{
	auto caster = SpawnCharacter();
	ASSERT_NE(caster, nullptr);

	const Vector3 start = caster->GetPosition();
	SkillSet& skills = caster->GetSkillSet();

	CastContext ctx;
	ctx.skillId = 130; // Vault: input_lock + dash, range 8, duration 1 (데미지 없음)
	ctx.targetPos = Vector3(start.x + 500.0f, start.y, start.z);

	ASSERT_EQ(skills.TryCast(caster.get(), ctx), CastResult::Success);
	skills.Update(caster.get(), 1.1f); // 지속시간 종료까지 진행

	const float* pos = map_->GetNavMap()->GetPos(caster->GetActorId());
	ASSERT_NE(pos, nullptr);
	float dx = pos[0] - start.x;
	float dz = pos[2] - start.z;
	EXPECT_LE(std::sqrt(dx * dx + dz * dz), 8.5f) << "range 를 넘어 돌진했습니다";
}

// knockback: 캐스터 주변 대상을 캐스터 반대 방향으로 밀어낸다(강타/전투 함성).
TEST_F(SkillSystemTest, KnockbackPushesTargetsAwayFromCaster)
{
	auto caster = SpawnCharacter();
	auto victim = SpawnCharacter(1.0f, 0.0f);
	ASSERT_NE(caster, nullptr);
	ASSERT_NE(victim, nullptr);

	const Vector3 casterPos = caster->GetPosition();
	auto distanceFromCaster = [&casterPos](const float* pos) {
		float dx = pos[0] - casterPos.x;
		float dz = pos[2] - casterPos.z;
		return std::sqrt(dx * dx + dz * dz);
	};

	const float* before = map_->GetNavMap()->GetPos(victim->GetActorId());
	ASSERT_NE(before, nullptr);
	float distanceBefore = distanceFromCaster(before);
	int healthBefore = victim->GetHealth();

	CastContext ctx;
	ctx.skillId = 123; // War Cry: damage(range 6, angle 360) + knockback 4
	ctx.targetPos = victim->GetPosition();
	ASSERT_EQ(caster->GetSkillSet().TryCast(caster.get(), ctx), CastResult::Success);

	const float* after = map_->GetNavMap()->GetPos(victim->GetActorId());
	ASSERT_NE(after, nullptr);
	EXPECT_GT(distanceFromCaster(after), distanceBefore + 1.0f) << "대상이 밀려나지 않았습니다";
	EXPECT_LT(victim->GetHealth(), healthBefore); // 데미지도 함께 적용된다

	// 캐스터 자신은 밀리지 않는다.
	const float* casterAfter = map_->GetNavMap()->GetPos(caster->GetActorId());
	ASSERT_NE(casterAfter, nullptr);
	EXPECT_LT(distanceFromCaster(casterAfter), 0.5f);
}

// 패시브(Holy Fire 오라): 유저 액션(시전) 없이 보유만으로 Update 마다 자동 적용된다.
// 오라는 type("passive")이 아니라 effect(aura_damage, phase="pulse")로 표현되고,
// pulse_interval 마다 캐스터 중심 반경에 데미지를 방출한다.
TEST_F(SkillSystemTest, PassiveAuraAutoPulsesDamageWithoutCasting)
{
	auto caster = SpawnCharacter();
	auto victim = SpawnMonster(1.0f, 0.0f);
	ASSERT_NE(caster, nullptr);
	ASSERT_NE(victim, nullptr);

	SkillSet& skills = caster->GetSkillSet();

	// 패시브는 시전 대상이 아니다 — 클라가 UseSkill 로 보내도 거부된다.
	CastContext ctx;
	ctx.skillId = 200; // Holy Fire: aura_damage pulse, interval 0.5, radius 5
	ctx.targetPos = caster->GetPosition();
	EXPECT_EQ(skills.TryCast(caster.get(), ctx), CastResult::SkillNotFound);

	// 시전 없이도 Update 만으로 지속 적용된다.
	int before = victim->GetHealth();
	skills.Update(caster.get(), 0.4f); // interval(0.5) 미달 — 아직 pulse 없음
	EXPECT_EQ(victim->GetHealth(), before);

	skills.Update(caster.get(), 0.2f); // 누적 0.6 ≥ 0.5 — 1회 pulse
	int afterOne = victim->GetHealth();
	EXPECT_LT(afterOne, before);

	skills.Update(caster.get(), 0.5f); // 추가 1회 pulse
	EXPECT_LT(victim->GetHealth(), afterOne);
	EXPECT_EQ(victim->GetLastAttackerActorId(), caster->GetActorId());
}

// 패시브(Prayer 오라): 보유만으로 pulse 마다 캐스터 체력을 회복한다.
// (Holy Fire 패시브도 함께 켜져 있지만 근처 대상이 없으면 캐스터에는 영향이 없다.)
TEST_F(SkillSystemTest, PassiveAuraAutoPulsesHeal)
{
	auto caster = SpawnCharacter();
	ASSERT_NE(caster, nullptr);
	caster->SetHealth(50);

	SkillSet& skills = caster->GetSkillSet();

	skills.Update(caster.get(), 1.0f); // Prayer(interval 1.0) 1회 pulse → +5
	EXPECT_EQ(caster->GetHealth(), 55);
	skills.Update(caster.get(), 2.0f); // 밀린 2회 pulse → +10
	EXPECT_EQ(caster->GetHealth(), 65);
}
