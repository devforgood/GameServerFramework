#include "Monster.h"
#include <random>
#include <functional>
#include "World.h"
#include "DetourCommon.h"
#include "MathHelper.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "Common.h"
#include "Map.h"
#include "INavMovement.h"
#include "MonsterAISystem.h"
#include "SkillRegistry.h"
#include "Player.h"
#include "Character.h"
#include "PlayerEventBrokerProxy.h"
#include "EventMessage.h"
#include "PartyCredit.h"


extern std::_Binder<std::_Unforced, std::uniform_int_distribution<>&, std::default_random_engine&> dice;

// 기본 백엔드는 ECS 다. 세 백엔드는 같은 노드 로직(MonsterBTNodes.h)을 공유하고 트리 구조도
// 1:1 로 같지만 실행 방식이 다르다 — ECS 는 트리를 전부가 공유하고, 개체는 상태 컴포넌트만
// 가지며, 배회 중에는 몇 틱에 한 번만 사고한다(Benchmark/PERFORMANCE.md).
Monster::BTBackend Monster::btBackend_ = Monster::BTBackend::Ecs;

// 기본값 꺼짐. 켜는 것은 디버깅 목적의 선택이지 운영 기본값이 아니다(Monster.h 참고).
bool Monster::debugBossOnBTCpp_ = false;

// 백엔드 정책. 개체마다 다른 백엔드를 쓰고 싶으면 규칙을 여기에만 추가한다 —
// 나머지 코드는 Monster::backend_ 만 보고, 어떤 규칙으로 그 값이 나왔는지 모른다.
Monster::BTBackend Monster::ResolveBTBackend(monsterai::AIProfile profile)
{
	// 보스는 한 맵에 한둘이라 트리를 개체마다 만드는 비용을 감당할 수 있고,
	// 대신 BT 디버그 뷰어로 판단 과정을 그대로 들여다볼 수 있다.
	if (debugBossOnBTCpp_ && profile == monsterai::AIProfile::Boss)
		return BTBackend::BTCpp;

	return btBackend_;
}

Monster::Monster(Map* map)
	: Actor(map)
{
	gameObjectType_ = syncnet::GameObjectType::GameObjectType_Monster;
	targetActorId_ = -1;

	// 기본 성향이 쓰는 스킬을 등록한다. 종류(monster.json)가 정해지면 SetDataId 가
	// 그 성향의 스킬로 다시 등록한다. 데이터가 없으면 nullptr 등록이라 TryCast 가
	// SkillNotFound 로 거부한다.
	ApplyAIProfile(aiProfile_);
}

Monster::~Monster()
{
	// BT 트리는 brain_ 이 소유한다(백엔드별 해제 방식도 그쪽에 있다).
}

bool Monster::Init(Vector3& pos)
{
	float speed = 3.5f;
	int actor_id = map_->GetNavMap()->AddAgent(pos.pos(), speed);
	if (actor_id < 0)
	{
		LOG.error("OnAddAgent error in Map.addAgent()");
		return false;
	}

	if (map_->actorMap_.find(actor_id) != map_->actorMap_.end())
	{
		LOG.error("OnAddAgent error already exist in actorMap_");
		return false;
	}

	this->SetPosition(pos.x, pos.y, pos.z);
	actorId_ = actor_id;
	this->speed = speed;
	auto& entityManager = map_->systemManager_->GetEntityManager();
	entityManager.GetComponent<engine::StateComponent>(entityId_).ActorID = actorId_;


	if (map_ != nullptr) {
		// 스폰 위치는 네비메시에 스냅된 현재 위치를 기준으로 삼는다(patrol 의 중심점).
		dtVcopy(spawnPos_, map_->GetNavMap()->GetPos(actorId_));
	}

	name_ = "Monster:" + std::to_string(actorId_);

	// 선택된 백엔드의 트리 하나만 만든다. 예전에는 둘 다 만들어 두고 하나만 틱했는데,
	// 스폰마다 Monster.xml 을 읽어 파싱하는 비용(behaviortree_cpp)을 쓰지도 않는 트리에 지불했다.
	//
	// 여기서는 아직 종류(monster.json)를 모르므로 기본 백엔드로 붙는다. 종류가 새겨지면
	// ApplyAIProfile 이 성향에 맞는 백엔드로 갈아탄다 — 규칙상 백엔드가 달라지는 몬스터
	// (기본값은 아무도 없다) 에서만 실제로 다시 만들어진다.
	brain_.Create(this, backend_);
	return true;
}

void Monster::Update(float dt)
{
	Actor::Update(dt);
	skillSet_.Update(this, dt); // 스킬 쿨다운/페이즈 진행
	brain_.Tick(this);
}

