// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "QuestFactory.h"



Quest* QuestFactory::Create(int32_t id) {
    Quest* obj = nullptr;
    const gamedata::Quest* res = ResourceLoader::Instance().GetQuests(id);
    if (res == nullptr) {
        return nullptr;
    }


    obj = new Quest();


    obj->gamedata = res;
    return obj;
} 