// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "LevelFactory.h"



Level* LevelFactory::Create(int32_t id) {
    Level* obj = nullptr;
    const gamedata::Level* res = ResourceLoader::Instance().GetLevel(id);
    if (res == nullptr) {
        return nullptr;
    }


    obj = new Level();


    obj->gamedata = res;
    return obj;
} 