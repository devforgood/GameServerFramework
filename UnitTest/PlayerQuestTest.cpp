#include <gtest/gtest.h>
#include "../Engine/PlayerQuest.h"
#include "../Engine/PlayerLoadData.h"
#include "../Engine/PlayerSaveData.h"
#include "../Engine/DbRecord.h"

// 실제 서버 흐름을 재현한 단위 테스트:
//   PlayerLoadData (DB 조회 결과) -> Load() -> 게임 로직 -> Save() -> PlayerSaveData (DB 저장 대상)
// 실제 DB 연결 없이 데이터 구조만 사용한다.

// ============================================================
// 헬퍼 함수
// ============================================================

static PlayerLoadData MakeNewPlayerData(int character_id = 1001)
{
    PlayerLoadData data{};
    data.player.id = character_id;
    data.player.name = "Tester";
    data.player.level = 1;
    // quest_state.character_id = 0 -> quest_state_is_new_ = true -> INSERT
    return data;
}

static PlayerLoadData MakeExistingPlayerData(
    int character_id,
    std::vector<QuestActiveVO> actives,
    std::string flags)
{
    PlayerLoadData data = MakeNewPlayerData(character_id);
    data.quest_actives = std::move(actives);
    data.quest_state.character_id = character_id;
    data.quest_state.flags = std::move(flags);
    return data;
}

static PlayerLoadData MakeExistingPlayerData(int character_id)
{
    return MakeExistingPlayerData(character_id, std::vector<QuestActiveVO>{}, std::string{});
}

static QuestActiveVO MakeQuestVO(
    int char_id, int quest_id,
    int state = 0, int p1 = 0, int p2 = 0, int p3 = 0)
{
    QuestActiveVO vo{};
    vo.character_id = char_id;
    vo.quest_id = quest_id;
    vo.state = state;
    vo.progress1 = p1;
    vo.progress2 = p2;
    vo.progress3 = p3;
    vo.accept_time = std::chrono::system_clock::now();
    return vo;
}

static PlayerSaveData DoSave(PlayerQuest& quest)
{
    PlayerSaveData save_data{};
    quest.Save(&save_data);
    return save_data;
}

// ============================================================
// Load 테스트
// ============================================================

TEST(PlayerQuestLoad, NoActiveQuests)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));

    EXPECT_FALSE(quest.IsActive(100));
    EXPECT_FALSE(quest.IsCompleted(0));
}

TEST(PlayerQuestLoad, WithActiveQuests_QuestsAreActive)
{
    std::vector<QuestActiveVO> actives;
    actives.push_back(MakeQuestVO(1001, 100));
    actives.push_back(MakeQuestVO(1001, 200));

    PlayerQuest quest;
    quest.Load(MakeExistingPlayerData(1001, actives, std::string{}));

    EXPECT_TRUE(quest.IsActive(100));
    EXPECT_TRUE(quest.IsActive(200));
    EXPECT_FALSE(quest.IsActive(300));
}

TEST(PlayerQuestLoad, ActiveQuestVO_AllFieldsPreserved)
{
    std::vector<QuestActiveVO> actives;
    actives.push_back(MakeQuestVO(1001, 100, 1, 5, 10, 15));

    PlayerQuest quest;
    quest.Load(MakeExistingPlayerData(1001, actives, std::string{}));

    const QuestActiveVO* q = quest.GetActiveQuest(100);
    EXPECT_NE(q, nullptr);
    if (q == nullptr) return;
    EXPECT_EQ(q->character_id, 1001);
    EXPECT_EQ(q->quest_id, 100);
    EXPECT_EQ(q->state, 1);
    EXPECT_EQ(q->progress1, 5);
    EXPECT_EQ(q->progress2, 10);
    EXPECT_EQ(q->progress3, 15);
}

