#include "PartyManager.h"
#include "PartyPolicy.h"

Party* PartyManager::Find(int party_id)
{
	auto it = parties_.find(party_id);
	return it != parties_.end() ? it->second.get() : nullptr;
}

Party* PartyManager::FindByPlayer(long player_id)
{
	auto it = playerToParty_.find(player_id);
	if (it == playerToParty_.end())
		return nullptr;
	return Find(it->second);
}

const Party* PartyManager::FindByPlayer(long player_id) const
{
	auto it = playerToParty_.find(player_id);
	if (it == playerToParty_.end())
		return nullptr;
	auto party = parties_.find(it->second);
	return party != parties_.end() ? party->second.get() : nullptr;
}

const PartyInvite* PartyManager::GetInvite(long invitee_id) const
{
	auto it = invites_.find(invitee_id);
	return it != invites_.end() ? &it->second : nullptr;
}

PartyResult PartyManager::Invite(long inviter_id, long invitee_id)
{
	if (inviter_id == invitee_id)
		return PartyResult::SelfTarget;

	Party* party = FindByPlayer(inviter_id);
	if (party != nullptr)
	{
		// 파티가 있으면 리더만 부를 수 있다. 아무나 부를 수 있으면 리더가 모르는 사이에
		// 자리가 차서, 정작 부르려던 사람을 못 부르는 일이 생긴다.
		if (!party->IsLeader(inviter_id))
			return PartyResult::NotLeader;

		if (static_cast<int>(party->Size()) >= PartyPolicy::Instance().GetMaxMembers())
			return PartyResult::PartyFull;
	}

	if (FindByPlayer(invitee_id) != nullptr)
		return PartyResult::TargetInParty;

	// 한 사람이 동시에 여러 초대를 들고 있으면 어느 쪽을 수락한 것인지 모호해진다.
	// 먼저 온 초대가 만료되거나 답해질 때까지 뒤의 초대는 거절한다.
	if (invites_.find(invitee_id) != invites_.end())
		return PartyResult::AlreadyInvited;

	PartyInvite invite;
	invite.party_id = party != nullptr ? party->GetId() : 0;
	invite.inviter_id = inviter_id;
	invite.remaining_sec = PartyPolicy::Instance().GetOfferTimeoutSec();
	invites_[invitee_id] = invite;

	return PartyResult::Ok;
}

PartyResult PartyManager::AcceptInvite(long invitee_id, Party** out_party)
{
	auto it = invites_.find(invitee_id);
	if (it == invites_.end())
		return PartyResult::NoInvite;

	const PartyInvite invite = it->second;

	if (FindByPlayer(invitee_id) != nullptr)
	{
		invites_.erase(it);
		return PartyResult::AlreadyInParty;
	}

	// 초대를 받은 뒤 답하기까지 사이에 초대한 쪽의 사정이 바뀔 수 있다(해산했거나,
	// 다른 파티에 들어갔거나). 초대에 적힌 party_id 를 믿지 않고 지금 상태를 다시 본다.
	Party* party = FindByPlayer(invite.inviter_id);
	if (party != nullptr)
	{
		if (static_cast<int>(party->Size()) >= PartyPolicy::Instance().GetMaxMembers())
		{
			invites_.erase(it);
			return PartyResult::PartyFull;
		}
		party->AddMember(invitee_id);
	}
	else
	{
		const int party_id = nextPartyId_++;
		auto created = std::unique_ptr<Party>(new Party(party_id, invite.inviter_id));
		created->AddMember(invitee_id);
		party = created.get();
		parties_[party_id] = std::move(created);
		playerToParty_[invite.inviter_id] = party_id;
	}

	playerToParty_[invitee_id] = party->GetId();
	invites_.erase(it);

	if (out_party != nullptr)
		*out_party = party;

	return PartyResult::Ok;
}

