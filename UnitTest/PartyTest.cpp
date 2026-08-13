#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include "GameObject.h"
#include "Party.h"
#include "PartyManager.h"
#include "PartyPolicy.h"
#include "PlayerEventBroker.h"
#include "PlayerItem.h"
#include "PlayerLevel.h"
#include "PlayerLoadData.h"
#include "PlayerParty.h"
#include "PlayerQuest.h"
#include "PlayerSender.h"
#include "PlayerSkill.h"
#include "PlayerWallet.h"
#include "QuestRegistry.h"
#include "GameData/ResourceLoader.h"
#include "SendMessage.h"
#include "syncnet_generated.h"

// 파티는 두 층으로 나뉘어 있고 여기서도 그대로 나눠 검증한다.
//
//   PartyManager : 플레이어를 id 로만 다루는 멤버십 규칙. 월드도 네트워크도 없이 검증된다.
//   PlayerParty  : 그 규칙을 실제 플레이어(컴포넌트)에 붙이는 층. 클라 동기화 플래그와
//                  퀘스트 공유가 여기 있다.
//
// 퀘스트 공유는 진짜 quest.json 을 읽는다(1003 은 shareable, 1005/3001 은 아니다).

namespace
{

constexpr int kWolfPelt = 1003;      // SubQuest, shareable, min_level 2, 선행 없음
constexpr int kFirstSteps = 1005;    // SubQuest, shareable = false
constexpr int kGoblinHunt = 1001;    // MainQuest, shareable, min_level 3

void EnsureResources()
{
	const std::string& path = GameDataPath::Resolve();
	ASSERT_TRUE(std::filesystem::exists(path + "quest.json"))
		<< "통합 GameData 폴더를 찾지 못했습니다: " << path;
	ASSERT_TRUE(ResourceLoader::Instance().LoadResources(path)) << "LoadResources 실패";
	QuestRegistry::Instance().Clear();
}

// 파티 조작에 필요한 만큼만 갖춘 플레이어. 실제 Player 는 세션/월드를 요구하므로
// 같은 컴포넌트 구성을 가진 GameObject 로 대신한다.
struct TestPlayer
{
	GameObject go;
	PlayerParty* party = nullptr;
	PlayerQuest* quests = nullptr;
	PlayerLevel* level = nullptr;

	// 세션 대신 이 플레이어가 클라로 보낸 메시지가 쌓인다.
	std::vector<std::shared_ptr<send_message>> sent;

	TestPlayer(long player_id, int character_id, int player_level, const char* name = "Tester")
	{
		// 브로커를 먼저 붙여야 뒤따르는 컴포넌트들이 Start 에서 구독할 수 있다.
		go.AddComponent<PlayerEventBroker>();
		quests = go.AddComponent<PlayerQuest>();
		go.AddComponent<PlayerItem>();
		go.AddComponent<PlayerSkill>();
		go.AddComponent<PlayerWallet>();
		level = go.AddComponent<PlayerLevel>();
		party = go.AddComponent<PlayerParty>();
		party->Bind(player_id);

		// 실제 세션 없이 와이어 포맷을 검증하기 위해 전송을 가로챈다.
		go.AddComponent<PlayerSender>()->SetSink(
			[this](std::shared_ptr<send_message>& msg) { sent.push_back(msg); });

		PlayerLoadData data{};
		data.player.id = character_id;
		data.player.name = name;
		data.player.level = player_level;
		go.ForEachComponent([&data](Component& component) { component.Load(data); });
	}

	void Tick(float dt) { go.Update(dt); }

	// 보낸 메시지 중 마지막 것을 종류로 찾는다. 없으면 nullptr.
	const syncnet::GameMessage* LastMessage(syncnet::GameMessages type) const
	{
		for (auto it = sent.rbegin(); it != sent.rend(); ++it)
		{
			const auto* msg = syncnet::GetGameMessage((*it)->GetBufferPointer());
			if (msg != nullptr && msg->msg_type() == type)
				return msg;
		}
		return nullptr;
	}
};

class PartyTest : public ::testing::Test
{
protected:
	void SetUp() override
	{
		PartyManager::Instance().Clear();
		PartyPolicy::Instance().Reset();
	}