TEST(PlayerQuestLoad, CompletedFlags_SingleBit)
{
    std::string flags(1, static_cast<char>(0x01));  // quest 0: byte 0, bit 0

    PlayerQuest quest;
    quest.Load(MakeExistingPlayerData(1001, std::vector<QuestActiveVO>{}, flags));

    EXPECT_TRUE(quest.IsCompleted(0));
    EXPECT_FALSE(quest.IsCompleted(1));
}

TEST(PlayerQuestLoad, CompletedFlags_CrossByteBoundary)
{
    std::string flags(2, '\0');
    flags[0] = static_cast<char>(0x80);  // quest 7: byte 0, bit 7
    flags[1] = static_cast<char>(0x01);  // quest 8: byte 1, bit 0

    PlayerQuest quest;
    quest.Load(MakeExistingPlayerData(1001, std::vector<QuestActiveVO>{}, flags));

    EXPECT_FALSE(quest.IsCompleted(6));
    EXPECT_TRUE(quest.IsCompleted(7));
    EXPECT_TRUE(quest.IsCompleted(8));
    EXPECT_FALSE(quest.IsCompleted(9));
}

// ============================================================
// Save 테스트 - quest_state action
// ============================================================

TEST(PlayerQuestSave, QuestState_InsertForNewPlayer)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));

    PlayerSaveData saved = DoSave(quest);

    EXPECT_TRUE(saved.quest_state.has_value());
    if (!saved.quest_state.has_value()) return;
    EXPECT_EQ(saved.quest_state->action, DbAction::Insert);
    EXPECT_EQ(saved.quest_state->vo.character_id, 1001);
}

TEST(PlayerQuestSave, QuestState_UpdateForExistingPlayer)
{
    PlayerQuest quest;
    quest.Load(MakeExistingPlayerData(1001));
    quest.AcceptQuest(100);
    quest.CompleteQuest(100);  // 완료 플래그 변경 -> quest_state 변경

    PlayerSaveData saved = DoSave(quest);

    EXPECT_TRUE(saved.quest_state.has_value());
    if (!saved.quest_state.has_value()) return;
    EXPECT_EQ(saved.quest_state->action, DbAction::Update);
}

TEST(PlayerQuestSave, QuestState_ExistingUnchanged_NoRecord)
{
    PlayerQuest quest;
    quest.Load(MakeExistingPlayerData(1001));

    PlayerSaveData saved = DoSave(quest);

    // 완료 플래그 변경 없음 -> quest_state 기록 없음
    EXPECT_FALSE(saved.quest_state.has_value());
}

TEST(PlayerQuestSave, QuestState_InsertBecomesUpdateAfterFirstSave)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));

    DoSave(quest);  // 1st save -> Insert

    quest.AcceptQuest(7);
    quest.CompleteQuest(7);                // 완료 플래그 변경
    PlayerSaveData saved = DoSave(quest);  // 2nd save -> Update

    EXPECT_TRUE(saved.quest_state.has_value());
    if (!saved.quest_state.has_value()) return;
    EXPECT_EQ(saved.quest_state->action, DbAction::Update);
}

TEST(PlayerQuestSave, QuestState_CharacterIdMatchesPlayer)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(9999));

    PlayerSaveData saved = DoSave(quest);

    EXPECT_TRUE(saved.quest_state.has_value());
    if (!saved.quest_state.has_value()) return;
    EXPECT_EQ(saved.quest_state->vo.character_id, 9999);
}

// ============================================================
// Save 테스트 - quest_actives action
// ============================================================

TEST(PlayerQuestSave, AcceptedQuest_GetsInsertAction)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(100);

    PlayerSaveData saved = DoSave(quest);

    EXPECT_TRUE(saved.quest_actives.has_value());
    if (!saved.quest_actives.has_value()) return;
    int count = static_cast<int>(saved.quest_actives->size());
    EXPECT_EQ(count, 1);
    if (count < 1) return;
    EXPECT_EQ((*saved.quest_actives)[0].action, DbAction::Insert);
    EXPECT_EQ((*saved.quest_actives)[0].vo.quest_id, 100);
    EXPECT_EQ((*saved.quest_actives)[0].vo.character_id, 1001);
}

