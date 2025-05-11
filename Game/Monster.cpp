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
#include "behaviortree_cpp/bt_factory.h"


extern std::_Binder<std::_Unforced, std::uniform_int_distribution<>&, std::default_random_engine&> dice;


Monster::Monster(World* world)
	: Actor(world), bt_(nullptr)
{

}

Monster::~Monster()
{
	if (bt_ != nullptr)
		delete bt_;
}
bool Monster::init(Vector3& pos)
{
	float speed = 3.5f;
	int agent_id = world_->map()->addAgent(pos.pos(), speed);
	if (agent_id < 0)
	{
		LOG.error("OnAddAgent error in Map.addAgent()");
		return false;
	}

	if (world_->game_object_map_.find(agent_id) != world_->game_object_map_.end())
	{
		LOG.error("OnAddAgent error already exist in monsters_map_");
		return false;
	}

	agent_id_ = agent_id;
	this->x = pos.x();
	this->y = pos.z();
	this->speed = speed;


	bt_ = MonsterCodeBaseBT::createTree(this);


	if (world_ != nullptr) {
		auto agent = world_->map()->getAgent(agent_id_);
		if (agent != nullptr)
		{
			dtVcopy(spawn_pos_, agent->npos);
			spawn_ref_ = agent->corridor.getPath()[0];
		}
	}

	name_ = "Monster:" + std::to_string(agent_id_);



	tree_ = MonsterBT::createTree(this);
	return true;
}

void Monster::update()
{
	Actor::update();
	//bt_->Tick();
	//runBehaviorTree(this);
	tree_->tickOnce();
}

int Monster::AttackRange()
{
	const dtCrowdAgent* this_agent = world_->map()->crowd()->getAgent(agent_id());
	const dtCrowdAgent* agent = world_->map()->crowd()->getAgent(target_agent_id_);

	if (ManhattanDistance(this_agent->npos, agent->npos) > 3)
		return -1;

	float hitPoint[3];
	if (world_->map()->raycast(agent_id(), agent->npos, hitPoint) == false)
	{
		return target_agent_id_;
	}
	return -1;
}

int Monster::Attack()
{
	dtCrowdAgent* this_agent = world_->map()->crowd()->getEditableAgent(agent_id());
	this_agent->desiredSpeed = 0.0f;
	return 0;
}
int Monster::Resume()
{
	dtCrowdAgent* this_agent = world_->map()->crowd()->getEditableAgent(agent_id());
	this_agent->desiredSpeed = this->speed;
	return 0;
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