	void TearDown() override
	{
		PartyManager::Instance().Clear();
		PartyPolicy::Instance().Reset();
	}
};

// 퀘스트 데이터가 필요한 테스트용.
class PartyQuestShareTest : public PartyTest
{
protected:
	void SetUp() override
	{
		PartyTest::SetUp();
		EnsureResources();
	}
};

} // namespace

// ============================================================
// PartyManager - 결성
// ============================================================

TEST_F(PartyTest, InviteThenAccept_CreatesParty)
{
	auto& manager = PartyManager::Instance();

	// 초대 시점에는 아직 파티가 없다. 거절당했을 때 혼자짜리 파티가 남지 않도록
	// 수락하는 순간에 만든다.
	EXPECT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	EXPECT_EQ(0u, manager.GetPartyCount());
	EXPECT_EQ(nullptr, manager.FindByPlayer(1));

	EXPECT_EQ(PartyResult::Ok, manager.AcceptInvite(2));

	Party* party = manager.FindByPlayer(1);
	ASSERT_NE(nullptr, party);
	EXPECT_EQ(party, manager.FindByPlayer(2));
	EXPECT_EQ(2u, party->Size());
	EXPECT_EQ(1, party->GetLeaderId());
	EXPECT_TRUE(party->IsLeader(1));
	EXPECT_FALSE(party->IsLeader(2));
	EXPECT_EQ(0u, manager.GetInviteCount());
}

TEST_F(PartyTest, DeclineInvite_LeavesNoParty)
{
	auto& manager = PartyManager::Instance();

	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	EXPECT_EQ(PartyResult::Ok, manager.DeclineInvite(2));

	EXPECT_EQ(0u, manager.GetPartyCount());
	EXPECT_EQ(0u, manager.GetInviteCount());
	EXPECT_EQ(PartyResult::NoInvite, manager.AcceptInvite(2));
}

TEST_F(PartyTest, InviteSelf_Rejected)
{
	EXPECT_EQ(PartyResult::SelfTarget, PartyManager::Instance().Invite(1, 1));
}

TEST_F(PartyTest, InviteAlreadyPartiedTarget_Rejected)
{
	auto& manager = PartyManager::Instance();
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));

	EXPECT_EQ(PartyResult::TargetInParty, manager.Invite(3, 2));
}

TEST_F(PartyTest, SecondInviteToSameTarget_Rejected)
{
	auto& manager = PartyManager::Instance();
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 3));

	// 두 개를 동시에 들고 있으면 "수락"이 어느 쪽인지 모호해진다.
	EXPECT_EQ(PartyResult::AlreadyInvited, manager.Invite(2, 3));
}

TEST_F(PartyTest, NonLeaderCannotInvite)
{
	auto& manager = PartyManager::Instance();
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));

	EXPECT_EQ(PartyResult::NotLeader, manager.Invite(2, 3));
	EXPECT_EQ(PartyResult::Ok, manager.Invite(1, 3));
}

TEST_F(PartyTest, InviteBeyondMaxMembers_Rejected)
{
	auto& manager = PartyManager::Instance();
	PartyPolicy::Instance().SetMaxMembers(3);

	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 3));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(3));

	ASSERT_EQ(3u, manager.FindByPlayer(1)->Size());
	EXPECT_EQ(PartyResult::PartyFull, manager.Invite(1, 4));
}

TEST_F(PartyTest, PartyFilledBetweenInviteAndAccept_RejectsAccept)
{
	auto& manager = PartyManager::Instance();
	PartyPolicy::Instance().SetMaxMembers(2);

	// 1이 2와 3을 모두 부르는 상황은 만들 수 없으므로(정원 검사가 초대에서 걸린다),
	// 정원을 넉넉히 둔 채 두 명을 부르고 나서 정원을 줄인다. 초대 시점의 판정을
	// 그대로 믿으면 정원을 넘겨 가입시키게 된다.
	PartyPolicy::Instance().SetMaxMembers(5);
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 3));

	PartyPolicy::Instance().SetMaxMembers(2);
	EXPECT_EQ(PartyResult::PartyFull, manager.AcceptInvite(3));
	EXPECT_EQ(2u, manager.FindByPlayer(1)->Size());
	EXPECT_EQ(nullptr, manager.FindByPlayer(3));
}

