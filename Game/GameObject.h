#pragma once
#include "syncnet_generated.h"

class World;
class Vector3;

class GameObject
{


protected:
	World* world_;
	syncnet::AIState state_;

public:
	GameObject(World* world) : world_(world)
	{

	}
	virtual ~GameObject() = default; // 반드시 virtual 소멸자를 추가
	virtual void update() {};

	virtual syncnet::GameObjectType type() { return syncnet::GameObjectType::GameObjectType_Monster; }
	virtual int agent_id() { return -1; }
	virtual bool init(Vector3& pos) { return false; }

	World* world() { return world_; }
	syncnet::AIState state() { return state_; }

};

