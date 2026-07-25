#include "Monster.h"
#include <random>
#include <functional>
#include "World.h"
#include "DetourCommon.h"
#include "MathHelper.h"
#include "behaviortree_cpp/bt_factory.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "MonsterBT.h"
#include "MonsterCodeBaseBT.h"
#include "../BehaviorTree/BehaviorTree.h" // bt_ 의 Tick/Release 호출에 완전한 타입 필요
#include "Common.h"
#include "Map.h"
#include "INavMovement.h"
#include "SkillRegistry.h"
#include "BTDebugManager.h"
#include "Player.h"
#include "Character.h"
#include "PlayerEventBrokerProxy.h"
#include "EventMessage.h"


extern std::_Binder<std::_Unforced, std::uniform_int_distribution<>&, std::default_random_engine&> dice;

// 기본 백엔드는 인하우스 BT 다. 두 트리는 같은 로직을 수행하지만 틱 비용이 크게 다르다
// (10,000마리 UpdateActors 기준 behaviortree_cpp 50ms vs 인하우스 12ms — Benchmark/PERFORMANCE.md).
// behaviortree_cpp 로 되돌리면 BT 디버그 뷰어(BTDebugManager)를 쓸 수 있다.
Monster::BTBackend Monster::btBackend_ = Monster::BTBackend::CodeBase;


Monster::Monster(Map* map)
	: Actor(map), bt_(nullptr), tree_(nullptr)
{
	gameObjectType_ = syncnet::GameObjectType::GameObjectType_Monster;
	targetAgentId_ = -1;

	// 근접 공격 스킬 등록. 데이터가 없으면 nullptr 등록이라 TryCast 가 SkillNotFound 로 거부한다.
	skillSet_.AddSkill(kMeleeSkillId, SkillRegistry::Instance().Get(kMeleeSkillId));
}

Monster::~Monster()
{
	if (bt_ != nullptr)
	{
		bt_->Release(); // 노드들을 해제한다(트리 객체는 delete 로).
		delete bt_;
		bt_ = nullptr;
	}

	if (tree_ != nullptr)
	{
		delete tree_;
		tree_ = nullptr;
	}
}

bool Monster::Init(Vector3& pos)
{
	float speed = 3.5f;
	int agent_id = map_->GetNavMap()->AddAgent(pos.pos(), speed);
	if (agent_id < 0)
	{
		LOG.error("OnAddAgent error in Map.addAgent()");
		return false;
	}

	if (map_->actorMap_.find(agent_id) != map_->actorMap_.end())
	{
		LOG.error("OnAddAgent error already exist in actorMap_");
		return false;
	}

	this->SetPosition(pos.x, pos.y, pos.z);
	actorId_ = agent_id;
	this->speed = speed;
	auto& entityManager = map_->systemManager_->GetEntityManager();
	entityManager.GetComponent<engine::StateComponent>(entityId_).ActorID = actorId_;


	if (map_ != nullptr) {
		// 스폰 위치는 네비메시에 스냅된 현재 위치를 기준으로 삼는다(patrol 의 중심점).
		dtVcopy(spawnPos_, map_->GetNavMap()->GetPos(actorId_));
	}

	name_ = "Monster:" + std::to_string(actorId_);

	// 실제로 틱할 트리 하나만 만든다. 예전에는 둘 다 만들어 두고 하나만 틱했는데,
	// 스폰마다 Monster.xml 을 읽어 파싱하는 비용(behaviortree_cpp)을 쓰지도 않는 트리에 지불했다.
	// 백엔드는 스폰 시점에 고정되므로, 바꾸려면 몬스터 생성 전에 btBackend_ 를 설정해야 한다.
	if (btBackend_ == BTBackend::CodeBase)
	{
		bt_ = MonsterCodeBaseBT::createTree(this);
	}
	else
	{
		tree_ = MonsterBT::createTree(this);
#if defined(ENABLE_BT_DEBUG)
		BTDebugManager::Instance().PublishTreeDefinition(this);
#endif
	}
	return true;
}

void Monster::Update(float dt)
{
	Actor::Update(dt);
	skillSet_.Update(this, dt); // 스킬 쿨다운/페이즈 진행
	//runBehaviorTree(this);
	if (bt_ != nullptr)
	{
		// 인하우스 BT(기본). 틱 비용이 낮은 대신 BT 디버그 뷰어를 지원하지 않는다.
		bt_->Tick();
	}
	else if (tree_ != nullptr)
	{
		BT_DEBUG_BEGIN_TICK(this);
		tree_->tickOnce();
		BT_DEBUG_END_TICK(this);
	}
}

int Monster::AttackRange()
{
	INavMovement* nav = map_->GetNavMap();
	const float* this_pos = nav->GetPos(GetActorId());
	const float* target_pos = nav->GetPos(targetAgentId_);

	if (ManhattanDistance(this_pos, target_pos) > 3)
		return -1;

	float hitPoint[3];
	if (nav->Raycast(GetActorId(), target_pos, hitPoint) == false)
	{
		return targetAgentId_;
	}
	return -1;
}

int Monster::Attack()
{
	map_->GetNavMap()->Stop(GetActorId());

	// 추격 대상 방향으로 근접 스킬을 시전한다 — 플레이어와 동일한 스킬 파이프라인.
	// 쿨다운 등으로 거부되면 이번 틱은 공격하지 않는다(BT 가 다음 틱에 재시도).
	auto target = map_->FindActor(targetAgentId_);
	if (target != nullptr)
	{
		CastContext ctx;
		ctx.skillId = kMeleeSkillId;
		ctx.targetActorId = targetAgentId_;
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

void Monster::NotifyKilledBy()
{
	if (deadNotified_)
		return;
	deadNotified_ = true;

	int killer_actor_id = GetLastAttackerActorId();
	if (killer_actor_id < 0)
		return; // 공격자 정보가 없으면 발행하지 않음

	// Actor 레이어는 플레이어를 모른다. 여기(게임 로직 레이어)에서 액터 → 캐릭터 → 플레이어로 변환한다.
	auto attacker = map_->FindActor(killer_actor_id);
	if (attacker == nullptr)
		return; // 공격자가 이미 떠났음

	// Possess 시점에 부착된 프록시를 통해 Player의 PlayerEventBroker로 발행한다.
	auto eventBroker = attacker->GetComponent<PlayerEventBrokerProxy>();
	if (eventBroker == nullptr)
		return;

	eventBroker->publish(EventActorDead{ killer_actor_id, actorId_ });
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
