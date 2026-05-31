// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "QuestFactory.h"



Quest* QuestFactory::Create(int32_t id) {
    Quest* obj = nullptr;
    switch (id) {
        
        default: return nullptr;
    }

    obj->gamedata = ResourceLoader::Instance().GetQuests(id);

    return obj;
} 