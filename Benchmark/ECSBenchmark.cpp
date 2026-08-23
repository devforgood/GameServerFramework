#include <benchmark/benchmark.h>
#include <memory>
#include <vector>
#include <random>
#include <algorithm>

#include "Systems.h"

using namespace engine;

// Cache Miss Pattern Analysis: Sequential Access
static void BM_ECSCacheSequential(benchmark::State& state) {
    SystemManager systemManager;
    auto& entityManager = systemManager.GetEntityManager();
    
    entityManager.RegisterComponent<PositionComponent>();
    entityManager.RegisterComponent<VelocityComponent>();

    const int entityCount = static_cast<int>(state.range(0));
    std::vector<EntityID> entities;
    
    for (int i = 0; i < entityCount; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{static_cast<float>(i), 0.0f, 0.0f});
        entityManager.AddComponent(entity, VelocityComponent{1.0f, 0.0f, 0.0f});
        entities.push_back(entity);
    }
    
    auto* positionArray = entityManager.GetComponentArray<PositionComponent>();
    auto* velocityArray = entityManager.GetComponentArray<VelocityComponent>();
    
    PositionComponent* positions = positionArray->GetArray();
    VelocityComponent* velocities = velocityArray->GetArray();
    
    for (auto _ : state) {
        for (size_t i = 0; i < entityCount; ++i) {
            positions[i].x += velocities[i].vx * 0.016f;
        }
    }
}

// Cache Miss Pattern Analysis: Random Access
static void BM_ECSCacheRandom(benchmark::State& state) {
    SystemManager systemManager;
    auto& entityManager = systemManager.GetEntityManager();
    
    entityManager.RegisterComponent<PositionComponent>();
    entityManager.RegisterComponent<VelocityComponent>();

    const int entityCount = static_cast<int>(state.range(0));
    std::vector<EntityID> entities;
    
    for (int i = 0; i < entityCount; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{static_cast<float>(i), 0.0f, 0.0f});
        entityManager.AddComponent(entity, VelocityComponent{1.0f, 0.0f, 0.0f});
        entities.push_back(entity);
    }
    
    auto* positionArray = entityManager.GetComponentArray<PositionComponent>();
    auto* velocityArray = entityManager.GetComponentArray<VelocityComponent>();
    
    PositionComponent* positions = positionArray->GetArray();
    VelocityComponent* velocities = velocityArray->GetArray();
    
    std::vector<size_t> randomIndices(entityCount);
    for (size_t i = 0; i < entityCount; ++i) {
        randomIndices[i] = i;
    }

    std::random_device rd;
    std::mt19937 g(rd());
    std::shuffle(randomIndices.begin(), randomIndices.end(), g);
    
    for (auto _ : state) {
        for (size_t i = 0; i < entityCount; ++i) {
            size_t idx = randomIndices[i];
            positions[idx].x += velocities[idx].vx * 0.016f;
        }
    }
}

// Cache Miss Pattern Analysis: Strided Access
static void BM_ECSCacheStrided(benchmark::State& state) {
    SystemManager systemManager;
    auto& entityManager = systemManager.GetEntityManager();
    
    entityManager.RegisterComponent<PositionComponent>();
    entityManager.RegisterComponent<VelocityComponent>();

    const int entityCount = static_cast<int>(state.range(0));
    std::vector<EntityID> entities;
    
    for (int i = 0; i < entityCount; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{static_cast<float>(i), 0.0f, 0.0f});
        entityManager.AddComponent(entity, VelocityComponent{1.0f, 0.0f, 0.0f});
        entities.push_back(entity);
    }
    
    auto* positionArray = entityManager.GetComponentArray<PositionComponent>();
    auto* velocityArray = entityManager.GetComponentArray<VelocityComponent>();
    
    PositionComponent* positions = positionArray->GetArray();
    VelocityComponent* velocities = velocityArray->GetArray();
    
    const size_t stride = 16;
    for (auto _ : state) {
        for (size_t i = 0; i < entityCount; i += stride) {
            positions[i].x += velocities[i].vx * 0.016f;
        }
    }
}

