// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "GameModeFactory.h"



GameMode* GameModeFactory::Create(int32_t id) {
    GameMode* obj = nullptr;
    const gamedata::GameMode* res = ResourceLoader::Instance().GetGameModes(id);
    if (res == nullptr) {
        return nullptr;
    }


    obj = new GameMode();


    obj->gamedata = res;
    return obj;
} 