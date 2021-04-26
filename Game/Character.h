#pragma once
#include "GameObject.h"

class Character : public GameObject
{
public:
	Character(int agent_id, World* world);
	virtual ~Character() {}
	virtual syncnet::GameObjectType GetType() { return syncnet::GameObjectType::GameObjectType_Character; }

};

