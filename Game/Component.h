#pragma once

#include <cstdint>
#include "ComponentTypeId.h"

class GameObject;

class Component
{
public:
    GameObject* game_object = nullptr;

    virtual ~Component() = default;

    virtual void Start()
    {
    }

    virtual void Update()
    {
    }

    virtual uint32_t GetTypeId() const = 0;
};

template<typename T>
class ComponentBase : public Component
{
public:
    static uint32_t StaticTypeId()
    {
        return ComponentTypeId<T>::Get();
    }

    uint32_t GetTypeId() const override
    {
        return StaticTypeId();
    }
};