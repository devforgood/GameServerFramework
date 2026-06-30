#include "PlayerController.h"
#include <iostream>
#include "World.h"
#include "Vector3.h"
#include "DetourNavMeshQuery.h"
#include "LogHelper.h"
#include "Player.h"
#include "Character.h"
#include "SendMessage.h"
#include "Map.h"
#include "PlayerRepository.h"

void PlayerController::handle(const syncnet::GameMessage* msg)
{
	lastMessageId_ = msg->id();

	switch (msg->msg_type())
	{
	case syncnet::GameMessages::GameMessages_AddAgent:			handle(msg->msg_as_AddAgent()); break;
	case syncnet::GameMessages::GameMessages_RemoveAgent:		handle(msg->msg_as_RemoveAgent()); break;
	case syncnet::GameMessages::GameMessages_SetMoveTarget:		handle(msg->msg_as_SetMoveTarget()); break;
	case syncnet::GameMessages::GameMessages_Ping:				handle(msg->msg_as_Ping()); break;
	case syncnet::GameMessages::GameMessages_SetRaycast:		handle(msg->msg_as_SetRaycast()); break;
	case syncnet::GameMessages::GameMessages_Login:				handle(msg->msg_as_Login()); break;
	case syncnet::GameMessages::GameMessages_UseSkill:			handle(msg->msg_as_UseSkill()); break;
	case syncnet::GameMessages::GameMessages_EnterGate:			handle(msg->msg_as_EnterGate()); break;
	}
}

void PlayerController::handle(const syncnet::AddAgent* msg)
{
	LOG.info("add agent pos:({},{},{})", msg->pos()->x(), msg->pos()->y(), msg->pos()->z());

	auto actor = world_->OnAddAgent(player_, msg->gameObjectType(), msg->pos());
	auto status = syncnet::StatusCode::StatusCode_Success;
	int agent_id = 0;
	if (!actor) {
		LOG.error("OnAddAgent 실패: Actor 생성에 실패했습니다.");
		status = syncnet::StatusCode::StatusCode_Failed;
	}
	else
	{ 
		agent_id = actor->GetActorId();
	}

	player_->Send(
		syncnet::CreateAddAgent
		, syncnet::GameMessages::GameMessages_AddAgent
		, lastMessageId_
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

	if(!player_->GetCharacter())
	{
		LOG.error("character is null");
		return;
	}

	if(player_->GetCharacter()->IsInputLocked())
	{
		LOG.debug("character is input locked");
		return;
	}

	LOG.debug("move target agent id :{}, pos:({},{},{})", player_->GetCharacter()->GetActorId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
	// 캐릭터가 현재 속한 맵으로 라우팅한다(게이트 이동 후 비기본 맵에 있을 수 있음).
	player_->GetCharacter()->GetMap()->OnSetMoveTarget(player_->GetCharacter()->GetActorId(), msg->pos());
}

void PlayerController::handle(const syncnet::Ping* msg)
{
	//std::cout << "ping seq : " << msg->seq() << std::endl;
	player_->Send(
		syncnet::CreatePing
		, syncnet::GameMessages_Ping
		, lastMessageId_
		, syncnet::StatusCode::StatusCode_Success
		, msg->seq()
	);
}


void PlayerController::handle(const syncnet::SetRaycast* msg)
{
	LOG.info("SetRaycast pos:({},{},{})", msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
	world_->OnSetRaycast(msg->pos());
}

void PlayerController::handle(const syncnet::Login* msg)
{
	LOG.info("Login id :{}, lastMessageId:{}", msg->userId()->c_str(), lastMessageId_);

	PlayerRepository::AsyncLoad(player_);

	// 최초 로그인 시 클라가 어느 맵(씬)을 로드하고 어디에 스폰할지 알 수 있도록
	// 현재(기본) 맵 id 와 스폰 위치를 함께 반환한다.
	int mapId = 0;
	syncnet::Vec3 spawnPos(0, 0, 0);
	Map* primaryMap = world_->GetPrimaryMap();
	if (primaryMap != nullptr)
	{
		mapId = primaryMap->GetMapId();
		spawnPos = primaryMap->GetPlayerSpawnPos();
	}

	player_->Send(
		syncnet::CreateLoginDirect
		, syncnet::GameMessages::GameMessages_Login
		, lastMessageId_
		, syncnet::StatusCode::StatusCode_Success
		, msg->userId()->c_str()
		, msg->password()->c_str()
		, mapId
		, &spawnPos
	);
}

void PlayerController::handle(const syncnet::UseSkill* msg)
{
	LOG.info("UseSkill id :{}, skillId :{}, targetId :{} pos:({},{},{})", msg->id(), msg->skillId(), msg->targetId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z());

	auto character = player_->GetCharacter();
	if (!character)
	{
		LOG.error("character is null");
		return;
	}

	if (player_->GetCharacter()->IsInputLocked())
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
	character->GetMap()->SendBroadcast(builder_ptr, player_);
}

void PlayerController::handle(const syncnet::EnterGate* msg)
{
	LOG.info("EnterGate mapId :{}, gateId :{}", msg->mapId(), msg->gateId());

	syncnet::Vec3 outPos(0, 0, 0);
	int outAgentId = 0;
	auto status = syncnet::StatusCode::StatusCode_Success;

	if (!player_ || !player_->GetCharacter())
	{
		LOG.error("EnterGate error: player or character is null");
		status = syncnet::StatusCode::StatusCode_Failed;
	}
	else if (player_->GetCharacter()->IsInputLocked())
	{
		LOG.debug("EnterGate ignored: character is input locked");
		status = syncnet::StatusCode::StatusCode_Failed;
	}
	else if (!world_->ChangeMap(player_, msg->mapId(), msg->gateId(), outPos, outAgentId))
	{
		status = syncnet::StatusCode::StatusCode_Failed;
	}

	// 응답을 먼저 보낸다. 클라는 이 응답으로 맵 프리팹을 교체하고 기존 액터를 정리한다.
	player_->Send(
		syncnet::CreateEnterGate
		, syncnet::GameMessages::GameMessages_EnterGate
		, lastMessageId_
		, status
		, msg->mapId()
		, msg->gateId()
		, &outPos
		, outAgentId
	);

	// 응답 이후에 새 맵의 액터 상태를 동기화한다(클라가 맵 교체를 마친 뒤 받도록).
	if (status == syncnet::StatusCode::StatusCode_Success)
	{
		Map* destMap = world_->FindMap(msg->mapId());
		if (destMap != nullptr)
			destMap->SendStateTo(player_);
	}
}