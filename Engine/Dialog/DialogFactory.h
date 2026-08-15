// This file is auto-generated. Do not modify directly.

#pragma once

#include <memory>
#include <string>
#include "Dialog.h"

class DialogFactory {
public:
    DialogFactory() = default;
    ~DialogFactory() = default;

    static Dialog* Create(int32_t id);
}; 