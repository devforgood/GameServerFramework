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
	int lastMessageId_;

	friend class GameSession;

public:
	void handle(const syncnet::GameMessage* msg);
	void handle(const syncnet::AddAgent* msg);
	void handle(const syncnet::RemoveAgent* msg);
	void handle(const syncnet::SetMoveTarget* msg);
	void handle(const syncnet::Ping* msg);
	void handle(const syncnet::SetRaycast* msg);
	void handle(const syncnet::Login* msg);
	void handle(const syncnet::UseSkill* msg);
	void handle(const syncnet::EnterGate* msg);
	void handle(const syncnet::TreeDebugRequest* msg);
	void handle(const syncnet::Interact* msg);
	void handle(const syncnet::QuestAccept* msg);
	void handle(const syncnet::QuestComplete* msg);
	void handle(const syncnet::QuestAbandon* msg);
};

