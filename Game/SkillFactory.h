// This file is auto-generated. Do not modify directly.

#pragma once

#include <memory>
#include <string>
#include "BaseSkill.h"

class SkillFactory {
public:
    SkillFactory() = default;
    ~SkillFactory() = default;

    static BaseSkill* Create(int32_t id);
}; 