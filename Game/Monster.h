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
	Monster(int agent_id);
	virtual ~Monster();
	virtual void Update();
	virtual syncnet::GameObjectType GetType() { return syncnet::GameObjectType::GameObjectType_Monster; }

};

