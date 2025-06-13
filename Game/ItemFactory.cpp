// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "ItemFactory.h"



Item* ItemFactory::Create(int32_t id) {
    Item* obj = nullptr;
    switch (id) {
        
        default: return nullptr;
    }

    obj->gamedata = ResourceLoader::Instance().GetSkills(id);

    return obj;
} 