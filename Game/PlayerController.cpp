#include "PlayerController.h"
#include <iostream>
#include "World.h"
#include "Vector3Converter.h"
#include "DetourNavMeshQuery.h"
#include "LogHelper.h"
#include "Player.h"
#include "Character.h"

void PlayerController::handle(const syncnet::GameMessage* msg)
{
	switch (msg->msg_type())
	{
	case syncnet::GameMessages::GameMessages_AddAgent:			handle(msg->msg_as_AddAgent()); break;
	case syncnet::GameMessages::GameMessages_RemoveAgent:		handle(msg->msg_as_RemoveAgent()); break;
	case syncnet::GameMessages::GameMessages_SetMoveTarget:		handle(msg->msg_as_SetMoveTarget()); break;
	case syncnet::GameMessages::GameMessages_Ping:				handle(msg->msg_as_Ping()); break;
	case syncnet::GameMessages::GameMessages_SetRaycast:		handle(msg->msg_as_SetRaycast()); break;
	case syncnet::GameMessages::GameMessages_Login:				handle(msg->msg_as_Login()); break;
	case syncnet::GameMessages::GameMessages_UseSkill:			handle(msg->msg_as_UseSkill()); break;
	}
}

void PlayerController::handle(const syncnet::AddAgent* msg)
{
	LOG.info("add agent pos:({},{},{})", msg->pos()->x(), msg->pos()->y(), msg->pos()->z());

	bool ret = world_->OnAddAgent(player_, msg->gameObjectType(), msg->pos());

	if(ret)
	{
		character_ = player_->character();
	}
}

void PlayerController::handle(const syncnet::RemoveAgent* msg)
{
	LOG.info("remove agent id :{}", msg->agentId());
	world_->OnRemoveAgent(msg->agentId());
}

void PlayerController::handle(const syncnet::SetMoveTarget* msg)
{
	LOG.info("move target agent id :{}, pos:({},{},{})", msg->agentId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
	world_->OnSetMoveTarget(msg->agentId(), msg->pos());
}

void PlayerController::handle(const syncnet::Ping* msg)
{
	//std::cout << "ping seq : " << msg->seq() << std::endl;
}


void PlayerController::handle(const syncnet::SetRaycast* msg)
{
	LOG.info("SetRaycast pos:({},{},{})", msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
	world_->OnSetRaycast(msg->pos());
}

void PlayerController::handle(const syncnet::Login* msg)
{
	LOG.info("Login id :{}", msg->userId()->c_str());

	player_->async_db_query();
}

void PlayerController::handle(const syncnet::UseSkill* msg)
{
	LOG.info("UseSkill id :{}, skillId :{}, targetId :{} pos:({},{},{})", msg->id(), msg->skillId(), msg->targetId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z());

	character_->use_skill(msg->skillId(), msg->pos());
}