#include "PlayerController.h"
#include <iostream>
#include <chrono>
#include "World.h"
#include "Vector3.h"
#include "DetourNavMeshQuery.h"
#include "LogHelper.h"
#include "Player.h"
#include "Character.h"
#include "SendMessage.h"
#include "Map.h"
#include "PlayerRepository.h"
#include "Server.h"

namespace
{
	// 게이트 연속 이동 방지 쿨타임(ms). 도착 직후 재진입/도배 요청을 서버에서 차단한다.
	constexpr uint64_t kGateCooldownMs = 1000;

	uint64_t NowMs()
	{
		return std::chrono::duration_cast<std::chrono::milliseconds>(
			std::chrono::system_clock::now().time_since_epoch()).count();
	}
}

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
	const std::string userId = msg->userId() != nullptr ? msg->userId()->c_str() : "";
	const char* password = msg->password() != nullptr ? msg->password()->c_str() : "";
	const std::string reconnectToken = msg->uuid() != nullptr ? msg->uuid()->c_str() : "";
	LOG.info("Login id :{}, uuid:'{}', lastMessageId:{}", userId, reconnectToken, lastMessageId_);

	// 재접속 핸드오버: 클라가 되돌려 보낸 uuid(재접속 토큰)로 유예 대기 중인 기존 플레이어를
	// 찾으면, 이 세션을 그 플레이어에 재바인딩하고 기존 캐릭터(맵/위치/agentId)를 넘겨받는다.
	// uuid 는 플레이어 고유값이라 다른 클라가 같은 userId 로 접속해도 가로챌 수 없다.
	auto oldPlayer = world_->TryReconnect(reconnectToken);
	if (oldPlayer != nullptr)
	{
		auto provisional = player_;                  // 이번 접속에서 새로 만든 임시 플레이어
		auto session = provisional->GetSession();
		if (session != nullptr)
		{
			// 세션의 player_/컨트롤러 player_ 를 기존 플레이어로 교체(this->player_ 도 갱신됨).
			session->SetPlayer(oldPlayer);
		}
		else
		{
			LOG.error("Login reconnect error: provisional session expired");
		}

		// 임시 플레이어를 월드에서 제거(캐릭터 없음 → 기본 맵/월드 브로드캐스트 목록에서 제거).
		world_->leave(provisional);

		oldPlayer->SetUserId(userId);

		auto character = oldPlayer->GetCharacter();
		int mapId = 0;
		syncnet::Vec3 pos(0, 0, 0);
		int agentId = 0;
		if (character != nullptr && character->GetMap() != nullptr)
		{
			mapId = character->GetMap()->GetMapId();
			const Vector3& p = character->GetPosition();
			pos = syncnet::Vec3(p.convert_x(), p.convert_y(), p.convert_z());
			agentId = character->GetActorId();
		}

		// 응답의 agentId 가 0 이 아니면 클라는 재접속으로 인식해 AddAgent 를 생략하고
		// 이 agentId 를 채택한다. 이어지는 SendStateTo 로 기존 캐릭터가 재생성된다.
		// uuid 는 기존 값 그대로(클라가 이미 보유) 다시 실어 보낸다.
		const std::string uuid = oldPlayer->GetUuid();
		oldPlayer->Send(
			syncnet::CreateLoginDirect
			, syncnet::GameMessages::GameMessages_Login
			, lastMessageId_
			, syncnet::StatusCode::StatusCode_Success
			, userId.c_str()
			, password
			, mapId
			, &pos
			, agentId
			, uuid.c_str()
		);

		if (character != nullptr && character->GetMap() != nullptr)
			character->GetMap()->SendStateTo(oldPlayer);
		return;
	}

	// 신규 로그인.
	player_->SetUserId(userId);
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

	// 최초 로그인 응답에 플레이어 uuid(재접속 토큰)를 실어 보낸다. 클라는 이를 저장했다가
	// 재접속 시 되돌려 보낸다.
	const std::string uuid = player_->GetUuid();
	player_->Send(
		syncnet::CreateLoginDirect
		, syncnet::GameMessages::GameMessages_Login
		, lastMessageId_
		, syncnet::StatusCode::StatusCode_Success
		, userId.c_str()
		, password
		, mapId
		, &spawnPos
		, 0 /* agentId: 신규 로그인은 0 */
		, uuid.c_str()
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
	const uint64_t nowMs = NowMs();

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
	else if (player_->IsGateOnCooldown(nowMs, kGateCooldownMs))
	{
		LOG.debug("EnterGate ignored: gate cooldown active (player {})", player_->GetPlayerId());
		status = syncnet::StatusCode::StatusCode_Failed;
	}
	else if (!world_->ChangeMap(player_, msg->mapId(), msg->gateId(), outPos, outAgentId))
	{
		status = syncnet::StatusCode::StatusCode_Failed;
	}
	else
	{
		// 이동에 성공한 경우에만 쿨타임을 갱신한다(실패한 요청은 쿨타임을 소모하지 않음).
		player_->MarkGateMoved(nowMs);
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