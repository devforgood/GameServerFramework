#pragma once
#include "ECS.h"
#include <vector>
#include <memory>
#include <functional>
#include <algorithm>
#include <tuple>
#include <utility>
#include <array>
#include <immintrin.h> // For SIMD intrinsics

namespace engine
{
    // Cache-optimized batch processing system
    template<typename... Components>
    class CacheOptimizedSystem : public engine::ISystem
    {
    public:
        using BatchUpdateFunction = std::function<void(float, size_t, Components*...)>;
        using SingleUpdateFunction = std::function<void(float, Components&...)>;
        
        static constexpr size_t BATCH_SIZE = 64; // Optimize for cache line size
        
        CacheOptimizedSystem(EntityManager* entityManager, BatchUpdateFunction batchFunc)
            : m_EntityManager(entityManager), m_BatchUpdateFunction(batchFunc), m_UseBatchProcessing(true)
        {
        }
        
        CacheOptimizedSystem(EntityManager* entityManager, SingleUpdateFunction singleFunc)
            : m_EntityManager(entityManager), m_SingleUpdateFunction(singleFunc), m_UseBatchProcessing(false)
        {
        }
        
        void Update(float deltaTime) override
        {
            if (m_UseBatchProcessing)
            {
                UpdateBatch(deltaTime);
            }
            else
            {
                UpdateSingle(deltaTime);
            }
        }
        
    private:
        EntityManager* m_EntityManager;
        BatchUpdateFunction m_BatchUpdateFunction;
        SingleUpdateFunction m_SingleUpdateFunction;
        bool m_UseBatchProcessing;
        
        void UpdateBatch(float deltaTime)
        {
            // Get component arrays
            auto componentArrays = GetComponentArrays();
            size_t totalSize = GetMinArraySize(componentArrays);
            
            if (totalSize == 0) return;
            
            // Get raw pointers for maximum cache efficiency
            auto rawArrays = GetRawArrays(componentArrays);
            
            // Process in batches for better cache utilization
            for (size_t batchStart = 0; batchStart < totalSize; batchStart += BATCH_SIZE)
            {
                size_t batchEnd = std::min(batchStart + BATCH_SIZE, totalSize);
                size_t batchSize = batchEnd - batchStart;
                
                // Call batch update function with offset pointers
                std::apply([&](auto*... arrays) {
                    m_BatchUpdateFunction(deltaTime, batchSize, (arrays + batchStart)...);
                }, rawArrays);
            }
        }
        
        void UpdateSingle(float deltaTime)
        {
            // Get component arrays
            auto componentArrays = GetComponentArrays();
            size_t totalSize = GetMinArraySize(componentArrays);
            
            if (totalSize == 0) return;
            
            // Get raw pointers for maximum cache efficiency
            auto rawArrays = GetRawArrays(componentArrays);
            
            // Cache-friendly single element processing
            for (size_t i = 0; i < totalSize; ++i)
            {
                auto components = GetComponentsAtIndexDirect(rawArrays, i);
                std::apply(m_SingleUpdateFunction, std::tuple_cat(std::make_tuple(deltaTime), components));
            }
        }
        
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
    
    // SIMD-optimized system for specific component types
    class SIMDOptimizedMovementSystem : public engine::ISystem
    {
    public:
        SIMDOptimizedMovementSystem(EntityManager* entityManager)
            : m_EntityManager(entityManager)
        {
        }
        
        void Update(float deltaTime) override
        {
            auto* positionArray = m_EntityManager->GetComponentArray<PositionComponent>();
            auto* velocityArray = m_EntityManager->GetComponentArray<VelocityComponent>();
            
            size_t size = std::min(positionArray->GetSize(), velocityArray->GetSize());
            if (size == 0) return;
            
            PositionComponent* positions = positionArray->GetArray();
            VelocityComponent* velocities = velocityArray->GetArray();
            
            // SIMD-optimized processing (4 components at a time)
            size_t simdSize = (size / 4) * 4;
            
            __m128 deltaTimeVec = _mm_set1_ps(deltaTime);
            __m128 frictionVec = _mm_set1_ps(0.95f);
            
            for (size_t i = 0; i < simdSize; i += 4)
            {
                // Load 4 position components
                __m128 posX = _mm_setr_ps(positions[i].x, positions[i+1].x, positions[i+2].x, positions[i+3].x);
                __m128 posY = _mm_setr_ps(positions[i].y, positions[i+1].y, positions[i+2].y, positions[i+3].y);
                __m128 posZ = _mm_setr_ps(positions[i].z, positions[i+1].z, positions[i+2].z, positions[i+3].z);
                
                // Load 4 velocity components
                __m128 velX = _mm_setr_ps(velocities[i].vx, velocities[i+1].vx, velocities[i+2].vx, velocities[i+3].vx);
                __m128 velY = _mm_setr_ps(velocities[i].vy, velocities[i+1].vy, velocities[i+2].vy, velocities[i+3].vy);
                __m128 velZ = _mm_setr_ps(velocities[i].vz, velocities[i+1].vz, velocities[i+2].vz, velocities[i+3].vz);
                
                // Update positions: pos += vel * deltaTime
                posX = _mm_add_ps(posX, _mm_mul_ps(velX, deltaTimeVec));
                posY = _mm_add_ps(posY, _mm_mul_ps(velY, deltaTimeVec));
                posZ = _mm_add_ps(posZ, _mm_mul_ps(velZ, deltaTimeVec));
                
                // Apply friction: vel *= friction
                velX = _mm_mul_ps(velX, frictionVec);
                velY = _mm_mul_ps(velY, frictionVec);
                velZ = _mm_mul_ps(velZ, frictionVec);
                
                // Store results back
                float posXResults[4], posYResults[4], posZResults[4];
                float velXResults[4], velYResults[4], velZResults[4];
                
                _mm_storeu_ps(posXResults, posX);
                _mm_storeu_ps(posYResults, posY);
                _mm_storeu_ps(posZResults, posZ);
                _mm_storeu_ps(velXResults, velX);
                _mm_storeu_ps(velYResults, velY);
                _mm_storeu_ps(velZResults, velZ);
                
                for (int j = 0; j < 4; ++j)
                {
                    positions[i + j].x = posXResults[j];
                    positions[i + j].y = posYResults[j];
                    positions[i + j].z = posZResults[j];
                    velocities[i + j].vx = velXResults[j];
                    velocities[i + j].vy = velYResults[j];
                    velocities[i + j].vz = velZResults[j];
                }
            }
            
            // Handle remaining elements
            for (size_t i = simdSize; i < size; ++i)
            {
                positions[i].x += velocities[i].vx * deltaTime;
                positions[i].y += velocities[i].vy * deltaTime;
                positions[i].z += velocities[i].vz * deltaTime;
                
                velocities[i].vx *= 0.95f;
                velocities[i].vy *= 0.95f;
                velocities[i].vz *= 0.95f;
            }
        }
        
    private:
        EntityManager* m_EntityManager;
    };
} 