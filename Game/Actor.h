#pragma once
#include "GameObject.h"
#include "SendMessage.h"

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

	virtual void set_position(float x, float y, float z) 
	{
		this->x = x;
		this->y = y;
		this->z = z;
		change_flag_ |= static_cast<long>(GameObjectChangeType::Position);
	};

	virtual void increment_health(int amount) 
	{
		health_ += amount;
		change_flag_ |= static_cast<long>(GameObjectChangeType::Health);
	}

	virtual void decrement_health(int amount) 
	{
		health_ -= amount;
		change_flag_ |= static_cast<long>(GameObjectChangeType::Health);
	}

	virtual bool is_changed_position(float x, float y, float z) 
	{ 
		return (this->x != x || this->y != y || this->z != z);
	}

	virtual bool is_changed() 
	{ 
		return change_flag_ != static_cast<long>(GameObjectChangeType::None)
			&& !is_input_locked_; // if input is locked, we don't want to change the position
	}
	virtual int health() { return health_; }

	virtual flatbuffers::Offset<syncnet::ActorInfo> get_actor_info(flatbuffers::FlatBufferBuilder& _fbb, GameObjectChangeType flag);

	bool is_input_locked() const { return is_input_locked_; }
	void set_input_locked(bool locked) { is_input_locked_ = locked; }

	float get_vecter2_x() const { return x; }
	float get_vecter2_y() const { return z; }



public:

	int gridX = -1;
	int gridY = -1;
	float speed = 0.0f;

private:
	float x;
	float y;
	float z;

	int health_ = 100;
};

