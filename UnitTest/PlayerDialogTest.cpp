#include "pch.h"
#include <gtest/gtest.h>
#include <algorithm>
#include <filesystem>
#include "DialogCondition.h"
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
//   3001 촌장 인사      → [0] 3002 로, [1] 3003 으로, [2] 닫기, [3] 3004 로
//   3002 고블린 이야기  → [0] 퀘스트 1001 수락, [1] 3001 로 되돌아가기, [2] 닫기
//   3003 상인 이야기    → [0] 퀘스트 1006 수락, [1] 퀘스트 1006 완료, [2] 3001 로
//   3101 대장장이       → [0] 닫기
//
// 조건(show_if)이 걸린 노드. 조건이 어긋난 선택지는 아예 내보내지 않으므로 클라가
// 세는 번호와 데이터의 번호가 다르다:
//   3004 마렌 스토리    → [0] 11001 수락(acceptable), [1] 11007 완료(ready_to_complete),
//                         [2] 11008 수락(acceptable), [3] 3001 로 되돌아가기(조건 없음)
//   3201 리네 인사      → [0] 11001 완료(ready_to_complete), [1] 3202 로(11001 completed),
//                         [2] 3203 으로(11002 completed), [3] 닫기(조건 없음)

namespace
{

constexpr int kElderNpc = 2001;
constexpr int kBlacksmithNpc = 2002;
constexpr int kMerchantNpc = 2003;   // 대화가 없는 NPC(호위 대상)
constexpr int kRinneNpc = 2004;

constexpr int kElderRoot = 3001;
constexpr int kGoblinTalk = 3002;
constexpr int kMerchantTalk = 3003;
constexpr int kBlacksmithRoot = 3101;
constexpr int kMarenStory = 3004;
constexpr int kRinneRoot = 3201;

constexpr int kGoblinHunt = 1001;
constexpr int kEscortMerchant = 1006;

constexpr int kMissingShips = 11001;   // 1막 1번. min_level 1, 선행 없음
constexpr int kFourWingedMural = 11008; // 1막 8번. 운영이 내려둔(disabled) 퀘스트

// 3001 에서 마렌의 메인 스토리 노드(3004)로 들어가는 선택지 번호.
// 3001 에는 조건이 없어 데이터의 번호가 그대로 나간다.
constexpr int kElderRootToStory = 3;

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

// ============================================================
// 조건부 선택지 (show_if)
// ============================================================

TEST_F(PlayerDialogTest, ChoiceWithoutConditionIsAlwaysVisible)
{
	// 3001 에는 조건이 하나도 없다. 조건을 넣기 전과 똑같이 전부 나가야 한다.
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));

	const gamedata::Dialog* node = player.dialog->GetCurrentNode();
	ASSERT_NE(nullptr, node);
	for (int i = 0; i < static_cast<int>(node->choices.size()); ++i)
		EXPECT_TRUE(player.dialog->IsChoiceVisible(i)) << "choices[" << i << "]";
}

TEST_F(PlayerDialogTest, HidesChoicesWhoseConditionIsNotMet)
{
	// 아무것도 안 한 플레이어에게 리네는 할 말이 없다. 11001 을 완료 대기로 만들지도,
	// 완료하지도 않았으니 앞의 세 선택지가 모두 빠지고 "그만 가 보겠습니다"만 남는다.
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kRinneNpc));
	ASSERT_EQ(kRinneRoot, player.dialog->GetCurrentNodeId());

	EXPECT_FALSE(player.dialog->IsChoiceVisible(0)); // 11001 완료
	EXPECT_FALSE(player.dialog->IsChoiceVisible(1)); // 3202 로
	EXPECT_FALSE(player.dialog->IsChoiceVisible(2)); // 3203 으로
	EXPECT_TRUE(player.dialog->IsChoiceVisible(3));  // 닫기

	// 클라가 받은 목록에서 0번은 닫기다. 데이터의 0번(완료)이 아니다.
	EXPECT_EQ(3, player.dialog->ResolveVisibleChoice(0));
	EXPECT_EQ(-1, player.dialog->ResolveVisibleChoice(1));
	EXPECT_EQ(DialogResult::Closed, player.dialog->Select(kRinneRoot, 0));
	EXPECT_FALSE(player.quests->IsCompleted(kMissingShips));
}

