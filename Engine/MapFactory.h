// This file is auto-generated. Do not modify directly.

#pragma once

#include <memory>
#include <string>
#include "Map.h"

class MapFactory {
public:
    MapFactory() = default;
    ~MapFactory() = default;

    static Map* Create(int32_t id);
}; 