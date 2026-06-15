#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include <memory>
#include <string>
#include <vector>

#include "GameData/ResourceLoader.h"
#include "GameObject.h"
#include "PlayerEventBroker.h"
#include "PlayerLevel.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "EventMessage.h"

// 이번 작업 검증 대상:
//   1) Level 테이블(level.json)  : 누적 필요 경험치 -> 레벨 매핑
//   2) MonsterData 테이블(monster.json) : 처치 보상 경험치(exp) 등 스탯
//   3) PlayerLevel 컴포넌트       : 처치 이벤트(EventActorDead) -> 경험치 획득 -> 레벨업 -> EventLevelUp 발행

namespace
{
    std::string ResolveGameDataPath()
    {
        const std::vector<std::string> candidates = {
            "GameData/",
            "../UnitTest/GameData/",
            "../../UnitTest/GameData/",
        };
        for (const auto& p : candidates)
        {
            if (std::filesystem::exists(p + "level.json") &&
                std::filesystem::exists(p + "monster.json"))
            {
                return p;
            }
        }
        return "";
    }

    PlayerLoadData MakePlayerData(int id, int level, std::string name = "Tester")
    {
        PlayerLoadData data{};
        data.player.id = id;
        data.player.name = std::move(name);
        data.player.level = level;
        return data;
    }

    // EventLevelUp 발행을 포착하기 위한 리스너
    class LevelUpListener
    {
    public:
        int count = 0;
        int lastNewLevel = 0;
        int lastPlayerId = 0;

        void OnLevelUp(const EventLevelUp& msg)
        {
            ++count;
            lastNewLevel = msg.new_level;
            lastPlayerId = msg.player_id;
        }
    };
}

class PlayerLevelTest : public ::testing::Test
{
protected:
    std::string dataPath_;

    void SetUp() override
    {
        dataPath_ = ResolveGameDataPath();
        ASSERT_FALSE(dataPath_.empty())
            << "level.json / monster.json 을 알려진 경로에서 찾지 못했습니다.";
        ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath_))
            << "LoadResources 실패";
    }

    // PlayerEventBroker + PlayerLevel 가 부착된 GameObject 를 구성한다.
    // (AddComponent 가 Start() 를 호출하므로 broker 를 먼저 추가해야 한다.)
    static PlayerLevel* AttachLevel(GameObject& go)
    {
        go.AddComponent<PlayerEventBroker>();
        return go.AddComponent<PlayerLevel>();
    }
};

//---------------------------------------------------------------------------------------
// Level 테이블 (level.json)
//---------------------------------------------------------------------------------------

TEST_F(PlayerLevelTest, LevelTable_Loaded)
{
    auto& loader = ResourceLoader::Instance();
    EXPECT_EQ(loader.GetLevels().size(), 20u);

    const gamedata::Level* l1 = loader.GetLevel(1);
    ASSERT_NE(l1, nullptr);
    EXPECT_EQ(l1->level, 1);
    EXPECT_EQ(l1->required_exp, 0);

    const gamedata::Level* l5 = loader.GetLevel(5);
    ASSERT_NE(l5, nullptr);
    EXPECT_EQ(l5->required_exp, 1000);

    const gamedata::Level* l20 = loader.GetLevel(20);
    ASSERT_NE(l20, nullptr);
    EXPECT_EQ(l20->required_exp, 19000);
}

TEST_F(PlayerLevelTest, LevelTable_RequiredExpIsMonotonic)
{
    auto& loader = ResourceLoader::Instance();
    int prev = -1;
    for (int lv = 1; lv <= 20; ++lv)
    {
        const gamedata::Level* l = loader.GetLevel(lv);
        ASSERT_NE(l, nullptr) << "레벨 " << lv << " 누락";
        EXPECT_GT(l->required_exp, prev) << "required_exp 가 단조 증가가 아님 (레벨 " << lv << ")";
        prev = l->required_exp;
    }
}

//---------------------------------------------------------------------------------------
// MonsterData 테이블 (monster.json)
//---------------------------------------------------------------------------------------

TEST_F(PlayerLevelTest, MonsterTable_Loaded)
{
    auto& loader = ResourceLoader::Instance();
    EXPECT_EQ(loader.GetMonsterDatas().size(), 10u);

    const gamedata::MonsterData* slime = loader.GetMonsterData(1);
    ASSERT_NE(slime, nullptr);
    EXPECT_EQ(slime->name, "Slime");
    EXPECT_EQ(slime->exp, 20);

    const gamedata::MonsterData* boss = loader.GetMonsterData(1001);
    ASSERT_NE(boss, nullptr);
    EXPECT_EQ(boss->exp, 5000);
}

TEST_F(PlayerLevelTest, MonsterTable_AllHaveRewardExp)
{
    auto& loader = ResourceLoader::Instance();
    for (const auto& [id, mob] : loader.GetMonsterDatas())
    {
        ASSERT_NE(mob, nullptr);
        EXPECT_GT(mob->exp, 0) << "monster id " << id << " 의 보상 경험치가 0 이하";
    }
}

//---------------------------------------------------------------------------------------
// PlayerLevel - 로드 / 경험치 복원
//---------------------------------------------------------------------------------------

TEST_F(PlayerLevelTest, Load_SetsLevelFromPlayerVO)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    EXPECT_EQ(level->GetLevel(), 1);
    EXPECT_EQ(level->GetExp(), 0); // 레벨 1 의 required_exp
}

