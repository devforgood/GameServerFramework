#include "PlayerParty.h"
#include "Character.h"
#include "GameObject.h"
#include "Map.h"
#include "PartyPolicy.h"
#include "Player.h"
#include "PlayerLevel.h"
#include "PlayerQuest.h"
#include "PlayerSender.h"
#include "QuestRegistry.h"
#include "SendMessage.h"
#include "gamedata.h"
#include "syncnet_generated.h"

std::unordered_map<long, PlayerParty*>& PlayerParty::registry()
{
	// 함수 지역 정적으로 두어, 다른 번역 단위의 전역 초기화 순서와 무관하게 만든다.
	static std::unordered_map<long, PlayerParty*> instance;
	return instance;
}

PlayerParty::~PlayerParty()
{
	if (playerId_ == 0)
		return;

	// 접속이 끊기면 파티에서도 빠진다. 컴포넌트 수명에 묶어 두면 어느 경로로 사라지든
	// (정상 종료, 재접속 유예 만료, 서버 정리) 한 번은 반드시 정리된다.
	PartyManager::Instance().RemovePlayer(playerId_);

	auto it = registry().find(playerId_);
	if (it != registry().end() && it->second == this)
		registry().erase(it);
}

void PlayerParty::Bind(long player_id)
{
	if (playerId_ == player_id)
		return;

	if (playerId_ != 0)
	{
		auto old = registry().find(playerId_);
		if (old != registry().end() && old->second == this)
			registry().erase(old);
	}

	playerId_ = player_id;
	if (playerId_ != 0)
		registry()[playerId_] = this;
}

PlayerParty* PlayerParty::Find(long player_id)
{
	auto it = registry().find(player_id);
	return it != registry().end() ? it->second : nullptr;
}

void PlayerParty::Update(float dt)
{
	// 로스터 변경 감지. 파티가 해산되면 FindByPlayer 가 nullptr 이 되므로
	// "파티에서 빠짐"도 같은 비교로 잡힌다.
	const Party* party = GetParty();
	const int party_id = party != nullptr ? party->GetId() : 0;
	const uint32_t version = party != nullptr ? party->GetVersion() : 0;
	if (party_id != lastPartyId_ || version != lastVersion_)
	{
		lastPartyId_ = party_id;
		lastVersion_ = version;
		rosterDirty_ = true;
	}

	// 초대 도착/소멸 감지. 만료는 PartyManager 가 처리하고, 여기서는 그 결과만 본다.
	const PartyInvite* invite = GetPendingInvite();
	const long inviter = invite != nullptr ? invite->inviter_id : 0;
	if (inviter != notifiedInviter_)
	{
		notifiedInviter_ = inviter;
		inviteNotifyPending_ = inviter != 0;
	}

	// 공유 제안 만료.
	if (share_.quest_id != 0)
	{
		share_.remaining_sec -= dt;
		if (share_.remaining_sec <= 0.0f)
			share_ = QuestShareOffer{};
	}

	sendSync();
}

std::string PlayerParty::resolveName(long player_id)
{
	PlayerParty* member = Find(player_id);
	if (member == nullptr || member->game_object == nullptr)
		return std::string();

	auto* level = member->game_object->GetComponent<PlayerLevel>();
	return level != nullptr ? level->GetName() : std::string();
}

int PlayerParty::resolveLevel(long player_id)
{
	PlayerParty* member = Find(player_id);
	if (member == nullptr || member->game_object == nullptr)
		return 1;

	auto* level = member->game_object->GetComponent<PlayerLevel>();
	return level != nullptr ? level->GetLevel() : 1;
}

