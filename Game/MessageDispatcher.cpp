#include "MessageDispatcher.h"
#include <iostream>
#include "World.h"
#include "Vector3Converter.h"
#include "DetourNavMeshQuery.h"
#include "LogHelper.h"
#include "Player.h"

void MessageDispatcher::dispatch(const syncnet::GameMessage* msg)
{
	switch (msg->msg_type())
	{
	case syncnet::GameMessages::GameMessages_AddAgent:			dispatch(msg->msg_as_AddAgent()); break;
	case syncnet::GameMessages::GameMessages_RemoveAgent:		dispatch(msg->msg_as_RemoveAgent()); break;
	case syncnet::GameMessages::GameMessages_SetMoveTarget:		dispatch(msg->msg_as_SetMoveTarget()); break;
	case syncnet::GameMessages::GameMessages_Ping:				dispatch(msg->msg_as_Ping()); break;
	case syncnet::GameMessages::GameMessages_SetRaycast:		dispatch(msg->msg_as_SetRaycast()); break;
	case syncnet::GameMessages::GameMessages_Login:				dispatch(msg->msg_as_Login()); break;
	case syncnet::GameMessages::GameMessages_UseSkill:			dispatch(msg->msg_as_UseSkill()); break;
	}
}

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

void MessageDispatcher::dispatch(const syncnet::UseSkill* msg)
{
	LOG.info("UseSkill id :{}, skillId :{}, targetId :{} pos:({},{},{})", msg->id(), msg->skillId(), msg->targetId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
}