TEST_F(PlayerDialogTest, ShowsChoiceOnceConditionIsMet)
{
	TalkingPlayer player;

	// 11001 을 받아 마지막 스테이지까지 채우면 완료 대기가 된다.
	ASSERT_EQ(QuestAcceptResult::Ok, player.quests->AcceptQuest(kMissingShips));
	ASSERT_TRUE(player.quests->GmSetProgress(kMissingShips, 3, 1, 0, 0));
	ASSERT_EQ(QuestState::ReadyToComplete, player.quests->GetState(kMissingShips));

	ASSERT_TRUE(player.dialog->Open(kRinneNpc));
	EXPECT_TRUE(player.dialog->IsChoiceVisible(0));  // 이제 완료 선택지가 보인다
	EXPECT_FALSE(player.dialog->IsChoiceVisible(1)); // 아직 완료는 아니라 다음 이야기는 잠겨 있다

	EXPECT_EQ(DialogResult::Closed, player.dialog->Select(kRinneRoot, 0));
	EXPECT_TRUE(player.quests->IsCompleted(kMissingShips));
}

TEST_F(PlayerDialogTest, DisabledQuestIsNotOfferedAsAcceptable)
{
	// acceptable 은 Quest::CanAccept 를 그대로 쓴다. 운영이 내려둔 퀘스트는 수락이
	// 거절되므로 선택지 자체가 나가지 않는다 — 눌러도 반드시 실패할 것을 보여주지 않는다.
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, kElderRootToStory));
	ASSERT_EQ(kMarenStory, player.dialog->GetCurrentNodeId());

	EXPECT_TRUE(player.dialog->IsChoiceVisible(0));  // 11001 수락
	EXPECT_FALSE(player.dialog->IsChoiceVisible(1)); // 11007 완료 — 받은 적도 없다
	EXPECT_FALSE(player.dialog->IsChoiceVisible(2)); // 11008 수락 — disabled
	EXPECT_TRUE(player.dialog->IsChoiceVisible(3));  // 되돌아가기

	EXPECT_FALSE(player.quests->IsActive(kFourWingedMural));
}

TEST_F(PlayerDialogTest, VisibleIndexMapsBackToOriginalChoice)
{
	// 3004 에서 실제로 나가는 것은 [11001 수락, 되돌아가기] 둘뿐이다.
	// 클라가 보낸 1번은 데이터의 3번(되돌아가기)으로 되짚어져야 한다.
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, kElderRootToStory));

	EXPECT_EQ(0, player.dialog->ResolveVisibleChoice(0));
	EXPECT_EQ(3, player.dialog->ResolveVisibleChoice(1));

	EXPECT_EQ(DialogResult::Ok, player.dialog->Select(kMarenStory, 1));
	EXPECT_EQ(kElderRoot, player.dialog->GetCurrentNodeId());
	EXPECT_FALSE(player.quests->IsActive(kMissingShips));
}

TEST_F(PlayerDialogTest, VisibleListIsFixedWhenTheNodeIsShown)
{
	// 노드를 보여 준 뒤 퀘스트 상태가 바뀌어도, 플레이어가 누른 번호는 그때 본 목록으로
	// 되짚는다. 매번 다시 계산하면 화면은 그대로인데 번호의 뜻만 조용히 바뀐다.
	TalkingPlayer player;
	ASSERT_TRUE(player.dialog->Open(kElderNpc));
	ASSERT_EQ(DialogResult::Ok, player.dialog->Select(kElderRoot, kElderRootToStory));
	ASSERT_EQ(3, player.dialog->ResolveVisibleChoice(1)); // 되돌아가기

	// 창이 열려 있는 사이에 다른 경로로 11001 을 받는다(파티 공유, GM 조작 등).
	// 지금 다시 판정하면 "11001 수락"이 빠져 1번이 범위를 벗어난다.
	ASSERT_EQ(QuestAcceptResult::Ok, player.quests->AcceptQuest(kMissingShips));

	EXPECT_EQ(3, player.dialog->ResolveVisibleChoice(1));
	EXPECT_EQ(DialogResult::Ok, player.dialog->Select(kMarenStory, 1));
	EXPECT_EQ(kElderRoot, player.dialog->GetCurrentNodeId());
}

TEST_F(PlayerDialogTest, EveryNodeKeepsOneChoiceWithoutCondition)
{
	// 조건이 전부 어긋나면 남는 선택지가 없어 대화를 닫을 방법이 사라진다.
	// 데이터 검증도 같은 것을 막지만, 실제로 읽은 데이터로 한 번 더 확인한다.
	for (const auto& [id, node] : ResourceLoader::Instance().GetDialogs())
	{
		ASSERT_NE(nullptr, node);
		const bool has_unconditional = std::any_of(
			node->choices.begin(), node->choices.end(),
			[](const gamedata::DialogChoice& c)
			{
				return c.show_if.quest_id == 0
					|| ParseDialogCondition(c.show_if.state) == DialogConditionType::None;
			});
		EXPECT_TRUE(has_unconditional) << "노드 " << id << " 에 조건 없는 선택지가 없습니다";
	}
}
