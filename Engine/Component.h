#pragma once

#include <cstdint>
#include "ComponentTypeId.h"
#include <any>

class GameObject;

class Component
{
public:
    GameObject* game_object = nullptr;

    virtual ~Component() = default;

    virtual void Start()
    {
    }

    virtual void Update(float dt)
    {
    }

    virtual uint32_t GetTypeId() const = 0;

    bool IsDirty() const { return dirty_; }
    void ClearDirty() { dirty_ = false; }
    virtual void Save(std::any data) { };
    virtual void Load(std::any data) {};

protected:
    void markDirty() { dirty_ = true; }

private:
    bool dirty_ = false;
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