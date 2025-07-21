// This file is auto-generated. Do not modify directly.

#pragma once

#include <memory>
#include <string>
#include "GameMode.h"

class GameModeFactory {
public:
    GameModeFactory() = default;
    ~GameModeFactory() = default;

    static GameMode* Create(int32_t id);
}; 