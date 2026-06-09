#pragma once

#include <cstdint>

class TypeIdGenerator
{
public:
    static uint32_t Next()
    {
        static uint32_t next_id = 0;
        return next_id++;
    }
};

template<typename T>
class ComponentTypeId
{
public:
    static uint32_t Get()
    {
        static const uint32_t id = TypeIdGenerator::Next();
        return id;
    }
};

