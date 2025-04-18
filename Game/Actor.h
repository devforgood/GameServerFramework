#pragma once
#include "GameObject.h"

class Actor : public GameObject
{
public:
	Actor(int agent_id, World* world) : GameObject(agent_id, world)
	{
	}
	virtual ~Actor() {}
	virtual void Update() override;



public:
	float x;
	float y;

	int gridX = -1;
	int gridY = -1;
};