TEST(PlayerQuestSave, ModifiedLoadedQuest_GetsUpdateAction)
{
    std::vector<QuestActiveVO> actives;
    actives.push_back(MakeQuestVO(1001, 100));

    PlayerQuest quest;
    quest.Load(MakeExistingPlayerData(1001, actives, std::string{}));
    quest.UpdateProgress(100, 5, 0, 0);  // 수정 -> UPDATE

    PlayerSaveData saved = DoSave(quest);

    EXPECT_TRUE(saved.quest_actives.has_value());
    if (!saved.quest_actives.has_value()) return;
    int count = static_cast<int>(saved.quest_actives->size());
    EXPECT_EQ(count, 1);
    if (count < 1) return;
    EXPECT_EQ((*saved.quest_actives)[0].action, DbAction::Update);
    EXPECT_EQ((*saved.quest_actives)[0].vo.quest_id, 100);
}

TEST(PlayerQuestSave, UnmodifiedLoadedQuest_NoRecord)
{
    std::vector<QuestActiveVO> actives;
    actives.push_back(MakeQuestVO(1001, 100));

    PlayerQuest quest;
    quest.Load(MakeExistingPlayerData(1001, actives, std::string{}));

    PlayerSaveData saved = DoSave(quest);

    // 변경 없음 -> quest_actives 기록 없음 (변경분만 기록)
    if (saved.quest_actives.has_value())
        EXPECT_TRUE(saved.quest_actives->empty());
}

TEST(PlayerQuestSave, AcceptedQuestBecomesUpdateAfterFirstSave)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(100);

    DoSave(quest);  // 1st save -> Insert

    quest.UpdateProgress(100, 9, 0, 0);    // 수정
    PlayerSaveData saved = DoSave(quest);  // 2nd save -> Update

    EXPECT_TRUE(saved.quest_actives.has_value());
    if (!saved.quest_actives.has_value()) return;
    int count = static_cast<int>(saved.quest_actives->size());
    EXPECT_EQ(count, 1);
    if (count < 1) return;
    EXPECT_EQ((*saved.quest_actives)[0].action, DbAction::Update);
}

TEST(PlayerQuestSave, CompletedLoadedQuest_GetsDeleteAction)
{
    std::vector<QuestActiveVO> actives;
    actives.push_back(MakeQuestVO(1001, 100));

    PlayerQuest quest;
    quest.Load(MakeExistingPlayerData(1001, actives, std::string{}));
    quest.CompleteQuest(100);

    PlayerSaveData saved = DoSave(quest);

    EXPECT_TRUE(saved.quest_actives.has_value());
    if (!saved.quest_actives.has_value()) return;
    int count = static_cast<int>(saved.quest_actives->size());
    EXPECT_EQ(count, 1);
    if (count < 1) return;
    EXPECT_EQ((*saved.quest_actives)[0].action, DbAction::Remove);
    EXPECT_EQ((*saved.quest_actives)[0].vo.quest_id, 100);
}

// 이번 세션에서 수락 후 저장 없이 완료 -> DB에 없으므로 Remove 레코드 불필요
TEST(PlayerQuestSave, AcceptAndCompleteBeforeSave_NoRecord)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(100);
    quest.CompleteQuest(100);

    PlayerSaveData saved = DoSave(quest);

    if (saved.quest_actives.has_value())
        EXPECT_TRUE(saved.quest_actives->empty());
}

TEST(PlayerQuestSave, CompletedFlags_PersistedInQuestStateVO)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(7);
    quest.CompleteQuest(7);  // bit 7 of byte 0 = 0x80

    PlayerSaveData saved = DoSave(quest);

    EXPECT_TRUE(saved.quest_state.has_value());
    if (!saved.quest_state.has_value()) return;
    const std::string& flags = saved.quest_state->vo.flags;
    EXPECT_FALSE(flags.empty());
    if (flags.empty()) return;
    EXPECT_TRUE((static_cast<uint8_t>(flags[0]) & 0x80) != 0);
}

