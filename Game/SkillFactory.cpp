// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "SkillFactory.h"

#include "JumpSkill.h"

#include "NormalAttackSkill.h"



Skill* SkillFactory::Create(int32_t id) {
    Skill* obj = nullptr;
    const gamedata::Skill* res = ResourceLoader::Instance().GetSkills(id);
    if (res == nullptr) {
        return nullptr;
    }


    const std::string& codeName = res->code_name();

    if (codeName == "JumpSkill") {
        obj = new JumpSkill();
    }

    else if (codeName == "NormalAttackSkill") {
        obj = new NormalAttackSkill();
    }

    else {

        return nullptr;

    }


    obj->gamedata = res;
    return obj;
} 