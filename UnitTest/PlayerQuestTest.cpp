#include "pch.h"
#include <gtest/gtest.h>
#include <chrono>
#include <filesystem>
#include "PlayerQuest.h"
#include "PlayerLevel.h"
#include "PlayerItem.h"
#include "PlayerSkill.h"
#include "PlayerWallet.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "DbRecord.h"
#include "GameObject.h"
#include "PlayerEventBroker.h"
#include "PlayerSender.h"
#include "EventMessage.h"
#include "SendMessage.h"
#include "syncnet_generated.h"
#include "Quest.h"
#include "QuestRegistry.h"
#include "QuestPolicy.h"
#include "GameData/ResourceLoader.h"
#include "gamedata.h"

// 실제 서버 흐름을 재현한 단위 테스트:
//   PlayerLoadData (DB 조회 결과) -> Load() -> 게임 로직 -> Save() -> PlayerSaveData (DB 저장 대상)
// 실제 DB 연결 없이 데이터 구조만 사용한다.
//
// 퀘스트 정의는 진짜 quest.json 을 읽는다. 목표/보상/선행조건이 데이터와 어긋나면
// 여기서 깨지는 편이, 데이터만 바꾸고 서버가 조용히 다르게 도는 것보다 낫다.
//
// 데이터에서 쓰는 퀘스트(quest.json):
//   1001 MainQuest      고블린 사냥. min_level 3. 3스테이지(대화 / 처치+수집 / 보고),
//                       선택 보상 2종. auto_complete=false, end_npc 2001
//   1002 MainQuest      고블린 족장. 선행 1001, min_level 5
//   1004 SubQuest       or 목표(슬라임 10 또는 늑대 5), auto_complete
//   1005 SubQuest       reach(맵 2) + level 3, auto_complete
//   2001 RepeatedQuest  일일 반복. 선행 1003, min_level 5
//   3001 LimitedTimeQuest 제한 시간 1800초, 쿨타임 3600초. 선행 1002, min_level 6

namespace
{

constexpr int kGoblinHunt = 1001;      // MainQuest
constexpr int kGoblinChief = 1002;     // MainQuest, 선행 1001
constexpr int kWolfPelt = 1003;        // SubQuest, 수집
constexpr int kFieldCleanup = 1004;    // SubQuest, or 목표 + auto_complete
constexpr int kFirstSteps = 1005;      // SubQuest, reach + level + auto_complete
constexpr int kEscortMerchant = 1006;  // SubQuest, 1스테이지 protect(20초) / 2스테이지 escort
constexpr int kDailyWolf = 2001;       // RepeatedQuest
constexpr int kAncientLetter = 3001;   // LimitedTimeQuest
constexpr int kDisabledEvent = 9001;   // 운영이 내려둔(disabled) 퀘스트

constexpr int kMerchantNpcId = 2003;   // 호위/보호 대상 NPC

constexpr int kGoblinMonsterId = 3;
constexpr int kOrcMonsterId = 4;
constexpr int kSlimeMonsterId = 1;
constexpr int kWolfMonsterId = 2;
constexpr int kGoblinEarItemId = 100;
constexpr int kElderNpcId = 2001;

void EnsureResources()
{
	const std::string& path = GameDataPath::Resolve();
	ASSERT_TRUE(std::filesystem::exists(path + "quest.json"))
		<< "통합 GameData 폴더를 찾지 못했습니다: " << path;
	ASSERT_TRUE(ResourceLoader::Instance().LoadResources(path)) << "LoadResources 실패";
	// 리소스를 다시 읽었으므로 정의 캐시도 새 gamedata 로 다시 묶이게 한다.
	QuestRegistry::Instance().Clear();
}

PlayerLoadData MakeNewPlayerData(int character_id = 1001, int level = 10)
{
	PlayerLoadData data{};
	data.player.id = character_id;
	data.player.name = "Tester";
	data.player.level = level;
	// quest_state.character_id = 0 -> 첫 저장 시 INSERT
	return data;
}

PlayerLoadData MakeExistingPlayerData(
	int character_id,
	std::vector<VOQuestActive> actives,
	std::string flags,
	int level = 10)
{
	PlayerLoadData data = MakeNewPlayerData(character_id, level);
	data.quest_actives = std::move(actives);
	data.quest_state.character_id = character_id;
	data.quest_state.flags = std::move(flags);
	return data;
}

PlayerLoadData MakeExistingPlayerData(int character_id, int level = 10)
{
	return MakeExistingPlayerData(character_id, std::vector<VOQuestActive>{}, std::string{}, level);
}

VOQuestActive MakeQuestVO(
	int char_id, int quest_id,
	int state = 0, int stage = 1, int p1 = 0, int p2 = 0, int p3 = 0)
{
	VOQuestActive vo{};
	vo.character_id = char_id;
	vo.quest_id = quest_id;
	vo.state = state;
	vo.stage = stage;
	vo.progress1 = p1;
	vo.progress2 = p2;
	vo.progress3 = p3;
	vo.accept_time = std::chrono::system_clock::now();
	return vo;
}

// 클라로 나가는 QuestSync 를 가로채는 최소 구성.
// PlayerQuest 가 자기 Update 끝에서 직접 보내므로, 세션 대신 sink 를 끼워 확인한다.
struct SyncHarness
{
	GameObject go;
	PlayerQuest* quest = nullptr;
	std::vector<std::shared_ptr<send_message>> sent;

	explicit SyncHarness(const PlayerLoadData& load)
	{
		go.AddComponent<PlayerEventBroker>();
		quest = go.AddComponent<PlayerQuest>();
		go.AddComponent<PlayerSender>()->Bind(
			[this](std::shared_ptr<send_message>& msg) { sent.push_back(msg); });
		quest->Load(load);
	}

	// 한 틱 돌려 이번에 나간 QuestSync 를 돌려준다. 나간 것이 없으면 nullptr.
	const syncnet::QuestSync* Flush()
	{
		sent.clear();
		go.Update(0.1f);

		for (auto it = sent.rbegin(); it != sent.rend(); ++it)
		{
			const auto* msg = syncnet::GetGameMessage((*it)->GetBufferPointer());
			if (msg != nullptr && msg->msg_type() == syncnet::GameMessages::GameMessages_QuestSync)
				return msg->msg_as_QuestSync();
		}
		return nullptr;
	}
};

PlayerSaveData DoSave(PlayerQuest& quest)
{
	PlayerSaveData save_data{};
	quest.Save(&save_data);
	return save_data;
}

// 완료 비트를 직접 세운 flags 문자열(선행 퀘스트를 이미 끝낸 상태를 만들 때 쓴다).
std::string FlagsWithCompleted(std::initializer_list<int> quest_ids)
{
	std::string flags;
	for (int quest_id : quest_ids)
	{
		const size_t byte_idx = static_cast<size_t>(quest_id) / 8;
		if (flags.size() <= byte_idx)
			flags.resize(byte_idx + 1, '\0');
		flags[byte_idx] = static_cast<char>(
			static_cast<uint8_t>(flags[byte_idx]) | (1 << (quest_id % 8)));
	}
	return flags;
}

// 브로커를 먼저 부착해야 PlayerQuest::Start 가 구독에 성공한다.
PlayerQuest* AttachQuest(GameObject& go)
{
	go.AddComponent<PlayerEventBroker>();
	return go.AddComponent<PlayerQuest>();
}

class PlayerQuestTest : public ::testing::Test
{
protected:
	void SetUp() override { EnsureResources(); }
};

// 1001 의 스테이지 2(고블린 10마리 + 귀 5개)까지 진행시킨다.
void AdvanceToKillStage(PlayerQuest& quest)
{
	ASSERT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);
	quest.ReportProgress(QuestObjectiveType::Talk, kElderNpcId, 1);
	ASSERT_EQ(quest.GetStage(kGoblinHunt), 2);
}

} // namespace

