#pragma once
#include "syncnet_generated.h"

class World;
class Player;
class MessageDispatcher
{
private:
	World* world_;
	Player* player_;

	friend class game_session;

public:
	void dispatch(const syncnet::AddAgent* msg);
	void dispatch(const syncnet::RemoveAgent* msg);
	void dispatch(const syncnet::SetMoveTarget* msg);
	void dispatch(const syncnet::GetAgents* msg);
	void dispatch(const syncnet::Ping* msg);
	void dispatch(const syncnet::SetRaycast* msg);
	void dispatch(const syncnet::Login* msg);
};