TEST_F(PartyTest, InviteExpiresAfterTimeout)
{
	auto& manager = PartyManager::Instance();
	PartyPolicy::Instance().SetOfferTimeoutSec(1.0f);

	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_NE(nullptr, manager.GetInvite(2));

	manager.Update(0.5f);
	EXPECT_NE(nullptr, manager.GetInvite(2));

	manager.Update(0.6f);
	EXPECT_EQ(nullptr, manager.GetInvite(2));
	EXPECT_EQ(PartyResult::NoInvite, manager.AcceptInvite(2));
}

TEST_F(PartyTest, AcceptInvite_UsesInvitersCurrentParty)
{
	auto& manager = PartyManager::Instance();

	// 1이 3을 부른 뒤, 답하기 전에 1이 2와 파티를 맺었다. 3은 초대 시점의 상태(파티 없음)가
	// 아니라 지금의 파티에 들어가야 한다.
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 3));
	ASSERT_EQ(PartyResult::Ok, manager.Invite(2, 1));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(1));

	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(3));

	Party* party = manager.FindByPlayer(3);
	ASSERT_NE(nullptr, party);
	EXPECT_EQ(3u, party->Size());
	EXPECT_EQ(2, party->GetLeaderId()); // 1을 부른 2가 리더
	EXPECT_EQ(1u, manager.GetPartyCount());
}

// ============================================================
// PartyManager - 탈퇴/추방/리더
// ============================================================

TEST_F(PartyTest, LeaveWithThreeMembers_KeepsParty)
{
	auto& manager = PartyManager::Instance();
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 3));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(3));

	EXPECT_EQ(PartyResult::Ok, manager.Leave(3));

	Party* party = manager.FindByPlayer(1);
	ASSERT_NE(nullptr, party);
	EXPECT_EQ(2u, party->Size());
	EXPECT_FALSE(party->HasMember(3));
	EXPECT_EQ(nullptr, manager.FindByPlayer(3));
}

TEST_F(PartyTest, LeaderLeaves_LeadershipPassesToOldestMember)
{
	auto& manager = PartyManager::Instance();
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 3));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(3));

	ASSERT_EQ(PartyResult::Ok, manager.Leave(1));

	Party* party = manager.FindByPlayer(2);
	ASSERT_NE(nullptr, party);
	EXPECT_EQ(2, party->GetLeaderId());
}

TEST_F(PartyTest, LeavingDownToOneMember_Disbands)
{
	auto& manager = PartyManager::Instance();
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));

	// 혼자 남은 파티는 파티가 아닌 것과 같다. 남겨 두면 "파티 중"인데 아무것도 못 하는
	// 상태가 되고, 다른 파티의 초대도 TargetInParty 로 거절된다.
	EXPECT_EQ(PartyResult::Ok, manager.Leave(2));
	EXPECT_EQ(0u, manager.GetPartyCount());
	EXPECT_EQ(nullptr, manager.FindByPlayer(1));
	EXPECT_EQ(nullptr, manager.FindByPlayer(2));
}

TEST_F(PartyTest, LeaveWithoutParty_Rejected)
{
	EXPECT_EQ(PartyResult::NotInParty, PartyManager::Instance().Leave(1));
}

TEST_F(PartyTest, KickRequiresLeadership)
{
	auto& manager = PartyManager::Instance();
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 3));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(3));

	EXPECT_EQ(PartyResult::NotLeader, manager.Kick(2, 3));
	EXPECT_EQ(PartyResult::SelfTarget, manager.Kick(1, 1));
	EXPECT_EQ(PartyResult::NotMember, manager.Kick(1, 99));

	EXPECT_EQ(PartyResult::Ok, manager.Kick(1, 3));
	EXPECT_EQ(nullptr, manager.FindByPlayer(3));
	EXPECT_EQ(2u, manager.FindByPlayer(1)->Size());
}

