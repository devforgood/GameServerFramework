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
#include "SendMessagePool.h"
#include "Map.h"
#include "PlayerRepository.h"
#include "PlayerDbDispatcher.h"
#include "IAuthenticator.h"
#include "MessagePolicy.h"
#include "ServerConfig.h"
#include "Server.h"
#include "Common.h" // gamedata::Map/MapGate 전체 정의
#include "PlayerLevel.h"
#include "PlayerLocation.h"
#include "PlayerEventBroker.h"
#include "PlayerQuest.h"
#include "PlayerParty.h"
#include "PlayerDialog.h"
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

	// 파티 조작 실패 사유도 마찬가지로 성공/실패 수준까지만 내려간다.
	syncnet::StatusCode ToStatusCode(PartyResult result)
	{
		switch (result)
		{
		case PartyResult::Ok:             return syncnet::StatusCode::StatusCode_Success;
		case PartyResult::TargetNotFound:
		case PartyResult::NoInvite:
		case PartyResult::NoOffer:        return syncnet::StatusCode::StatusCode_NotFound;
		case PartyResult::AlreadyInParty:
		case PartyResult::TargetInParty:
		case PartyResult::AlreadyInvited: return syncnet::StatusCode::StatusCode_AlreadyExists;
		default:                          return syncnet::StatusCode::StatusCode_Failed;
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

	const syncnet::GameMessages type = msg->msg_type();

	// --- 페이로드 존재 확인 ---
	// FlatBuffers Verifier 는 union 페이로드가 아예 없는 경우를 통과시킨다
	// (VerifyTable(nullptr) 이 true). 그래서 "msg_type=UseSkill 인데 본문 없음" 같은 패킷이
	// 검증을 지나 핸들러까지 오고, msg_as_UseSkill() 이 준 nullptr 를 역참조해 죽는다.
	// 타입이 붙어 있는데 본문이 없으면 정상 클라이언트가 만들 수 없는 패킷이므로 여기서 끊는다.
	if (type != syncnet::GameMessages::GameMessages_NONE && msg->msg() == nullptr)
	{
		LOG.warn("본문 없는 메시지 거부: type {}. 세션을 끊는다.", static_cast<int>(type));
		if (player_ != nullptr)
			player_->Close();
		return;
	}

	// --- 인가 게이트 ---
	// 접속만으로 Player 가 만들어지므로, 이 검사가 없으면 로그인하지 않은 연결이
	// 모든 핸들러에 도달한다.
	auto session = player_ != nullptr ? player_->GetSession() : nullptr;
	const bool authenticated = session != nullptr && session->IsAuthenticated();

	if (!authenticated && !message_policy::IsAllowedBeforeAuth(type))
	{
		LOG.warn("인증 전 메시지 거부: type {}. 세션을 끊는다.", static_cast<int>(type));
		if (player_ != nullptr)
			player_->Close();
		return;
	}

	if (message_policy::IsDebugOnly(type) && !ServerConfig::Instance().Network().allow_debug_commands)
	{
		LOG.warn("디버그 전용 메시지 거부: type {} (allow_debug_commands=false)", static_cast<int>(type));
		return;
	}

	switch (type)
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
	case syncnet::GameMessages::GameMessages_PartyInvite:		handle(msg->msg_as_PartyInvite()); break;
	case syncnet::GameMessages::GameMessages_PartyInviteReply:	handle(msg->msg_as_PartyInviteReply()); break;
	case syncnet::GameMessages::GameMessages_PartyLeave:		handle(msg->msg_as_PartyLeave()); break;
	case syncnet::GameMessages::GameMessages_PartyKick:			handle(msg->msg_as_PartyKick()); break;
	case syncnet::GameMessages::GameMessages_PartyLeaderChange:	handle(msg->msg_as_PartyLeaderChange()); break;
	case syncnet::GameMessages::GameMessages_PartyQuestShare:	handle(msg->msg_as_PartyQuestShare()); break;
	case syncnet::GameMessages::GameMessages_PartyQuestShareReply: handle(msg->msg_as_PartyQuestShareReply()); break;
	case syncnet::GameMessages::GameMessages_DialogSelect:		handle(msg->msg_as_DialogSelect()); break;
	}
}