// ============================================================
// 퀘스트 정의(데이터) 자체
// ============================================================

TEST_F(PlayerQuestTest, Definition_LoadedFromData)
{
	Quest* quest = QuestRegistry::Instance().Get(kGoblinHunt);
	ASSERT_NE(quest, nullptr);
	ASSERT_NE(quest->gamedata, nullptr);

	EXPECT_EQ(quest->GetId(), kGoblinHunt);
	EXPECT_EQ(quest->StageCount(), 3);
	EXPECT_FALSE(quest->IsAbandonable());   // MainQuest 는 포기 불가
	EXPECT_FALSE(quest->IsRepeatable());
	EXPECT_FALSE(quest->IsAutoComplete());
}

TEST_F(PlayerQuestTest, Definition_UnknownIdIsNull)
{
	EXPECT_EQ(QuestRegistry::Instance().Get(999999), nullptr);
}

TEST_F(PlayerQuestTest, Definition_SharedInstancePerId)
{
	// 정의는 무상태라 플레이어마다 만들지 않는다.
	EXPECT_EQ(QuestRegistry::Instance().Get(kGoblinHunt),
		QuestRegistry::Instance().Get(kGoblinHunt));
}

TEST_F(PlayerQuestTest, Definition_TypePolicies)
{
	EXPECT_TRUE(QuestRegistry::Instance().Get(kDailyWolf)->IsRepeatable());
	EXPECT_EQ(QuestRegistry::Instance().Get(kDailyWolf)->GetResetType(), QuestResetType::Daily);
	EXPECT_GT(QuestRegistry::Instance().Get(kAncientLetter)->GetTimeLimitSeconds(), 0);
	EXPECT_TRUE(QuestRegistry::Instance().Get(kWolfPelt)->IsAbandonable()); // SubQuest
}

TEST_F(PlayerQuestTest, Definition_MatchObjectives)
{
	Quest* quest = QuestRegistry::Instance().Get(kGoblinHunt);
	ASSERT_NE(quest, nullptr);

	// 스테이지 2 = 고블린 처치(슬롯 0) + 귀 수집(슬롯 1)
	auto kill = quest->MatchObjectives(2, QuestObjectiveType::Kill, kGoblinMonsterId);
	ASSERT_EQ(kill.size(), 1u);
	EXPECT_EQ(kill[0].slot, 0);

	auto collect = quest->MatchObjectives(2, QuestObjectiveType::Collect, kGoblinEarItemId);
	ASSERT_EQ(collect.size(), 1u);
	EXPECT_EQ(collect[0].slot, 1);

	// 다른 몬스터/다른 스테이지는 걸리지 않는다.
	EXPECT_TRUE(quest->MatchObjectives(2, QuestObjectiveType::Kill, kWolfMonsterId).empty());
	EXPECT_TRUE(quest->MatchObjectives(1, QuestObjectiveType::Kill, kGoblinMonsterId).empty());
	EXPECT_TRUE(quest->MatchObjectives(99, QuestObjectiveType::Kill, kGoblinMonsterId).empty());
}

TEST_F(PlayerQuestTest, Definition_StageCompletionLogic)
{
	Quest* and_quest = QuestRegistry::Instance().Get(kGoblinHunt);   // logic "and"
	Quest* or_quest = QuestRegistry::Instance().Get(kFieldCleanup);  // logic "or"
	ASSERT_NE(and_quest, nullptr);
	ASSERT_NE(or_quest, nullptr);

	const int one_done[3] = { 10, 0, 0 };   // 고블린만 채움
	const int both_done[3] = { 10, 5, 0 };
	EXPECT_FALSE(and_quest->IsStageComplete(2, one_done, 3));
	EXPECT_TRUE(and_quest->IsStageComplete(2, both_done, 3));

	const int slime_only[3] = { 10, 0, 0 }; // or: 슬라임 10 만으로 충분
	EXPECT_TRUE(or_quest->IsStageComplete(1, slime_only, 3));
}

TEST_F(PlayerQuestTest, Definition_ResetBoundary)
{
	using namespace std::chrono;
	const auto now = system_clock::now();

	EXPECT_FALSE(QuestResetBoundaryPassed(QuestResetType::None, now - hours(240), now));
	EXPECT_FALSE(QuestResetBoundaryPassed(QuestResetType::Daily, now - minutes(1), now));
	EXPECT_TRUE(QuestResetBoundaryPassed(QuestResetType::Daily, now - hours(48), now));
	EXPECT_FALSE(QuestResetBoundaryPassed(QuestResetType::Weekly, now - hours(1), now));
	EXPECT_TRUE(QuestResetBoundaryPassed(QuestResetType::Weekly, now - hours(24 * 14), now));
}

// ============================================================
// 수락 조건
// ============================================================

TEST_F(PlayerQuestTest, Accept_UnknownQuestIsRejected)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	EXPECT_EQ(quest.AcceptQuest(999999), QuestAcceptResult::NotFound);
}

TEST_F(PlayerQuestTest, Accept_LevelTooLow)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData(1001, 1)); // min_level 3
	EXPECT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::LevelTooLow);
	EXPECT_FALSE(quest.IsActive(kGoblinHunt));
}

TEST_F(PlayerQuestTest, Accept_AlreadyActive)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	EXPECT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);
	EXPECT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::AlreadyActive);
}

TEST_F(PlayerQuestTest, Accept_PrerequisiteQuestMissing)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	// 1002 는 1001 완료가 선행
	EXPECT_EQ(quest.AcceptQuest(kGoblinChief), QuestAcceptResult::PrerequisiteQuest);
}

TEST_F(PlayerQuestTest, Accept_PrerequisiteSatisfied)
{
	PlayerQuest quest;
	quest.Load(MakeExistingPlayerData(
		1001, std::vector<VOQuestActive>{}, FlagsWithCompleted({ kGoblinHunt })));

	EXPECT_TRUE(quest.IsCompleted(kGoblinHunt));
	EXPECT_EQ(quest.AcceptQuest(kGoblinChief), QuestAcceptResult::Ok);
}

TEST_F(PlayerQuestTest, Accept_CompletedNonRepeatableIsRejected)
{
	PlayerQuest quest;
	quest.Load(MakeExistingPlayerData(
		1001, std::vector<VOQuestActive>{}, FlagsWithCompleted({ kGoblinHunt })));

	EXPECT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::AlreadyCompleted);
}

TEST_F(PlayerQuestTest, Accept_StartsAtStageOne)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	ASSERT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);

	EXPECT_TRUE(quest.IsActive(kGoblinHunt));
	EXPECT_EQ(quest.GetStage(kGoblinHunt), 1);
	EXPECT_EQ(quest.GetProgress(kGoblinHunt, 0), 0);
	EXPECT_EQ(quest.GetState(kGoblinHunt), QuestState::InProgress);
}

// ============================================================
// 목표 진행 / 스테이지 전환
// ============================================================

TEST_F(PlayerQuestTest, Progress_KillCountsOnlyMatchingMonster)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	AdvanceToKillStage(quest);

	quest.ReportProgress(QuestObjectiveType::Kill, kWolfMonsterId, 3); // 관계없는 몬스터
	EXPECT_EQ(quest.GetProgress(kGoblinHunt, 0), 0);

	quest.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 4);
	EXPECT_EQ(quest.GetProgress(kGoblinHunt, 0), 4);
}

