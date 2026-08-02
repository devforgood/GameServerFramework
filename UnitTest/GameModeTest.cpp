#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include <memory>
#include <string>
#include <vector>

#include "spdlog/spdlog.h"
#include "spdlog/sinks/stdout_sinks.h"

#include "GameData/ResourceLoader.h"
#include "GameMode.h"
#include "GameModeFactory.h"
#include "World.h"

namespace
{
    // 리소스는 통합 폴더(Client/Assets/Resources/GameData) 한 곳에서 읽는다.
    // (json 과 lua 스크립트가 같은 폴더에서 관리된다.)
    std::string ResolveGameDataPath()
    {
        const std::string& p = GameDataPath::Resolve();
        if (std::filesystem::exists(p + "GameMode.json") &&
            std::filesystem::exists(p + "gamemode_raid.lua"))
        {
            return p;
        }
        return "";
    }

    // World::ValidateGameData 는 LOG("net") 로거를 쓴다(등록 안 돼 있으면 null 역참조).
    void EnsureNetLogger()
    {
        if (!spdlog::get("net"))
        {
            auto logger = std::make_shared<spdlog::logger>(
                "net", std::make_shared<spdlog::sinks::stdout_sink_mt>());
            logger->set_level(spdlog::level::debug);
            spdlog::register_logger(logger);
        }
    }
}

class GameModeTest : public ::testing::Test
{
protected:
    std::string dataPath_;

    void SetUp() override
    {
        dataPath_ = ResolveGameDataPath();
        ASSERT_FALSE(dataPath_.empty())
            << "GameMode.json / gamemode_raid.lua 를 알려진 경로에서 찾지 못했습니다.";
        ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath_))
            << "LoadResources 실패";
        // 진행 로직 lua 스크립트도 같은 경로에서 로드하도록 base path 를 맞춘다.
        GameMode::InitializeLua(dataPath_);
    }
};

//---------------------------------------------------------------------------------------
// 데이터 작업 검증: Field/Raid 만 남고 Dungeon/Arena 는 제거, script 연결 필드 존재
//---------------------------------------------------------------------------------------

TEST_F(GameModeTest, OnlyFieldAndRaidRemain)
{
    auto& loader = ResourceLoader::Instance();
    EXPECT_EQ(loader.GetGameModes().size(), 2u);
    EXPECT_NE(loader.GetGameMode(1), nullptr); // Field
    EXPECT_NE(loader.GetGameMode(2), nullptr); // Raid
    EXPECT_EQ(loader.GetGameMode(3), nullptr); // Dungeon 제거됨
    EXPECT_EQ(loader.GetGameMode(4), nullptr); // Arena 제거됨
}

// Map.json 과 GameMode.json 이 서로를 가리키는 id 가 전부 맞아야 한다.
// 모드 3/4(Dungeon/Arena)를 지웠을 때 그 모드를 가리키던 맵들이 아무 경고 없이
// 로드 대상에서 빠져 있던 적이 있어서, 그 상태를 테스트로 고정한다.
//
// 경고(도달 불가 맵 등)는 검사만 하고 실패시키지 않는다. 인스턴스 맵으로 들어가는
// 게이트가 없는 것은 데이터 오류가 아니라 아직 만들지 않은 콘텐츠라서다.
TEST_F(GameModeTest, MapAndGameModeReferencesAreConsistent)
{
    EnsureNetLogger();
    int warnings = 0;
    EXPECT_EQ(World::ValidateGameData(&warnings), 0)
        << "Map.json / GameMode.json 참조 정합성 오류 — 위 로그를 확인하세요.";
}

TEST_F(GameModeTest, EveryMapHasAnExistingGameMode)
{
    auto& loader = ResourceLoader::Instance();
    for (const auto& [id, mapData] : loader.GetMaps())
    {
        EXPECT_NE(loader.GetGameMode(mapData->game_mode_id), nullptr)
            << "map " << id << " ('" << mapData->name << "') 의 game_mode_id "
            << mapData->game_mode_id << " 가 GameMode.json 에 없습니다.";
    }
}

