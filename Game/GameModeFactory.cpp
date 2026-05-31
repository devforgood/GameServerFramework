// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "GameModeFactory.h"



GameMode* GameModeFactory::Create(int32_t id) {
    GameMode* obj = nullptr;
    switch (id) {
        
        default: return nullptr;
    }

    obj->gamedata = ResourceLoader::Instance().GetGameModes(id);

    return obj;
} 