TEST_F(PartyTest, TransferLeader)
{
	auto& manager = PartyManager::Instance();
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));

	EXPECT_EQ(PartyResult::NotMember, manager.TransferLeader(1, 99));
	EXPECT_EQ(PartyResult::Ok, manager.TransferLeader(1, 2));

	Party* party = manager.FindByPlayer(1);
	ASSERT_NE(nullptr, party);
	EXPECT_EQ(2, party->GetLeaderId());

	// 넘긴 쪽은 더 이상 리더 조작을 할 수 없다.
	EXPECT_EQ(PartyResult::NotLeader, manager.Kick(1, 2));
}

TEST_F(PartyTest, RemovePlayer_ClearsPartyAndInvites)
{
	auto& manager = PartyManager::Instance();
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 3));

	manager.RemovePlayer(1);

	// 초대한 사람이 사라졌으므로 그 초대는 갈 곳이 없다.
	EXPECT_EQ(nullptr, manager.GetInvite(3));
	EXPECT_EQ(0u, manager.GetPartyCount());
}

TEST_F(PartyTest, VersionChangesOnRosterChange)
{
	auto& manager = PartyManager::Instance();
	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 2));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(2));

	Party* party = manager.FindByPlayer(1);
	ASSERT_NE(nullptr, party);
	const uint32_t after_join = party->GetVersion();

	ASSERT_EQ(PartyResult::Ok, manager.Invite(1, 3));
	ASSERT_EQ(PartyResult::Ok, manager.AcceptInvite(3));
	const uint32_t after_second_join = party->GetVersion();
	EXPECT_NE(after_join, after_second_join);

	ASSERT_EQ(PartyResult::Ok, manager.TransferLeader(1, 2));
	EXPECT_NE(after_second_join, party->GetVersion());
}

// ============================================================
// PartyPolicy - 경험치 분배
// ============================================================

TEST_F(PartyTest, ShareExp_SoloGetsFullAmount)
{
	EXPECT_EQ(100, PartyPolicy::Instance().ShareExp(100, 1));
	EXPECT_EQ(100, PartyPolicy::Instance().ShareExp(100, 0));
}

TEST_F(PartyTest, ShareExp_SplitsWithBonus)
{
	auto& policy = PartyPolicy::Instance();
	policy.SetExpBonusPerMember(0.1);

	// 2명: 총 110 을 나눠 55. 혼자일 때(100)보다 1인분은 적지만 합은 많다.
	EXPECT_EQ(55, policy.ShareExp(100, 2));
	EXPECT_LT(policy.ShareExp(100, 2), policy.ShareExp(100, 1));
	EXPECT_GT(policy.ShareExp(100, 2) * 2, policy.ShareExp(100, 1));

	// 5명: 총 140 을 나눠 28.
	EXPECT_EQ(28, policy.ShareExp(100, 5));
}

TEST_F(PartyTest, ShareExp_NeverDropsToZero)
{
	// 인원이 많다고 0이 되면 파티가 벌칙이 된다.
	EXPECT_EQ(1, PartyPolicy::Instance().ShareExp(1, 5));
	EXPECT_EQ(0, PartyPolicy::Instance().ShareExp(0, 5));
}

TEST_F(PartyTest, MaxMembersIsClamped)
{
	auto& policy = PartyPolicy::Instance();
	policy.SetMaxMembers(1);
	EXPECT_EQ(2, policy.GetMaxMembers()); // 혼자짜리 파티는 만들 수 없다

	policy.SetMaxMembers(1000);
	EXPECT_GE(policy.GetMaxMembers(), 2);
	EXPECT_LE(policy.GetMaxMembers(), 12);
}

// ============================================================
// PlayerParty - 컴포넌트 층
// ============================================================

