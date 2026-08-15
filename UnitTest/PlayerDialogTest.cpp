#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include "GameObject.h"
#include "PlayerDialog.h"
#include "PlayerEventBroker.h"
#include "PlayerItem.h"
#include "PlayerLevel.h"
#include "PlayerLoadData.h"
#include "PlayerQuest.h"
#include "PlayerSkill.h"
#include "PlayerWallet.h"
#include "QuestRegistry.h"
#include "GameData/ResourceLoader.h"
#include "gamedata.h"

// 대화는 진짜 dialog.json 을 읽는다. 노드 참조가 끊기거나 선택지가 사라지면 여기서 깨지는
// 편이, 데이터만 바꾸고 서버가 조용히 다르게 도는 것보다 낫다.
//
// 데이터에서 쓰는 노드(dialog.json):
//   3001 촌장 인사      → [0] 3002 로, [1] 3003 으로, [2] 닫기
//   3002 고블린 이야기  → [0] 퀘스트 1001 수락, [1] 3001 로 되돌아가기, [2] 닫기
//   3003 상인 이야기    → [0] 퀘스트 1006 수락, [1] 퀘스트 1006 완료, [2] 3001 로
//   3101 대장장이       → [0] 닫기

namespace
{

constexpr int kElderNpc = 2001;
constexpr int kBlacksmithNpc = 2002;
constexpr int kMerchantNpc = 2003;   // 대화가 없는 NPC(호위 대상)

constexpr int kElderRoot = 3001;
constexpr int kGoblinTalk = 3002;
constexpr int kMerchantTalk = 3003;
constexpr int kBlacksmithRoot = 3101;

constexpr int kGoblinHunt = 1001;
constexpr int kEscortMerchant = 1006;

void EnsureResources()
{
	const std::string& path = GameDataPath::Resolve();
	ASSERT_TRUE(std::filesystem::exists(path + "dialog.json"))
		<< "통합 GameData 폴더를 찾지 못했습니다: " << path;
	ASSERT_TRUE(ResourceLoader::Instance().LoadResources(path)) << "LoadResources 실패";
	QuestRegistry::Instance().Clear();
}

// 대화가 퀘스트를 건드리므로 퀘스트 컴포넌트까지 갖춘 최소 구성.
struct TalkingPlayer
{
	GameObject go;
	PlayerDialog* dialog = nullptr;
	PlayerQuest* quests = nullptr;

	explicit TalkingPlayer(int level = 10)
	{
		go.AddComponent<PlayerEventBroker>();
		quests = go.AddComponent<PlayerQuest>();
		go.AddComponent<PlayerItem>();
		go.AddComponent<PlayerSkill>();
		go.AddComponent<PlayerWallet>();
		go.AddComponent<PlayerLevel>();
		dialog = go.AddComponent<PlayerDialog>();

		PlayerLoadData data{};
		data.player.id = 1001;
		data.player.name = "Tester";
		data.player.level = level;
		go.ForEachComponent([&data](Component& component) { component.Load(data); });
	}
};

class PlayerDialogTest : public ::testing::Test
{
protected:
	void SetUp() override { EnsureResources(); }
};

} // namespace

// ============================================================
// 열기 / 닫기
// ============================================================

TEST_F(PlayerDialogTest, OpensRootNodeOfNpc)
{
	TalkingPlayer player;

	EXPECT_FALSE(player.dialog->IsOpen());
	ASSERT_TRUE(player.dialog->Open(kElderNpc));

	EXPECT_TRUE(player.dialog->IsOpen());
	EXPECT_EQ(kElderRoot, player.dialog->GetCurrentNodeId());
	EXPECT_EQ(kElderNpc, player.dialog->GetNpcId());

	const gamedata::Dialog* node = player.dialog->GetCurrentNode();
	ASSERT_NE(nullptr, node);
	EXPECT_FALSE(node->choices.empty());
}

TEST_F(PlayerDialogTest, NpcWithoutDialogDoesNotOpen)
{
	TalkingPlayer player;

	// 호위 대상 NPC 는 대화가 없다. 상호작용은 되지만 창은 뜨지 않아야 한다.
	EXPECT_FALSE(player.dialog->Open(kMerchantNpc));
	EXPECT_FALSE(player.dialog->IsOpen());

	EXPECT_FALSE(player.dialog->Open(9999)); // 없는 NPC
	EXPECT_FALSE(player.dialog->IsOpen());
}

TEST_F(PlayerDialogTest, OpeningAnotherNpcReplacesCurrentDialog)
{
	TalkingPlayer player;

	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_TRUE(player.dialog->Open(kBlacksmithNpc));

	EXPECT_EQ(kBlacksmithNpc, player.dialog->GetNpcId());
	EXPECT_EQ(kBlacksmithRoot, player.dialog->GetCurrentNodeId());
}

TEST_F(PlayerDialogTest, CloseEndsDialog)
{
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));

	player.dialog->Close();
	EXPECT_FALSE(player.dialog->IsOpen());
	EXPECT_EQ(nullptr, player.dialog->GetCurrentNode());
	EXPECT_EQ(DialogResult::NoDialog, player.dialog->Select(kElderRoot, 0));
}

