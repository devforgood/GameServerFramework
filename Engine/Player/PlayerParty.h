#pragma once
#include "Component.h"
#include "PartyManager.h"
#include "Quest.h"
#include <string>
#include <unordered_map>

class Party;

// 플레이어 한 명의 파티 상태.
//
// 멤버십 자체는 PartyManager 가 id 로만 들고 있고, 이 컴포넌트는 그것을 "이 플레이어의
// 것"으로 옮긴다: 클라에 무엇을 내려보낼지 모아 두고, 다른 파티원의 컴포넌트를 찾아
// 퀘스트 공유 제안을 건넨다.
//
// 파티 변경을 다른 컴포넌트가 밀어 주는 대신, 각자 자기 Update 에서 파티 버전을 확인해
// 바뀐 것을 알아챈다. 파티 쪽이 멤버의 수명을 알 필요가 없어져, 접속 종료/재접속에서
// 죽은 포인터를 들고 있을 여지가 사라진다.
class PlayerParty : public ComponentBase<PlayerParty>
{
public:
	// 받아 두고 아직 답하지 않은 퀘스트 공유 제안.
	struct QuestShareOffer
	{
		int quest_id = 0;
		long from_player_id = 0;
		float remaining_sec = 0.0f;
	};

	~PlayerParty() override;

	virtual void Update(float dt) override;

	// 이 컴포넌트가 대변하는 플레이어를 등록한다. Player 생성자가 부른다.
	// 등록해야 다른 파티원이 이 플레이어를 찾을 수 있다.
	void Bind(long player_id);
	long GetPlayerId() const { return playerId_; }

	// 이 플레이어의 캐릭터가 있는 곳. 컴포넌트 층에서는 캐릭터를 볼 수 없으므로
	// 빙의/해제 시점에 Player 가 알려 준다(맵을 옮기면 캐릭터가 재생성되어 둘 다 바뀐다).
	// 로스터에 실어 보낼 때 파티원의 것을 각자의 컴포넌트에서 읽는다 — 이름/레벨과 같은 방식.
	void SetLocation(int actor_id, int map_id);
	int GetActorId() const { return actorId_; }
	int GetMapId() const { return mapId_; }

	// 플레이어 id 로 컴포넌트를 찾는다. 등록/해제는 Bind 와 소멸자가 맡으므로
	// 여기서 돌아온 포인터는 그 플레이어가 살아 있는 동안만 유효하다(같은 틱 안에서 쓸 것).
	static PlayerParty* Find(long player_id);

	// ---- 파티 조작
	PartyResult Invite(long target_player_id);
	PartyResult RespondInvite(bool accept);
	PartyResult Leave();
	PartyResult Kick(long target_player_id);
	PartyResult TransferLeader(long target_player_id);

	// ---- 퀘스트 공유
	// 진행 중인 퀘스트를 파티원들에게 제안한다. 데이터가 shareable 이 아니면 거절한다.
	// 받을 수 없는 상태(이미 진행/완료, 다른 제안 대기 중)인 파티원은 조용히 건너뛴다.
	// out_offer_count 에 실제로 제안이 간 인원 수를 채운다(0 이어도 Ok 다 — 실패가 아니라
	// "받을 사람이 없었다"는 뜻이고, 클라는 이 수를 보고 안내하면 된다).
	PartyResult ShareQuest(int quest_id, int* out_offer_count = nullptr);

	// 받은 제안에 답한다. 수락이면 그대로 퀘스트 수락 경로를 탄다
	// (조건 검사는 평소와 똑같이 한다 — 공유는 조건을 면제해 주지 않는다).
	PartyResult RespondShare(int quest_id, bool accept, QuestAcceptResult* out_accept = nullptr);

	// ---- 조회
	const Party* GetParty() const;
	bool IsInParty() const { return GetParty() != nullptr; }
	bool IsLeader() const;
	const PartyInvite* GetPendingInvite() const;
	const QuestShareOffer* GetPendingShare() const { return share_.quest_id != 0 ? &share_ : nullptr; }

private:
	// ---- 클라 동기화 (sendSync 만 쓴다)
	// 로스터가 바뀌었는지. 파티에서 빠진 것도 변경이며, 이때 GetParty() 는 nullptr 이다
	// (클라는 partyId 0 짜리 빈 로스터를 받고 파티 창을 닫는다).
	bool HasPendingRosterSync() const { return rosterDirty_; }

	// 새로 들어온 초대/공유 제안을 꺼낸다(꺼내면 알림 표시는 지워지고, 제안 자체는
	// 만료되거나 답할 때까지 남는다).
	bool TakeInviteNotify(PartyInvite& out);
	bool TakeShareNotify(QuestShareOffer& out);

	// 다른 파티원이 이 플레이어에게 공유를 건넬 때 쓰는 입구.
	bool offerShare(int quest_id, long from_player_id);

	// 바뀐 것과 새로 받은 제안을 클라에 내보낸다. Update 끝에서 호출.
	void sendSync();

	// 파티원의 표시 정보. 이름/레벨은 상대의 컴포넌트에서 바로 읽는다 — 월드 조회 없이
	// 얻을 수 있으므로 세션이 없는 곳(테스트)에서도 채워진다.
	static std::string resolveName(long player_id);
	static int resolveLevel(long player_id);
	static PlayerParty* findMember(long player_id);

	static std::unordered_map<long, PlayerParty*>& registry();

	long playerId_ = 0;

	// 마지막으로 클라에 반영한 파티. 이 둘과 지금 상태가 다르면 보낼 것이 있다.
	int lastPartyId_ = 0;
	uint32_t lastVersion_ = 0;
	bool rosterDirty_ = false;

	// 마지막으로 알림을 띄운 초대의 발신자. 초대가 사라지면 0 으로 되돌려,
	// 같은 사람이 다시 부르면 다시 알림이 뜨게 한다.
	long notifiedInviter_ = 0;
	bool inviteNotifyPending_ = false;

	QuestShareOffer share_;
	bool shareNotifyPending_ = false;

	// 내 캐릭터의 위치. 빙의 전이거나 캐릭터가 없으면 0.
	int actorId_ = 0;
	int mapId_ = 0;
};
