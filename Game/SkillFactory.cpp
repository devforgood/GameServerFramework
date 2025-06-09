// This file is auto-generated. Do not modify directly.

#include "SkillFactory.h"

#include "JumpSkill.h"

#include "NormalAttackSkill.h"



BaseSkill* SkillFactory::Create(int32_t id) {
    switch (id) {
        
        case 2: return new JumpSkill();
        
        case 1: return new NormalAttackSkill();
        
        default: return nullptr;
    }
} 