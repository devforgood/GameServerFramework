// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "MapFactory.h"



Map* MapFactory::Create(int32_t id) {
    Map* obj = nullptr;
    const gamedata::Map* res = ResourceLoader::Instance().GetMaps(id);
    if (res == nullptr) {
        return nullptr;
    }


    obj = new Map();


    obj->gamedata = res;
    return obj;
} 