TEST_F(PlayerQuestTest, Progress_ClampedToRequiredCount)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	AdvanceToKillStage(quest);

	quest.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 100);
	EXPECT_EQ(quest.GetProgress(kGoblinHunt, 0), 10); // 목표치를 넘겨 저장하지 않는다
}

TEST_F(PlayerQuestTest, Progress_AndStageNeedsEveryObjective)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	AdvanceToKillStage(quest);

	quest.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 10);
	EXPECT_EQ(quest.GetStage(kGoblinHunt), 2); // 수집이 남아 아직 스테이지 2

	quest.ReportProgress(QuestObjectiveType::Collect, kGoblinEarItemId, 5);
	EXPECT_EQ(quest.GetStage(kGoblinHunt), 3); // 둘 다 채워 다음 스테이지로
}

TEST_F(PlayerQuestTest, Progress_StageAdvanceResetsCounters)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	AdvanceToKillStage(quest);

	quest.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 10);
	quest.ReportProgress(QuestObjectiveType::Collect, kGoblinEarItemId, 5);

	ASSERT_EQ(quest.GetStage(kGoblinHunt), 3);
	EXPECT_EQ(quest.GetProgress(kGoblinHunt, 0), 0);
	EXPECT_EQ(quest.GetProgress(kGoblinHunt, 1), 0);
}

TEST_F(PlayerQuestTest, Progress_LastStageMakesQuestReadyToComplete)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	AdvanceToKillStage(quest);
	quest.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 10);
	quest.ReportProgress(QuestObjectiveType::Collect, kGoblinEarItemId, 5);

	// 스테이지 3 = 촌장에게 보고
	quest.ReportProgress(QuestObjectiveType::Talk, kElderNpcId, 1);

	EXPECT_EQ(quest.GetState(kGoblinHunt), QuestState::ReadyToComplete);
	EXPECT_TRUE(quest.IsActive(kGoblinHunt));      // 아직 완료는 아니다
	EXPECT_FALSE(quest.IsCompleted(kGoblinHunt));
}

TEST_F(PlayerQuestTest, Progress_OrStageCompletesWithOneObjective)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	ASSERT_EQ(quest.AcceptQuest(kFieldCleanup), QuestAcceptResult::Ok);

	// or 목표: 슬라임 10 또는 늑대 5. auto_complete 라 채우는 즉시 끝난다.
	quest.ReportProgress(QuestObjectiveType::Kill, kSlimeMonsterId, 10);

	EXPECT_TRUE(quest.IsCompleted(kFieldCleanup));
	EXPECT_FALSE(quest.IsActive(kFieldCleanup));
}

TEST_F(PlayerQuestTest, Progress_IgnoredForQuestNotInThatStage)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	ASSERT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);

	// 스테이지 1(대화) 인데 처치 이벤트가 왔다 -> 아무 일도 없어야 한다
	quest.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 5);
	EXPECT_EQ(quest.GetStage(kGoblinHunt), 1);
	EXPECT_EQ(quest.GetProgress(kGoblinHunt, 0), 0);
}

TEST_F(PlayerQuestTest, Progress_LevelObjectiveTakesHighestNotSum)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData(1001, 1)); // 레벨 1 로 시작(목표 레벨 3)
	ASSERT_EQ(quest.AcceptQuest(kFirstSteps), QuestAcceptResult::Ok);

	quest.ReportProgress(QuestObjectiveType::Level, 0, 2);
	EXPECT_EQ(quest.GetProgress(kFirstSteps, 1), 2);

	// 누적이면 2+3=5 가 되겠지만, 도달 레벨이므로 3 이어야 한다.
	quest.ReportProgress(QuestObjectiveType::Level, 0, 3);
	EXPECT_EQ(quest.GetProgress(kFirstSteps, 1), 3);
}

TEST_F(PlayerQuestTest, Progress_LevelObjectiveSeededOnAccept)
{
	PlayerQuest quest;
	// 이미 레벨 10 이다. 수락 후 레벨이 오를 일이 없어도 목표는 채워져 있어야 한다.
	quest.Load(MakeNewPlayerData(1001, 10));
	ASSERT_EQ(quest.AcceptQuest(kFirstSteps), QuestAcceptResult::Ok);

	EXPECT_EQ(quest.GetProgress(kFirstSteps, 1), 3); // 목표치 3 으로 클램프
}

// ============================================================
// 완료 / 보상 / 포기
// ============================================================

TEST_F(PlayerQuestTest, Complete_RejectedWhileStagesRemain)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	ASSERT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);

	EXPECT_FALSE(quest.CompleteQuest(kGoblinHunt, 0));
	EXPECT_FALSE(quest.IsCompleted(kGoblinHunt));
}

TEST_F(PlayerQuestTest, Complete_RequiresValidChoiceWhenRewardsAreSelectable)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	AdvanceToKillStage(quest);
	quest.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 10);
	quest.ReportProgress(QuestObjectiveType::Collect, kGoblinEarItemId, 5);
	quest.ReportProgress(QuestObjectiveType::Talk, kElderNpcId, 1);
	ASSERT_EQ(quest.GetState(kGoblinHunt), QuestState::ReadyToComplete);

	EXPECT_FALSE(quest.CompleteQuest(kGoblinHunt, -1)); // 선택 없음
	EXPECT_FALSE(quest.CompleteQuest(kGoblinHunt, 7));  // 범위 밖
	EXPECT_TRUE(quest.CompleteQuest(kGoblinHunt, 1));
	EXPECT_TRUE(quest.IsCompleted(kGoblinHunt));
	EXPECT_FALSE(quest.IsActive(kGoblinHunt));
}

TEST_F(PlayerQuestTest, Complete_GrantsRewardsIncludingChosenItem)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	AdvanceToKillStage(quest);
	quest.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 10);
	quest.ReportProgress(QuestObjectiveType::Collect, kGoblinEarItemId, 5);
	quest.ReportProgress(QuestObjectiveType::Talk, kElderNpcId, 1);
	ASSERT_TRUE(quest.CompleteQuest(kGoblinHunt, 1)); // 두 번째 선택 보상(방패 id 5)

	const auto rewards = quest.TakePendingRewards();
	ASSERT_EQ(rewards.size(), 1u);
	EXPECT_EQ(rewards[0].quest_id, kGoblinHunt);
	EXPECT_EQ(rewards[0].exp, 500);
	EXPECT_EQ(rewards[0].gold, 200);

	// 고정 보상(포션 5개) + 고른 보상 1개
	ASSERT_EQ(rewards[0].items.size(), 2u);
	EXPECT_EQ(rewards[0].items[0].first, 1);
	EXPECT_EQ(rewards[0].items[0].second, 5);
	EXPECT_EQ(rewards[0].items[1].first, 5);

	EXPECT_TRUE(quest.GetPendingRewards().empty()); // 꺼내면 비워진다
}

TEST_F(PlayerQuestTest, Complete_ExpGoesToPlayerLevel)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerLevel* level = go.AddComponent<PlayerLevel>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();

	level->Load(MakeNewPlayerData(1001, 1));
	quest->Load(MakeNewPlayerData(1001, 1));

	const int before = level->GetExp();
	ASSERT_EQ(quest->AcceptQuest(kFieldCleanup), QuestAcceptResult::Ok); // 보상 exp 200
	quest->ReportProgress(QuestObjectiveType::Kill, kSlimeMonsterId, 10);

	ASSERT_TRUE(quest->IsCompleted(kFieldCleanup));
	EXPECT_EQ(level->GetExp(), before + 200);
}

