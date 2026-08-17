#pragma once
#include <string>

#include "syncnet_generated.h"

class World;
class Player;
class Character;

namespace gamedata
{
	struct Dialog;
}

class PlayerController
{
private:
	World* world_;
	std::shared_ptr<Player> player_;
	int lastMessageId_;

	friend class GameSession;

public:
	void handle(const syncnet::GameMessage* msg);
	void handle(const syncnet::AddAgent* msg);
	void handle(const syncnet::RemoveAgent* msg);
	void handle(const syncnet::SetMoveTarget* msg);
	void handle(const syncnet::Ping* msg);
	void handle(const syncnet::SetRaycast* msg);
	void handle(const syncnet::Login* msg);
	void handle(const syncnet::UseSkill* msg);
	void handle(const syncnet::EnterGate* msg);
	void handle(const syncnet::TreeDebugRequest* msg);
	void handle(const syncnet::Interact* msg);
	void handle(const syncnet::QuestAccept* msg);
	void handle(const syncnet::QuestComplete* msg);
	void handle(const syncnet::QuestAbandon* msg);
	void handle(const syncnet::PartyInvite* msg);
	void handle(const syncnet::PartyInviteReply* msg);
	void handle(const syncnet::PartyLeave* msg);
	void handle(const syncnet::PartyKick* msg);
	void handle(const syncnet::PartyLeaderChange* msg);
	void handle(const syncnet::PartyQuestShare* msg);
	void handle(const syncnet::PartyQuestShareReply* msg);
	void handle(const syncnet::DialogSelect* msg);

	// 인증 검증(DB 스레드 왕복) 이후 게임 스레드에서 이어지는 로그인 처리.
	// handle(Login) / 로드 완료 콜백이 비동기로 호출하므로 public 이다.
	//   CompleteLogin    : 인증 통과 직후. 재접속 핸드오버를 처리하거나 계정 로드를 건다.
	//   SendLoginSuccess : 계정 로드까지 끝난 뒤 스폰 정보를 담아 응답한다.
	void CompleteLogin(const std::string& userId, const std::string& reconnectToken,
		int messageId, long long authPlayerId);
	void SendLoginSuccess(const std::string& userId, int messageId);
	void RejectLogin(int messageId, const std::string& userId, const std::string& reason);

private:
	// 지금 열려 있는 대화 노드를 클라에 보낸다. node 가 nullptr 이면 "닫힘"을 보낸다.
	void SendDialogNode(const gamedata::Dialog* node, int npc_id, syncnet::StatusCode status);
};

