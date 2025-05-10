#pragma once
#include "GameObject.h"

class Actor : public GameObject
{
protected:
	int agent_id_ = -1;

public:
	Actor(World* world) : GameObject(world)
	{
	}
	virtual ~Actor() {}
	virtual void update() override;

	virtual int agent_id() override { return agent_id_; }

public:
	float x;
	float y;

	int gridX = -1;
	int gridY = -1;
	float speed = 0.0f;
};

