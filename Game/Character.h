#pragma once
#include "Actor.h"

class BaseSkill;
class Vector3;

class Character : public Actor
{
private:
	long player_id_;
	std::unordered_map<int, BaseSkill*> skills_;

public:
	Character(World* world);
	virtual ~Character();

	virtual syncnet::GameObjectType type() { return syncnet::GameObjectType::GameObjectType_Character; }

	void set_player_id(long player_id)
	{
		player_id_ = player_id;
	}
	long player_id() { return player_id_; }
	void use_skill(const syncnet::UseSkill* msg);
	virtual void update(float deltaTime) override;
	virtual bool init(Vector3& pos) override;
};

