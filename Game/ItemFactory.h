// This file is auto-generated. Do not modify directly.

#pragma once

#include <memory>
#include <string>
#include "Item.h"

class ItemFactory {
public:
    ItemFactory() = default;
    ~ItemFactory() = default;

    static Item* Create(int32_t id);
}; 