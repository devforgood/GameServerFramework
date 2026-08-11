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
#include "Common.h" // gamedata::Map/MapGate 전체 정의
#include "PlayerLevel.h"
#include "PlayerEventBroker.h"
#include "PlayerQuest.h"
#include "BTDebugManager.h"

namespace
{
	// 게이트 연속 이동 방지 쿨타임(ms). 도착 직후 재진입/도배 요청을 서버에서 차단한다.
	constexpr uint64_t kGateCooldownMs = 1000;

	// 게이트 이용 허용 최대 거리(서버 좌표, xz 평면). 클라 트리거 박스 크기 + 이동 동기화 지연을 감안한 여유값.
	constexpr float kGateEnterMaxDistance = 5.0f;

	// NPC/오브젝트 상호작용 허용 거리 기본값. NPC 데이터에 interact_range 가 있으면 그 값이 이긴다.
	constexpr float kInteractMaxDistance = 3.0f;

	// 퀘스트 수락 실패 사유를 클라가 구분할 수 있는 선에서 상태 코드로 옮긴다.
	// 세부 사유는 서버 로그에 남는다(프로토콜에 사유 코드를 늘리기 전까지).
	syncnet::StatusCode ToStatusCode(QuestAcceptResult result)
	{
		switch (result)
		{
		case QuestAcceptResult::Ok:               return syncnet::StatusCode::StatusCode_Success;
		case QuestAcceptResult::NotFound:         return syncnet::StatusCode::StatusCode_NotFound;
		case QuestAcceptResult::AlreadyActive:
		case QuestAcceptResult::AlreadyCompleted: return syncnet::StatusCode::StatusCode_AlreadyExists;
		default:                                  return syncnet::StatusCode::StatusCode_Failed;
		}
	}

	uint64_t NowMs()
	{
		return std::chrono::duration_cast<std::chrono::milliseconds>(
			std::chrono::system_clock::now().time_since_epoch()).count();
	}