TEST_F(PlayerLevelTest, Load_ReconstructsExpFromCurrentLevel)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 5));

    EXPECT_EQ(level->GetLevel(), 5);
    EXPECT_EQ(level->GetExp(), 1000); // 레벨 5 의 누적 필요 경험치
}

TEST_F(PlayerLevelTest, Load_InvalidLevelDefaultsToOne)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 0));

    EXPECT_EQ(level->GetLevel(), 1);
}

//---------------------------------------------------------------------------------------
// PlayerLevel - GainExp 레벨업 로직
//---------------------------------------------------------------------------------------

TEST_F(PlayerLevelTest, GainExp_BelowThreshold_NoLevelUp)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    level->GainExp(50); // 레벨 2 는 100 필요
    EXPECT_EQ(level->GetLevel(), 1);
    EXPECT_EQ(level->GetExp(), 50);
}

TEST_F(PlayerLevelTest, GainExp_ReachesThreshold_LevelsUp)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    level->GainExp(100);
    EXPECT_EQ(level->GetLevel(), 2);
}

TEST_F(PlayerLevelTest, GainExp_SkipsMultipleLevelsAtOnce)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    level->GainExp(1000); // 레벨 5 의 누적 필요 경험치
    EXPECT_EQ(level->GetLevel(), 5);
}

TEST_F(PlayerLevelTest, GainExp_CapsAtMaxLevel)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    level->GainExp(1000000); // 최대 레벨(20)을 초과하는 경험치
    EXPECT_EQ(level->GetLevel(), 20);
}

TEST_F(PlayerLevelTest, GainExp_NonPositiveIsIgnored)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    level->GainExp(0);
    level->GainExp(-100);
    EXPECT_EQ(level->GetLevel(), 1);
    EXPECT_EQ(level->GetExp(), 0);
}

//---------------------------------------------------------------------------------------
// PlayerLevel - EventLevelUp 발행
//---------------------------------------------------------------------------------------

TEST_F(PlayerLevelTest, LevelUp_PublishesEventLevelUp)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    LevelUpListener listener;
    go.GetComponent<PlayerEventBroker>()
        ->subscribe<LevelUpListener, EventLevelUp, &LevelUpListener::OnLevelUp>(&listener);

    level->GainExp(100); // 레벨 2 도달

    EXPECT_EQ(listener.count, 1);
    EXPECT_EQ(listener.lastNewLevel, 2);
    EXPECT_EQ(listener.lastPlayerId, 1001);
}

TEST_F(PlayerLevelTest, MultiLevelJump_PublishesOnceWithFinalLevel)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    LevelUpListener listener;
    go.GetComponent<PlayerEventBroker>()
        ->subscribe<LevelUpListener, EventLevelUp, &LevelUpListener::OnLevelUp>(&listener);

    level->GainExp(1000); // 레벨 5 까지 한 번에

    EXPECT_EQ(listener.count, 1);
    EXPECT_EQ(listener.lastNewLevel, 5);
}

TEST_F(PlayerLevelTest, NoLevelUp_DoesNotPublish)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    LevelUpListener listener;
    go.GetComponent<PlayerEventBroker>()
        ->subscribe<LevelUpListener, EventLevelUp, &LevelUpListener::OnLevelUp>(&listener);

    level->GainExp(50); // 임계치 미달

    EXPECT_EQ(listener.count, 0);
}

//---------------------------------------------------------------------------------------
// PlayerLevel - 몬스터 처치 이벤트(EventActorDead) 연동
//---------------------------------------------------------------------------------------

TEST_F(PlayerLevelTest, OnEventActorDead_GrantsExp)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    auto* broker = go.GetComponent<PlayerEventBroker>();
    broker->publish(EventActorDead{ 1, 100 }); // killer, victim

    EXPECT_EQ(level->GetExp(), 50); // 처치당 50 exp
    EXPECT_EQ(level->GetLevel(), 1);
}

TEST_F(PlayerLevelTest, OnEventActorDead_RepeatedKillsLevelUp)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    auto* broker = go.GetComponent<PlayerEventBroker>();
    broker->publish(EventActorDead{ 1, 100 }); // 50
    broker->publish(EventActorDead{ 1, 101 }); // 100 -> 레벨 2

    EXPECT_EQ(level->GetLevel(), 2);
}

//---------------------------------------------------------------------------------------
// PlayerLevel - 저장 / Dirty
//---------------------------------------------------------------------------------------

TEST_F(PlayerLevelTest, Save_WritesPlayerVO_PreservesNameAndId)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(7777, 1, "Hero"));
    level->GainExp(100); // 레벨 2

    PlayerSaveData save_data{};
    level->Save(&save_data);

    ASSERT_TRUE(save_data.player.has_value());
    EXPECT_EQ(save_data.player->id, 7777);
    EXPECT_EQ(save_data.player->name, "Hero");
    EXPECT_EQ(save_data.player->level, 2);
}

TEST_F(PlayerLevelTest, DirtyFlag_CleanAfterLoad)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    EXPECT_FALSE(level->IsDirty());
}

TEST_F(PlayerLevelTest, DirtyFlag_SetOnLevelUp)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    level->GainExp(100); // 레벨업
    EXPECT_TRUE(level->IsDirty());
}

TEST_F(PlayerLevelTest, DirtyFlag_NotSetWhenNoLevelUp)
{
    GameObject go;
    PlayerLevel* level = AttachLevel(go);
    level->Load(MakePlayerData(1001, 1));

    level->GainExp(50); // 레벨 변동 없음 -> exp 만 누적(미영속)
    EXPECT_FALSE(level->IsDirty());
}