void PlayerController::handle(const syncnet::AddAgent* msg)
{
	// 클라가 만들 수 있는 것은 자기 캐릭터 하나뿐이다.
	// 예전에는 요청의 gameObjectType 을 그대로 썼는데, 그러면 클라가 몬스터를 무제한
	// 스폰할 수 있었다. 타입은 서버가 정하고, 이미 캐릭터가 있으면 거부한다.
	if (player_->GetCharacter() != nullptr)
	{
		LOG.warn("AddAgent 거부: 플레이어 {} 는 이미 캐릭터를 가지고 있다", player_->GetPlayerId());
		player_->Send(
			syncnet::CreateAddAgent
			, syncnet::GameMessages::GameMessages_AddAgent
			, lastMessageId_
			, syncnet::StatusCode::StatusCode_AlreadyExists
			, syncnet::GameObjectType::GameObjectType_Character
			, msg->pos()
			, 0
		);
		return;
	}

	// 스폰 위치도 클라가 정하지 않는다. 로그인 응답에서 서버가 이미 정해 클라에 알려준
	// 좌표를 그대로 쓴다(요청의 pos 를 믿으면 임의 좌표로 들어올 수 있다).
	syncnet::Vec3 spawnPos = player_->GetSpawnPos();

	LOG.info("add agent pos:({},{},{})", spawnPos.x(), spawnPos.y(), spawnPos.z());

	auto actor = world_->OnAddAgent(player_, syncnet::GameObjectType::GameObjectType_Character, &spawnPos);
	auto status = syncnet::StatusCode::StatusCode_Success;
	int actor_id = 0;
	if (!actor) {
		LOG.error("OnAddAgent 실패: Actor 생성에 실패했습니다.");
		status = syncnet::StatusCode::StatusCode_Failed;
	}
	else
	{
		actor_id = actor->GetActorId();
	}

	player_->Send(
		syncnet::CreateAddAgent
		, syncnet::GameMessages::GameMessages_AddAgent
		, lastMessageId_
		, status
		, syncnet::GameObjectType::GameObjectType_Character
		, &spawnPos
		, actor_id
	);

}

