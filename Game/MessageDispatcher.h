#pragma once
#include "syncnet_generated.h"

class World;
class MessageDispatcher
{
public:
	void dispatch(const syncnet::AddAgent* msg);
	void dispatch(const syncnet::RemoveAgent* msg);
	void dispatch(const syncnet::SetMoveTarget* msg);
	void dispatch(const syncnet::GetAgents* msg);
	void dispatch(const syncnet::Ping* msg);
	void dispatch(const syncnet::SetRaycast* msg);

	World * world_;
};