TEST_F(PartyTest, ComponentRegistersForLookup)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);

	EXPECT_EQ(alice.party, PlayerParty::Find(11));
	EXPECT_EQ(bob.party, PlayerParty::Find(12));
	EXPECT_EQ(nullptr, PlayerParty::Find(999));
}

TEST_F(PartyTest, InviteUnknownPlayer_Rejected)
{
	TestPlayer alice(11, 101, 10);

	// PartyManager 는 id 만 보므로 존재 여부는 컴포넌트 층에서 걸러야 한다.
	EXPECT_EQ(PartyResult::TargetNotFound, alice.party->Invite(999));
}

TEST_F(PartyTest, ComponentInviteFlow_SendsInviteThenRoster)
{
	TestPlayer alice(11, 101, 10, "Alice");
	TestPlayer bob(12, 102, 7, "Bob");

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));

	// 초대 알림은 받은 쪽의 틱에서 나간다.
	bob.Tick(0.1f);
	const syncnet::GameMessage* msg =
		bob.LastMessage(syncnet::GameMessages::GameMessages_PartyInvite);
	ASSERT_NE(nullptr, msg);
	const syncnet::PartyInvite* invite = msg->msg_as_PartyInvite();
	EXPECT_EQ(11, invite->inviterId());
	ASSERT_NE(nullptr, invite->inviterName());
	EXPECT_STREQ("Alice", invite->inviterName()->c_str());

	bob.sent.clear();
	bob.Tick(0.1f);
	// 같은 초대로 알림이 다시 나가면 클라에 창이 두 번 뜬다.
	EXPECT_EQ(nullptr, bob.LastMessage(syncnet::GameMessages::GameMessages_PartyInvite));

	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));

	alice.Tick(0.1f);
	bob.Tick(0.1f);
	EXPECT_TRUE(alice.party->IsLeader());
	EXPECT_FALSE(bob.party->IsLeader());

	msg = alice.LastMessage(syncnet::GameMessages::GameMessages_PartySync);
	ASSERT_NE(nullptr, msg);
	const syncnet::PartySync* roster = msg->msg_as_PartySync();
	EXPECT_NE(0, roster->partyId());
	EXPECT_EQ(11, roster->leaderId());
	ASSERT_NE(nullptr, roster->members());
	ASSERT_EQ(2u, roster->members()->size());

	// 이름/레벨은 상대의 컴포넌트에서 읽으므로 세션 없이도 채워진다.
	const syncnet::PartyMemberInfo* second = roster->members()->Get(1);
	EXPECT_EQ(12, second->playerId());
	ASSERT_NE(nullptr, second->name());
	EXPECT_STREQ("Bob", second->name()->c_str());
	EXPECT_EQ(7, second->level());

	alice.sent.clear();
	alice.Tick(0.1f);
	// 바뀐 것이 없으면 다시 보내지 않는다.
	EXPECT_EQ(nullptr, alice.LastMessage(syncnet::GameMessages::GameMessages_PartySync));
}

TEST_F(PartyTest, DisbandSendsEmptyRoster)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));
	alice.Tick(0.1f);
	alice.sent.clear();

	ASSERT_EQ(PartyResult::Ok, bob.party->Leave());

	// 2인 파티에서 한 명이 나가면 해산된다. 남은 쪽은 partyId 0 짜리 빈 로스터를 받는다.
	alice.Tick(0.1f);
	const syncnet::GameMessage* msg =
		alice.LastMessage(syncnet::GameMessages::GameMessages_PartySync);
	ASSERT_NE(nullptr, msg);
	EXPECT_EQ(0, msg->msg_as_PartySync()->partyId());
	EXPECT_EQ(0u, msg->msg_as_PartySync()->members()->size());
	EXPECT_FALSE(alice.party->IsInParty());
}

