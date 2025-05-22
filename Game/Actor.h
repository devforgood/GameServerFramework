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
	virtual void update(float dt) override;

	virtual int agent_id() override { return agent_id_; }

	virtual bool is_changed_position(float x, float y) 
	{ 
		return (this->x != x || this->y != y);
	}

	virtual bool is_changed_state() 
	{ 
		return last_state_ != state_;
	}


public:
	float x;
	float y;

	int gridX = -1;
	int gridY = -1;
	float speed = 0.0f;
};

