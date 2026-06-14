// This file is auto-generated. Do not modify directly.

#pragma once

#include <memory>
#include <string>
#include "Skill.h"

class SkillFactory {
public:
    SkillFactory() = default;
    ~SkillFactory() = default;

    static Skill* Create(int32_t id);
}; 