PartyResult PartyManager::DeclineInvite(long invitee_id)
{
	auto it = invites_.find(invitee_id);
	if (it == invites_.end())
		return PartyResult::NoInvite;

	invites_.erase(it);
	return PartyResult::Ok;
}

PartyResult PartyManager::Leave(long player_id, int* out_party_id)
{
	Party* party = FindByPlayer(player_id);
	if (party == nullptr)
		return PartyResult::NotInParty;

	if (out_party_id != nullptr)
		*out_party_id = party->GetId();

	party->RemoveMember(player_id);
	playerToParty_.erase(player_id);

	if (party->Size() <= 1)
	{
		disband(party);
		return PartyResult::Ok;
	}

	// 리더가 나갔으면 남은 사람 중 가장 오래된 멤버가 이어받는다. 리더 없는 파티는
	// 초대도 추방도 할 수 없어 아무것도 못 하는 상태가 된다.
	if (party->GetLeaderId() == player_id)
		party->SetLeader(party->GetMembers().front());

	return PartyResult::Ok;
}

PartyResult PartyManager::Kick(long leader_id, long target_id)
{
	if (leader_id == target_id)
		return PartyResult::SelfTarget;

	Party* party = FindByPlayer(leader_id);
	if (party == nullptr)
		return PartyResult::NotInParty;
	if (!party->IsLeader(leader_id))
		return PartyResult::NotLeader;
	if (!party->HasMember(target_id))
		return PartyResult::NotMember;

	party->RemoveMember(target_id);
	playerToParty_.erase(target_id);

	if (party->Size() <= 1)
		disband(party);

	return PartyResult::Ok;
}

PartyResult PartyManager::TransferLeader(long leader_id, long target_id)
{
	if (leader_id == target_id)
		return PartyResult::SelfTarget;

	Party* party = FindByPlayer(leader_id);
	if (party == nullptr)
		return PartyResult::NotInParty;
	if (!party->IsLeader(leader_id))
		return PartyResult::NotLeader;
	if (!party->HasMember(target_id))
		return PartyResult::NotMember;

	party->SetLeader(target_id);
	return PartyResult::Ok;
}

void PartyManager::RemovePlayer(long player_id)
{
	Leave(player_id);

	invites_.erase(player_id);

	// 이 사람이 보낸 초대도 지운다. 수락해 봐야 없는 사람의 파티에 들어가게 된다.
	for (auto it = invites_.begin(); it != invites_.end();)
	{
		if (it->second.inviter_id == player_id)
			it = invites_.erase(it);
		else
			++it;
	}
}

void PartyManager::Update(float dt)
{
	if (invites_.empty())
		return;

	for (auto it = invites_.begin(); it != invites_.end();)
	{
		it->second.remaining_sec -= dt;
		if (it->second.remaining_sec <= 0.0f)
			it = invites_.erase(it);
		else
			++it;
	}
}

void PartyManager::Clear()
{
	parties_.clear();
	playerToParty_.clear();
	invites_.clear();
	nextPartyId_ = 1;
}

void PartyManager::disband(Party* party)
{
	if (party == nullptr)
		return;

	const int party_id = party->GetId();

	// 남아 있는 멤버(해산 직전이면 0~1명)의 색인을 먼저 끊는다.
	for (long member_id : party->GetMembers())
	{
		auto it = playerToParty_.find(member_id);
		if (it != playerToParty_.end() && it->second == party_id)
			playerToParty_.erase(it);
	}

	// 이 파티로 보낸 초대는 갈 곳이 없어졌다. 수락 시점에 다시 확인하긴 하지만,
	// 그때 새 파티가 만들어져 초대한 쪽이 의도치 않게 파티장이 되는 것을 막는다.
	for (auto it = invites_.begin(); it != invites_.end();)
	{
		if (it->second.party_id == party_id)
			it = invites_.erase(it);
		else
			++it;
	}

	parties_.erase(party_id);
}