int Monster::AttackRange()
{
	INavMovement* nav = map_->GetNavMap();
	const float* this_pos = nav->GetPos(GetActorId());
	const float* target_pos = nav->GetPos(targetActorId_);

	// 사거리는 성향과 페이즈가 정한다 — 보스가 2페이즈에서 멀찍이 서서 때리는 것이
	// 여기서 나온다(1페이즈에는 붙어야 한다).
	if (ManhattanDistance(this_pos, target_pos) > GetAttackPattern().attackRange)
		return -1;

	float hitPoint[3];
	if (nav->Raycast(GetActorId(), target_pos, hitPoint) == false)
	{
		return targetActorId_;
	}
	return -1;
}

int Monster::Attack()
{
	return Attack(GetAttackPattern().skillId);
}

int Monster::Attack(int skillId)
{
	map_->GetNavMap()->Stop(GetActorId());

	// 추격 대상 방향으로 스킬을 시전한다 — 플레이어와 동일한 스킬 파이프라인.
	// 쿨다운 등으로 거부되면 이번 틱은 공격하지 않는다(BT 가 다음 틱에 재시도).
	auto target = map_->FindActor(targetActorId_);
	if (target != nullptr)
	{
		CastContext ctx;
		ctx.skillId = skillId;
		ctx.targetActorId = targetActorId_;
		ctx.targetPos = target->GetPosition();
		skillSet_.TryCast(this, ctx);
	}
	return 0;
}
int Monster::Resume()
{
	map_->GetNavMap()->Resume(GetActorId());
	return 0;
}

void Monster::WakeAI()
{
	if (map_ == nullptr)
		return;

	monsterai::MonsterAISystem* aiSystem = map_->GetAISystem();
	if (aiSystem != nullptr)
		aiSystem->Wake(this);
}

void Monster::SetHealth(int health)
{
	Actor::SetHealth(health);
	WakeAI();
}

void Monster::DecrementHealth(int amount)
{
	Actor::DecrementHealth(amount);

	// 킬 크레딧은 체력 감소 직전에 새겨진다(combat::ApplyDamage). 그래서 여기서
	// "누가 때렸는지" 를 알 수 있다.
	OnDamaged(GetLastAttackerActorId());
}

void Monster::OnDamaged(int attackerActorId)
{
	// 여기서 하는 일은 정수 비교 몇 번이 전부다. 표적 판정(맵 조회 + 진영 확인)은
	// 다음 사고 때 AcquireRetaliationTarget 이 한 번만 한다 — 피격은 잦고 사고는 드물다.
	if (targetActorId_ < 0 && attackerActorId >= 0 && attackerActorId != actorId_)
		pendingAttackerActorId_ = attackerActorId;

	// 깨워 두면 ECS 백엔드도 다음 틱에 사고한다(BT 둘은 원래 매 틱 사고한다).
	WakeAI();
}

bool Monster::AcquireRetaliationTarget()
{
	// 한 번 본 공격자는 승격에 실패하더라도 지운다. 남겨 두면 이미 사라진 공격자를
	// 사고할 때마다 다시 조회한다.
	const int attackerActorId = pendingAttackerActorId_;
	pendingAttackerActorId_ = -1;

	if (attackerActorId < 0 || targetActorId_ >= 0 || map_ == nullptr)
		return false;

	// 탐지가 잡을 수 있는 상대만 문다 — 광역기는 아군 몬스터도 때리는데(combat 에
	// 진영 구분이 없다), 그것까지 표적이 되면 몬스터끼리 싸우기 시작한다.
	auto attacker = map_->FindActor(attackerActorId);
	if (attacker == nullptr || !attacker->IsMonsterTarget())
		return false;

	targetActorId_ = attackerActorId;
	return true;
}

const monsterai::AttackPattern& Monster::GetAttackPattern() const
{
	return monsterai::PatternOf(aiProfile_, combatPhase_);
}

bool Monster::UpdateCombatPhase()
{
	const int maxHealth = GetMaxHealth();
	const float ratio = maxHealth > 0
		? static_cast<float>(health_) / static_cast<float>(maxHealth)
		: 0.0f;

	const uint8_t phase = monsterai::PhaseFor(aiProfile_, ratio);
	if (phase == combatPhase_)
		return false;

	combatPhase_ = phase;

	const monsterai::AttackPattern& pattern = GetAttackPattern();
	LOG.info("Monster {} ({}) 페이즈 {} 진입 — 스킬 {} / 사거리 {}",
		actorId_, monsterai::ProfileName(aiProfile_), phase, pattern.skillId, pattern.attackRange);
	return true;
}