TEST_F(PlayerQuestTest, Complete_DeliversItemsAndGoldToComponents)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerItem* inventory = go.AddComponent<PlayerItem>();
	PlayerWallet* wallet = go.AddComponent<PlayerWallet>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();

	const PlayerLoadData load = MakeNewPlayerData();
	inventory->Load(load);
	wallet->Load(load);
	quest->Load(load);

	AdvanceToKillStage(*quest);
	quest->ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 10);
	quest->ReportProgress(QuestObjectiveType::Collect, kGoblinEarItemId, 5);
	quest->ReportProgress(QuestObjectiveType::Talk, kElderNpcId, 1);

	ASSERT_TRUE(quest->CompleteQuest(kGoblinHunt, 0)); // 첫 번째 선택 보상(검 id 4)

	// 고정 보상 포션 5개 + 고른 검 1개가 실제로 인벤토리에 들어가야 한다.
	EXPECT_EQ(inventory->GetCount(1), 5);
	EXPECT_EQ(inventory->GetCount(4), 1);
	EXPECT_EQ(wallet->GetGold(), 200);
}

TEST_F(PlayerQuestTest, Complete_DiscardsQuestItemsButKeepsRewards)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerItem* inventory = go.AddComponent<PlayerItem>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();

	const PlayerLoadData load = MakeNewPlayerData();
	inventory->Load(load);
	quest->Load(load);

	ASSERT_EQ(quest->AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);
	quest->ReportProgress(QuestObjectiveType::Talk, kElderNpcId, 1);

	// 수집 목표는 인벤토리 획득 이벤트로 올라간다.
	quest->ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 10);
	inventory->AddItem(kGoblinEarItemId, 5);
	ASSERT_EQ(quest->GetStage(kGoblinHunt), 3);

	quest->ReportProgress(QuestObjectiveType::Talk, kElderNpcId, 1);
	ASSERT_TRUE(quest->CompleteQuest(kGoblinHunt, 0));

	// 퀘스트 전용 아이템은 회수되고, 보상은 남는다.
	EXPECT_EQ(inventory->GetCount(kGoblinEarItemId), 0);
	EXPECT_EQ(inventory->GetCount(1), 5);
	EXPECT_EQ(inventory->GetCount(4), 1);
}

TEST_F(PlayerQuestTest, Abandon_DiscardsQuestItems)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerItem* inventory = go.AddComponent<PlayerItem>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();

	const PlayerLoadData load = MakeNewPlayerData();
	inventory->Load(load);
	quest->Load(load);

	ASSERT_EQ(quest->AcceptQuest(kWolfPelt), QuestAcceptResult::Ok);
	inventory->AddItem(101, 3); // 늑대 가죽 (quest 1003 전용)

	ASSERT_TRUE(quest->AbandonQuest(kWolfPelt));
	EXPECT_EQ(inventory->GetCount(101), 0);
}

TEST_F(PlayerQuestTest, Progress_CollectAdvancesFromInventoryEvent)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerItem* inventory = go.AddComponent<PlayerItem>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();

	const PlayerLoadData load = MakeNewPlayerData();
	inventory->Load(load);
	quest->Load(load);

	AdvanceToKillStage(*quest);

	inventory->AddItem(kGoblinEarItemId, 2);
	EXPECT_EQ(quest->GetProgress(kGoblinHunt, 1), 2);

	inventory->AddItem(kGoblinEarItemId, 9); // 목표치 5 를 넘겨도 5 로 잘린다
	EXPECT_EQ(quest->GetProgress(kGoblinHunt, 1), 5);
}

TEST_F(PlayerQuestTest, Sync_SendsChangedRemovedAndCompleted)
{
	SyncHarness harness(MakeNewPlayerData());

	ASSERT_EQ(harness.quest->AcceptQuest(kFieldCleanup), QuestAcceptResult::Ok);

	const syncnet::QuestSync* sync = harness.Flush();
	ASSERT_NE(nullptr, sync);
	ASSERT_EQ(1u, sync->quests()->size());
	EXPECT_EQ(kFieldCleanup, sync->quests()->Get(0)->questId());
	EXPECT_EQ(0u, sync->removed()->size());
	EXPECT_EQ(0u, sync->completed()->size());

	EXPECT_EQ(nullptr, harness.Flush()); // 바뀐 것이 없으면 보내지 않는다

	// auto_complete 퀘스트가 끝나면 목록에서 빠지고 완료로 보고된다.
	harness.quest->ReportProgress(QuestObjectiveType::Kill, kSlimeMonsterId, 10);

	sync = harness.Flush();
	ASSERT_NE(nullptr, sync);
	// 사라진 퀘스트를 변경으로도 보내면 클라가 그것을 다시 만든다.
	EXPECT_EQ(0u, sync->quests()->size());
	ASSERT_EQ(1u, sync->removed()->size());
	EXPECT_EQ(kFieldCleanup, sync->removed()->Get(0));
	ASSERT_EQ(1u, sync->completed()->size());
	EXPECT_EQ(kFieldCleanup, sync->completed()->Get(0));
}

TEST_F(PlayerQuestTest, Sync_MarkAllRestoresLogOnLogin)
{
	std::vector<VOQuestActive> actives;
	actives.push_back(MakeQuestVO(1001, kGoblinHunt, 0, 2, 5, 3, 0));

	SyncHarness harness(MakeExistingPlayerData(1001, actives, std::string{}));
	EXPECT_EQ(nullptr, harness.Flush()); // 로드만으로는 아무것도 나가지 않는다

	harness.quest->MarkAllForSync();

	const syncnet::QuestSync* sync = harness.Flush();
	ASSERT_NE(nullptr, sync);
	ASSERT_EQ(1u, sync->quests()->size());

	const syncnet::QuestInfo* info = sync->quests()->Get(0);
	EXPECT_EQ(kGoblinHunt, info->questId());
	EXPECT_EQ(2, info->stage());
	ASSERT_EQ(3u, info->progress()->size());
	EXPECT_EQ(5, info->progress()->Get(0));
	EXPECT_EQ(3, info->progress()->Get(1));
}

TEST_F(PlayerQuestTest, Prerequisite_ItemAndSkillCheckRealComponents)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerItem* inventory = go.AddComponent<PlayerItem>();
	PlayerSkill* skills = go.AddComponent<PlayerSkill>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();

	const PlayerLoadData load = MakeNewPlayerData();
	inventory->Load(load);
	skills->Load(load);
	quest->Load(load);

	// IQuestOwner 조회가 실제 컴포넌트를 본다(데이터에 아이템/스킬 선행조건이 없어
	// 조회 자체를 직접 확인한다).
	const IQuestOwner& owner = *quest;
	EXPECT_FALSE(owner.HasItem(1));
	EXPECT_FALSE(owner.HasSkill(1));

	inventory->AddItem(1, 1);
	skills->LearnSkill(1);

	EXPECT_TRUE(owner.HasItem(1));
	EXPECT_TRUE(owner.HasSkill(1));
}

TEST_F(PlayerQuestTest, Abandon_MainQuestIsRefused)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	ASSERT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);

	EXPECT_FALSE(quest.AbandonQuest(kGoblinHunt));
	EXPECT_TRUE(quest.IsActive(kGoblinHunt));
}

