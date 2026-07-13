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
public:
	// 틱에 사용할 BT 프레임워크. 두 트리(bt_, tree_)는 항상 같이 생성되며 동일한 로직을
	// 수행하므로, 벤치마크에서 이 값만 바꿔 프레임워크 오버헤드를 비교할 수 있다.
	enum class BTBackend
	{
		BTCpp,    // behaviortree_cpp (GameData/Monster.xml) — 기본값.
		CodeBase, // ../BehaviorTree (인하우스, MonsterCodeBaseBT)
	};
	static BTBackend btBackend_;

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

