// This file is auto-generated. Do not modify directly.

#pragma once

#include <memory>
#include <string>
#include "BaseItem.h"

class ItemFactory {
public:
    ItemFactory() = default;
    ~ItemFactory() = default;

    static BaseItem* Create(int32_t id);
}; 