void PlayerParty::sendSync()
{
	auto* sender = game_object != nullptr ? game_object->GetComponent<PlayerSender>() : nullptr;
	if (sender == nullptr)
		return;

	// 받은 초대.
	PartyInvite invite;
	if (TakeInviteNotify(invite))
	{
		auto builder_ptr = std::make_shared<send_message>();
		auto name_offset = builder_ptr->CreateString(resolveName(invite.inviter_id));
		auto payload = syncnet::CreatePartyInvite(
			*builder_ptr,
			0 /* actorId: 서버 -> 클라 방향에서는 쓰지 않는다 */,
			invite.party_id,
			invite.inviter_id,
			name_offset,
			invite.remaining_sec);
		auto send_msg = syncnet::CreateGameMessage(
			*builder_ptr,
			syncnet::GameMessages::GameMessages_PartyInvite,
			payload.Union(),
			0,
			syncnet::StatusCode::StatusCode_Success);
		builder_ptr->Finish(send_msg);
		sender->Send(builder_ptr);
	}

	// 받은 퀘스트 공유 제안.
	QuestShareOffer offer;
	if (TakeShareNotify(offer))
	{
		auto builder_ptr = std::make_shared<send_message>();
		auto name_offset = builder_ptr->CreateString(resolveName(offer.from_player_id));
		auto payload = syncnet::CreatePartyQuestShare(
			*builder_ptr,
			offer.quest_id,
			offer.from_player_id,
			name_offset,
			offer.remaining_sec);
		auto send_msg = syncnet::CreateGameMessage(
			*builder_ptr,
			syncnet::GameMessages::GameMessages_PartyQuestShare,
			payload.Union(),
			0,
			syncnet::StatusCode::StatusCode_Success);
		builder_ptr->Finish(send_msg);
		sender->Send(builder_ptr);
	}

	if (!rosterDirty_)
		return;

	rosterDirty_ = false;

	// 파티가 없으면 partyId 0 짜리 빈 로스터를 보낸다. 탈퇴/추방/해산이 모두 이 형태로
	// 전달되므로, 클라는 사유별 메시지를 따로 다루지 않고 이것만 보면 된다.
	const Party* roster = GetParty();

	// 캐릭터(actor id, 맵)는 컴포넌트 층에서 볼 수 없다. 소유 Player 가 있을 때만 채우고,
	// 없으면 0 으로 둔다 — 이름/레벨은 그래도 정상적으로 나간다.
	Player* owner = sender->GetOwner();
	int myMapId = 0;
	if (owner != nullptr)
	{
		auto& self = owner->GetCharacter();
		if (self != nullptr && self->GetMap() != nullptr)
			myMapId = self->GetMap()->GetMapId();
	}

	auto builder_ptr = std::make_shared<send_message>();
	std::vector<flatbuffers::Offset<syncnet::PartyMemberInfo>> members;

	if (roster != nullptr)
	{
		members.reserve(roster->Size());
		for (long member_id : roster->GetMembers())
		{
			// 다른 맵에 있는 파티원의 actorId 는 이 클라에 존재하지 않는 액터라 0 으로 둔다.
			int actorId = 0;
			int mapId = 0;
			if (owner != nullptr)
			{
				if (auto member = owner->FindPlayerInWorld(member_id))
				{
					if (auto& character = member->GetCharacter())
					{
						if (character->GetMap() != nullptr)
							mapId = character->GetMap()->GetMapId();
						if (mapId != 0 && mapId == myMapId)
							actorId = character->GetActorId();
					}
				}
			}

			members.push_back(syncnet::CreatePartyMemberInfo(
				*builder_ptr,
				member_id,
				builder_ptr->CreateString(resolveName(member_id)),
				resolveLevel(member_id),
				actorId,
				mapId));
		}
	}

	auto payload = syncnet::CreatePartySync(
		*builder_ptr,
		roster != nullptr ? roster->GetId() : 0,
		roster != nullptr ? roster->GetLeaderId() : 0,
		builder_ptr->CreateVector(members));

	auto send_msg = syncnet::CreateGameMessage(
		*builder_ptr,
		syncnet::GameMessages::GameMessages_PartySync,
		payload.Union(),
		0 /* 요청/응답 짝이 아니라 알림이므로 0 */,
		syncnet::StatusCode::StatusCode_Success);
	builder_ptr->Finish(send_msg);

	sender->Send(builder_ptr);
}

const Party* PlayerParty::GetParty() const
{
	if (playerId_ == 0)
		return nullptr;
	return PartyManager::Instance().FindByPlayer(playerId_);
}

bool PlayerParty::IsLeader() const
{
	const Party* party = GetParty();
	return party != nullptr && party->IsLeader(playerId_);
}

const PartyInvite* PlayerParty::GetPendingInvite() const
{
	if (playerId_ == 0)
		return nullptr;
	return PartyManager::Instance().GetInvite(playerId_);
}

PartyResult PlayerParty::Invite(long target_player_id)
{
	// 등록되지 않은 대상은 월드에 없는 사람이다. PartyManager 는 id 만 보므로
	// 존재 여부는 여기서 확인한다.
	if (Find(target_player_id) == nullptr)
		return PartyResult::TargetNotFound;

	return PartyManager::Instance().Invite(playerId_, target_player_id);
}