// ============================================================
// 선택지
// ============================================================

TEST_F(PlayerDialogTest, GotoMovesToNextNode)
{
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));

	const gamedata::Dialog* next = nullptr;
	EXPECT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, 0, &next));

	ASSERT_NE(nullptr, next);
	EXPECT_EQ(kGoblinTalk, next->id);
	EXPECT_EQ(kGoblinTalk, player.dialog->GetCurrentNodeId());
}

TEST_F(PlayerDialogTest, CloseChoiceEndsDialog)
{
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));

	const gamedata::Dialog* next = nullptr;
	EXPECT_EQ(DialogResult::Closed, player.dialog->Select(kElderRoot, 2, &next));
	EXPECT_EQ(nullptr, next);
	EXPECT_FALSE(player.dialog->IsOpen());
}

TEST_F(PlayerDialogTest, StaleNodeIsRejected)
{
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, 0)); // 3002 로 이동

	// 클라가 창을 두 번 눌러 지난 노드의 선택지가 뒤늦게 도착했다. 그대로 받으면
	// 지난 화면의 번호로 지금 노드의 동작이 실행된다.
	EXPECT_EQ(DialogResult::StaleNode, player.dialog->Select(kElderRoot, 0));
	EXPECT_EQ(kGoblinTalk, player.dialog->GetCurrentNodeId());
}

TEST_F(PlayerDialogTest, InvalidChoiceIndexIsRejected)
{
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));

	EXPECT_EQ(DialogResult::InvalidChoice, player.dialog->Select(kElderRoot, -1));
	EXPECT_EQ(DialogResult::InvalidChoice, player.dialog->Select(kElderRoot, 99));
	EXPECT_TRUE(player.dialog->IsOpen()); // 거절돼도 대화는 그대로다
}

TEST_F(PlayerDialogTest, BackChoiceReturnsToRoot)
{
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, 0)); // 3002

	const gamedata::Dialog* next = nullptr;
	EXPECT_EQ(DialogResult::Ok, player.dialog->Select(kGoblinTalk, 1, &next));
	ASSERT_NE(nullptr, next);
	EXPECT_EQ(kElderRoot, next->id);
}

// ============================================================
// 퀘스트 연동
// ============================================================

TEST_F(PlayerDialogTest, AcceptQuestChoiceAcceptsAndCloses)
{
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, 0)); // 3002

	EXPECT_FALSE(player.quests->IsActive(kGoblinHunt));
	EXPECT_EQ(DialogResult::Closed, player.dialog->Select(kGoblinTalk, 0));

	EXPECT_TRUE(player.quests->IsActive(kGoblinHunt));
	EXPECT_FALSE(player.dialog->IsOpen());
}

TEST_F(PlayerDialogTest, AcceptQuestKeepsDialogOpenWhenConditionsFail)
{
	// 1001 은 min_level 3 이다. 레벨이 모자라면 대화로 받는다고 면제되지 않는다.
	TalkingPlayer player(1);
	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, 0));

	EXPECT_EQ(DialogResult::ActionFailed, player.dialog->Select(kGoblinTalk, 0));
	EXPECT_FALSE(player.quests->IsActive(kGoblinHunt));

	// 실패했다고 창을 닫아 버리면 왜 안 됐는지 보여줄 자리가 사라진다.
	EXPECT_TRUE(player.dialog->IsOpen());
	EXPECT_EQ(kGoblinTalk, player.dialog->GetCurrentNodeId());
}

TEST_F(PlayerDialogTest, AcceptingTwiceFailsTheSecondTime)
{
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, 0));
	ASSERT_EQ(DialogResult::Closed, player.dialog->Select(kGoblinTalk, 0));
	ASSERT_TRUE(player.quests->IsActive(kGoblinHunt));

	// 같은 대화를 다시 열어 또 수락하면 이미 진행 중이라 거절된다.
	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, 0));
	EXPECT_EQ(DialogResult::ActionFailed, player.dialog->Select(kGoblinTalk, 0));
}

TEST_F(PlayerDialogTest, CompleteQuestChoiceRequiresReadyQuest)
{
	TalkingPlayer player;

	// 아직 받지도 않은 퀘스트는 완료할 수 없다.
	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, 1)); // 3003
	EXPECT_EQ(DialogResult::ActionFailed, player.dialog->Select(kMerchantTalk, 1));

	// 받아서 끝까지 진행하면 같은 선택지가 통한다.
	ASSERT_EQ(DialogResult::Closed, player.dialog->Select(kMerchantTalk, 0));
	ASSERT_TRUE(player.quests->IsActive(kEscortMerchant));
	ASSERT_TRUE(player.quests->GmSetProgress(kEscortMerchant, 2, 0, 0, 0));
	player.go.GetComponent<PlayerEventBroker>()->publish(EventNpcEscorted{ 1, kMerchantNpc });
	ASSERT_EQ(QuestState::ReadyToComplete, player.quests->GetState(kEscortMerchant));

	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, 1));
	EXPECT_EQ(DialogResult::Closed, player.dialog->Select(kMerchantTalk, 1));
	EXPECT_TRUE(player.quests->IsCompleted(kEscortMerchant));
}
