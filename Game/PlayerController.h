#pragma once
#include "syncnet_generated.h"

class World;
class Player;
class Character;
class PlayerController
{
private:
	World* world_;
	std::shared_ptr<Player> player_;

	friend class game_session;

public:
	void handle(const syncnet::GameMessage* msg);
	void handle(const syncnet::AddAgent* msg);
	void handle(const syncnet::RemoveAgent* msg);
	void handle(const syncnet::SetMoveTarget* msg);
	void handle(const syncnet::Ping* msg);
	void handle(const syncnet::SetRaycast* msg);
	void handle(const syncnet::Login* msg);
	void handle(const syncnet::UseSkill* msg);
};

