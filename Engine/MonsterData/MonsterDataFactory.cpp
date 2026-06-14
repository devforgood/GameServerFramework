// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "MonsterDataFactory.h"



MonsterData* MonsterDataFactory::Create(int32_t id) {
    MonsterData* obj = nullptr;
    const gamedata::MonsterData* res = ResourceLoader::Instance().GetMonsterData(id);
    if (res == nullptr) {
        return nullptr;
    }


    obj = new MonsterData();


    obj->gamedata = res;
    return obj;
} 