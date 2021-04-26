#pragma once
#include "syncnet_generated.h"

class GameObject
{
private:
	int agent_id_;

public:
	GameObject(int agent_id) : agent_id_(agent_id)
	{

	}
	virtual ~GameObject() {}
	virtual void Update() {};

	virtual syncnet::GameObjectType GetType() { return syncnet::GameObjectType::GameObjectType_Monster; }

	int agent_id() { return agent_id_; }

};

