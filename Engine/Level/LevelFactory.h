// This file is auto-generated. Do not modify directly.

#pragma once

#include <memory>
#include <string>
#include "Level.h"

class LevelFactory {
public:
    LevelFactory() = default;
    ~LevelFactory() = default;

    static Level* Create(int32_t id);
}; 