TEST(PlayerQuestSave, UpdatedProgress_ReflectedInRecord)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(100);
    quest.UpdateProgress(100, 3, 5, 7);

    PlayerSaveData saved = DoSave(quest);

    EXPECT_TRUE(saved.quest_actives.has_value());
    if (!saved.quest_actives.has_value()) return;
    int count = static_cast<int>(saved.quest_actives->size());
    EXPECT_EQ(count, 1);
    if (count < 1) return;
    EXPECT_EQ((*saved.quest_actives)[0].vo.progress1, 3);
    EXPECT_EQ((*saved.quest_actives)[0].vo.progress2, 5);
    EXPECT_EQ((*saved.quest_actives)[0].vo.progress3, 7);
}

// ============================================================
// 게임 로직 테스트
// ============================================================

TEST(PlayerQuestLogic, AcceptQuest_ReturnsFalseIfAlreadyActive)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));

    EXPECT_TRUE(quest.AcceptQuest(100));
    EXPECT_FALSE(quest.AcceptQuest(100));
}

TEST(PlayerQuestLogic, CompleteQuest_ReturnsFalseIfNotActive)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));

    EXPECT_FALSE(quest.CompleteQuest(999));
}

TEST(PlayerQuestLogic, CompleteQuest_SetsIsCompleted_RemovesFromActive)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(100);
    EXPECT_FALSE(quest.IsCompleted(100));

    quest.CompleteQuest(100);

    EXPECT_TRUE(quest.IsCompleted(100));
    EXPECT_FALSE(quest.IsActive(100));
    EXPECT_EQ(quest.GetActiveQuest(100), nullptr);
}

TEST(PlayerQuestLogic, UpdateProgress_UpdatesFieldsCorrectly)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(100);
    quest.UpdateProgress(100, 3, 5, 7);

    const QuestActiveVO* vo = quest.GetActiveQuest(100);
    EXPECT_NE(vo, nullptr);
    if (vo == nullptr) return;
    EXPECT_EQ(vo->progress1, 3);
    EXPECT_EQ(vo->progress2, 5);
    EXPECT_EQ(vo->progress3, 7);
}

TEST(PlayerQuestLogic, UpdateProgress_NoopIfQuestNotActive)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.UpdateProgress(999, 10, 0, 0);  // 크래시 없이 무시

    EXPECT_EQ(quest.GetActiveQuest(999), nullptr);
}

// ============================================================
// Dirty flag 테스트
// ============================================================

TEST(PlayerQuestDirtyFlag, CleanAfterLoad)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));

    EXPECT_FALSE(quest.IsDirty());
}

TEST(PlayerQuestDirtyFlag, DirtyAfterAcceptQuest)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(100);

    EXPECT_TRUE(quest.IsDirty());
}

TEST(PlayerQuestDirtyFlag, DirtyAfterCompleteQuest)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(100);
    quest.ClearDirty();

    quest.CompleteQuest(100);
    EXPECT_TRUE(quest.IsDirty());
}

TEST(PlayerQuestDirtyFlag, DirtyAfterUpdateProgress)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(100);
    quest.ClearDirty();

    quest.UpdateProgress(100, 1, 0, 0);
    EXPECT_TRUE(quest.IsDirty());
}

TEST(PlayerQuestDirtyFlag, CleanAfterSaveAndClearDirty)
{
    PlayerQuest quest;
    quest.Load(MakeNewPlayerData(1001));
    quest.AcceptQuest(100);
    EXPECT_TRUE(quest.IsDirty());

    PlayerSaveData save_data{};
    quest.Save(&save_data);
    quest.ClearDirty();
    EXPECT_FALSE(quest.IsDirty());
}