TEST_F(PartyTest, DestroyingComponentLeavesParty)
{
	TestPlayer alice(11, 101, 10);

	{
		TestPlayer bob(12, 102, 10);
		TestPlayer carol(13, 103, 10);
		ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
		ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));
		ASSERT_EQ(PartyResult::Ok, alice.party->Invite(13));
		ASSERT_EQ(PartyResult::Ok, carol.party->RespondInvite(true));
		ASSERT_EQ(3u, alice.party->GetParty()->Size());
	}

	// 접속이 끊기면(컴포넌트가 사라지면) 파티에서도 빠진다. 두 명이 한꺼번에 빠져
	// 혼자 남았으므로 파티는 해산된다.
	EXPECT_EQ(nullptr, PlayerParty::Find(12));
	EXPECT_FALSE(alice.party->IsInParty());
}

TEST_F(PartyTest, KickedMemberSeesRosterChange)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);
	TestPlayer carol(13, 103, 10);

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));
	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(13));
	ASSERT_EQ(PartyResult::Ok, carol.party->RespondInvite(true));

	carol.Tick(0.1f);
	carol.sent.clear();

	EXPECT_EQ(PartyResult::NotLeader, bob.party->Kick(13));
	ASSERT_EQ(PartyResult::Ok, alice.party->Kick(13));

	carol.Tick(0.1f);
	const syncnet::GameMessage* msg =
		carol.LastMessage(syncnet::GameMessages::GameMessages_PartySync);
	ASSERT_NE(nullptr, msg);
	EXPECT_EQ(0, msg->msg_as_PartySync()->partyId());
	EXPECT_FALSE(carol.party->IsInParty());
	EXPECT_EQ(2u, alice.party->GetParty()->Size());
}

TEST_F(PartyTest, QuestShareOfferIsSentToTarget)
{
	TestPlayer alice(11, 101, 10, "Alice");
	TestPlayer bob(12, 102, 10, "Bob");

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));

	EnsureResources();
	ASSERT_EQ(QuestAcceptResult::Ok, alice.quests->AcceptQuest(kWolfPelt));
	ASSERT_EQ(PartyResult::Ok, alice.party->ShareQuest(kWolfPelt));

	bob.Tick(0.1f);
	const syncnet::GameMessage* msg =
		bob.LastMessage(syncnet::GameMessages::GameMessages_PartyQuestShare);
	ASSERT_NE(nullptr, msg);
	const syncnet::PartyQuestShare* share = msg->msg_as_PartyQuestShare();
	EXPECT_EQ(kWolfPelt, share->questId());
	EXPECT_EQ(11, share->fromPlayerId());
	ASSERT_NE(nullptr, share->fromName());
	EXPECT_STREQ("Alice", share->fromName()->c_str());
}

// ============================================================
// 퀘스트 공유
// ============================================================

TEST_F(PartyQuestShareTest, ShareOffersToEligibleMembers)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));
	ASSERT_EQ(QuestAcceptResult::Ok, alice.quests->AcceptQuest(kWolfPelt));

	int offered = 0;
	EXPECT_EQ(PartyResult::Ok, alice.party->ShareQuest(kWolfPelt, &offered));
	EXPECT_EQ(1, offered);

	const PlayerParty::QuestShareOffer* offer = bob.party->GetPendingShare();
	ASSERT_NE(nullptr, offer);
	EXPECT_EQ(kWolfPelt, offer->quest_id);
	EXPECT_EQ(11, offer->from_player_id);

	// 공유는 조건을 면제해 주지 않는다 — 평소의 수락 경로를 그대로 탄다.
	EXPECT_EQ(PartyResult::Ok, bob.party->RespondShare(kWolfPelt, true));
	EXPECT_TRUE(bob.quests->IsActive(kWolfPelt));
	EXPECT_EQ(nullptr, bob.party->GetPendingShare());
}

