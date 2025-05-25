#include "PlayerController.h"
#include <iostream>
#include "World.h"
#include "Vector3.h"
#include "DetourNavMeshQuery.h"
#include "LogHelper.h"
#include "Player.h"
#include "Character.h"
#include "SendMessage.h"

void PlayerController::handle(const syncnet::GameMessage* msg)
{
	last_message_id_ = msg->id();

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

	auto game_object = world_->OnAddAgent(player_, msg->gameObjectType(), msg->pos());
	auto status = syncnet::StatusCode::StatusCode_Success;
	int agent_id = 0;
	if (!game_object) {
		LOG.error("OnAddAgent 실패: GameObject 생성에 실패했습니다.");
		status = syncnet::StatusCode::StatusCode_Failed;
	}
	else
	{ 
		agent_id = game_object->agent_id();
	}

	player_->send(
		syncnet::CreateAddAgent
		, syncnet::GameMessages::GameMessages_AddAgent
		, last_message_id_
		, status
		, msg->gameObjectType()
		, msg->pos()
		, agent_id
	);

}

void PlayerController::handle(const syncnet::RemoveAgent* msg)
{
	LOG.info("remove agent id :{}", msg->agentId());
	world_->OnRemoveAgent(msg->agentId());
}

void PlayerController::handle(const syncnet::SetMoveTarget* msg)
{
	if(!player_)
	{
		LOG.error("player is null");
		return;
	}

	if(!player_->character())
	{
		LOG.error("character is null");
		return;
	}

	if(player_->character()->is_input_locked())
	{
		LOG.debug("character is input locked");
		return;
	}

	LOG.debug("move target agent id :{}, pos:({},{},{})", player_->character()->agent_id(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
	world_->OnSetMoveTarget(player_->character()->agent_id(), msg->pos());
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
	LOG.info("Login id :{}, lastMessageId:{}", msg->userId()->c_str(), last_message_id_);

	player_->async_db_query();

	player_->send(
		syncnet::CreateLoginDirect
		, syncnet::GameMessages::GameMessages_Login
		, last_message_id_
		, syncnet::StatusCode::StatusCode_Success
		, msg->userId()->c_str()
		, msg->password()->c_str()
	);
}

void PlayerController::handle(const syncnet::UseSkill* msg)
{
	LOG.info("UseSkill id :{}, skillId :{}, targetId :{} pos:({},{},{})", msg->id(), msg->skillId(), msg->targetId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z());

	auto character = player_->character();
	if (!character)
	{
		LOG.error("character is null");
		return;
	}

	if (player_->character()->is_input_locked())
	{
		LOG.debug("character is input locked");
		return;
	}

	character->use_skill(msg);

	auto builder_ptr = std::make_shared<send_message>();
	auto send_msg = syncnet::CreateGameMessage(
		*builder_ptr,
		syncnet::GameMessages::GameMessages_UseSkill,
		syncnet::CreateUseSkill(
			*builder_ptr
			, msg->id()
			, msg->skillId()
			, msg->targetId()
			, msg->pos()
			, msg->dir()
			, msg->timestamp()
			, msg->duration()
		).Union()
	);
	builder_ptr->Finish(send_msg);
	world_->SendBroadcast(builder_ptr, player_);
}