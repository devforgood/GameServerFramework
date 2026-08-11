// This file is auto-generated. Do not modify directly.

#pragma once

#include <memory>
#include <string>
#include "Npc.h"

class NpcFactory {
public:
    NpcFactory() = default;
    ~NpcFactory() = default;

    static Npc* Create(int32_t id);
}; 