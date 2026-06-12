#pragma once
#include "Actor.h"

class Skill;
class Vector3;

class Character : public Actor
{
private:
	long playerId_;
	std::unordered_map<int, Skill*> skills_;

public:
	Character(Map* map);
	virtual ~Character();



	void set_player_id(long player_id)
	{
		playerId_ = player_id;
	}
	long GetPlayerId() const override { return playerId_; }
	void use_skill(const syncnet::UseSkill* msg);
	virtual void Update(float deltaTime) override;
	virtual bool PreCreate(std::shared_ptr<Player> player) override;
	virtual bool Init(Vector3& pos) override;
	virtual bool PostCreate(std::shared_ptr<Player> player, std::shared_ptr<GameObject> game_object) override;
};

