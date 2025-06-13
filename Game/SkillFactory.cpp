// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "SkillFactory.h"

#include "JumpSkill.h"

#include "NormalAttackSkill.h"



Skill* SkillFactory::Create(int32_t id) {
    Skill* obj = nullptr;
    switch (id) {
        
        case 2: obj = new JumpSkill(); break;
        
        case 1: obj = new NormalAttackSkill(); break;
        
        default: return nullptr;
    }

    obj->gamedata = ResourceLoader::Instance().GetSkills(id);

    return obj;
} 