	// 클라가 밟았다고 주장하는 게이트를 찾는다. 게이트 id 는 전역 유일이라 바로 조회되지만,
	// 그 게이트가 정말 이 플레이어가 있는 맵의 것인지(=parent)는 서버가 확인해야 한다.
	// 확인하지 않으면 아무 게이트 id 나 보내 임의의 맵으로 순간이동할 수 있다.
	const gamedata::MapGate* FindGateInMap(Map* map, int gateId)
	{
		if (map == nullptr || map->GetMapData() == nullptr)
			return nullptr;

		const gamedata::MapGate* gate = ResourceLoader::Instance().GetMapGate(gateId);
		if (gate == nullptr || gate->parent != map->GetMapData())
			return nullptr;

		return gate;
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
	case syncnet::GameMessages::GameMessages_TreeDebugRequest:	handle(msg->msg_as_TreeDebugRequest()); break;
	case syncnet::GameMessages::GameMessages_Interact:			handle(msg->msg_as_Interact()); break;
	case syncnet::GameMessages::GameMessages_QuestAccept:		handle(msg->msg_as_QuestAccept()); break;
	case syncnet::GameMessages::GameMessages_QuestComplete:		handle(msg->msg_as_QuestComplete()); break;
	case syncnet::GameMessages::GameMessages_QuestAbandon:		handle(msg->msg_as_QuestAbandon()); break;
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
	// 맵 id 와 스폰 위치를 함께 반환한다. 기본은 기본 맵의 player_spawn 마커,
	// 같은 userId 로 로그아웃한 이력이 있으면 그 마지막 위치(맵 포함)를 우선한다.
	int mapId = 0;
	syncnet::Vec3 spawnPos(0, 0, 0);
	Map* primaryMap = world_->GetPrimaryMap();
	if (primaryMap != nullptr)
	{
		mapId = primaryMap->GetMapId();
		spawnPos = primaryMap->GetPlayerSpawnPos();
	}

	int lastMapId = 0;
	syncnet::Vec3 lastPos(0, 0, 0);
	if (world_->GetLastLocation(userId, lastMapId, lastPos) && world_->FindMap(lastMapId) != nullptr)
	{
		mapId = lastMapId;
		spawnPos = lastPos;
		LOG.info("Login: userId '{}' 이전 위치로 스폰. mapId {}, pos({},{},{})",
			userId, mapId, spawnPos.x(), spawnPos.y(), spawnPos.z());
	}

	// 클라의 AddAgent(Character) 를 이 맵으로 라우팅하기 위해 기억해 둔다.
	player_->SetSpawnMapId(mapId);

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

	// 서버 권위: 검증(쿨다운/페이즈/입력잠금)을 통과해 실제로 시전된 경우에만
	// 다른 클라이언트에 브로드캐스트한다.
	CastResult result = character->IsInputLocked() ? CastResult::InputLocked : character->use_skill(msg);
	if (result != CastResult::Success)
	{
		LOG.debug("UseSkill rejected: skillId {}, reason code {}", msg->skillId(), static_cast<int>(result));

		// 거부는 캐스터에게 반드시 되돌려준다. 클라는 전송 직후 연출을 낙관적으로 재생하므로,
		// 응답이 없으면 "이펙트는 나오는데 캐릭터는 그대로"인 헛연출이 남는다(차지에서 특히 눈에 띈다).
		// 클라는 이 응답으로 연출을 취소하고 로컬 쿨다운 예측을 서버 기준으로 되돌린다.
		player_->Send(
			syncnet::CreateUseSkill
			, syncnet::GameMessages::GameMessages_UseSkill
			, 0 /* GameMessage id: 요청/응답 짝이 아니라 알림이므로 0 */
			, result == CastResult::SkillNotFound
				? syncnet::StatusCode::StatusCode_NotFound
				: syncnet::StatusCode::StatusCode_Failed
			, msg->id()
			, msg->skillId()
			, msg->targetId()
			, msg->pos()
			, msg->dir()
			, msg->timestamp()
			, msg->duration()
		);
		return;
	}

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

	// 서버가 실제로 시전을 인정한 뒤에만 알린다(스킬 사용 퀘스트가 이 이벤트를 센다).
	if (auto* broker = player_->GetComponent<PlayerEventBroker>())
	{
		broker->publish(EventSkillUsed{
			static_cast<int>(player_->GetPlayerId()), msg->skillId() });
	}
}

void PlayerController::handle(const syncnet::EnterGate* msg)
{
	// 클라는 자기가 밟은 게이트 id 만 보낸다. 목적지는 그 게이트의 target_id 가 정하므로
	// 클라가 목적지를 고를 수 없다(요청의 mapId 는 쓰지 않고, 응답에 도착한 맵을 담아 준다).
	LOG.info("EnterGate gateId :{}", msg->gateId());

	syncnet::Vec3 outPos(0, 0, 0);
	int outAgentId = 0;
	int outMapId = 0;
	int outTargetId = 0; // 도착 지점 마커(게이트 또는 player_spawn) id
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
	else
	{
		auto& character = player_->GetCharacter();

		// 보낸 게이트가 실제로 이 플레이어가 있는 맵의 게이트여야 한다(임의 맵 순간이동 차단).
		const gamedata::MapGate* srcGate = FindGateInMap(character->GetMap(), msg->gateId());

		// 게이트 위치는 Map.json(클라 좌표계) 기준이므로 서버 좌표계로 변환해 거리 비교한다.
		float distSq = 0.0f;
		if (srcGate != nullptr)
		{
			const Vector3& pos = character->GetPosition();
			const float dx = pos.x - Vector3::convert_x(static_cast<float>(srcGate->position.x));
			const float dz = pos.z - static_cast<float>(srcGate->position.z);
			distSq = dx * dx + dz * dz;
		}

		const PlayerLevel* levelComp = player_->GetComponent<PlayerLevel>();
		const int playerLevel = levelComp != nullptr ? levelComp->GetLevel() : 1;

		if (srcGate == nullptr)
		{
			LOG.error("EnterGate rejected: gate {} is not in the player's current map (player {})",
				msg->gateId(), player_->GetPlayerId());
			status = syncnet::StatusCode::StatusCode_Failed;
		}
		else if (distSq > kGateEnterMaxDistance * kGateEnterMaxDistance)
		{
			LOG.error("EnterGate rejected: player {} too far from gate {} (dist {:.1f})",
				player_->GetPlayerId(), srcGate->id, std::sqrt(distSq));
			status = syncnet::StatusCode::StatusCode_Failed;
		}
		else if (playerLevel < srcGate->required_level)
		{
			LOG.info("EnterGate rejected: player {} level {} < required {} (gate {})",
				player_->GetPlayerId(), playerLevel, srcGate->required_level, srcGate->id);
			status = syncnet::StatusCode::StatusCode_Failed;
		}
		else if (!world_->ChangeMap(player_, srcGate->target_id, outMapId, outPos, outAgentId))
		{
			status = syncnet::StatusCode::StatusCode_Failed;
		}
		else
		{
			// 이동에 성공한 경우에만 쿨타임을 갱신한다(실패한 요청은 쿨타임을 소모하지 않음).
			player_->MarkGateMoved(nowMs);
			outTargetId = srcGate->target_id;
		}
	}

	// 응답을 먼저 보낸다. 클라는 이 응답으로 맵 프리팹을 교체하고 기존 액터를 정리한다.
	// 응답의 mapId 는 서버가 정한 도착 맵(클라가 씬을 고르는 근거),
	// gateId 는 도착 지점 마커 id 다(요청의 '밟은 게이트'와 의미가 다르다).
	player_->Send(
		syncnet::CreateEnterGate
		, syncnet::GameMessages::GameMessages_EnterGate
		, lastMessageId_
		, status
		, outMapId
		, outTargetId
		, &outPos
		, outAgentId
	);

	// 응답 이후에 새 맵의 액터 상태를 동기화한다(클라가 맵 교체를 마친 뒤 받도록).
	// 목적지는 캐릭터가 실제로 들어간 맵에서 가져온다 — 인스턴스(레이드 등)는 방금
	// 새로 만들어진 것이라 FindMap(mapId) 으로는 찾을 수 없다.
	if (status == syncnet::StatusCode::StatusCode_Success)
	{
		auto& character = player_->GetCharacter();
		Map* destMap = character != nullptr ? character->GetMap() : nullptr;
		if (destMap != nullptr)
			destMap->SendStateTo(player_);
	}
}

void PlayerController::handle(const syncnet::TreeDebugRequest* msg)
{
#if defined(ENABLE_BT_DEBUG)
	LOG.debug("TreeDebugRequest monsterId:{}", msg->monsterId());
	// 다음 맵 틱의 SendTreeDebugSync 브로드캐스트에 정의+현재 상태가 실려 나간다.
	BTDebugManager::Instance().PublishMonsterSnapshot(msg->monsterId());
#endif
}

void PlayerController::handle(const syncnet::Interact* msg)
{
	auto status = syncnet::StatusCode::StatusCode_Success;

	if (!player_ || !player_->GetCharacter())
	{
		LOG.error("Interact error: player or character is null");
		status = syncnet::StatusCode::StatusCode_Failed;
	}
	else
	{
		auto& character = player_->GetCharacter();
		Map* map = character->GetMap();
		const int targetId = msg->targetId();

		// 목표 위치와 소속 맵을 찾는다. NPC(npc.json)와 맵 오브젝트(Map.json)는 id 공간이
		// 겹치지 않으므로 어느 쪽인지 id 하나로 갈린다.
		bool found = false;
		bool isNpc = false;
		int targetMapId = 0;
		double tx = 0, ty = 0, tz = 0;
		float range = kInteractMaxDistance;

		auto& resource = ResourceLoader::Instance();
		if (const gamedata::Npc* npc = resource.GetNpc(targetId))
		{
			found = true;
			isNpc = true;
			targetMapId = npc->map_id;
			tx = npc->position.x; ty = npc->position.y; tz = npc->position.z;
			if (npc->interact_range > 0.0)
				range = static_cast<float>(npc->interact_range);
		}
		else if (const gamedata::MapObjectsStaticObject* obj = resource.GetMapObjectsStaticObject(targetId))
		{
			found = true;
			targetMapId = obj->parent != nullptr ? obj->parent->id : 0;
			tx = obj->position.x; ty = obj->position.y; tz = obj->position.z;
		}
		else if (const gamedata::MapObjectsMovableObject* obj = resource.GetMapObjectsMovableObject(targetId))
		{
			// 움직이는 오브젝트는 데이터의 시작 위치만 알 수 있다. 실제 위치와 벌어질 수
			// 있으므로 이동 반경만큼 거리 허용치를 넓힌다.
			found = true;
			targetMapId = obj->parent != nullptr ? obj->parent->id : 0;
			tx = obj->position.x; ty = obj->position.y; tz = obj->position.z;
			range += static_cast<float>(obj->movement_range);
		}

		if (!found)
		{
			LOG.error("Interact rejected: unknown target {} (player {})", targetId, player_->GetPlayerId());
			status = syncnet::StatusCode::StatusCode_NotFound;
		}
		else if (map == nullptr || map->GetMapId() != targetMapId)
		{
			LOG.error("Interact rejected: target {} is in map {}, player {} is elsewhere",
				targetId, targetMapId, player_->GetPlayerId());
			status = syncnet::StatusCode::StatusCode_Failed;
		}
		else
		{
			// 데이터 좌표는 클라 좌표계다. 게이트 판정과 같은 방식으로 변환해 비교한다.
			const Vector3& pos = character->GetPosition();
			const float dx = pos.x - Vector3::convert_x(static_cast<float>(tx));
			const float dz = pos.z - static_cast<float>(tz);
			const float distSq = dx * dx + dz * dz;

			if (distSq > range * range)
			{
				LOG.info("Interact rejected: player {} too far from target {} (dist {:.1f}, max {:.1f})",
					player_->GetPlayerId(), targetId, std::sqrt(distSq), range);
				status = syncnet::StatusCode::StatusCode_Failed;
			}
			else if (auto* broker = player_->GetComponent<PlayerEventBroker>())
			{
				const int playerId = static_cast<int>(player_->GetPlayerId());
				if (isNpc)
					broker->publish(EventNpcInteracted{ playerId, targetId });
				else
					broker->publish(EventObjectInteracted{ playerId, targetId });
			}
		}
	}

	player_->Send(
		syncnet::CreateInteract
		, syncnet::GameMessages::GameMessages_Interact
		, lastMessageId_
		, status
		, msg->targetId()
	);
}

void PlayerController::handle(const syncnet::QuestAccept* msg)
{
	auto* quests = player_ != nullptr ? player_->GetComponent<PlayerQuest>() : nullptr;
	if (quests == nullptr)
	{
		LOG.error("QuestAccept error: PlayerQuest component missing");
		return;
	}

	const QuestAcceptResult result = quests->AcceptQuest(msg->questId());
	LOG.info("QuestAccept questId:{} result:{}", msg->questId(), static_cast<int>(result));

	player_->Send(
		syncnet::CreateQuestAccept
		, syncnet::GameMessages::GameMessages_QuestAccept
		, lastMessageId_
		, ToStatusCode(result)
		, msg->questId()
	);
}

void PlayerController::handle(const syncnet::QuestComplete* msg)
{
	auto* quests = player_ != nullptr ? player_->GetComponent<PlayerQuest>() : nullptr;
	if (quests == nullptr)
	{
		LOG.error("QuestComplete error: PlayerQuest component missing");
		return;
	}

	const bool ok = quests->CompleteQuest(msg->questId(), msg->rewardChoice());
	LOG.info("QuestComplete questId:{} choice:{} ok:{}", msg->questId(), msg->rewardChoice(), ok);

	player_->Send(
		syncnet::CreateQuestComplete
		, syncnet::GameMessages::GameMessages_QuestComplete
		, lastMessageId_
		, ok ? syncnet::StatusCode::StatusCode_Success : syncnet::StatusCode::StatusCode_Failed
		, msg->questId()
		, msg->rewardChoice()
	);
}

void PlayerController::handle(const syncnet::QuestAbandon* msg)
{
	auto* quests = player_ != nullptr ? player_->GetComponent<PlayerQuest>() : nullptr;
	if (quests == nullptr)
	{
		LOG.error("QuestAbandon error: PlayerQuest component missing");
		return;
	}

	const bool ok = quests->AbandonQuest(msg->questId());
	LOG.info("QuestAbandon questId:{} ok:{}", msg->questId(), ok);

	player_->Send(
		syncnet::CreateQuestAbandon
		, syncnet::GameMessages::GameMessages_QuestAbandon
		, lastMessageId_
		, ok ? syncnet::StatusCode::StatusCode_Success : syncnet::StatusCode::StatusCode_Failed
		, msg->questId()
	);
}