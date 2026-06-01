// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "QuestFactory.h"

#include "LimitedTimeQuest.h"

#include "MainQuest.h"

#include "RepeatedQuest.h"



Quest* QuestFactory::Create(int32_t id) {
    Quest* obj = nullptr;
    const gamedata::Quest* res = ResourceLoader::Instance().GetQuests(id);
    if (res == nullptr) {
        return nullptr;
    }


    const std::string& codeName = res->code_name();

    if (codeName == "LimitedTimeQuest") {
        obj = new LimitedTimeQuest();
    }

    else if (codeName == "MainQuest") {
        obj = new MainQuest();
    }

    else if (codeName == "RepeatedQuest") {
        obj = new RepeatedQuest();
    }

    else {

        return nullptr;

    }


    obj->gamedata = res;
    return obj;
} 