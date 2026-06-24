#pragma once
#include "Actor.h"
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
	float spawnPos_[3];
	BT::Tree* tree_;
	bool deadNotified_ = false; // 사망 이벤트 중복 발행 방지

public:
	int targetAgentId_;
	std::string name_;

public:

	Monster(Map* map);
	virtual ~Monster();
	virtual void Update(float dt);
	virtual bool Init(Vector3& pos) override;


	int AttackRange();
	int Attack();
	int Resume();

	// 사망 시 킬한 플레이어에게 EventActorDead 이벤트를 발행한다(최초 1회).
	void NotifyKilledBy();

	static void registerLuaFunctionAll();

	friend Action_Patrol;
	friend ActionPatrol;
};

