#pragma once
#include "ECS.h"
#include <vector>
#include <memory>
#include <functional>
#include <algorithm>
#include <tuple>
#include <utility>
#include <array>

namespace Engine
{
    // Cache-friendly system that processes components in arrays
    template<typename... Components>
    class ComponentSystem : public Engine::ISystem
    {
    public:
        using UpdateFunction = std::function<void(float, Components&...)>;
        
        ComponentSystem(EntityManager* entityManager, UpdateFunction updateFunc)
            : m_EntityManager(entityManager), m_UpdateFunction(updateFunc)
        {
        }
        
        void Update(float deltaTime) override
        {
            // Get component arrays for cache-friendly iteration
            auto componentArrays = GetComponentArrays();
            
            // Get the minimum size among all component arrays
            size_t minSize = GetMinArraySize(componentArrays);
            
            // Get raw pointers for maximum cache efficiency
            auto rawArrays = GetRawArrays(componentArrays);
            
            // Cache-friendly iteration over packed arrays using raw pointers
            for (size_t i = 0; i < minSize; ++i)
            {
                // Get components at current index using direct pointer access
                auto components = GetComponentsAtIndexDirect(rawArrays, i);
                
                // Call update function with components
                std::apply(m_UpdateFunction, std::tuple_cat(std::make_tuple(deltaTime), components));
            }
        }
        
    private:
        EntityManager* m_EntityManager;
        UpdateFunction m_UpdateFunction;
        
        // Get all component arrays for this system
        std::tuple<ComponentArray<Components>*...> GetComponentArrays()
        {
            return std::make_tuple(m_EntityManager->GetComponentArray<Components>()...);
        }
        
        // Get minimum size among all component arrays
        template<size_t... Is>
        size_t GetMinArraySizeImpl(const std::tuple<ComponentArray<Components>*...>& arrays, std::index_sequence<Is...>)
        {
            std::array<size_t, sizeof...(Components)> sizes = {std::get<Is>(arrays)->GetSize()...};
            return *std::min_element(sizes.begin(), sizes.end());
        }
        
        size_t GetMinArraySize(const std::tuple<ComponentArray<Components>*...>& arrays)
        {
            return GetMinArraySizeImpl(arrays, std::make_index_sequence<sizeof...(Components)>{});
        }
        
        // Get components at specific index from all arrays
        template<size_t... Is>
        std::tuple<Components&...> GetComponentsAtIndexImpl(
            const std::tuple<ComponentArray<Components>*...>& arrays, size_t index, std::index_sequence<Is...>)
        {
            return std::make_tuple(std::ref(std::get<Is>(arrays)->GetArray()[index])...);
        }
        
        std::tuple<Components&...> GetComponentsAtIndex(
            const std::tuple<ComponentArray<Components>*...>& arrays, size_t index)
        {
            return GetComponentsAtIndexImpl(arrays, index, std::make_index_sequence<sizeof...(Components)>{});
        }
        
        // Get raw array pointers for maximum cache efficiency
        std::tuple<Components*...> GetRawArrays(const std::tuple<ComponentArray<Components>*...>& arrays)
        {
            return GetRawArraysImpl(arrays, std::make_index_sequence<sizeof...(Components)>{});
        }
        
        template<size_t... Is>
        std::tuple<Components*...> GetRawArraysImpl(
            const std::tuple<ComponentArray<Components>*...>& arrays, std::index_sequence<Is...>)
        {
            return std::make_tuple(std::get<Is>(arrays)->GetArray()...);
        }
        
        // Get components using direct pointer access
        std::tuple<Components&...> GetComponentsAtIndexDirect(
            const std::tuple<Components*...>& rawArrays, size_t index)
        {
            return GetComponentsAtIndexDirectImpl(rawArrays, index, std::make_index_sequence<sizeof...(Components)>{});
        }
        
        template<size_t... Is>
        std::tuple<Components&...> GetComponentsAtIndexDirectImpl(
            const std::tuple<Components*...>& rawArrays, size_t index, std::index_sequence<Is...>)
        {
            return std::make_tuple(std::ref(std::get<Is>(rawArrays)[index])...);
        }
    };
    
    // System Manager for managing all systems
    class SystemManager
    {
    public:
        template<typename... Components>
        void RegisterSystem(std::function<void(float, Components&...)> updateFunction)
        {
            auto system = std::make_unique<ComponentSystem<Components...>>(&m_EntityManager, updateFunction);
            m_Systems.push_back(std::move(system));
        }
        
        void Update(float deltaTime)
        {
            for (auto& system : m_Systems)
            {
                system->Update(deltaTime);
            }
        }
        
        EntityManager& GetEntityManager() { return m_EntityManager; }
        
    private:
        EntityManager m_EntityManager;
        std::vector<std::unique_ptr<Engine::ISystem>> m_Systems;
    };
} 