TEST_F(PartyQuestShareTest, DeclinedShareDoesNotAcceptQuest)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));
	ASSERT_EQ(QuestAcceptResult::Ok, alice.quests->AcceptQuest(kWolfPelt));
	ASSERT_EQ(PartyResult::Ok, alice.party->ShareQuest(kWolfPelt));

	EXPECT_EQ(PartyResult::Ok, bob.party->RespondShare(kWolfPelt, false));
	EXPECT_FALSE(bob.quests->IsActive(kWolfPelt));
	EXPECT_EQ(nullptr, bob.party->GetPendingShare());

	// 이미 답한 제안에는 다시 답할 수 없다.
	EXPECT_EQ(PartyResult::NoOffer, bob.party->RespondShare(kWolfPelt, true));
}

TEST_F(PartyQuestShareTest, NonShareableQuestRejected)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));
	ASSERT_EQ(QuestAcceptResult::Ok, alice.quests->AcceptQuest(kFirstSteps));

	EXPECT_EQ(PartyResult::QuestNotShareable, alice.party->ShareQuest(kFirstSteps));
	EXPECT_EQ(nullptr, bob.party->GetPendingShare());
}

TEST_F(PartyQuestShareTest, ShareRequiresActiveQuestAndParty)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);

	// 파티가 없으면 공유할 곳이 없다.
	ASSERT_EQ(QuestAcceptResult::Ok, alice.quests->AcceptQuest(kWolfPelt));
	EXPECT_EQ(PartyResult::NotInParty, alice.party->ShareQuest(kWolfPelt));

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));

	// 진행 중이 아닌 퀘스트는 공유할 수 없다.
	EXPECT_EQ(PartyResult::QuestNotActive, alice.party->ShareQuest(kGoblinHunt));
}

TEST_F(PartyQuestShareTest, IneligibleMemberGetsNoOffer)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 1); // min_level 2 미달

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));
	ASSERT_EQ(QuestAcceptResult::Ok, alice.quests->AcceptQuest(kWolfPelt));

	// 받을 수 없는 사람에게 창을 띄우지 않는다. 제안이 0건이어도 조작 자체는 성공이다.
	int offered = -1;
	EXPECT_EQ(PartyResult::Ok, alice.party->ShareQuest(kWolfPelt, &offered));
	EXPECT_EQ(0, offered);
	EXPECT_EQ(nullptr, bob.party->GetPendingShare());
}

TEST_F(PartyQuestShareTest, MemberAlreadyOnQuestGetsNoOffer)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));
	ASSERT_EQ(QuestAcceptResult::Ok, alice.quests->AcceptQuest(kWolfPelt));
	ASSERT_EQ(QuestAcceptResult::Ok, bob.quests->AcceptQuest(kWolfPelt));

	int offered = -1;
	EXPECT_EQ(PartyResult::Ok, alice.party->ShareQuest(kWolfPelt, &offered));
	EXPECT_EQ(0, offered);
}

TEST_F(PartyQuestShareTest, PendingOfferIsNotOverwritten)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);
	TestPlayer carol(13, 103, 10);

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));
	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(13));
	ASSERT_EQ(PartyResult::Ok, carol.party->RespondInvite(true));

	ASSERT_EQ(QuestAcceptResult::Ok, alice.quests->AcceptQuest(kWolfPelt));
	ASSERT_EQ(QuestAcceptResult::Ok, carol.quests->AcceptQuest(kGoblinHunt));

	ASSERT_EQ(PartyResult::Ok, alice.party->ShareQuest(kWolfPelt));
	ASSERT_NE(nullptr, bob.party->GetPendingShare());

	// 답하지 않은 제안이 있는 동안 다른 공유가 덮어쓰면, 클라가 띄운 창과 서버가 아는
	// 제안이 어긋난다. bob 은 건너뛰고 alice 에게만 간다.
	int offered = -1;
	ASSERT_EQ(PartyResult::Ok, carol.party->ShareQuest(kGoblinHunt, &offered));
	EXPECT_EQ(1, offered);
	EXPECT_EQ(kWolfPelt, bob.party->GetPendingShare()->quest_id);

	ASSERT_NE(nullptr, alice.party->GetPendingShare());
	EXPECT_EQ(kGoblinHunt, alice.party->GetPendingShare()->quest_id);
}

