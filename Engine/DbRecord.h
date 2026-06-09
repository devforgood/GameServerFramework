#pragma once
#include <cstdint>

enum class DbAction : uint8_t
{
    Insert,
    Update,
    Remove
};

template<typename TVO>
struct DbRecord
{
    TVO      vo;
    DbAction action;
};
