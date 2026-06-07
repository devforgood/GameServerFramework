#pragma once

#include <string>
#include <vector>
#include <memory>
#include <algorithm>

#include "Component.h"

class GameObject
{
public:
    static constexpr size_t MaxComponents = 16;

private:
    std::string name_;

    std::vector<std::unique_ptr<Component>> components_;

public:
    GameObject()
    {
        components_.reserve(MaxComponents);
    }

    explicit GameObject(std::string name)
        : name_(std::move(name))
    {
        components_.reserve(MaxComponents);
    }

    virtual ~GameObject() = default;

public:
    const std::string& GetName() const
    {
        return name_;
    }

public:
    virtual void update(float dt)
    {
        (void)dt;

        for (auto& component : components_)
        {
            component->Update();
        }
    }

public:
    template<typename T>
    T* GetComponent() const
    {
        const uint32_t target_id = T::StaticTypeId();

        for (const auto& component : components_)
        {
            if (component->GetTypeId() == target_id)
            {
                return static_cast<T*>(component.get());
            }
        }

        return nullptr;
    }

    template<typename T>
    bool HasComponent() const
    {
        return GetComponent<T>() != nullptr;
    }

    template<typename T, typename... Args>
    T* AddComponent(Args&&... args)
    {
        if (auto* existing = GetComponent<T>())
        {
            return existing;
        }

        auto component =
            std::make_unique<T>(std::forward<Args>(args)...);

        component->game_object = this;

        T* ptr = component.get();

        components_.push_back(std::move(component));

        ptr->Start();

        return ptr;
    }

    template<typename T>
    bool RemoveComponent()
    {
        const uint32_t target_id = T::StaticTypeId();

        auto it = std::find_if(
            components_.begin(),
            components_.end(),
            [&](const auto& component)
            {
                return component->GetTypeId() == target_id;
            });

        if (it == components_.end())
        {
            return false;
        }

        components_.erase(it);

        return true;
    }

public:
    template<typename Func>
    void ForEachComponent(Func&& func)
    {
        for (auto& component : components_)
        {
            func(*component);
        }
    }

    template<typename Func>
    void ForEachComponent(Func&& func) const
    {
        for (const auto& component : components_)
        {
            func(*component);
        }
    }

public:
    size_t ComponentCount() const
    {
        return components_.size();
    }
};