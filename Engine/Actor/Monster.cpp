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
#include "Common.h"
#include "Map.h"
#include "BTDebugManager.h"
#include "Player.h"
#include "Character.h"
#include "PlayerEventBrokerProxy.h"
#include "EventMessage.h"


extern std::_Binder<std::_Unforced, std::uniform_int_distribution<>&, std::default_random_engine&> dice;


Monster::Monster(Map* map)
	: Actor(map), bt_(nullptr), tree_(nullptr)
{
	gameObjectType_ = syncnet::GameObjectType::GameObjectType_Monster;
	targetAgentId_ = -1;
}

Monster::~Monster()
{
	if (bt_ != nullptr)
	{
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
	int agent_id = map_->GetNavMap()->addAgent(pos.pos(), speed);
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


	bt_ = MonsterCodeBaseBT::createTree(this);


	if (map_ != nullptr) {
		auto agent = map_->GetNavMap()->getAgent(actorId_);
		if (agent != nullptr)
		{
			dtVcopy(spawnPos_, agent->npos);
			spawnRef_ = agent->corridor.getPath()[0];
		}
	}

	name_ = "Monster:" + std::to_string(actorId_);



	tree_ = MonsterBT::createTree(this);
#if defined(ENABLE_BT_DEBUG)
	BTDebugManager::Instance().PublishTreeDefinition(this);
#endif
	return true;
}

void Monster::Update(float dt)
{
	Actor::Update(dt);
	//bt_->Tick();
	//runBehaviorTree(this);
	BT_DEBUG_BEGIN_TICK(this);
	tree_->tickOnce();
	BT_DEBUG_END_TICK(this);
}

int Monster::AttackRange()
{
	const dtCrowdAgent* this_agent = map_->GetNavMap()->crowd()->getAgent(GetActorId());
	const dtCrowdAgent* agent = map_->GetNavMap()->crowd()->getAgent(targetAgentId_);

	if (ManhattanDistance(this_agent->npos, agent->npos) > 3)
		return -1;

	float hitPoint[3];
	if (map_->GetNavMap()->raycast(GetActorId(), agent->npos, hitPoint) == false)
	{
		return targetAgentId_;
	}
	return -1;
}

int Monster::Attack()
{
	dtCrowdAgent* this_agent = map_->GetNavMap()->crowd()->getEditableAgent(GetActorId());
	this_agent->desiredSpeed = 0.0f;
	return 0;
}
int Monster::Resume()
{
	dtCrowdAgent* this_agent = map_->GetNavMap()->crowd()->getEditableAgent(GetActorId());
	this_agent->desiredSpeed = this->speed;
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