TEST_F(GameModeTest, FieldDataMatches)
{
    const gamedata::GameMode* f = ResourceLoader::Instance().GetGameMode(1);
    ASSERT_NE(f, nullptr);
    EXPECT_EQ(f->name, "Field");
    EXPECT_EQ(f->type, "field");
    EXPECT_EQ(f->script, "gamemode_field.lua"); // lua 연결
    EXPECT_FALSE(f->rules.has_time_limit);
    EXPECT_TRUE(f->rules.allow_pvp);
    EXPECT_TRUE(f->rules.respawn_enabled);
}

TEST_F(GameModeTest, RaidDataMatches)
{
    const gamedata::GameMode* r = ResourceLoader::Instance().GetGameMode(2);
    ASSERT_NE(r, nullptr);
    EXPECT_EQ(r->name, "Raid");
    EXPECT_EQ(r->type, "raid");
    EXPECT_EQ(r->script, "gamemode_raid.lua"); // lua 연결
    EXPECT_TRUE(r->rules.has_time_limit);
    EXPECT_EQ(r->rules.time_limit, 1800);
    EXPECT_EQ(r->boss_info.boss_id, 1001);
}

//---------------------------------------------------------------------------------------
// Field 모드 진행 로직 (lua)
//---------------------------------------------------------------------------------------

TEST_F(GameModeTest, FieldScriptLoads)
{
    std::unique_ptr<GameMode> mode(GameModeFactory::Create(1));
    ASSERT_NE(mode, nullptr);
    EXPECT_TRUE(mode->LoadScript());
}

TEST_F(GameModeTest, FieldNeverEnds)
{
    std::unique_ptr<GameMode> mode(GameModeFactory::Create(1));
    ASSERT_NE(mode, nullptr);
    ASSERT_TRUE(mode->LoadScript());

    mode->Start();
    mode->Update(1.0f);
    mode->Update(100.0f);

    // Field 는 end_condition = none → 절대 종료되지 않는다.
    EXPECT_FALSE(mode->IsEnded());
}

//---------------------------------------------------------------------------------------
// Raid 모드 진행 로직 (lua)
//---------------------------------------------------------------------------------------

TEST_F(GameModeTest, RaidStartInitsTimer)
{
    std::unique_ptr<GameMode> mode(GameModeFactory::Create(2));
    ASSERT_NE(mode, nullptr);
    ASSERT_TRUE(mode->LoadScript());

    mode->Start();

    // gamemode_raid.lua on_start: GM_StartTimer(GM_GetTimeLimit) → 1800초
    EXPECT_FLOAT_EQ(mode->remaining_time(), 1800.0f);
    EXPECT_FALSE(mode->IsEnded());
}

TEST_F(GameModeTest, RaidClearsOnBossDead)
{
    std::unique_ptr<GameMode> mode(GameModeFactory::Create(2));
    ASSERT_NE(mode, nullptr);
    ASSERT_TRUE(mode->LoadScript());

    mode->Start();
    mode->set_boss_dead(true);
    mode->Update(1.0f); // check_end: IsBossDead → "clear" 종료

    EXPECT_TRUE(mode->IsEnded());
    EXPECT_EQ(mode->result(), "clear");
}

TEST_F(GameModeTest, RaidFailsOnTimeout)
{
    std::unique_ptr<GameMode> mode(GameModeFactory::Create(2));
    ASSERT_NE(mode, nullptr);
    ASSERT_TRUE(mode->LoadScript());

    mode->Start();
    mode->Update(2000.0f); // 1800초 타이머 소진 → on_update 가 fail_timeout 처리

    EXPECT_TRUE(mode->IsEnded());
    EXPECT_EQ(mode->result(), "fail_timeout");
}

TEST_F(GameModeTest, RaidFailsOnWipe)
{
    std::unique_ptr<GameMode> mode(GameModeFactory::Create(2));
    ASSERT_NE(mode, nullptr);
    ASSERT_TRUE(mode->LoadScript());

    mode->Start();
    mode->set_alive_player_count(0);
    mode->OnPlayerDead(nullptr); // alive==0 → fail_wipe + RequestEnd

    EXPECT_TRUE(mode->end_requested());
    EXPECT_EQ(mode->result(), "fail_wipe");

    mode->Update(1.0f); // check_end → IsEndRequested → 종료
    EXPECT_TRUE(mode->IsEnded());
}
