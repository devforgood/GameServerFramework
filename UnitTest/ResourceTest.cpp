#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include "GameData/ResourceLoader.h"

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
