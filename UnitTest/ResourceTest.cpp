#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include <unordered_set>
#include "GameData/ResourceLoader.h"

namespace
{
    void EnsureLoaded()
    {
        const std::string& path = GameDataPath::Resolve();
        ASSERT_TRUE(std::filesystem::exists(path + "Map.json"))
            << "통합 GameData 폴더를 찾지 못했습니다: " << path;
        ASSERT_TRUE(ResourceLoader::Instance().LoadResources(path)) << "LoadResources 실패";
    }
}

TEST(ResourceTest, LoadItems)
{
    // 리소스는 통합 폴더(Client/Assets/Resources/GameData) 한 곳에서 읽는다.
    const std::string& path = GameDataPath::Resolve();
    ASSERT_TRUE(std::filesystem::exists(path + "skill.json"))
        << "통합 GameData 폴더를 찾지 못했습니다: " << path;

    bool loaded = ResourceLoader::Instance().LoadResources(path);
    EXPECT_TRUE(loaded) << "Could not find or load Resource Data.";

    auto itr = ResourceLoader::Instance().GetItems().find(1);
    EXPECT_NE(itr, ResourceLoader::Instance().GetItems().end());
    if (itr != ResourceLoader::Instance().GetItems().end())
    {
        EXPECT_EQ(itr->first, 1);
        EXPECT_FALSE(itr->second->name_id.empty());
    }
}

//---------------------------------------------------------------------------------------
// id 를 가진 중첩 오브젝트(게이트/스폰 지점)의 인덱스와 parent 연결.
//
// id 는 종류마다 파일 전체에서 유일하고, ResourceLoader 가 종류별 인덱스 테이블을 만든다.
// 찾은 오브젝트는 parent 로 소속 상위 데이터를 돌려주므로, 게이트가 목적지 맵 id 를
// 따로 들고 다니지 않아도 된다(target_id 하나로 맵까지 따라간다).
//---------------------------------------------------------------------------------------

TEST(ResourceTest, NestedObjectIndexesAreBuilt)
{
    EnsureLoaded();
    auto& loader = ResourceLoader::Instance();

    // 맵 데이터 안의 게이트가 전부 인덱스에 올라와 있어야 한다.
    size_t gateCount = 0;
    for (const auto& [id, map] : loader.GetMaps())
        gateCount += map->gates.size();

    EXPECT_GT(gateCount, 0u) << "게이트가 하나도 없어 검증할 수 없습니다.";
    EXPECT_EQ(loader.GetMapGates().size(), gateCount)
        << "게이트 id 가 겹쳐 인덱스에서 서로를 덮어썼습니다.";

    size_t playerSpawnCount = 0;
    for (const auto& [id, map] : loader.GetMaps())
        playerSpawnCount += map->spawn_points.player_spawn.size();
    EXPECT_EQ(loader.GetMapSpawnPointsPlayerSpawns().size(), playerSpawnCount);
}

TEST(ResourceTest, NestedObjectsPointBackToTheirOwner)
{
    EnsureLoaded();
    auto& loader = ResourceLoader::Instance();

    for (const auto& [mapId, map] : loader.GetMaps())
    {
        for (const auto& gate : map->gates)
        {
            EXPECT_EQ(gate.parent, map)
                << "map " << mapId << " gate " << gate.id << " 의 parent 가 자기 맵이 아닙니다.";
            EXPECT_EQ(loader.GetMapGate(gate.id), &gate)
                << "map " << mapId << " gate " << gate.id << " 를 id 로 찾지 못했습니다.";
        }
        for (const auto& spawn : map->spawn_points.player_spawn)
        {
            EXPECT_EQ(spawn.parent, map);
            EXPECT_EQ(loader.GetMapSpawnPointsPlayerSpawn(spawn.id), &spawn);
        }
        for (const auto& spawn : map->spawn_points.monster_spawn)
            EXPECT_EQ(spawn.parent, map);
    }
}

// 게이트의 target_id 는 게이트이거나 player_spawn 이어야 하고, 그 marker 의 parent 가
// 목적지 맵이다. 이게 깨지면 게이트 이동이 통째로 실패한다(target_map_id 를 없앤 근거).
TEST(ResourceTest, GateTargetsResolveToAMap)
{
    EnsureLoaded();
    auto& loader = ResourceLoader::Instance();

    int checked = 0;
    for (const auto& [mapId, map] : loader.GetMaps())
    {
        for (const auto& gate : map->gates)
        {
            const gamedata::Map* dest = nullptr;
            if (const auto* target = loader.GetMapGate(gate.target_id))
                dest = target->parent;
            else if (const auto* spawn = loader.GetMapSpawnPointsPlayerSpawn(gate.target_id))
                dest = spawn->parent;

            EXPECT_NE(dest, nullptr)
                << "map " << mapId << " gate " << gate.id
                << " 의 target_id " << gate.target_id << " 를 풀지 못했습니다.";
            ++checked;
        }
    }
    EXPECT_GT(checked, 0);
}

// 스폰 지점 세 종류는 유니티 SpawnPoint 컴포넌트 하나에서 나오므로 id 공간을 공유한다.
// 종류가 달라도 겹치면 맵툴이 다음 스캔에서 번호를 다시 매겨 씬과 JSON 이 어긋난다.
TEST(ResourceTest, SpawnIdsAreUniqueAcrossSpawnTypes)
{
    EnsureLoaded();

    std::unordered_set<int> seen;
    for (const auto& [mapId, map] : ResourceLoader::Instance().GetMaps())
    {
        for (const auto& s : map->spawn_points.player_spawn)
            EXPECT_TRUE(seen.insert(s.id).second) << "스폰 id " << s.id << " 중복 (map " << mapId << ")";
        for (const auto& s : map->spawn_points.monster_spawn)
            EXPECT_TRUE(seen.insert(s.id).second) << "스폰 id " << s.id << " 중복 (map " << mapId << ")";
        for (const auto& s : map->spawn_points.boss_spawn)
            EXPECT_TRUE(seen.insert(s.id).second) << "스폰 id " << s.id << " 중복 (map " << mapId << ")";
    }
}