PartyResult PlayerParty::RespondInvite(bool accept)
{
	if (!accept)
		return PartyManager::Instance().DeclineInvite(playerId_);

	return PartyManager::Instance().AcceptInvite(playerId_);
}

PartyResult PlayerParty::Leave()
{
	return PartyManager::Instance().Leave(playerId_);
}

PartyResult PlayerParty::Kick(long target_player_id)
{
	return PartyManager::Instance().Kick(playerId_, target_player_id);
}

PartyResult PlayerParty::TransferLeader(long target_player_id)
{
	return PartyManager::Instance().TransferLeader(playerId_, target_player_id);
}

PartyResult PlayerParty::ShareQuest(int quest_id, int* out_offer_count)
{
	if (out_offer_count != nullptr)
		*out_offer_count = 0;

	const Party* party = GetParty();
	if (party == nullptr)
		return PartyResult::NotInParty;

	auto* quests = game_object != nullptr ? game_object->GetComponent<PlayerQuest>() : nullptr;
	if (quests == nullptr || !quests->IsActive(quest_id))
		return PartyResult::QuestNotActive;

	const Quest* definition = QuestRegistry::Instance().Get(quest_id);
	if (definition == nullptr || definition->gamedata == nullptr)
		return PartyResult::QuestNotActive;

	if (!definition->gamedata->shareable)
		return PartyResult::QuestNotShareable;

	int offered = 0;
	for (long member_id : party->GetMembers())
	{
		if (member_id == playerId_)
			continue;

		PlayerParty* member = Find(member_id);
		if (member == nullptr || member->game_object == nullptr)
			continue;

		auto* member_quests = member->game_object->GetComponent<PlayerQuest>();
		if (member_quests == nullptr)
			continue;

		// 받을 수 없는 사람에게 창을 띄우지 않는다. 조건은 실제 수락과 같은 것을 본다.
		if (definition->CanAccept(*member_quests) != QuestAcceptResult::Ok)
			continue;

		if (member->offerShare(quest_id, playerId_))
			++offered;
	}

	if (out_offer_count != nullptr)
		*out_offer_count = offered;

	return PartyResult::Ok;
}

bool PlayerParty::offerShare(int quest_id, long from_player_id)
{
	// 답하지 않은 제안이 있으면 덮어쓰지 않는다. 여러 명이 동시에 공유를 눌렀을 때
	// 마지막 사람 것으로 바뀌면, 클라가 띄운 창과 서버가 아는 제안이 어긋난다.
	if (share_.quest_id != 0)
		return false;

	share_.quest_id = quest_id;
	share_.from_player_id = from_player_id;
	share_.remaining_sec = PartyPolicy::Instance().GetOfferTimeoutSec();
	shareNotifyPending_ = true;
	return true;
}

PartyResult PlayerParty::RespondShare(int quest_id, bool accept, QuestAcceptResult* out_accept)
{
	if (share_.quest_id == 0 || share_.quest_id != quest_id)
		return PartyResult::NoOffer;

	share_ = QuestShareOffer{};
	shareNotifyPending_ = false;

	if (!accept)
		return PartyResult::Ok;

	auto* quests = game_object != nullptr ? game_object->GetComponent<PlayerQuest>() : nullptr;
	if (quests == nullptr)
		return PartyResult::QuestNotActive;

	const QuestAcceptResult result = quests->AcceptQuest(quest_id);
	if (out_accept != nullptr)
		*out_accept = result;

	// 제안을 받은 뒤 답하기까지 사이에 조건이 깨졌을 수 있다(레벨이 아니라 선행 퀘스트를
	// 포기했다든가). 파티 조작 자체는 성공했고, 수락 여부는 out_accept 로 구분한다.
	return PartyResult::Ok;
}

bool PlayerParty::TakeInviteNotify(PartyInvite& out)
{
	if (!inviteNotifyPending_)
		return false;

	const PartyInvite* invite = GetPendingInvite();
	if (invite == nullptr)
	{
		inviteNotifyPending_ = false;
		return false;
	}

	out = *invite;
	inviteNotifyPending_ = false;
	return true;
}

bool PlayerParty::TakeShareNotify(QuestShareOffer& out)
{
	if (!shareNotifyPending_ || share_.quest_id == 0)
	{
		shareNotifyPending_ = false;
		return false;
	}

	out = share_;
	shareNotifyPending_ = false;
	return true;
}
