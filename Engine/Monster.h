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
	bool dead_notified_ = false; // 사망 이벤트 중복 발행 방지

public:
	int target_agent_id_;
	std::string name_;

public:

	Monster(Map* map);
	virtual ~Monster();
	virtual void update(float dt);
	virtual bool init(Vector3& pos) override;


	int AttackRange();
	int Attack();
	int Resume();

	// 사망 시 킬한 플레이어에게 EventActorDead 이벤트를 발행한다(최초 1회).
	void NotifyKilledBy();

	static void registerLuaFunctionAll();

	friend Action_Patrol;
	friend ActionPatrol;
};