TEST_F(PlayerQuestTest, Abandon_SubQuestIsRemovedWithoutCompleting)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	ASSERT_EQ(quest.AcceptQuest(kFieldCleanup), QuestAcceptResult::Ok);

	EXPECT_TRUE(quest.AbandonQuest(kFieldCleanup));
	EXPECT_FALSE(quest.IsActive(kFieldCleanup));
	EXPECT_FALSE(quest.IsCompleted(kFieldCleanup));
}

// ============================================================
// 시간 정책 (반복 / 쿨타임 / 제한 시간)
// ============================================================

TEST_F(PlayerQuestTest, Repeatable_CannotBeReacceptedBeforeDailyReset)
{
	using namespace std::chrono;
	auto fake_now = system_clock::now();

	PlayerQuest quest;
	quest.SetClock([&fake_now] { return fake_now; });
	quest.Load(MakeExistingPlayerData(
		1001, std::vector<VOQuestActive>{}, FlagsWithCompleted({ kWolfPelt })));

	ASSERT_EQ(quest.AcceptQuest(kDailyWolf), QuestAcceptResult::Ok);
	quest.ReportProgress(QuestObjectiveType::Kill, kWolfMonsterId, 10);
	ASSERT_EQ(quest.GetState(kDailyWolf), QuestState::ReadyToComplete);
	ASSERT_TRUE(quest.CompleteQuest(kDailyWolf));

	// 완료 직후 같은 날 -> 아직 못 받는다(행은 쿨타임 상태로 남아 완료 시각을 기억한다)
	EXPECT_TRUE(quest.IsCompleted(kDailyWolf));
	EXPECT_FALSE(quest.IsActive(kDailyWolf));
	EXPECT_EQ(quest.GetState(kDailyWolf), QuestState::Cooldown);
	EXPECT_EQ(quest.AcceptQuest(kDailyWolf), QuestAcceptResult::OnCooldown);

	// 이틀 뒤 -> 일일 리셋 경계를 넘겨 다시 받을 수 있다
	fake_now += hours(48);
	EXPECT_EQ(quest.AcceptQuest(kDailyWolf), QuestAcceptResult::Ok);
	EXPECT_EQ(quest.GetStage(kDailyWolf), 1);
	EXPECT_EQ(quest.GetProgress(kDailyWolf, 0), 0);
}

TEST_F(PlayerQuestTest, LimitedTime_ExpiresIntoFailedState)
{
	using namespace std::chrono;
	auto fake_now = system_clock::now();

	PlayerQuest quest;
	quest.SetClock([&fake_now] { return fake_now; });
	quest.Load(MakeExistingPlayerData(
		1001, std::vector<VOQuestActive>{},
		FlagsWithCompleted({ kGoblinHunt, kGoblinChief })));

	ASSERT_EQ(quest.AcceptQuest(kAncientLetter), QuestAcceptResult::Ok);

	// 제한 시간 1800초. 아직 안 지났으면 그대로 진행 중.
	fake_now += seconds(600);
	quest.Update(2.0f);
	EXPECT_EQ(quest.GetState(kAncientLetter), QuestState::InProgress);

	fake_now += seconds(1800);
	quest.Update(2.0f);
	EXPECT_EQ(quest.GetState(kAncientLetter), QuestState::Failed);
	EXPECT_FALSE(quest.IsActive(kAncientLetter));
}

TEST_F(PlayerQuestTest, LimitedTime_FailedQuestCanBeRetried)
{
	using namespace std::chrono;
	auto fake_now = system_clock::now();

	PlayerQuest quest;
	quest.SetClock([&fake_now] { return fake_now; });
	quest.Load(MakeExistingPlayerData(
		1001, std::vector<VOQuestActive>{},
		FlagsWithCompleted({ kGoblinHunt, kGoblinChief })));

	ASSERT_EQ(quest.AcceptQuest(kAncientLetter), QuestAcceptResult::Ok);
	fake_now += seconds(3600);
	quest.Update(2.0f);
	ASSERT_EQ(quest.GetState(kAncientLetter), QuestState::Failed);

	EXPECT_EQ(quest.AcceptQuest(kAncientLetter), QuestAcceptResult::Ok);
	EXPECT_EQ(quest.GetState(kAncientLetter), QuestState::InProgress);
	EXPECT_EQ(quest.GetStage(kAncientLetter), 1);
}

// ============================================================
// 이벤트 연동 (PlayerEventBroker 구독 -> 핸들러)
// ============================================================

TEST_F(PlayerQuestTest, Event_ActorDeadAdvancesKillObjective)
{
	GameObject go;
	PlayerQuest* quest = AttachQuest(go);
	quest->Load(MakeNewPlayerData());
	AdvanceToKillStage(*quest);

	auto* broker = go.GetComponent<PlayerEventBroker>();
	for (int i = 0; i < 3; ++i)
		broker->publish(EventActorDead{ 1, 500 + i, kGoblinMonsterId });

	EXPECT_EQ(quest->GetProgress(kGoblinHunt, 0), 3);
}

TEST_F(PlayerQuestTest, Event_ActorDeadWithoutDataIdIsIgnored)
{
	GameObject go;
	PlayerQuest* quest = AttachQuest(go);
	quest->Load(MakeNewPlayerData());
	AdvanceToKillStage(*quest);

	// 종류를 모르는 대상(플레이어 등)은 처치 목표로 셀 수 없다.
	go.GetComponent<PlayerEventBroker>()->publish(EventActorDead{ 1, 500, 0 });

	EXPECT_EQ(quest->GetProgress(kGoblinHunt, 0), 0);
}

TEST_F(PlayerQuestTest, Event_NpcInteractionCompletesReadyQuest)
{
	GameObject go;
	PlayerQuest* quest = AttachQuest(go);
	quest->Load(MakeExistingPlayerData(
		1001, std::vector<VOQuestActive>{}, FlagsWithCompleted({ kGoblinHunt })));

	// 1002 는 선택 보상이 없으므로 완료 NPC 와 대화하는 것만으로 끝난다.
	ASSERT_EQ(quest->AcceptQuest(kGoblinChief), QuestAcceptResult::Ok);
	auto* broker = go.GetComponent<PlayerEventBroker>();

	broker->publish(EventActorDead{ 1, 500, kOrcMonsterId }); // 스테이지 1 완료
	ASSERT_EQ(quest->GetStage(kGoblinChief), 2);

	broker->publish(EventNpcInteracted{ 1001, kElderNpcId }); // 보고 + 완료 접수
	EXPECT_TRUE(quest->IsCompleted(kGoblinChief));
	EXPECT_FALSE(quest->IsActive(kGoblinChief));
}

TEST_F(PlayerQuestTest, Event_NpcInteractionDoesNotAutoCompleteChoiceRewardQuest)
{
	GameObject go;
	PlayerQuest* quest = AttachQuest(go);
	quest->Load(MakeNewPlayerData());
	AdvanceToKillStage(*quest);

	auto* broker = go.GetComponent<PlayerEventBroker>();
	broker->publish(EventActorDead{ 1, 500, kGoblinMonsterId });
	quest->ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 9);
	quest->ReportProgress(QuestObjectiveType::Collect, kGoblinEarItemId, 5);
	ASSERT_EQ(quest->GetStage(kGoblinHunt), 3);

	// 1001 은 선택 보상이 있어 대화만으로 끝나면 안 된다(무엇을 줄지 정할 수 없다).
	broker->publish(EventNpcInteracted{ 1001, kElderNpcId });
	EXPECT_EQ(quest->GetState(kGoblinHunt), QuestState::ReadyToComplete);
	EXPECT_FALSE(quest->IsCompleted(kGoblinHunt));
}

