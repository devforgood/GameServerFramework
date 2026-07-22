// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "SkillFactory.h"

#include "AuraSkill.h"

#include "JumpSkill.h"

#include "NormalAttackSkill.h"



Skill* SkillFactory::Create(int32_t id) {
    Skill* obj = nullptr;
    const gamedata::Skill* res = ResourceLoader::Instance().GetSkill(id);
    if (res == nullptr) {
        return nullptr;
    }


    const std::string& codeName = res->code_name;

    if (codeName == "AuraSkill") {
        obj = new AuraSkill();
    }

    else if (codeName == "JumpSkill") {
        obj = new JumpSkill();
    }

    else if (codeName == "NormalAttackSkill") {
        obj = new NormalAttackSkill();
    }

    else {

        obj = new Skill();

    }


    obj->gamedata = res;
    return obj;
} 