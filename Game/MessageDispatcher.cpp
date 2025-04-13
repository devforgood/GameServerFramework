#include "MessageDispatcher.h"
#include <iostream>
#include "World.h"
#include "Vector3Converter.h"
#include "DetourNavMeshQuery.h"
#include "LogHelper.h"
#include "Player.h"

void MessageDispatcher::dispatch(const syncnet::AddAgent* msg)
{
	LOG.info("add agent pos:({},{},{})", msg->pos()->x(), msg->pos()->y(), msg->pos()->z());

	world_->OnAddAgent(player_, msg->gameObjectType(), msg->pos());
}

void MessageDispatcher::dispatch(const syncnet::RemoveAgent* msg)
{
	LOG.info("remove agent id :{}", msg->agentId());
	world_->OnRemoveAgent(msg->agentId());
}

void MessageDispatcher::dispatch(const syncnet::SetMoveTarget* msg)
{
	LOG.info("move target agent id :{}, pos:({},{},{})", msg->agentId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
	world_->OnSetMoveTarget(msg->agentId(), msg->pos());
}

void MessageDispatcher::dispatch(const syncnet::GetAgents* msg)
{
}

void MessageDispatcher::dispatch(const syncnet::Ping* msg)
{
	//std::cout << "ping seq : " << msg->seq() << std::endl;
}


void MessageDispatcher::dispatch(const syncnet::SetRaycast* msg)
{
	LOG.info("SetRaycast pos:({},{},{})", msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
	world_->OnSetRaycast(msg->pos());
}

void MessageDispatcher::dispatch(const syncnet::Login* msg)
{
	LOG.info("Login id :{}", msg->userId()->c_str());

	player_->async_db_query();
}