TEST_F(PlayerQuestTest, Event_AreaEnteredAdvancesReachObjective)
{
	GameObject go;
	PlayerQuest* quest = AttachQuest(go);
	quest->Load(MakeNewPlayerData(1001, 1));
	ASSERT_EQ(quest->AcceptQuest(kFirstSteps), QuestAcceptResult::Ok);

	auto* broker = go.GetComponent<PlayerEventBroker>();
	broker->publish(EventAreaEntered{ 1001, 1 });  // 다른 맵
	EXPECT_EQ(quest->GetProgress(kFirstSteps, 0), 0);

	broker->publish(EventAreaEntered{ 1001, 2 });  // 목표 맵(Dark Forest)
	EXPECT_EQ(quest->GetProgress(kFirstSteps, 0), 1);
}

TEST_F(PlayerQuestTest, Event_LevelUpCompletesAutoCompleteQuest)
{
	GameObject go;
	PlayerQuest* quest = AttachQuest(go);
	quest->Load(MakeNewPlayerData(1001, 1));
	ASSERT_EQ(quest->AcceptQuest(kFirstSteps), QuestAcceptResult::Ok);

	auto* broker = go.GetComponent<PlayerEventBroker>();
	broker->publish(EventAreaEntered{ 1001, 2 });
	EXPECT_FALSE(quest->IsCompleted(kFirstSteps));

	broker->publish(EventLevelUp{ 1001, 3 });
	EXPECT_TRUE(quest->IsCompleted(kFirstSteps)); // auto_complete 라 즉시 끝난다
}

TEST_F(PlayerQuestTest, Event_QuestAcceptedAndCompletedArePublished)
{
	struct Listener
	{
		int accepted = 0;
		int completed = 0;
		void OnAccepted(const EventQuestAccepted&) { ++accepted; }
		void OnCompleted(const EventQuestCompleted&) { ++completed; }
	} listener;

	GameObject go;
	PlayerQuest* quest = AttachQuest(go);
	auto* broker = go.GetComponent<PlayerEventBroker>();
	broker->subscribe<Listener, EventQuestAccepted, &Listener::OnAccepted>(&listener);
	broker->subscribe<Listener, EventQuestCompleted, &Listener::OnCompleted>(&listener);

	quest->Load(MakeNewPlayerData());
	ASSERT_EQ(quest->AcceptQuest(kFieldCleanup), QuestAcceptResult::Ok);
	EXPECT_EQ(listener.accepted, 1);

	quest->ReportProgress(QuestObjectiveType::Kill, kSlimeMonsterId, 10);
	EXPECT_EQ(listener.completed, 1);
}

// ============================================================
// 운영(GM)
// ============================================================

TEST_F(PlayerQuestTest, Gm_DisabledQuestCannotBeAcceptedNormally)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());

	// 9001 은 데이터에서 내려둔 퀘스트다.
	EXPECT_FALSE(QuestRegistry::Instance().Get(kDisabledEvent)->IsEnabled());
	EXPECT_EQ(quest.AcceptQuest(kDisabledEvent), QuestAcceptResult::Disabled);
	EXPECT_FALSE(quest.IsActive(kDisabledEvent));
}

TEST_F(PlayerQuestTest, Gm_ForceAcceptBypassesConditions)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData(1001, 1)); // 레벨 1 — 1002 는 레벨/선행 모두 미달

	// 레벨을 먼저 보므로 레벨 사유가 나온다(둘 다 미달인 상태를 만든 것이 요점).
	ASSERT_EQ(quest.AcceptQuest(kGoblinChief), QuestAcceptResult::LevelTooLow);

	EXPECT_TRUE(quest.GmForceAccept(kGoblinChief));
	EXPECT_TRUE(quest.IsActive(kGoblinChief));
	EXPECT_EQ(quest.GetStage(kGoblinChief), 1);

	// 내려둔 퀘스트도 운영은 넣을 수 있다.
	EXPECT_TRUE(quest.GmForceAccept(kDisabledEvent));
	EXPECT_TRUE(quest.IsActive(kDisabledEvent));
}

TEST_F(PlayerQuestTest, Gm_ForceCompleteSkipsRemainingStages)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerItem* inventory = go.AddComponent<PlayerItem>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();

	const PlayerLoadData load = MakeNewPlayerData();
	inventory->Load(load);
	quest->Load(load);

	// 수락조차 하지 않은 상태에서 바로 완료시킨다(문의 대응에서 흔한 경우).
	EXPECT_TRUE(quest->GmForceComplete(kGoblinHunt));

	EXPECT_TRUE(quest->IsCompleted(kGoblinHunt));
	EXPECT_FALSE(quest->IsActive(kGoblinHunt));
	// 선택 보상을 지정하지 않으면 첫 번째(검 id 4)를 준다.
	EXPECT_EQ(inventory->GetCount(4), 1);
}

TEST_F(PlayerQuestTest, Gm_SetProgressAdvancesStageWhenFilled)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	ASSERT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);

	// 스테이지 2 의 두 목표를 한 번에 채워 준다 -> 스테이지 3 으로 넘어가야 한다.
	EXPECT_TRUE(quest.GmSetProgress(kGoblinHunt, 2, 10, 5));
	EXPECT_EQ(quest.GetStage(kGoblinHunt), 3);

	// 스테이지 범위를 벗어나면 거절한다.
	EXPECT_FALSE(quest.GmSetProgress(kGoblinHunt, 99, 1));
	EXPECT_FALSE(quest.GmSetProgress(kGoblinHunt, 0, 1));
}

TEST_F(PlayerQuestTest, Gm_ResetClearsProgressAndCompletion)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	ASSERT_TRUE(quest.GmForceComplete(kFieldCleanup));
	ASSERT_TRUE(quest.IsCompleted(kFieldCleanup));

	EXPECT_TRUE(quest.GmResetQuest(kFieldCleanup));

	EXPECT_FALSE(quest.IsCompleted(kFieldCleanup));
	EXPECT_FALSE(quest.IsActive(kFieldCleanup));
	// 한 번도 안 받은 상태이므로 정상 경로로 다시 받을 수 있어야 한다.
	EXPECT_EQ(quest.AcceptQuest(kFieldCleanup), QuestAcceptResult::Ok);
}

TEST_F(PlayerQuestTest, Gm_RewardMultipliersApplyAtGrantTime)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerWallet* wallet = go.AddComponent<PlayerWallet>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();

	const PlayerLoadData load = MakeNewPlayerData();
	wallet->Load(load);
	quest->Load(load);

	QuestPolicy::Instance().SetExpMultiplier(2.0);
	QuestPolicy::Instance().SetGoldMultiplier(3.0);

	ASSERT_EQ(quest->AcceptQuest(kFieldCleanup), QuestAcceptResult::Ok); // exp 200 / gold 100
	quest->ReportProgress(QuestObjectiveType::Kill, kSlimeMonsterId, 10);
	ASSERT_TRUE(quest->IsCompleted(kFieldCleanup));

	const auto rewards = quest->TakePendingRewards();
	ASSERT_EQ(rewards.size(), 1u);
	EXPECT_EQ(rewards[0].exp, 400);
	EXPECT_EQ(rewards[0].gold, 300);
	EXPECT_EQ(wallet->GetGold(), 300);

	QuestPolicy::Instance().Reset(); // 전역이므로 다른 테스트에 새지 않게 되돌린다
}

