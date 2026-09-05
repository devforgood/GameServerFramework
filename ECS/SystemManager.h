#pragma once
#include "ECS.h"
#include <vector>
#include <memory>
#include <functional>
#include <algorithm>
#include <tuple>
#include <utility>
#include <array>

namespace engine
{
    // Cache-friendly system that processes components in arrays
    template<typename... Components>
    class ComponentSystem : public engine::ISystem
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
    
    //-----------------------------------------------------------------------------------
    // 시스템이 도는 지점.
    //
    // 한 틱은 "무엇을 할지 정한다 -> 이동을 시뮬레이션한다 -> 그 결과를 반영한다" 순서이고,
    // 시스템마다 이 중 어디에 끼어야 하는지가 다르다. 이동 목표를 정하는 시스템이 이동
    // 뒤에 돌면 결정이 한 틱 늦게 반영되고, 이동 결과를 읽는 시스템이 이동 앞에 돌면
    // 지난 틱의 위치를 본다. 그래서 순서는 취향이 아니라 정확성이다.
    //
    //   PreMovement  : 이동 목표를 정하는 쪽 (몬스터 AI)
    //   PostMovement : 이동 결과를 읽는 쪽 (위치 변경 감지, 타이머)
    //
    // 같은 단계 안에서는 등록 순서대로 돈다.
    //-----------------------------------------------------------------------------------
    enum class SystemPhase
    {
        PreMovement,
        PostMovement,
    };

    inline constexpr size_t kSystemPhaseCount = 2;

    // System Manager for managing all systems
    class SystemManager
    {
    public:
        // 컴포넌트 배열을 원소마다 도는 시스템.
        template<typename... Components>
        void RegisterSystem(std::function<void(float, Components&...)> updateFunction,
            SystemPhase phase = SystemPhase::PostMovement)
        {
            auto system = std::make_unique<ComponentSystem<Components...>>(&m_EntityManager, updateFunction);
            AddSystem(std::move(system), phase);
        }

        // 원소마다 도는 형태가 아닌 시스템을 그대로 등록한다. 배치 패스로 짠 시스템
        // (예: 조건을 좁혀 가며 훑고 행동별 버킷으로 실행하는 몬스터 AI)이 이쪽이다.
        void AddSystem(std::unique_ptr<engine::ISystem> system,
            SystemPhase phase = SystemPhase::PostMovement)
        {
            m_Systems[static_cast<size_t>(phase)].push_back(std::move(system));
        }

        // 한 단계만 돌린다. 단계 사이에 ECS 밖의 일(이동 시뮬레이션 등)이 끼는 쪽에서 쓴다.
        void UpdatePhase(SystemPhase phase, float deltaTime)
        {
            for (auto& system : m_Systems[static_cast<size_t>(phase)])
            {
                system->Update(deltaTime);
            }
        }

        // 모든 단계를 순서대로 돌린다. 단계를 나눌 필요가 없는 쪽에서 쓴다 -
        // UpdatePhase 와 섞어 부르면 같은 시스템이 한 틱에 두 번 돈다.
        void Update(float deltaTime)
        {
            for (size_t phase = 0; phase < kSystemPhaseCount; ++phase)
            {
                UpdatePhase(static_cast<SystemPhase>(phase), deltaTime);
            }
        }
        
        EntityManager& GetEntityManager() { return m_EntityManager; }
        
    private:
        EntityManager m_EntityManager;
        std::array<std::vector<std::unique_ptr<engine::ISystem>>, kSystemPhaseCount> m_Systems;
    };
} 