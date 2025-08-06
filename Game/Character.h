#pragma once
#include "Actor.h"

class Skill;
class Vector3;

class Character : public Actor
{
private:
	long player_id_;
	std::unordered_map<int, Skill*> skills_;

public:
	Character(Map* map);
	virtual ~Character();



	void set_player_id(long player_id)
	{
		player_id_ = player_id;
	}
	long player_id() { return player_id_; }
	void use_skill(const syncnet::UseSkill* msg);
	virtual void update(float deltaTime) override;
	virtual bool init(Vector3& pos) override;
};