TEST_F(PlayerQuestTest, Gm_RewardMultiplierNeverZeroesAReward)
{
	QuestPolicy::Instance().SetExpMultiplier(0.001);
	EXPECT_EQ(QuestPolicy::Instance().ApplyExp(200), 1); // 0 이 되어 보상이 사라지면 안 된다
	EXPECT_EQ(QuestPolicy::Instance().ApplyExp(0), 0);   // 원래 없던 보상은 그대로 없다

	// 0 이하 배율은 무시하고 1배로 되돌린다.
	QuestPolicy::Instance().SetExpMultiplier(-5.0);
	EXPECT_EQ(QuestPolicy::Instance().GetExpMultiplier(), 1.0);

	QuestPolicy::Instance().Reset();
}

// ============================================================
// Load / Save (DB 왕복)
// ============================================================

TEST_F(PlayerQuestTest, Load_NoActiveQuests)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());

	EXPECT_FALSE(quest.IsActive(kGoblinHunt));
	EXPECT_FALSE(quest.IsCompleted(0));
	EXPECT_FALSE(quest.IsDirty());
}

TEST_F(PlayerQuestTest, Load_ActiveQuestFieldsPreserved)
{
	std::vector<VOQuestActive> actives;
	actives.push_back(MakeQuestVO(1001, kGoblinHunt, 0, 2, 5, 3, 0));

	PlayerQuest quest;
	quest.Load(MakeExistingPlayerData(1001, actives, std::string{}));

	const VOQuestActive* vo = quest.GetActiveQuest(kGoblinHunt);
	ASSERT_NE(vo, nullptr);
	EXPECT_EQ(vo->character_id, 1001);
	EXPECT_EQ(vo->stage, 2);
	EXPECT_EQ(vo->progress1, 5);
	EXPECT_EQ(vo->progress2, 3);
	EXPECT_TRUE(quest.IsActive(kGoblinHunt));
}

TEST_F(PlayerQuestTest, Load_LegacyRowWithoutStageBecomesStageOne)
{
	// stage 컬럼이 없던 시절 행은 0 으로 들어온다.
	std::vector<VOQuestActive> actives;
	actives.push_back(MakeQuestVO(1001, kGoblinHunt, 0, 0, 0, 0, 0));

	PlayerQuest quest;
	quest.Load(MakeExistingPlayerData(1001, actives, std::string{}));

	EXPECT_EQ(quest.GetStage(kGoblinHunt), 1);
}

TEST_F(PlayerQuestTest, Load_CompletedFlagsCrossByteBoundary)
{
	std::string flags(2, '\0');
	flags[0] = static_cast<char>(0x80);  // quest 7
	flags[1] = static_cast<char>(0x01);  // quest 8

	PlayerQuest quest;
	quest.Load(MakeExistingPlayerData(1001, std::vector<VOQuestActive>{}, flags));

	EXPECT_FALSE(quest.IsCompleted(6));
	EXPECT_TRUE(quest.IsCompleted(7));
	EXPECT_TRUE(quest.IsCompleted(8));
	EXPECT_FALSE(quest.IsCompleted(9));
}

TEST_F(PlayerQuestTest, Save_AcceptedQuestGetsInsertAction)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	ASSERT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);

	PlayerSaveData saved = DoSave(quest);

	ASSERT_TRUE(saved.quest_actives.has_value());
	ASSERT_EQ(saved.quest_actives->size(), 1u);
	EXPECT_EQ((*saved.quest_actives)[0].action, DbAction::Insert);
	EXPECT_EQ((*saved.quest_actives)[0].vo.quest_id, kGoblinHunt);
	EXPECT_EQ((*saved.quest_actives)[0].vo.stage, 1);
}

TEST_F(PlayerQuestTest, Save_ProgressOnLoadedQuestGetsUpdateAction)
{
	std::vector<VOQuestActive> actives;
	actives.push_back(MakeQuestVO(1001, kGoblinHunt, 0, 2, 0, 0, 0));

	PlayerQuest quest;
	quest.Load(MakeExistingPlayerData(1001, actives, std::string{}));
	quest.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 4);

	PlayerSaveData saved = DoSave(quest);

	ASSERT_TRUE(saved.quest_actives.has_value());
	ASSERT_EQ(saved.quest_actives->size(), 1u);
	EXPECT_EQ((*saved.quest_actives)[0].action, DbAction::Update);
	EXPECT_EQ((*saved.quest_actives)[0].vo.progress1, 4);
}

TEST_F(PlayerQuestTest, Save_UnrelatedEventDoesNotDirtyTheRow)
{
	std::vector<VOQuestActive> actives;
	actives.push_back(MakeQuestVO(1001, kGoblinHunt, 0, 2, 0, 0, 0));

	PlayerQuest quest;
	quest.Load(MakeExistingPlayerData(1001, actives, std::string{}));

	// 이 스테이지의 목표와 무관한 처치 -> UPDATE 가 쌓이면 안 된다
	quest.ReportProgress(QuestObjectiveType::Kill, kWolfMonsterId, 5);

	PlayerSaveData saved = DoSave(quest);
	if (saved.quest_actives.has_value())
		EXPECT_TRUE(saved.quest_actives->empty());
}

TEST_F(PlayerQuestTest, Save_CompletedLoadedQuestGetsDeleteAction)
{
	std::vector<VOQuestActive> actives;
	// 스테이지 1(or 목표) 진행 중인 auto_complete 퀘스트
	actives.push_back(MakeQuestVO(1001, kFieldCleanup, 0, 1, 0, 0, 0));

	PlayerQuest quest;
	quest.Load(MakeExistingPlayerData(1001, actives, std::string{}));
	quest.ReportProgress(QuestObjectiveType::Kill, kSlimeMonsterId, 10);

	PlayerSaveData saved = DoSave(quest);

	ASSERT_TRUE(saved.quest_actives.has_value());
	ASSERT_EQ(saved.quest_actives->size(), 1u);
	EXPECT_EQ((*saved.quest_actives)[0].action, DbAction::Remove);
	EXPECT_EQ((*saved.quest_actives)[0].vo.quest_id, kFieldCleanup);
}

TEST_F(PlayerQuestTest, Save_QuestStateInsertForNewPlayerThenUpdate)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());

	PlayerSaveData first = DoSave(quest);
	ASSERT_TRUE(first.quest_state.has_value());
	EXPECT_EQ(first.quest_state->action, DbAction::Insert);
	EXPECT_EQ(first.quest_state->vo.character_id, 1001);

	ASSERT_EQ(quest.AcceptQuest(kFieldCleanup), QuestAcceptResult::Ok);
	quest.ReportProgress(QuestObjectiveType::Kill, kSlimeMonsterId, 10);

	PlayerSaveData second = DoSave(quest);
	ASSERT_TRUE(second.quest_state.has_value());
	EXPECT_EQ(second.quest_state->action, DbAction::Update);
}

TEST_F(PlayerQuestTest, Save_UnchangedExistingPlayerWritesNothing)
{
	PlayerQuest quest;
	quest.Load(MakeExistingPlayerData(1001));

	PlayerSaveData saved = DoSave(quest);
	EXPECT_FALSE(saved.quest_state.has_value());
}

TEST_F(PlayerQuestTest, DirtyFlag_TracksMutations)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());
	EXPECT_FALSE(quest.IsDirty());

	ASSERT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);
	EXPECT_TRUE(quest.IsDirty());

	PlayerSaveData save_data{};
	quest.Save(&save_data);
	quest.ClearDirty();
	EXPECT_FALSE(quest.IsDirty());

	quest.ReportProgress(QuestObjectiveType::Talk, kElderNpcId, 1);
	EXPECT_TRUE(quest.IsDirty());
}

