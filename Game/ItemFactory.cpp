// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "ItemFactory.h"



Item* ItemFactory::Create(int32_t id) {
    Item* obj = nullptr;
    const gamedata::Item* res = ResourceLoader::Instance().GetItems(id);
    if (res == nullptr) {
        return nullptr;
    }


    obj = new Item();


    obj->gamedata = res;
    return obj;
} 