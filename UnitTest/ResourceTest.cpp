#include "pch.h"
#include <gtest/gtest.h>
#include "../GameDataProtobuf/ResourceLoader.h"

TEST(ResourceTest, LoadItems)
{
    // Ensure resources are loaded
    bool loaded = ResourceLoader::Instance().LoadResources();
    EXPECT_TRUE(loaded);

    auto itr = ResourceLoader::Instance().GetItems().find(1);
    EXPECT_NE(itr, ResourceLoader::Instance().GetItems().end());
    if (itr != ResourceLoader::Instance().GetItems().end())
    {
        EXPECT_EQ(itr->first, 1);
        EXPECT_FALSE(itr->second->name_id().empty());
    }
}