TEST_F(PlayerQuestTest, RoundTrip_ProgressSurvivesReload)
{
	PlayerQuest quest;
	quest.Load(MakeNewPlayerData());

	ASSERT_EQ(quest.AcceptQuest(kGoblinHunt), QuestAcceptResult::Ok);
	quest.ReportProgress(QuestObjectiveType::Talk, kElderNpcId, 1);
	quest.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 6);

	PlayerSaveData saved = DoSave(quest);
	ASSERT_TRUE(saved.quest_actives.has_value());
	ASSERT_EQ(saved.quest_actives->size(), 1u);

	// 재접속: 저장된 행을 그대로 다시 로드한다.
	std::vector<VOQuestActive> reloaded;
	reloaded.push_back((*saved.quest_actives)[0].vo);

	std::string flags;
	if (saved.quest_state.has_value())
		flags = saved.quest_state->vo.flags;

	PlayerQuest reborn;
	reborn.Load(MakeExistingPlayerData(1001, reloaded, flags));

	EXPECT_TRUE(reborn.IsActive(kGoblinHunt));
	EXPECT_EQ(reborn.GetStage(kGoblinHunt), 2);
	EXPECT_EQ(reborn.GetProgress(kGoblinHunt, 0), 6);

	// 이어서 진행하면 그대로 스테이지가 넘어간다.
	reborn.ReportProgress(QuestObjectiveType::Kill, kGoblinMonsterId, 4);
	reborn.ReportProgress(QuestObjectiveType::Collect, kGoblinEarItemId, 5);
	EXPECT_EQ(reborn.GetStage(kGoblinHunt), 3);
}

// ============================================================
// 호위 / 보호
// ============================================================
// NPC 액터의 이동·사망 판정은 맵과 navmesh 가 필요해 여기서 다루지 않는다
// (NonPlayerCharacter 가 맡는다). 여기서는 그 결과로 오는 이벤트가 퀘스트에
// 어떻게 반영되는지를 본다.

TEST_F(PlayerQuestTest, Protect_AccumulatesSecondsAndAdvancesStage)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();
	quest->Load(MakeNewPlayerData());

	ASSERT_EQ(quest->AcceptQuest(kEscortMerchant), QuestAcceptResult::Ok);
	EXPECT_EQ(quest->GetStage(kEscortMerchant), 1);

	// 보호 목표의 진행도는 흐른 시간(초)이다. Update 가 1초 단위로 누적한다.
	for (int i = 0; i < 5; ++i)
		go.Update(1.0f);
	EXPECT_EQ(quest->GetProgress(kEscortMerchant, 0), 5);
	EXPECT_EQ(quest->GetStage(kEscortMerchant), 1);

	// 20초를 채우면 스테이지가 넘어간다.
	for (int i = 0; i < 15; ++i)
		go.Update(1.0f);
	EXPECT_EQ(quest->GetStage(kEscortMerchant), 2);
}

TEST_F(PlayerQuestTest, Protect_DoesNotTickBeforeItsStage)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();
	quest->Load(MakeNewPlayerData());

	// 보호 목표가 없는 퀘스트만 진행 중이면 시간이 흘러도 아무 일도 없어야 한다.
	ASSERT_EQ(quest->AcceptQuest(kWolfPelt), QuestAcceptResult::Ok);
	for (int i = 0; i < 30; ++i)
		go.Update(1.0f);
	EXPECT_EQ(quest->GetProgress(kWolfPelt, 0), 0);
}

TEST_F(PlayerQuestTest, Escort_ArrivalCompletesStage)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();
	quest->Load(MakeNewPlayerData());

	ASSERT_EQ(quest->AcceptQuest(kEscortMerchant), QuestAcceptResult::Ok);
	ASSERT_TRUE(quest->GmSetProgress(kEscortMerchant, 2, 0, 0, 0)); // 호위 스테이지로

	auto* broker = go.GetComponent<PlayerEventBroker>();
	broker->publish(EventNpcEscorted{ 1, kMerchantNpcId });

	// 마지막 스테이지를 끝냈으므로 완료 대기 상태가 된다(auto_complete 아님).
	EXPECT_EQ(quest->GetState(kEscortMerchant), QuestState::ReadyToComplete);
}

TEST_F(PlayerQuestTest, Escort_OtherNpcArrivalIsIgnored)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();
	quest->Load(MakeNewPlayerData());

	ASSERT_EQ(quest->AcceptQuest(kEscortMerchant), QuestAcceptResult::Ok);
	ASSERT_TRUE(quest->GmSetProgress(kEscortMerchant, 2, 0, 0, 0));

	go.GetComponent<PlayerEventBroker>()->publish(EventNpcEscorted{ 1, kElderNpcId });
	EXPECT_EQ(quest->GetState(kEscortMerchant), QuestState::InProgress);
	EXPECT_EQ(quest->GetProgress(kEscortMerchant, 0), 0);
}

TEST_F(PlayerQuestTest, NpcDeath_FailsProtectingQuest)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();
	quest->Load(MakeNewPlayerData());

	ASSERT_EQ(quest->AcceptQuest(kEscortMerchant), QuestAcceptResult::Ok);
	go.Update(1.0f);
	ASSERT_EQ(quest->GetProgress(kEscortMerchant, 0), 1);

	go.GetComponent<PlayerEventBroker>()->publish(EventNpcDead{ 1, kMerchantNpcId });

	// 지키던 대상이 죽었으면 되살아난 NPC 로 이어서 할 수 없다.
	EXPECT_EQ(quest->GetState(kEscortMerchant), QuestState::Failed);

	// 실패한 뒤에는 시간이 더 흘러도 진행도가 오르지 않는다.
	const int before = quest->GetProgress(kEscortMerchant, 0);
	for (int i = 0; i < 5; ++i)
		go.Update(1.0f);
	EXPECT_EQ(quest->GetProgress(kEscortMerchant, 0), before);
}

TEST_F(PlayerQuestTest, NpcDeath_FailsEscortingQuest)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();
	quest->Load(MakeNewPlayerData());

	ASSERT_EQ(quest->AcceptQuest(kEscortMerchant), QuestAcceptResult::Ok);
	ASSERT_TRUE(quest->GmSetProgress(kEscortMerchant, 2, 0, 0, 0));

	go.GetComponent<PlayerEventBroker>()->publish(EventNpcDead{ 1, kMerchantNpcId });
	EXPECT_EQ(quest->GetState(kEscortMerchant), QuestState::Failed);

	// 죽은 뒤 도착 이벤트가 와도(리스폰 등) 실패한 퀘스트는 되살아나지 않는다.
	go.GetComponent<PlayerEventBroker>()->publish(EventNpcEscorted{ 1, kMerchantNpcId });
	EXPECT_EQ(quest->GetState(kEscortMerchant), QuestState::Failed);
}

TEST_F(PlayerQuestTest, NpcDeath_LeavesUnrelatedQuestsAlone)
{
	GameObject go;
	go.AddComponent<PlayerEventBroker>();
	PlayerQuest* quest = go.AddComponent<PlayerQuest>();
	quest->Load(MakeNewPlayerData());

	ASSERT_EQ(quest->AcceptQuest(kEscortMerchant), QuestAcceptResult::Ok);
	ASSERT_EQ(quest->AcceptQuest(kWolfPelt), QuestAcceptResult::Ok);

	go.GetComponent<PlayerEventBroker>()->publish(EventNpcDead{ 1, kMerchantNpcId });

	EXPECT_EQ(quest->GetState(kEscortMerchant), QuestState::Failed);
	EXPECT_EQ(quest->GetState(kWolfPelt), QuestState::InProgress);
}
