#pragma once
#include "Actor.h"
#include "DetourNavMesh.h"
#include "LuaObject.h"

#include <string>

namespace BT
{
	class BehaviorTree;
	class Tree;
}

class Action_Patrol;
class ActionPatrol;
class Vector3;

class Monster : public Actor, public LuaObject<Monster>
{
private:
	BT::BehaviorTree * bt_;
	float spawn_pos_[3];
	dtPolyRef spawn_ref_;
	BT::Tree* tree_;

public:
	int target_agent_id_;
	std::string name_;

public:

	Monster(World * world);
	virtual ~Monster();
	virtual void update(float dt);
	virtual syncnet::GameObjectType type() { return syncnet::GameObjectType::GameObjectType_Monster; }
	virtual bool init(Vector3& pos) override;


	int AttackRange();
	int Attack();
	int Resume();

	static void registerLuaFunctionAll();

	friend Action_Patrol;
	friend ActionPatrol;
};

