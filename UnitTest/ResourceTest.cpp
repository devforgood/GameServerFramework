#include "pch.h"
#include <gtest/gtest.h>
#include <filesystem>
#include "../GameDataProtobuf/ResourceLoader.h"

TEST(ResourceTest, LoadItems)
{
    // Try multiple possible paths depending on where the test is executed from
    std::vector<std::string> possiblePaths = {
        "GameData/",
        "../Game/GameData/",
        "../../Game/GameData/",
        "../../../Game/GameData/"
    };

    bool loaded = false;
    for (const auto& path : possiblePaths)
    {
        if (std::filesystem::exists(path + "skill.bytes"))
        {
            loaded = ResourceLoader::Instance().LoadResources(path);
            break;
        }
    }

    EXPECT_TRUE(loaded) << "Could not find or load Resource Data.";

    auto itr = ResourceLoader::Instance().GetItems().find(1);
    EXPECT_NE(itr, ResourceLoader::Instance().GetItems().end());
    if (itr != ResourceLoader::Instance().GetItems().end())
    {
        EXPECT_EQ(itr->first, 1);
        EXPECT_FALSE(itr->second->name_id().empty());
    }
}
