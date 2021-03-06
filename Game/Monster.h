#pragma once
#include "GameObject.h"
#include "DetourNavMesh.h"

namespace BT
{
	class BehaviorTree;
}

class Action_Patrol;
class Monster : public GameObject
{
private:
	BT::BehaviorTree * bt_;
	float spawn_pos_[3];
	dtPolyRef spawn_ref_;

public:
	int target_agent_id_;

public:
	Monster(int agent_id, World * world);
	virtual ~Monster();
	virtual void Update();
	virtual syncnet::GameObjectType GetType() { return syncnet::GameObjectType::GameObjectType_Monster; }

	void SetState(syncnet::AIState state) { state_ = state; }
	int AttackRange();


	friend Action_Patrol;
};

