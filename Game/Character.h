#pragma once
#include "Actor.h"

class Character : public Actor
{
private:
	long player_id_;

public:
	Character(int agent_id, World* world);
	virtual ~Character() {}
	virtual syncnet::GameObjectType GetType() { return syncnet::GameObjectType::GameObjectType_Character; }

	void set_player_id(long player_id)
	{
		player_id_ = player_id;
	}
	long player_id() { return player_id_; }
};