void PlayerController::handle(const syncnet::RemoveAgent* msg)
{
	// 자기 캐릭터만 제거할 수 있다. 예전에는 요청의 actorId 를 그대로 넘겨서
	// 남의 캐릭터나 몬스터를 지울 수 있었다.
	auto character = player_->GetCharacter();
	if (character == nullptr || character->GetActorId() != msg->actorId())
	{
		LOG.warn("RemoveAgent 거부: 플레이어 {} 가 소유하지 않은 actorId {}",
			player_->GetPlayerId(), msg->actorId());
		return;
	}

	LOG.info("remove actor id :{}", msg->actorId());
	world_->OnRemoveAgent(msg->actorId());
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

	// pos 는 선택 필드라 Verifier 를 통과해도 없을 수 있다(조작 패킷).
	if (msg->pos() == nullptr)
	{
		LOG.warn("SetMoveTarget 거부: pos 없음");
		return;
	}

	LOG.debug("move target actor id :{}, pos:({},{},{})", player_->GetCharacter()->GetActorId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
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
	if (msg->pos() == nullptr)
	{
		LOG.warn("SetRaycast 거부: pos 없음");
		return;
	}

	LOG.info("SetRaycast pos:({},{},{})", msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
	world_->OnSetRaycast(msg->pos());
}

void PlayerController::handle(const syncnet::Login* msg)
{
	const std::string userId = msg->userId() != nullptr ? msg->userId()->c_str() : "";
	const std::string authToken = msg->authToken() != nullptr ? msg->authToken()->c_str() : "";
	const std::string reconnectToken = msg->uuid() != nullptr ? msg->uuid()->c_str() : "";
	const int messageId = lastMessageId_;
	LOG.info("Login id :{}, uuid:'{}', lastMessageId:{}", userId, reconnectToken, messageId);

	auto session = player_->GetSession();
	if (session == nullptr)
	{
		LOG.error("Login: session expired");
		return;
	}

	// 이미 인증된 세션의 재로그인 요청은 무시한다. 인증 후 다른 계정으로 갈아타는 경로를
	// 열어두면, 로그인 한 번으로 얻은 세션으로 임의 계정을 돌아가며 점유할 수 있다.
	if (session->IsAuthenticated())
	{
		LOG.warn("Login: 이미 인증된 세션의 중복 로그인 요청 거부 (userId '{}')", userId);
		return;
	}

	// 인증은 DB 조회(블로킹)라 게임 스레드에서 할 수 없다. DB 스레드에서 검증하고
	// 결과를 게임 스레드로 되돌려 CompleteLogin 으로 이어간다.
	//
	// 세션은 검증 왕복 동안 끊어질 수 있고, Player 는 재접속 유예 때문에 세션보다
	// 오래 살 수 있다. 그래서 Player 가 아니라 세션을 weak 으로 잡고, 살아있을 때만 잇는다.
	std::weak_ptr<GameSession> weakSession = session;

	PlayerDbDispatcher::Dispatch(player_, "auth.verify",
		[userId, authToken](sql::Connection* conn, long) {
			auto result = std::make_shared<AuthResult>();
			try
			{
				IAuthenticator* authenticator = AuthService::Instance().Get();
				*result = authenticator != nullptr
					? authenticator->Verify(conn, userId, authToken)
					: AuthResult::Fail("authenticator not initialized");
			}
			catch (const std::exception& e)
			{
				// 인증 경로의 예외는 절대 통과로 해석하지 않는다.
				*result = AuthResult::Fail(std::string("verify threw: ") + e.what());
			}
			return result;
		},
		[weakSession, userId, reconnectToken, messageId](Player&, const AuthResult& result) {
			auto session = weakSession.lock();
			if (session == nullptr)
				return; // 검증 도중 끊겼다.

			PlayerController* controller = session->GetController();
			if (controller == nullptr)
				return;

			if (!result.ok)
			{
				controller->RejectLogin(messageId, userId, result.reason);
				return;
			}

			session->SetAuthenticated(true);
			controller->CompleteLogin(userId, reconnectToken, messageId, result.playerId);
		});
}

void PlayerController::RejectLogin(int messageId, const std::string& userId, const std::string& reason)
{
	// 사유는 로그에만 남긴다. 클라에 그대로 내려주면 "계정 없음/토큰 만료"가 구분돼
	// 계정 존재 여부를 캐낼 수 있다.
	LOG.warn("Login rejected (userId '{}'): {}", userId, reason);

	player_->Send(
		syncnet::CreateLoginDirect
		, syncnet::GameMessages::GameMessages_Login
		, messageId
		, syncnet::StatusCode::StatusCode_Failed
		, userId.c_str()
		, 0 /* mapId */
		, nullptr /* pos */
		, 0 /* actorId */
		, nullptr /* uuid */
		, nullptr /* authToken: 응답에는 절대 담지 않는다 */
	);

	// 인증에 실패한 연결은 유지하지 않는다(재시도 도배로 DB 조회를 반복시키지 못하게).
	player_->Close();
}

void PlayerController::CompleteLogin(const std::string& userId, const std::string& reconnectToken,
	int messageId, long long authPlayerId)
{
	lastMessageId_ = messageId;

	// 재접속 핸드오버: 클라가 되돌려 보낸 uuid(재접속 토큰)로 유예 대기 중인 기존 플레이어를
	// 찾으면, 이 세션을 그 플레이어에 재바인딩하고 기존 캐릭터(맵/위치/actorId)를 넘겨받는다.
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
		int actorId = 0;
		if (character != nullptr && character->GetMap() != nullptr)
		{
			mapId = character->GetMap()->GetMapId();
			const Vector3& p = character->GetPosition();
			pos = syncnet::Vec3(p.convert_x(), p.convert_y(), p.convert_z());
			actorId = character->GetActorId();
		}

		// 응답의 actorId 가 0 이 아니면 클라는 재접속으로 인식해 AddAgent 를 생략하고
		// 이 actorId 를 채택한다. 이어지는 SendStateTo 로 기존 캐릭터가 재생성된다.
		// uuid 는 기존 값 그대로(클라가 이미 보유) 다시 실어 보낸다.
		const std::string uuid = oldPlayer->GetUuid();
		oldPlayer->Send(
			syncnet::CreateLoginDirect
			, syncnet::GameMessages::GameMessages_Login
			, lastMessageId_
			, syncnet::StatusCode::StatusCode_Success
			, userId.c_str()
			, mapId
			, &pos
			, actorId
			, uuid.c_str()
			, nullptr /* authToken: 응답에는 담지 않는다 */
		);

		if (character != nullptr && character->GetMap() != nullptr)
			character->GetMap()->SendStateTo(oldPlayer);
		return;
	}

	// 신규 로그인.
	player_->SetUserId(userId);

	// 같은 계정이 이미 들어와 있으면 그쪽을 정리한다. 한 계정이 두 세션으로 동시에
	// 돌아다니면 두 세션이 같은 DB 행에 번갈아 저장해 서로의 진행을 덮어쓴다.
	world_->EvictExistingLogin(userId, player_.get());

	// 계정 행을 확정하고 로드가 끝난 뒤에 응답한다.
	// 예전에는 로드를 던져 놓고 곧바로 응답했는데, 그러면 저장된 위치가 아직 없어서
	// 항상 기본 스폰 지점으로 보냈다. 스폰 좌표는 로드 결과가 있어야 알 수 있다.
	std::weak_ptr<GameSession> weakSession = player_->GetSession();

	PlayerRepository::AsyncResolveAndLoad(player_, userId, authPlayerId,
		[weakSession, userId, messageId](Player& player, bool ok) {
			auto session = weakSession.lock();
			if (session == nullptr)
				return;

			PlayerController* controller = session->GetController();
			if (controller == nullptr)
				return;

			if (!ok)
			{
				// 계정 행을 확정하지 못했다(DB 장애 등). 이 상태로 진행하면 진행 상황이
				// 저장되지 않는 유령 세션이 된다 — 차라리 로그인을 실패시킨다.
				controller->RejectLogin(messageId, userId, "failed to resolve account row");
				return;
			}

			controller->SendLoginSuccess(userId, messageId);
		});
}

void PlayerController::SendLoginSuccess(const std::string& userId, int messageId)
{
	lastMessageId_ = messageId;

	// 클라가 어느 맵(씬)을 로드하고 어디에 스폰할지 알려준다. 기본은 기본 맵의
	// player_spawn 마커, 저장된 마지막 위치가 있으면 그쪽을 우선한다.
	int mapId = 0;
	syncnet::Vec3 spawnPos(0, 0, 0);
	Map* primaryMap = world_->GetPrimaryMap();
	if (primaryMap != nullptr)
	{
		mapId = primaryMap->GetMapId();
		spawnPos = primaryMap->GetPlayerSpawnPos();
	}

	int savedMapId = 0;
	float savedX = 0.0f, savedY = 0.0f, savedZ = 0.0f;
	if (auto* location = player_->GetComponent<PlayerLocation>())
	{
		// 저장된 맵이 지금 이 서버에 로드되어 있을 때만 쓴다(맵 구성이 바뀔 수 있다).
		if (location->TryGet(savedMapId, savedX, savedY, savedZ)
			&& world_->FindMap(savedMapId) != nullptr)
		{
			mapId = savedMapId;
			// 저장은 서버 좌표계, 프로토콜(Login 응답 / Map.json 스폰 마커)은 클라 좌표계다.
			// 변환을 빠뜨리면 x 부호가 뒤집힌 지점으로 스폰된다.
			spawnPos = syncnet::Vec3(
				Vector3::convert_x(savedX),
				Vector3::convert_y(savedY),
				Vector3::convert_z(savedZ));
			LOG.info("Login: userId '{}' 저장된 위치로 스폰. mapId {}, pos({},{},{})",
				userId, mapId, spawnPos.x(), spawnPos.y(), spawnPos.z());
		}
	}

	// 클라의 AddAgent(Character) 가 쓸 맵/좌표로 기억해 둔다(응답과 실제 스폰을 일치시킨다).
	player_->SetSpawnLocation(mapId, spawnPos);

	// 최초 로그인 응답에 플레이어 uuid(재접속 토큰)를 실어 보낸다. 클라는 이를 저장했다가
	// 재접속 시 되돌려 보낸다.
	const std::string uuid = player_->GetUuid();
	player_->Send(
		syncnet::CreateLoginDirect
		, syncnet::GameMessages::GameMessages_Login
		, lastMessageId_
		, syncnet::StatusCode::StatusCode_Success
		, userId.c_str()
		, mapId
		, &spawnPos
		, 0 /* actorId: 신규 로그인은 0 */
		, uuid.c_str()
		, nullptr /* authToken: 응답에는 담지 않는다 */
	);
}

void PlayerController::handle(const syncnet::UseSkill* msg)
{
	// pos 는 선택 필드라 Verifier 를 통과해도 없을 수 있다(조작 패킷).
	// Character::use_skill 도 같은 검사를 하지만, 여기 로그가 먼저 역참조한다.
	if (msg->pos() == nullptr)
	{
		LOG.warn("UseSkill 거부: pos 없음");
		return;
	}

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

	auto builder_ptr = SendMessagePool::Acquire();
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
	int outActorId = 0;
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
		else if (!world_->ChangeMap(player_, srcGate->target_id, outMapId, outPos, outActorId))
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
		, outActorId
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

	// 대화가 열렸으면 상호작용 응답 뒤에 보낼 첫 노드.
	const gamedata::Dialog* openedDialog = nullptr;

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
			else
			{
				if (auto* broker = player_->GetComponent<PlayerEventBroker>())
				{
					const int playerId = static_cast<int>(player_->GetPlayerId());
					if (isNpc)
						broker->publish(EventNpcInteracted{ playerId, targetId });
					else
						broker->publish(EventObjectInteracted{ playerId, targetId });
				}

				// 대화가 걸린 NPC 면 첫 노드를 이어서 내려보낸다. 대화를 여는 메시지를 따로
				// 두지 않는 이유는 NPC 를 누르는 동작 하나로 족하기 때문이다. 대화 목표(talk)와
				// 완료 접수는 위 이벤트가 이미 처리했다 — 대화는 그 위에 얹히는 표현이다.
				if (isNpc)
				{
					if (auto* dialog = player_->GetComponent<PlayerDialog>())
					{
						if (dialog->Open(targetId))
							openedDialog = dialog->GetCurrentNode();
					}
				}
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

	// 상호작용 응답 뒤에 보낸다. 클라가 먼저 상호작용 성공을 확인하고 대화 창을 연다.
	if (openedDialog != nullptr)
	{
		SendDialogNode(openedDialog, msg->targetId(), syncnet::StatusCode::StatusCode_Success);
	}
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

namespace
{
	// 파티 조작은 모두 PlayerParty 를 거친다. 컴포넌트가 없으면(빙의 전 등) 아무것도 못 한다.
	PlayerParty* GetParty(const std::shared_ptr<Player>& player)
	{
		return player != nullptr ? player->GetComponent<PlayerParty>() : nullptr;
	}
}

void PlayerController::handle(const syncnet::PartyInvite* msg)
{
	auto* party = GetParty(player_);
	PartyResult result = PartyResult::TargetNotFound;

	// 클라는 화면에서 고른 캐릭터의 actor id 를 보낸다. 그 액터가 정말 같은 맵의
	// 캐릭터인지는 서버가 확인한다(임의의 id 로 아무나 부르지 못하게).
	if (party != nullptr && player_->GetCharacter() != nullptr && player_->GetCharacter()->GetMap() != nullptr)
	{
		auto target = player_->GetCharacter()->GetMap()->FindActor(msg->actorId());
		if (target != nullptr && target->IsCharacter())
			result = party->Invite(static_cast<Character*>(target.get())->GetPlayerId());
	}

	LOG.info("PartyInvite actorId:{} result:{}", msg->actorId(), static_cast<int>(result));

	player_->Send(
		syncnet::CreatePartyInviteDirect
		, syncnet::GameMessages::GameMessages_PartyInvite
		, lastMessageId_
		, ToStatusCode(result)
		, msg->actorId()
		, 0
		, 0
		, nullptr /* inviterName: 요청에 대한 응답이라 비운다 */
		, 0.0f
	);
}

void PlayerController::handle(const syncnet::PartyInviteReply* msg)
{
	auto* party = GetParty(player_);
	const PartyResult result = party != nullptr
		? party->RespondInvite(msg->accept())
		: PartyResult::NoInvite;

	LOG.info("PartyInviteReply accept:{} result:{}", msg->accept(), static_cast<int>(result));

	player_->Send(
		syncnet::CreatePartyInviteReply
		, syncnet::GameMessages::GameMessages_PartyInviteReply
		, lastMessageId_
		, ToStatusCode(result)
		, msg->accept()
	);
}

void PlayerController::handle(const syncnet::PartyLeave* msg)
{
	auto* party = GetParty(player_);
	const PartyResult result = party != nullptr ? party->Leave() : PartyResult::NotInParty;

	LOG.info("PartyLeave result:{}", static_cast<int>(result));

	player_->Send(
		syncnet::CreatePartyLeave
		, syncnet::GameMessages::GameMessages_PartyLeave
		, lastMessageId_
		, ToStatusCode(result)
	);
}

void PlayerController::handle(const syncnet::PartyKick* msg)
{
	auto* party = GetParty(player_);
	const PartyResult result = party != nullptr
		? party->Kick(static_cast<long>(msg->playerId()))
		: PartyResult::NotInParty;

	LOG.info("PartyKick playerId:{} result:{}", msg->playerId(), static_cast<int>(result));

	player_->Send(
		syncnet::CreatePartyKick
		, syncnet::GameMessages::GameMessages_PartyKick
		, lastMessageId_
		, ToStatusCode(result)
		, msg->playerId()
	);
}

void PlayerController::handle(const syncnet::PartyLeaderChange* msg)
{
	auto* party = GetParty(player_);
	const PartyResult result = party != nullptr
		? party->TransferLeader(static_cast<long>(msg->playerId()))
		: PartyResult::NotInParty;

	LOG.info("PartyLeaderChange playerId:{} result:{}", msg->playerId(), static_cast<int>(result));

	player_->Send(
		syncnet::CreatePartyLeaderChange
		, syncnet::GameMessages::GameMessages_PartyLeaderChange
		, lastMessageId_
		, ToStatusCode(result)
		, msg->playerId()
	);
}

void PlayerController::handle(const syncnet::PartyQuestShare* msg)
{
	auto* party = GetParty(player_);
	int offered = 0;
	const PartyResult result = party != nullptr
		? party->ShareQuest(msg->questId(), &offered)
		: PartyResult::NotInParty;

	LOG.info("PartyQuestShare questId:{} offered:{} result:{}",
		msg->questId(), offered, static_cast<int>(result));

	// 응답의 fromPlayerId 자리에 제안이 실제로 간 인원 수를 담는다. 0 이어도 성공이다
	// (받을 수 있는 파티원이 없었을 뿐이며, 클라는 이 수로 안내 문구를 고른다).
	player_->Send(
		syncnet::CreatePartyQuestShareDirect
		, syncnet::GameMessages::GameMessages_PartyQuestShare
		, lastMessageId_
		, ToStatusCode(result)
		, msg->questId()
		, static_cast<int64_t>(offered)
		, nullptr /* fromName */
		, 0.0f
	);
}

void PlayerController::handle(const syncnet::PartyQuestShareReply* msg)
{
	auto* party = GetParty(player_);
	QuestAcceptResult accept_result = QuestAcceptResult::Ok;
	const PartyResult result = party != nullptr
		? party->RespondShare(msg->questId(), msg->accept(), &accept_result)
		: PartyResult::NoOffer;

	LOG.info("PartyQuestShareReply questId:{} accept:{} result:{} acceptResult:{}",
		msg->questId(), msg->accept(), static_cast<int>(result), static_cast<int>(accept_result));

	// 제안에 답한 것과 퀘스트를 실제로 받은 것은 다르다. 수락했는데 조건이 안 되면
	// 클라 입장에서는 실패이므로 둘 다 성공일 때만 성공으로 내려 준다.
	syncnet::StatusCode status = ToStatusCode(result);
	if (status == syncnet::StatusCode::StatusCode_Success
		&& msg->accept() && accept_result != QuestAcceptResult::Ok)
	{
		status = ToStatusCode(accept_result);
	}

	player_->Send(
		syncnet::CreatePartyQuestShareReply
		, syncnet::GameMessages::GameMessages_PartyQuestShareReply
		, lastMessageId_
		, status
		, msg->questId()
		, msg->accept()
	);
}
void PlayerController::SendDialogNode(const gamedata::Dialog* node, int npc_id, syncnet::StatusCode status)
{
	auto builder_ptr = SendMessagePool::Acquire();

	std::vector<flatbuffers::Offset<syncnet::DialogChoiceInfo>> choices;
	flatbuffers::Offset<flatbuffers::String> text_offset = 0;

	if (node != nullptr)
	{
		// node 는 언제나 지금 열려 있는 노드다(호출부 전부가 그렇게 부른다). 무엇을
		// 내보낼지는 그 노드로 옮겨 올 때 이미 정해져 있으므로 여기서는 읽기만 한다 —
		// 여기서 다시 판정하면 클라가 본 목록과 서버가 번호를 되짚는 목록이 갈릴 수 있다.
		auto* dialog = player_ != nullptr ? player_->GetComponent<PlayerDialog>() : nullptr;

		text_offset = builder_ptr->CreateString(node->text_id);
		choices.reserve(node->choices.size());
		for (int i = 0; i < static_cast<int>(node->choices.size()); ++i)
		{
			if (dialog != nullptr && !dialog->IsChoiceVisible(i))
				continue;

			const auto& choice = node->choices[i];
			choices.push_back(syncnet::CreateDialogChoiceInfo(
				*builder_ptr,
				builder_ptr->CreateString(choice.text_id),
				builder_ptr->CreateString(choice.action)));
		}
	}

	// node 가 없으면 nodeId 0 짜리 빈 노드를 보낸다 — 클라는 이것으로 창을 닫는다.
	auto payload = syncnet::CreateDialogNode(
		*builder_ptr,
		node != nullptr ? node->id : 0,
		node != nullptr ? npc_id : 0,
		text_offset,
		builder_ptr->CreateVector(choices));

	auto send_msg = syncnet::CreateGameMessage(
		*builder_ptr,
		syncnet::GameMessages::GameMessages_DialogNode,
		payload.Union(),
		lastMessageId_,
		status);
	builder_ptr->Finish(send_msg);

	player_->Send(builder_ptr);
}

void PlayerController::handle(const syncnet::DialogSelect* msg)
{
	auto* dialog = player_ != nullptr ? player_->GetComponent<PlayerDialog>() : nullptr;
	if (dialog == nullptr)
	{
		LOG.error("DialogSelect error: PlayerDialog component missing");
		return;
	}

	// 음수 인덱스는 "창을 닫았다"는 뜻이다. 별도 메시지를 두지 않는 이유는, 닫기가
	// 선택지 중 하나(close)로도 일어나서 클라가 두 경로를 구분할 이유가 없기 때문이다.
	if (msg->choiceIndex() < 0)
	{
		dialog->Close();
		SendDialogNode(nullptr, 0, syncnet::StatusCode::StatusCode_Success);
		return;
	}

	const gamedata::Dialog* next = nullptr;
	const DialogResult result = dialog->Select(msg->nodeId(), msg->choiceIndex(), &next);

	LOG.info("DialogSelect nodeId:{} choice:{} result:{}",
		msg->nodeId(), msg->choiceIndex(), static_cast<int>(result));

	switch (result)
	{
	case DialogResult::Ok:
		SendDialogNode(next, dialog->GetNpcId(), syncnet::StatusCode::StatusCode_Success);
		break;

	case DialogResult::Closed:
		SendDialogNode(nullptr, 0, syncnet::StatusCode::StatusCode_Success);
		break;

	case DialogResult::ActionFailed:
		// 대화는 그 자리에 남는다. 왜 안 됐는지 보여줄 화면을 유지해야 하기 때문이다.
		SendDialogNode(dialog->GetCurrentNode(), dialog->GetNpcId(),
			syncnet::StatusCode::StatusCode_Failed);
		break;

	default:
		SendDialogNode(nullptr, 0, syncnet::StatusCode::StatusCode_Failed);
		break;
	}
}