TEST_F(PartyQuestShareTest, OfferExpires)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);
	PartyPolicy::Instance().SetOfferTimeoutSec(1.0f);

	ASSERT_EQ(PartyResult::Ok, alice.party->Invite(12));
	ASSERT_EQ(PartyResult::Ok, bob.party->RespondInvite(true));
	ASSERT_EQ(QuestAcceptResult::Ok, alice.quests->AcceptQuest(kWolfPelt));
	ASSERT_EQ(PartyResult::Ok, alice.party->ShareQuest(kWolfPelt));

	bob.Tick(0.5f);
	EXPECT_NE(nullptr, bob.party->GetPendingShare());

	bob.Tick(0.6f);
	EXPECT_EQ(nullptr, bob.party->GetPendingShare());
	EXPECT_EQ(PartyResult::NoOffer, bob.party->RespondShare(kWolfPelt, true));
	EXPECT_FALSE(bob.quests->IsActive(kWolfPelt));
}

// ============================================================
// 킬 크레딧이 퀘스트/경험치에 미치는 영향
// ============================================================
// 반경/맵 판정은 Map 과 Character 가 필요해 여기서 다루지 않는다(party_credit 이 맡는다).
// 여기서는 그 결과로 발행되는 이벤트가 각 컴포넌트에 어떻게 반영되는지를 본다.

TEST_F(PartyQuestShareTest, SharedKillCountsFullyForEachMemberQuest)
{
	TestPlayer alice(11, 101, 10);
	TestPlayer bob(12, 102, 10);

	ASSERT_EQ(QuestAcceptResult::Ok, alice.quests->AcceptQuest(kGoblinHunt));
	ASSERT_EQ(QuestAcceptResult::Ok, bob.quests->AcceptQuest(kGoblinHunt));

	// 퀘스트 목표는 나눠 갖지 않는다. 파티로 잡아도 각자 1마리로 센다 —
	// 그러지 않으면 파티 사냥이 퀘스트 진행에는 손해가 된다.
	constexpr int kGoblinMonsterId = 3;
	alice.go.GetComponent<PlayerEventBroker>()->publish(
		EventActorDead{ 1, 500, kGoblinMonsterId, 2 });
	bob.go.GetComponent<PlayerEventBroker>()->publish(
		EventActorDead{ 1, 500, kGoblinMonsterId, 2 });

	// 1001 은 1스테이지가 대화라 처치는 아직 세지 않는다. 스테이지를 넘겨 놓고 확인한다.
	ASSERT_TRUE(alice.quests->GmSetProgress(kGoblinHunt, 2, 0, 0, 0));
	ASSERT_TRUE(bob.quests->GmSetProgress(kGoblinHunt, 2, 0, 0, 0));

	alice.go.GetComponent<PlayerEventBroker>()->publish(
		EventActorDead{ 1, 501, kGoblinMonsterId, 2 });
	bob.go.GetComponent<PlayerEventBroker>()->publish(
		EventActorDead{ 1, 501, kGoblinMonsterId, 2 });

	EXPECT_EQ(1, alice.quests->GetProgress(kGoblinHunt, 0));
	EXPECT_EQ(1, bob.quests->GetProgress(kGoblinHunt, 0));
}

TEST_F(PartyQuestShareTest, SharedKillSplitsExp)
{
	TestPlayer solo(11, 101, 1);
	TestPlayer shared(12, 102, 1);

	const int before_solo = solo.level->GetExp();
	const int before_shared = shared.level->GetExp();

	solo.go.GetComponent<PlayerEventBroker>()->publish(EventActorDead{ 1, 500, 3, 1 });
	shared.go.GetComponent<PlayerEventBroker>()->publish(EventActorDead{ 1, 500, 3, 2 });

	const int solo_gain = solo.level->GetExp() - before_solo;
	const int shared_gain = shared.level->GetExp() - before_shared;

	EXPECT_GT(solo_gain, 0);
	EXPECT_LT(shared_gain, solo_gain);          // 1인분은 줄고
	EXPECT_GT(shared_gain * 2, solo_gain);      // 파티 전체로는 늘어난다
}