// ============================================================
// 전체 라운드트립 테스트
// 실제 서버 흐름: Load -> 게임 플레이 -> Save -> 재로그인(Load) 재현
// ============================================================

TEST(PlayerQuestRoundTrip, LoadPlaySaveReload)
{
    // 초기 상태: quest 0 완료, quest 50 진행 중(progress1 = 2)
    std::string initial_flags(1, static_cast<char>(0x01));
    std::vector<QuestActiveVO> initial_actives;
    initial_actives.push_back(MakeQuestVO(1001, 50, 0, 2, 0, 0));

    PlayerQuest quest;
    quest.Load(MakeExistingPlayerData(1001, initial_actives, initial_flags));
    EXPECT_FALSE(quest.IsDirty());

    // 게임 플레이: quest 50 완료, quest 100 수락 후 진행도 업데이트
    quest.CompleteQuest(50);
    quest.AcceptQuest(100);
    quest.UpdateProgress(100, 3, 0, 0);
    EXPECT_TRUE(quest.IsDirty());

    // Player::SavePlayerData 흐름 재현
    PlayerSaveData save_data{};
    quest.Save(&save_data);
    quest.ClearDirty();
    EXPECT_FALSE(quest.IsDirty());

    // quest_actives 검증: quest 50 DELETE + quest 100 INSERT
    EXPECT_TRUE(save_data.quest_actives.has_value());
    if (!save_data.quest_actives.has_value()) return;
    int record_count = static_cast<int>(save_data.quest_actives->size());
    EXPECT_EQ(record_count, 2);
    if (record_count < 2) return;

    const DbRecord<QuestActiveVO>* del_rec = nullptr;
    const DbRecord<QuestActiveVO>* ins_rec = nullptr;
    for (int i = 0; i < record_count; ++i)
    {
        const DbRecord<QuestActiveVO>& r = (*save_data.quest_actives)[i];
        if (r.action == DbAction::Remove) del_rec = &r;
        if (r.action == DbAction::Insert) ins_rec = &r;
    }

    EXPECT_NE(del_rec, nullptr);
    if (del_rec != nullptr)
        EXPECT_EQ(del_rec->vo.quest_id, 50);

    EXPECT_NE(ins_rec, nullptr);
    if (ins_rec != nullptr)
    {
        EXPECT_EQ(ins_rec->vo.quest_id, 100);
        EXPECT_EQ(ins_rec->vo.progress1, 3);
    }

    // quest_state 검증: UPDATE + quest 0, 50 모두 완료 처리됨
    EXPECT_TRUE(save_data.quest_state.has_value());
    if (!save_data.quest_state.has_value()) return;
    EXPECT_EQ(save_data.quest_state->action, DbAction::Update);

    const std::string& flags = save_data.quest_state->vo.flags;
    int flags_size = static_cast<int>(flags.size());
    EXPECT_GE(flags_size, 7);
    if (flags_size >= 1)
        EXPECT_TRUE((static_cast<uint8_t>(flags[0]) & 0x01) != 0);  // quest 0
    if (flags_size >= 7)
        EXPECT_TRUE((static_cast<uint8_t>(flags[6]) & 0x04) != 0);  // quest 50: byte 6, bit 2

    // 재로그인 시뮬레이션: 이전 Save 결과로 다시 Load
    std::vector<QuestActiveVO> reloaded_actives;
    reloaded_actives.push_back(MakeQuestVO(1001, 100, 0, 3, 0, 0));
    quest.Load(MakeExistingPlayerData(1001, reloaded_actives, flags));

    EXPECT_TRUE(quest.IsActive(100));
    EXPECT_FALSE(quest.IsActive(50));
    EXPECT_TRUE(quest.IsCompleted(0));
    EXPECT_TRUE(quest.IsCompleted(50));
    EXPECT_FALSE(quest.IsCompleted(100));
}
