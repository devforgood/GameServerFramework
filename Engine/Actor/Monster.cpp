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
// behaviortree_cpp 로 되돌리면 BT 디버그 뷰어(BTDebugManager)를 쓸 수 있다.
Monster::BTBackend Monster::btBackend_ = Monster::BTBackend::Ecs;


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
	// 백엔드는 스폰 시점에 고정되므로, 바꾸려면 몬스터 생성 전에 btBackend_ 를 설정해야 한다.
	brain_.Create(this);
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
	// 먼저 공격하지 않는 성향이 교전에 들어가는 유일한 경로다. 백엔드와 무관하게
	// 여기서 대상을 잡으므로, 세 백엔드가 같은 반격 규칙을 쓴다.
	//
	// 탐지가 잡을 수 있는 상대만 문다 — 광역기는 아군 몬스터도 때리는데(combat 에
	// 진영 구분이 없다), 그것까지 표적이 되면 몬스터끼리 싸우기 시작한다.
	if (targetActorId_ < 0 && attackerActorId >= 0 && attackerActorId != actorId_ && map_ != nullptr)
	{
		auto attacker = map_->FindActor(attackerActorId);
		if (attacker != nullptr && attacker->IsMonsterTarget())
			targetActorId_ = attackerActorId;
	}

	if (map_ == nullptr)
		return;

	monsterai::MonsterAISystem* aiSystem = map_->GetAISystem();
	if (aiSystem != nullptr)
		aiSystem->OnDamaged(this); // 깨우고, 방금 잡은 대상을 슬롯에 반영한다.
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
