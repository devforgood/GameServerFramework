// This file is auto-generated. Do not modify directly.

#include "Common.h"
#include "DialogFactory.h"



Dialog* DialogFactory::Create(int32_t id) {
    Dialog* obj = nullptr;
    const gamedata::Dialog* res = ResourceLoader::Instance().GetDialog(id);
    if (res == nullptr) {
        return nullptr;
    }


    obj = new Dialog();


    obj->gamedata = res;
    return obj;
} 