// Performance: ECS Update
static void BM_ECSUpdateSystem(benchmark::State& state) {
    SystemManager systemManager;
    auto& entityManager = systemManager.GetEntityManager();
    
    entityManager.RegisterComponent<PositionComponent>();
    entityManager.RegisterComponent<VelocityComponent>();
    entityManager.RegisterComponent<HealthComponent>();

    const int entityCount = static_cast<int>(state.range(0));
    
    for (int i = 0; i < entityCount; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{static_cast<float>(i), 0.0f, 0.0f});
        entityManager.AddComponent(entity, VelocityComponent{1.0f, 0.0f, 0.0f});
        entityManager.AddComponent(entity, HealthComponent{100.0f, 100.0f, true});
    }
    
    systemManager.RegisterSystem<PositionComponent, VelocityComponent>(
        [](float deltaTime, PositionComponent& position, VelocityComponent& velocity) {
            MovementSystem::Update(deltaTime, position, velocity);
        });
    
    systemManager.RegisterSystem<HealthComponent>(
        [](float deltaTime, HealthComponent& health) {
            HealthSystem::Update(deltaTime, health);
        });
    
    for (auto _ : state) {
        systemManager.Update(0.016f);
    }
}

// Linked list node for performance comparison
struct LinkedListNode {
    PositionComponent position;
    VelocityComponent velocity;
    HealthComponent health;
    LinkedListNode* next;
    
    LinkedListNode() : next(nullptr) {}
};

// Performance: Linked List Update
static void BM_LinkedListUpdate(benchmark::State& state) {
    const int entityCount = static_cast<int>(state.range(0));
    
    LinkedListNode* head = nullptr;
    LinkedListNode* tail = nullptr;
    
    for (int i = 0; i < entityCount; ++i) {
        LinkedListNode* node = new LinkedListNode();
        node->position = PositionComponent{static_cast<float>(i), 0.0f, 0.0f};
        node->velocity = VelocityComponent{1.0f, 0.0f, 0.0f};
        node->health = HealthComponent{100.0f, 100.0f, true};
        
        if (head == nullptr) {
            head = node;
            tail = node;
        } else {
            tail->next = node;
            tail = node;
        }
    }
    
    for (auto _ : state) {
        LinkedListNode* current = head;
        while (current != nullptr) {
            current->position.x += current->velocity.vx * 0.016f;
            current->position.y += current->velocity.vy * 0.016f;
            current->position.z += current->velocity.vz * 0.016f;
            
            current->velocity.vx *= 0.95f;
            current->velocity.vy *= 0.95f;
            current->velocity.vz *= 0.95f;
            
            if (current->health.isAlive && current->health.currentHealth < current->health.maxHealth) {
                current->health.currentHealth += 5.0f * 0.016f;
                if (current->health.currentHealth > current->health.maxHealth) {
                    current->health.currentHealth = current->health.maxHealth;
                }
            }
            
            current = current->next;
        }
    }
    
    // Cleanup
    LinkedListNode* current = head;
    while (current != nullptr) {
        LinkedListNode* next = current->next;
        delete current;
        current = next;
    }
}

BENCHMARK(BM_ECSCacheSequential)->RangeMultiplier(4)->Range(1024, 1<<20);
BENCHMARK(BM_ECSCacheRandom)->RangeMultiplier(4)->Range(1024, 1<<20);
BENCHMARK(BM_ECSCacheStrided)->RangeMultiplier(4)->Range(1024, 1<<20);
BENCHMARK(BM_ECSUpdateSystem)->RangeMultiplier(4)->Range(1024, 1<<20);
BENCHMARK(BM_LinkedListUpdate)->RangeMultiplier(4)->Range(1024, 1<<20);
