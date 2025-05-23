#pragma once
#include "GameObject.h"

class Actor : public GameObject
{
protected:
	int agent_id_ = -1;
	bool is_input_locked_ = false;

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

	virtual bool is_changed() 
	{ 
		return is_changed_
			&& !is_input_locked_; // if input is locked, we don't want to change the position
	}

	bool is_input_locked() const { return is_input_locked_; }
	void set_input_locked(bool locked) { is_input_locked_ = locked; }

public:
	float x;
	float y;

	int gridX = -1;
	int gridY = -1;
	float speed = 0.0f;
};

