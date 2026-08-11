// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "NpcFactory.h"



Npc* NpcFactory::Create(int32_t id) {
    Npc* obj = nullptr;
    const gamedata::Npc* res = ResourceLoader::Instance().GetNpc(id);
    if (res == nullptr) {
        return nullptr;
    }


    obj = new Npc();


    obj->gamedata = res;
    return obj;
} 