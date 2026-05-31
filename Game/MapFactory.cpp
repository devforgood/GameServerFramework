// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "MapFactory.h"



Map* MapFactory::Create(int32_t id) {
    Map* obj = nullptr;
    switch (id) {
        
        default: return nullptr;
    }

    obj->gamedata = ResourceLoader::Instance().GetMaps(id);

    return obj;
} 