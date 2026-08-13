#pragma once
#include "Party.h"
#include <memory>
#include <unordered_map>
#include <vector>

// 파티 조작의 실패 사유. 클라에 그대로 내려 주지는 않지만(프로토콜은 성공/실패만 싣는다),
// 로그와 테스트가 "왜 거절됐는지"를 구분할 수 있어야 한다.
enum class PartyResult
{
	Ok,
	NotInParty,           // 요청자가 파티에 없다
	AlreadyInParty,       // 요청자가 이미 다른 파티에 있다
	TargetInParty,        // 대상이 이미 파티에 있다
	TargetNotFound,       // 대상 플레이어를 찾을 수 없다
	NotLeader,            // 리더만 할 수 있는 조작이다
	NotMember,            // 대상이 내 파티원이 아니다
	PartyFull,
	SelfTarget,           // 자기 자신을 대상으로 삼았다
	AlreadyInvited,       // 대상이 이미 다른 초대를 받고 대기 중이다
	NoInvite,             // 응답할 초대가 없다(만료 포함)

	// 퀘스트 공유(PlayerParty)에서만 쓴다.
	QuestNotActive,       // 공유하려는 퀘스트를 진행 중이 아니다
	QuestNotShareable,    // 데이터가 공유를 허용하지 않는 퀘스트다
	NoOffer,              // 응답할 공유 제안이 없다(만료 포함)
};

// 받아 두고 아직 답하지 않은 초대.
struct PartyInvite
{
	// 초대한 쪽이 아직 파티가 없으면 0. 이 상태에서 수락하면 그때 파티를 만든다.
	// 초대 시점에 미리 만들어 두면, 거절당했을 때 혼자짜리 파티가 남는다.
	int party_id = 0;
	long inviter_id = 0;
	float remaining_sec = 0.0f;
};

// 파티 멤버십의 단일 출처.
//
// 여기서는 플레이어를 id(long) 로만 다룬다. Player/Character/네트워크를 모르기 때문에
// 월드 없이도 그대로 시험할 수 있고, 반대로 "누구에게 무엇을 보낼지"는 PlayerParty 가 맡는다.
class PartyManager
{
public:
	static PartyManager& Instance()
	{
		static PartyManager instance;
		return instance;
	}

	Party* Find(int party_id);
	Party* FindByPlayer(long player_id);
	const Party* FindByPlayer(long player_id) const;

	// 초대. 초대한 쪽이 파티가 없으면 수락 시점에 새 파티를 만든다.
	PartyResult Invite(long inviter_id, long invitee_id);

	// 수락. 성공 시 out_party 에 가입한 파티를 채운다.
	PartyResult AcceptInvite(long invitee_id, Party** out_party = nullptr);
	PartyResult DeclineInvite(long invitee_id);

	const PartyInvite* GetInvite(long invitee_id) const;

	// 탈퇴. 리더가 나가면 남은 멤버 중 가장 오래된 쪽이 리더가 된다.
	// 남는 인원이 1명이면 파티를 해산한다 — 혼자짜리 파티는 파티가 아닌 것과 같다.
	// out_party_id 에는 (해산 여부와 무관하게) 떠난 파티의 id 가 담긴다.
	PartyResult Leave(long player_id, int* out_party_id = nullptr);

	PartyResult Kick(long leader_id, long target_id);
	PartyResult TransferLeader(long leader_id, long target_id);

	// 플레이어가 월드에서 사라질 때의 정리. 파티에서 빼고, 그가 주고받은 초대를 모두 지운다.
	// 초대는 id 만 들고 있어서 초대한 쪽이 사라진 것을 스스로 알 수 없다 — 지우지 않으면
	// 없는 사람의 파티에 가입하는 수락이 남는다.
	void RemovePlayer(long player_id);

	// 초대 만료 처리. 월드 틱에서 호출한다.
	void Update(float dt);

	// 서버 종료/테스트용. 모든 파티와 초대를 지운다.
	void Clear();

	size_t GetPartyCount() const { return parties_.size(); }
	size_t GetInviteCount() const { return invites_.size(); }

private:
	PartyManager() = default;

	// 파티를 해산하고 색인을 정리한다.
	void disband(Party* party);

	std::unordered_map<int, std::unique_ptr<Party>> parties_;
	std::unordered_map<long, int> playerToParty_;
	std::unordered_map<long, PartyInvite> invites_;

	int nextPartyId_ = 1;
};