void Monster::ApplyAIProfile(monsterai::AIProfile profile)
{
	aiProfile_ = profile;

	// 이 성향이 페이즈마다 쓰는 스킬을 전부 등록한다. 페이즈가 넘어간 뒤에 등록하면
	// 그 틱의 시전이 SkillNotFound 로 거부된다.
	const monsterai::ProfileTraits& traits = monsterai::TraitsOf(profile);
	for (uint8_t i = 0; i < traits.patternCount; ++i)
	{
		const int skillId = traits.patterns[i].skillId;
		skillSet_.AddSkill(skillId, SkillRegistry::Instance().Get(skillId));
	}

	// 지금 체력에 맞는 페이즈로 맞춘다(성향이 바뀌면 페이즈 번호의 의미도 달라진다).
	combatPhase_ = 0;
	UpdateCombatPhase();

	// 성향이 정해졌으니 이제 이 몬스터를 어느 백엔드로 돌릴지도 정해진다.
	const BTBackend desired = ResolveBTBackend(profile);
	const bool switching = brain_.IsValid() && desired != backend_;
	backend_ = desired;

	// 스폰(Init)에서 붙인 백엔드와 다르면 갈아탄다 — Create 가 이전 백엔드를 먼저
	// 정리하므로(ECS 는 컴포넌트를 반납한다) 두 백엔드가 한 몬스터를 겹쳐 도는 일은 없다.
	// 아직 스폰 전(생성자에서 온 호출)이라면 Init 이 이 값으로 붙인다.
	if (switching)
		brain_.Create(this, backend_);

	// 스폰(=AI 등록) 뒤에 종류가 새겨지므로, 이미 등록된 슬롯이면 성향을 밀어 넣는다.
	if (map_ != nullptr)
	{
		monsterai::MonsterAISystem* aiSystem = map_->GetAISystem();
		if (aiSystem != nullptr)
			aiSystem->ApplyProfile(this);
	}
}

void Monster::SetDataId(int dataId)
{
	dataId_ = dataId;

	const gamedata::MonsterData* data = ResourceLoader::Instance().GetMonsterData(dataId);
	if (data == nullptr)
	{
		LOG.warn("Monster: monster.json 에 id {} 가 없다. 기본 스탯으로 스폰한다.", dataId);
		return;
	}

	// 스폰 시점이므로 체력을 최대치로 채운다.
	SetCombatStats(data->hp, data->attack, data->defense, /*resetHealth=*/true);
	name_ = data->name.empty() ? name_ : data->name;

	// 성향도 종류가 정한다. 체력을 채운 뒤여야 보스의 시작 페이즈가 1페이즈로 잡힌다.
	ApplyAIProfile(monsterai::ParseProfile(data->ai));
}

int Monster::GetRewardExp() const
{
	const gamedata::MonsterData* data = ResourceLoader::Instance().GetMonsterData(dataId_);
	return data != nullptr ? data->exp : 0;
}

void Monster::NotifyKilledBy()
{
	if (deadNotified_)
		return;
	deadNotified_ = true;

	int killer_actor_id = GetLastAttackerActorId();
	if (killer_actor_id < 0)
		return; // 공격자 정보가 없으면 발행하지 않음

	// 액터 → 캐릭터 → 플레이어 변환과 파티 크레딧 분배는 party_credit 이 맡는다.
	// 여기서는 "이 액터가 여기서 죽었다"까지만 알린다.
	party_credit::PublishActorDead(map_, killer_actor_id, position_, actorId_, dataId_, GetRewardExp());
}


//---------------------------------------------------------------------------------------
// Lua function bindings
int lua_Attack(lua_State* L) {
	Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
	std::cout << "Executing Attack! " << monster->name_ << std::endl;
	lua_pushstring(L, "SUCCESS"); // 결과 반환
	return 1;
}

int lua_Defend(lua_State* L) {
	Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
	std::cout << "Executing Defend! " << monster->name_ << std::endl;
	lua_pushstring(L, "FAILURE");
	return 1;
}

int lua_Patrol(lua_State* L) {
	Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
	std::cout << "Executing Patrol! " << monster->name_ << std::endl;
	lua_pushstring(L, "SUCCESS");
	return 1;
}

int lua_LookAround(lua_State* L) {
	Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
	std::cout << "Executing LookAround! " << monster->name_ << std::endl;
	lua_pushstring(L, "SUCCESS");
	return 1;
}

void Monster::registerLuaFunctionAll()
{
	registerLuaFunction("Attack", lua_Attack);
	registerLuaFunction("Defend", lua_Defend);
	registerLuaFunction("Patrol", lua_Patrol);
	registerLuaFunction("LookAround", lua_LookAround);
}
//---------------------------------------------------------------------------------------
