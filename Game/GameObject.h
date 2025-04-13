#pragma once
#include "syncnet_generated.h"

class World;
class GameObject
{
private:
	int agent_id_;

protected:
	World* world_;
	syncnet::AIState state_;

public:
	GameObject(int agent_id, World* world) : agent_id_(agent_id), world_(world)
	{

	}
	virtual ~GameObject() = default; // 반드시 virtual 소멸자를 추가
	virtual void Update() {};

	virtual syncnet::GameObjectType GetType() { return syncnet::GameObjectType::GameObjectType_Monster; }

	int agent_id() { return agent_id_; }
	World* world() { return world_; }
	syncnet::AIState state() { return state_; }
};

