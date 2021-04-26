#pragma once
#include "GameObject.h"

namespace BT
{
	class BehaviorTree;
}

class Monster : public GameObject
{
private:
	BT::BehaviorTree * bt_;

public:
	int target_agent_id_;

public:
	Monster(int agent_id, World * world);
	virtual ~Monster();
	virtual void Update();
	virtual syncnet::GameObjectType GetType() { return syncnet::GameObjectType::GameObjectType_Monster; }

	void SetState(syncnet::AIState state) { state_ = state; }

};

