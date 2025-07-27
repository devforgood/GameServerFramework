#include <gtest/gtest.h>
#include <memory>
#include <vector>
#include <chrono>
#include <iomanip>
#include <algorithm>
#include <random>
#include <array>
#include <tuple>
#include <functional>
#include "Common.h"
#include "Systems.h"

using namespace Engine;

// Cache optimized system for testing
namespace Engine {
    template<typename... Components>
    class CacheOptimizedSystem {
    public:
        using UpdateFunction = std::function<void(float, size_t, Components*...)>;
        
        CacheOptimizedSystem(EntityManager* entityManager, UpdateFunction updateFunction)
            : m_EntityManager(entityManager), m_UpdateFunction(updateFunction) {}
        
        void Update(float deltaTime) {
            auto componentArrays = GetComponentArrays();
            size_t minSize = GetMinArraySize(componentArrays);
            
            if (minSize == 0) return;
            
            auto rawArrays = GetRawArrays(componentArrays);
            m_UpdateFunction(deltaTime, minSize, std::get<Components*>(rawArrays)...);
        }
        
    private:
        EntityManager* m_EntityManager;
        UpdateFunction m_UpdateFunction;
        
        std::tuple<ComponentArray<Components>*...> GetComponentArrays() {
            return std::make_tuple(m_EntityManager->GetComponentArray<Components>()...);
        }
        
        template<size_t... Is>
        size_t GetMinArraySizeImpl(const std::tuple<ComponentArray<Components>*...>& arrays, std::index_sequence<Is...>) {
            std::array<size_t, sizeof...(Components)> sizes = {std::get<Is>(arrays)->GetSize()...};
            return *std::min_element(sizes.begin(), sizes.end());
        }
        
        size_t GetMinArraySize(const std::tuple<ComponentArray<Components>*...>& arrays) {
            return GetMinArraySizeImpl(arrays, std::make_index_sequence<sizeof...(Components)>{});
        }
        
        std::tuple<Components*...> GetRawArrays(const std::tuple<ComponentArray<Components>*...>& arrays) {
            return GetRawArraysImpl(arrays, std::make_index_sequence<sizeof...(Components)>{});
        }
        
        template<size_t... Is>
        std::tuple<Components*...> GetRawArraysImpl(
            const std::tuple<ComponentArray<Components>*...>& arrays, std::index_sequence<Is...>) {
            return std::make_tuple(std::get<Is>(arrays)->GetArray()...);
        }
    };
    
    // SIMD optimized movement system for testing
    class SIMDOptimizedMovementSystem {
    public:
        SIMDOptimizedMovementSystem(EntityManager* entityManager) : m_EntityManager(entityManager) {}
        
        void Update(float deltaTime) {
            auto* positionArray = m_EntityManager->GetComponentArray<PositionComponent>();
            auto* velocityArray = m_EntityManager->GetComponentArray<VelocityComponent>();
            
            if (!positionArray || !velocityArray) return;
            
            size_t size = std::min(positionArray->GetSize(), velocityArray->GetSize());
            PositionComponent* positions = positionArray->GetArray();
            VelocityComponent* velocities = velocityArray->GetArray();
            
            // Simple SIMD-like optimization (manual loop unrolling)
            size_t i = 0;
            for (; i + 3 < size; i += 4) {
                // Process 4 components at once
                positions[i].x += velocities[i].vx * deltaTime;
                positions[i].y += velocities[i].vy * deltaTime;
                positions[i].z += velocities[i].vz * deltaTime;
                velocities[i].vx *= 0.95f;
                velocities[i].vy *= 0.95f;
                velocities[i].vz *= 0.95f;
                
                positions[i+1].x += velocities[i+1].vx * deltaTime;
                positions[i+1].y += velocities[i+1].vy * deltaTime;
                positions[i+1].z += velocities[i+1].vz * deltaTime;
                velocities[i+1].vx *= 0.95f;
                velocities[i+1].vy *= 0.95f;
                velocities[i+1].vz *= 0.95f;
                
                positions[i+2].x += velocities[i+2].vx * deltaTime;
                positions[i+2].y += velocities[i+2].vy * deltaTime;
                positions[i+2].z += velocities[i+2].vz * deltaTime;
                velocities[i+2].vx *= 0.95f;
                velocities[i+2].vy *= 0.95f;
                velocities[i+2].vz *= 0.95f;
                
                positions[i+3].x += velocities[i+3].vx * deltaTime;
                positions[i+3].y += velocities[i+3].vy * deltaTime;
                positions[i+3].z += velocities[i+3].vz * deltaTime;
                velocities[i+3].vx *= 0.95f;
                velocities[i+3].vy *= 0.95f;
                velocities[i+3].vz *= 0.95f;
            }
            
            // Handle remaining components
            for (; i < size; ++i) {
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

// ECS test class
class ECSTest : public ::testing::Test {
public:
    void SetUp() override {
        // Setup before each test
        systemManager = std::make_unique<SystemManager>();
        auto& entityManager = systemManager->GetEntityManager();
        
        // Register all component types
        entityManager.RegisterComponent<PositionComponent>();
        entityManager.RegisterComponent<VelocityComponent>();
        entityManager.RegisterComponent<HealthComponent>();
        entityManager.RegisterComponent<PhysicsComponent>();
        entityManager.RegisterComponent<AIComponent>();
        entityManager.RegisterComponent<CollisionComponent>();
        entityManager.RegisterComponent<InputComponent>();
        entityManager.RegisterComponent<AnimationComponent>();
        entityManager.RegisterComponent<TimerComponent>();
        entityManager.RegisterComponent<ParticleComponent>();
        entityManager.RegisterComponent<NetworkComponent>();
    }

    void TearDown() override {
        // Cleanup after each test
        systemManager.reset();
    }

    std::unique_ptr<SystemManager> systemManager;
};

// Basic ECS constructor test
TEST_F(ECSTest, Constructor) {
    EXPECT_NO_THROW({
        SystemManager sm;
    });
}

// Entity creation test
TEST_F(ECSTest, CreateEntity) {
    auto& entityManager = systemManager->GetEntityManager();
    
    EntityID entity1 = entityManager.CreateEntity();
    EntityID entity2 = entityManager.CreateEntity();
    
    EXPECT_NE(entity1, entity2);
    EXPECT_GT(entity2, entity1);
}

// Component addition test
TEST_F(ECSTest, AddComponent) {
    auto& entityManager = systemManager->GetEntityManager();
    EntityID entity = entityManager.CreateEntity();
    
    PositionComponent pos{10.0f, 20.0f, 30.0f};
    entityManager.AddComponent(entity, pos);
    
    EXPECT_TRUE(entityManager.HasComponent<PositionComponent>(entity));
    
    auto& retrievedPos = entityManager.GetComponent<PositionComponent>(entity);
    EXPECT_FLOAT_EQ(retrievedPos.x, 10.0f);
    EXPECT_FLOAT_EQ(retrievedPos.y, 20.0f);
    EXPECT_FLOAT_EQ(retrievedPos.z, 30.0f);
}

// Component removal test
TEST_F(ECSTest, RemoveComponent) {
    auto& entityManager = systemManager->GetEntityManager();
    EntityID entity = entityManager.CreateEntity();
    
    PositionComponent pos{10.0f, 20.0f, 30.0f};
    entityManager.AddComponent(entity, pos);
    
    EXPECT_TRUE(entityManager.HasComponent<PositionComponent>(entity));
    
    entityManager.RemoveComponent<PositionComponent>(entity);
    
    EXPECT_FALSE(entityManager.HasComponent<PositionComponent>(entity));
}

// Entity destruction test
TEST_F(ECSTest, DestroyEntity) {
    auto& entityManager = systemManager->GetEntityManager();
    EntityID entity = entityManager.CreateEntity();
    
    PositionComponent pos{10.0f, 20.0f, 30.0f};
    VelocityComponent vel{1.0f, 2.0f, 3.0f};
    entityManager.AddComponent(entity, pos);
    entityManager.AddComponent(entity, vel);
    
    EXPECT_TRUE(entityManager.HasComponent<PositionComponent>(entity));
    EXPECT_TRUE(entityManager.HasComponent<VelocityComponent>(entity));
    
    entityManager.DestroyEntity(entity);
    
    EXPECT_FALSE(entityManager.HasComponent<PositionComponent>(entity));
    EXPECT_FALSE(entityManager.HasComponent<VelocityComponent>(entity));
}

// Component array access test
TEST_F(ECSTest, ComponentArrayAccess) {
    auto& entityManager = systemManager->GetEntityManager();
    
    // Create multiple entities
    std::vector<EntityID> entities;
    for (int i = 0; i < 5; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{static_cast<float>(i), 0.0f, 0.0f});
        entityManager.AddComponent(entity, VelocityComponent{static_cast<float>(i), 0.0f, 0.0f});
        entities.push_back(entity);
    }
    
    auto* positionArray = entityManager.GetComponentArray<PositionComponent>();
    auto* velocityArray = entityManager.GetComponentArray<VelocityComponent>();
    
    EXPECT_EQ(positionArray->GetSize(), 5);
    EXPECT_EQ(velocityArray->GetSize(), 5);
    
    // Direct array access test
    PositionComponent* positions = positionArray->GetArray();
    VelocityComponent* velocities = velocityArray->GetArray();
    
    for (size_t i = 0; i < 5; ++i) {
        EXPECT_FLOAT_EQ(positions[i].x, static_cast<float>(i));
        EXPECT_FLOAT_EQ(velocities[i].vx, static_cast<float>(i));
        }
}

// Cache miss pattern analysis test
TEST_F(ECSTest, CacheMissPatternAnalysis) {
    auto& entityManager = systemManager->GetEntityManager();
    
    const int entityCount = 50000;
    std::vector<EntityID> entities;
    
    // Create entities
    for (int i = 0; i < entityCount; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{
            static_cast<float>(i), 0.0f, 0.0f
        });
        entityManager.AddComponent(entity, VelocityComponent{
            1.0f, 0.0f, 0.0f
        });
        entities.push_back(entity);
    }
    
    auto* positionArray = entityManager.GetComponentArray<PositionComponent>();
    auto* velocityArray = entityManager.GetComponentArray<VelocityComponent>();
    
    PositionComponent* positions = positionArray->GetArray();
    VelocityComponent* velocities = velocityArray->GetArray();
    
    const int iterations = 1000;
    
    // 1. Sequential access (cache-friendly)
    auto start = std::chrono::high_resolution_clock::now();
    
    for (int iter = 0; iter < iterations; ++iter) {
        for (size_t i = 0; i < entityCount; ++i) {
            positions[i].x += velocities[i].vx * 0.016f;
        }
    }
    
    auto end = std::chrono::high_resolution_clock::now();
    auto sequentialTime = std::chrono::duration_cast<std::chrono::nanoseconds>(end - start);
    
    // 2. Random access (cache misses)
    std::vector<size_t> randomIndices(entityCount);
    for (size_t i = 0; i < entityCount; ++i) {
        randomIndices[i] = i;
    }

    std::random_device rd;
    std::mt19937 g(rd());
    std::shuffle(randomIndices.begin(), randomIndices.end(), g);
    
    start = std::chrono::high_resolution_clock::now();
    
    for (int iter = 0; iter < iterations; ++iter) {
        for (size_t i = 0; i < entityCount; ++i) {
            size_t idx = randomIndices[i];
            positions[idx].x += velocities[idx].vx * 0.016f;
        }
    }
    
    end = std::chrono::high_resolution_clock::now();
    auto randomTime = std::chrono::duration_cast<std::chrono::nanoseconds>(end - start);
    
    double sequentialAvg = static_cast<double>(sequentialTime.count()) / iterations;
    double randomAvg = static_cast<double>(randomTime.count()) / iterations;
    
    double randomSlowdown = randomAvg / sequentialAvg;
    
    std::cout << "\n=== Cache Miss Pattern Analysis ===" << std::endl;
    std::cout << "Entity count: " << entityCount << std::endl;
    std::cout << "Sequential access: " << std::fixed << std::setprecision(2) << sequentialAvg << " ns/iter" << std::endl;
    std::cout << "Random access: " << std::fixed << std::setprecision(2) << randomAvg << " ns/iter (slowdown: " << randomSlowdown << "x)" << std::endl;
    
    // Cache-friendly access should be at least 1.5x faster than random access
    EXPECT_GT(randomSlowdown, 1.5);
    
    // Cleanup
    for (auto entity : entities) {
        entityManager.DestroyEntity(entity);
    }
}

// Memory layout optimization verification test
TEST_F(ECSTest, MemoryLayoutOptimization) {
    auto& entityManager = systemManager->GetEntityManager();
    
    const int entityCount = 10000;
    
    std::cout << "\n=== Memory Layout Analysis ===" << std::endl;
    
    // Component size information
    std::cout << "Component sizes:" << std::endl;
    std::cout << "  PositionComponent: " << sizeof(PositionComponent) << " bytes" << std::endl;
    std::cout << "  VelocityComponent: " << sizeof(VelocityComponent) << " bytes" << std::endl;
    std::cout << "  HealthComponent: " << sizeof(HealthComponent) << " bytes" << std::endl;
    
    // Cache line size (typically 64 bytes)
    const size_t cacheLineSize = 64;
    size_t positionsPerCacheLine = cacheLineSize / sizeof(PositionComponent);
    size_t velocitiesPerCacheLine = cacheLineSize / sizeof(VelocityComponent);
    
    std::cout << "Cache line optimization:" << std::endl;
    std::cout << "  Cache line size: " << cacheLineSize << " bytes" << std::endl;
    std::cout << "  Positions per cache line: " << positionsPerCacheLine << std::endl;
    std::cout << "  Velocities per cache line: " << velocitiesPerCacheLine << std::endl;
    
    // Create entities and analyze memory addresses
    std::vector<EntityID> entities;
    for (int i = 0; i < entityCount; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{0.0f, 0.0f, 0.0f});
        entityManager.AddComponent(entity, VelocityComponent{0.0f, 0.0f, 0.0f});
        entities.push_back(entity);
    }
    
    auto* positionArray = entityManager.GetComponentArray<PositionComponent>();
    auto* velocityArray = entityManager.GetComponentArray<VelocityComponent>();
    
    PositionComponent* positions = positionArray->GetArray();
    VelocityComponent* velocities = velocityArray->GetArray();
    
    // Memory contiguity verification
    bool isPositionContiguous = true;
    bool isVelocityContiguous = true;
    
    for (size_t i = 1; i < entityCount && i < 100; ++i) {
        ptrdiff_t positionDiff = reinterpret_cast<char*>(&positions[i]) - reinterpret_cast<char*>(&positions[i-1]);
        ptrdiff_t velocityDiff = reinterpret_cast<char*>(&velocities[i]) - reinterpret_cast<char*>(&velocities[i-1]);
        
        if (positionDiff != sizeof(PositionComponent)) {
            isPositionContiguous = false;
        }
        if (velocityDiff != sizeof(VelocityComponent)) {
            isVelocityContiguous = false;
        }
    }
    
    std::cout << "Memory contiguity:" << std::endl;
    std::cout << "  Position components contiguous: " << (isPositionContiguous ? "Yes" : "No") << std::endl;
    std::cout << "  Velocity components contiguous: " << (isVelocityContiguous ? "Yes" : "No") << std::endl;
    
    // Memory alignment verification
    uintptr_t positionAlignment = reinterpret_cast<uintptr_t>(positions) % alignof(PositionComponent);
    uintptr_t velocityAlignment = reinterpret_cast<uintptr_t>(velocities) % alignof(VelocityComponent);
    
    std::cout << "Memory alignment:" << std::endl;
    std::cout << "  Position array aligned: " << (positionAlignment == 0 ? "Yes" : "No") << std::endl;
    std::cout << "  Velocity array aligned: " << (velocityAlignment == 0 ? "Yes" : "No") << std::endl;
    
    // Memory usage
    size_t totalMemory = entityCount * (sizeof(PositionComponent) + sizeof(VelocityComponent));
    size_t cacheLines = (totalMemory + cacheLineSize - 1) / cacheLineSize;
    
    std::cout << "Memory usage:" << std::endl;
    std::cout << "  Total memory: " << totalMemory << " bytes (" << (totalMemory / 1024) << " KB)" << std::endl;
    std::cout << "  Cache lines used: " << cacheLines << std::endl;
    
    // Verification
    EXPECT_TRUE(isPositionContiguous);
    EXPECT_TRUE(isVelocityContiguous);
    EXPECT_EQ(positionAlignment, 0);
    EXPECT_EQ(velocityAlignment, 0);
    
    // Cleanup
    for (auto entity : entities) {
        entityManager.DestroyEntity(entity);
    }
}

  // System registration and execution test
TEST_F(ECSTest, SystemRegistrationAndExecution) {
    auto& entityManager = systemManager->GetEntityManager();
    
    // Create test entities
    EntityID entity = entityManager.CreateEntity();
    entityManager.AddComponent(entity, PositionComponent{0.0f, 0.0f, 0.0f});
    entityManager.AddComponent(entity, VelocityComponent{1.0f, 2.0f, 3.0f});
    
    // Register system
    bool systemExecuted = false;
    systemManager->RegisterSystem<PositionComponent, VelocityComponent>(
        [&systemExecuted](float deltaTime, PositionComponent& position, VelocityComponent& velocity) {
            systemExecuted = true;
            // Simple movement update
            position.x += velocity.vx * deltaTime;
            position.y += velocity.vy * deltaTime;
            position.z += velocity.vz * deltaTime;
        });
    
    // Execute system
    systemManager->Update(1.0f);
    
    EXPECT_TRUE(systemExecuted);
    
    // Check if position was updated
    auto& position = entityManager.GetComponent<PositionComponent>(entity);
    EXPECT_FLOAT_EQ(position.x, 1.0f);
    EXPECT_FLOAT_EQ(position.y, 2.0f);
    EXPECT_FLOAT_EQ(position.z, 3.0f);
}

// MovementSystem test
TEST_F(ECSTest, MovementSystem) {
    auto& entityManager = systemManager->GetEntityManager();
    
    EntityID entity = entityManager.CreateEntity();
    entityManager.AddComponent(entity, PositionComponent{0.0f, 0.0f, 0.0f});
    entityManager.AddComponent(entity, VelocityComponent{5.0f, 10.0f, 15.0f});
    
    systemManager->RegisterSystem<PositionComponent, VelocityComponent>(
        [](float deltaTime, PositionComponent& position, VelocityComponent& velocity) {
            MovementSystem::Update(deltaTime, position, velocity);
        });
    
    systemManager->Update(1.0f);
    
    auto& position = entityManager.GetComponent<PositionComponent>(entity);
    auto& velocity = entityManager.GetComponent<VelocityComponent>(entity);
    
    // Check position update
    EXPECT_FLOAT_EQ(position.x, 5.0f);
    EXPECT_FLOAT_EQ(position.y, 10.0f);
    EXPECT_FLOAT_EQ(position.z, 15.0f);
    
    // Check friction application
    EXPECT_FLOAT_EQ(velocity.vx, 5.0f * 0.95f);
    EXPECT_FLOAT_EQ(velocity.vy, 10.0f * 0.95f);
    EXPECT_FLOAT_EQ(velocity.vz, 15.0f * 0.95f);
}

// PhysicsSystem test
TEST_F(ECSTest, PhysicsSystem) {
    auto& entityManager = systemManager->GetEntityManager();
    
    EntityID entity = entityManager.CreateEntity();
    entityManager.AddComponent(entity, PositionComponent{0.0f, 10.0f, 0.0f});
    entityManager.AddComponent(entity, VelocityComponent{0.0f, 0.0f, 0.0f});
    entityManager.AddComponent(entity, PhysicsComponent{1.0f, 0.1f, 0.5f, false, true});
    
    systemManager->RegisterSystem<PositionComponent, VelocityComponent, PhysicsComponent>(
        [](float deltaTime, PositionComponent& position, VelocityComponent& velocity, PhysicsComponent& physics) {
            PhysicsSystem::Update(deltaTime, position, velocity, physics);
        });
    
    // Test gravity application
    systemManager->Update(1.0f);
    
    auto& position = entityManager.GetComponent<PositionComponent>(entity);
    auto& velocity = entityManager.GetComponent<VelocityComponent>(entity);
    
    // Check gravity application (g = -9.81)
    EXPECT_FLOAT_EQ(velocity.vy, -9.81f);
    EXPECT_FLOAT_EQ(position.y, 10.0f - 9.81f);
}

// HealthSystem test
TEST_F(ECSTest, HealthSystem) {
    auto& entityManager = systemManager->GetEntityManager();
    
    EntityID entity = entityManager.CreateEntity();
    entityManager.AddComponent(entity, HealthComponent{50.0f, 100.0f, true});
    
    systemManager->RegisterSystem<HealthComponent>(
        [](float deltaTime, HealthComponent& health) {
            HealthSystem::Update(deltaTime, health);
        });
    
    systemManager->Update(1.0f);
    
    auto& health = entityManager.GetComponent<HealthComponent>(entity);
    
    // Check health regeneration (5 HP per second)
    EXPECT_FLOAT_EQ(health.currentHealth, 55.0f);
    EXPECT_TRUE(health.isAlive);
}

// AISystem test
TEST_F(ECSTest, AISystem) {
    auto& entityManager = systemManager->GetEntityManager();
    
    EntityID entity = entityManager.CreateEntity();
    entityManager.AddComponent(entity, AIComponent{0, 0.0f, 1.0f});
    
    systemManager->RegisterSystem<AIComponent>(
        [](float deltaTime, AIComponent& ai) {
            AISystem::Update(deltaTime, ai);
        });
    
    auto& ai = entityManager.GetComponent<AIComponent>(entity);
    uint32_t initialState = ai.aiState;
    
    systemManager->Update(1.0f);
    
    // Check if AI state changed
    EXPECT_NE(ai.aiState, initialState);
    EXPECT_FLOAT_EQ(ai.aiTimer, 0.0f);
}

// CollisionSystem test
TEST_F(ECSTest, CollisionSystem) {
    auto& entityManager = systemManager->GetEntityManager();
    
    EntityID entity = entityManager.CreateEntity();
    entityManager.AddComponent(entity, PositionComponent{0.0f, -5.0f, 0.0f});
    entityManager.AddComponent(entity, CollisionComponent{2.0f, true, 1});
    
    systemManager->RegisterSystem<PositionComponent, CollisionComponent>(
        [](float deltaTime, PositionComponent& position, CollisionComponent& collision) {
            CollisionSystem::Update(deltaTime, position, collision);
        });
    
    systemManager->Update(1.0f);
    
    auto& position = entityManager.GetComponent<PositionComponent>(entity);
    
    // Check ground collision handling
    EXPECT_FLOAT_EQ(position.y, 2.0f); // Adjusted to radius value
}

// Component combination query test
TEST_F(ECSTest, ComponentQuery) {
    auto& entityManager = systemManager->GetEntityManager();
    
    // Create entities with different component combinations
    EntityID entity1 = entityManager.CreateEntity();
    entityManager.AddComponent(entity1, PositionComponent{0.0f, 0.0f, 0.0f});
    entityManager.AddComponent(entity1, VelocityComponent{1.0f, 0.0f, 0.0f});
    entityManager.AddComponent(entity1, HealthComponent{100.0f, 100.0f, true});
    
    EntityID entity2 = entityManager.CreateEntity();
    entityManager.AddComponent(entity2, PositionComponent{10.0f, 0.0f, 0.0f});
    entityManager.AddComponent(entity2, VelocityComponent{0.0f, 1.0f, 0.0f});
    
    EntityID entity3 = entityManager.CreateEntity();
    entityManager.AddComponent(entity3, HealthComponent{50.0f, 100.0f, true});
    
    // Query entities with both Position and Velocity
    auto movingEntities = entityManager.GetEntitiesWithComponents<PositionComponent, VelocityComponent>();
    EXPECT_EQ(movingEntities.size(), 2);
    
    // Query entities with Position, Velocity, and Health
    auto fullEntities = entityManager.GetEntitiesWithComponents<PositionComponent, VelocityComponent, HealthComponent>();
    EXPECT_EQ(fullEntities.size(), 1);
    EXPECT_EQ(fullEntities[0], entity1);
}

// Performance test
TEST_F(ECSTest, Performance) {
    auto& entityManager = systemManager->GetEntityManager();
    
    // Create a large number of entities
    const int entityCount = 100000;
    std::vector<EntityID> entities;
    
    for (int i = 0; i < entityCount; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{static_cast<float>(i), 0.0f, 0.0f});
        entityManager.AddComponent(entity, VelocityComponent{1.0f, 0.0f, 0.0f});
        entityManager.AddComponent(entity, HealthComponent{100.0f, 100.0f, true});
        entities.push_back(entity);
    }
    
    systemManager->RegisterSystem<PositionComponent, VelocityComponent>(
        [](float deltaTime, PositionComponent& position, VelocityComponent& velocity) {
            MovementSystem::Update(deltaTime, position, velocity);
        });
    
    systemManager->RegisterSystem<HealthComponent>(
        [](float deltaTime, HealthComponent& health) {
            HealthSystem::Update(deltaTime, health);
        });
    
    // Measure performance
    auto start = std::chrono::high_resolution_clock::now();
    
    for (int i = 0; i < 100; ++i) {
        systemManager->Update(0.016f); // 60 FPS simulation
    }
    
    auto end = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
    
    // Output performance results
    std::cout << "Performance test: " << entityCount << " entities, 100 frames in " 
              << duration.count() << " microseconds" << std::endl;
    std::cout << "Average frame time: " << (duration.count() / 100) << " microseconds" << std::endl;
    
    // Performance verification (10,000 entities, should complete within 1 second)
    EXPECT_LT(duration.count(), 1000000); // Less than 1 second
}

// Cache-friendly access test
TEST_F(ECSTest, CacheFriendlyAccess) {
    auto& entityManager = systemManager->GetEntityManager();
    
    const int entityCount = 1000;
    std::vector<EntityID> entities;
    
    // Create entities
    for (int i = 0; i < entityCount; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{static_cast<float>(i), 0.0f, 0.0f});
        entityManager.AddComponent(entity, VelocityComponent{1.0f, 0.0f, 0.0f});
        entities.push_back(entity);
    }
    
    // Measure performance with direct array access
    auto* positionArray = entityManager.GetComponentArray<PositionComponent>();
    auto* velocityArray = entityManager.GetComponentArray<VelocityComponent>();
    
    PositionComponent* positions = positionArray->GetArray();
    VelocityComponent* velocities = velocityArray->GetArray();
    
    auto start = std::chrono::high_resolution_clock::now();
    
    // Cache-friendly sequential access
    for (size_t i = 0; i < positionArray->GetSize(); ++i) {
        positions[i].x += velocities[i].vx * 0.016f;
        positions[i].y += velocities[i].vy * 0.016f;
        positions[i].z += velocities[i].vz * 0.016f;
    }
    
    auto end = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::nanoseconds>(end - start);
    
    std::cout << "Cache-friendly access: " << entityCount << " entities in " 
              << duration.count() << " nanoseconds" << std::endl;
    
    // Result verification
    EXPECT_FLOAT_EQ(positions[0].x, 0.016f);
    EXPECT_FLOAT_EQ(positions[entityCount-1].x, static_cast<float>(entityCount-1) + 0.016f);
}

// Massive component loop performance test
TEST_F(ECSTest, MassiveComponentLoopPerformance) {
    auto& entityManager = systemManager->GetEntityManager();
    
    // Test with different numbers of entities
    std::vector<int> entityCounts = {1000, 10000, 50000, 100000};
    
    for (int entityCount : entityCounts) {
        std::cout << "\n=== Testing with " << entityCount << " entities ===" << std::endl;
        
        // Create entities
        std::vector<EntityID> entities;
        entities.reserve(entityCount);
        
        auto setupStart = std::chrono::high_resolution_clock::now();
        
        for (int i = 0; i < entityCount; ++i) {
            EntityID entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, PositionComponent{
                static_cast<float>(i % 1000), 
                static_cast<float>((i * 2) % 1000), 
                static_cast<float>((i * 3) % 1000)
            });
            entityManager.AddComponent(entity, VelocityComponent{
                static_cast<float>((i % 10) - 5), 
                static_cast<float>(((i * 2) % 10) - 5), 
                static_cast<float>(((i * 3) % 10) - 5)
            });
            entityManager.AddComponent(entity, HealthComponent{100.0f, 100.0f, true});
            entities.push_back(entity);
        }
        
        auto setupEnd = std::chrono::high_resolution_clock::now();
        auto setupTime = std::chrono::duration_cast<std::chrono::milliseconds>(setupEnd - setupStart);
        std::cout << "Setup time: " << setupTime.count() << " ms" << std::endl;
        
        // 1. Basic system performance test
        systemManager->RegisterSystem<PositionComponent, VelocityComponent>(
            [](float deltaTime, PositionComponent& position, VelocityComponent& velocity) {
                position.x += velocity.vx * deltaTime;
                position.y += velocity.vy * deltaTime;
                position.z += velocity.vz * deltaTime;
                velocity.vx *= 0.99f;
                velocity.vy *= 0.99f;
                velocity.vz *= 0.99f;
            });
        
        const int iterations = 100;
        auto start = std::chrono::high_resolution_clock::now();
        
        for (int i = 0; i < iterations; ++i) {
            systemManager->Update(0.016f);
        }
        
        auto end = std::chrono::high_resolution_clock::now();
        auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
        
        double avgFrameTime = static_cast<double>(duration.count()) / iterations;
        double entitiesPerSecond = (entityCount * 1000000.0) / avgFrameTime;
        
        std::cout << "Basic System Performance:" << std::endl;
        std::cout << "  Total time: " << duration.count() << " μs" << std::endl;
        std::cout << "  Average frame time: " << std::fixed << std::setprecision(2) << avgFrameTime << " μs" << std::endl;
        std::cout << "  Entities per second: " << std::scientific << std::setprecision(2) << entitiesPerSecond << std::endl;
        
        // 2. Direct array access performance test
        auto* positionArray = entityManager.GetComponentArray<PositionComponent>();
        auto* velocityArray = entityManager.GetComponentArray<VelocityComponent>();
        
        PositionComponent* positions = positionArray->GetArray();
        VelocityComponent* velocities = velocityArray->GetArray();
        size_t size = positionArray->GetSize();
        
        start = std::chrono::high_resolution_clock::now();
        
        for (int iter = 0; iter < iterations; ++iter) {
            for (size_t i = 0; i < size; ++i) {
                positions[i].x += velocities[i].vx * 0.016f;
                positions[i].y += velocities[i].vy * 0.016f;
                positions[i].z += velocities[i].vz * 0.016f;
                velocities[i].vx *= 0.99f;
                velocities[i].vy *= 0.99f;
                velocities[i].vz *= 0.99f;
            }
        }
        
        end = std::chrono::high_resolution_clock::now();
        duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
        
        avgFrameTime = static_cast<double>(duration.count()) / iterations;
        entitiesPerSecond = (entityCount * 1000000.0) / avgFrameTime;
        
        std::cout << "Direct Array Access Performance:" << std::endl;
        std::cout << "  Total time: " << duration.count() << " μs" << std::endl;
        std::cout << "  Average frame time: " << std::fixed << std::setprecision(2) << avgFrameTime << " μs" << std::endl;
        std::cout << "  Entities per second: " << std::scientific << std::setprecision(2) << entitiesPerSecond << std::endl;
        
        // 3. Cache-optimized batch processing performance test
        SystemManager batchSystemManager;
        auto& batchEntityManager = batchSystemManager.GetEntityManager();
        batchEntityManager.RegisterComponent<PositionComponent>();
        batchEntityManager.RegisterComponent<VelocityComponent>();
        
        // Re-create entities (for batch system)
        std::vector<EntityID> batchEntities;
        for (int i = 0; i < entityCount; ++i) {
            EntityID entity = batchEntityManager.CreateEntity();
            batchEntityManager.AddComponent(entity, PositionComponent{
                static_cast<float>(i % 1000), 
                static_cast<float>((i * 2) % 1000), 
                static_cast<float>((i * 3) % 1000)
            });
            batchEntityManager.AddComponent(entity, VelocityComponent{
                static_cast<float>((i % 10) - 5), 
                static_cast<float>(((i * 2) % 10) - 5), 
                static_cast<float>(((i * 3) % 10) - 5)
            });
            batchEntities.push_back(entity);
        }
        
        auto batchSystem = std::make_unique<CacheOptimizedSystem<PositionComponent, VelocityComponent>>(
            &batchEntityManager,
            [](float deltaTime, size_t batchSize, PositionComponent* positions, VelocityComponent* velocities) {
                for (size_t i = 0; i < batchSize; ++i) {
                    positions[i].x += velocities[i].vx * deltaTime;
                    positions[i].y += velocities[i].vy * deltaTime;
                    positions[i].z += velocities[i].vz * deltaTime;
                    velocities[i].vx *= 0.99f;
                    velocities[i].vy *= 0.99f;
                    velocities[i].vz *= 0.99f;
                }
            });
        
        start = std::chrono::high_resolution_clock::now();
        
        for (int i = 0; i < iterations; ++i) {
            batchSystem->Update(0.016f);
        }
        
        end = std::chrono::high_resolution_clock::now();
        duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
        
        avgFrameTime = static_cast<double>(duration.count()) / iterations;
        entitiesPerSecond = (entityCount * 1000000.0) / avgFrameTime;
        
        std::cout << "Cache-Optimized Batch Processing Performance:" << std::endl;
        std::cout << "  Total time: " << duration.count() << " μs" << std::endl;
        std::cout << "  Average frame time: " << std::fixed << std::setprecision(2) << avgFrameTime << " μs" << std::endl;
        std::cout << "  Entities per second: " << std::scientific << std::setprecision(2) << entitiesPerSecond << std::endl;
        
        // Memory usage information
        size_t memoryUsage = entityCount * (sizeof(PositionComponent) + sizeof(VelocityComponent) + sizeof(HealthComponent));
        std::cout << "Memory usage: " << (memoryUsage / 1024) << " KB" << std::endl;
        
        // Cleanup
        for (auto entity : entities) {
            entityManager.DestroyEntity(entity);
        }
        for (auto entity : batchEntities) {
            batchEntityManager.DestroyEntity(entity);
        }
        
        // Performance verification (100,000 entities should process within 1ms)
        if (entityCount == 100000) {
            EXPECT_LT(avgFrameTime, 1000.0); // Less than 1ms
        }
    }
}

// SIMD optimized performance test
TEST_F(ECSTest, SIMDOptimizedPerformance) {
    auto& entityManager = systemManager->GetEntityManager();
    
    const int entityCount = 100000;
    std::vector<EntityID> entities;
    
    // Create entities
    for (int i = 0; i < entityCount; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{
            static_cast<float>(i % 1000), 
            static_cast<float>((i * 2) % 1000), 
            static_cast<float>((i * 3) % 1000)
        });
        entityManager.AddComponent(entity, VelocityComponent{
            static_cast<float>((i % 10) - 5), 
            static_cast<float>(((i * 2) % 10) - 5), 
            static_cast<float>(((i * 3) % 10) - 5)
        });
        entities.push_back(entity);
    }
    
    // Compare general system vs SIMD optimized system
    const int iterations = 100;
    
    // 1. General system
    systemManager->RegisterSystem<PositionComponent, VelocityComponent>(
        [](float deltaTime, PositionComponent& position, VelocityComponent& velocity) {
            position.x += velocity.vx * deltaTime;
            position.y += velocity.vy * deltaTime;
            position.z += velocity.vz * deltaTime;
            velocity.vx *= 0.95f;
            velocity.vy *= 0.95f;
            velocity.vz *= 0.95f;
        });
    
    auto start = std::chrono::high_resolution_clock::now();
    for (int i = 0; i < iterations; ++i) {
        systemManager->Update(0.016f);
    }
    auto end = std::chrono::high_resolution_clock::now();
    auto normalDuration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
    
    // 2. SIMD optimized system
    auto simdSystem = std::make_unique<SIMDOptimizedMovementSystem>(&entityManager);
    
    start = std::chrono::high_resolution_clock::now();
    for (int i = 0; i < iterations; ++i) {
        simdSystem->Update(0.016f);
    }
    end = std::chrono::high_resolution_clock::now();
    auto simdDuration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
    
    double normalAvg = static_cast<double>(normalDuration.count()) / iterations;
    double simdAvg = static_cast<double>(simdDuration.count()) / iterations;
    double speedup = normalAvg / simdAvg;
    
    std::cout << "\n=== SIMD Performance Comparison ===" << std::endl;
    std::cout << "Entity count: " << entityCount << std::endl;
    std::cout << "Normal system average: " << std::fixed << std::setprecision(2) << normalAvg << " μs" << std::endl;
    std::cout << "SIMD system average: " << std::fixed << std::setprecision(2) << simdAvg << " μs" << std::endl;
    std::cout << "Speedup: " << std::fixed << std::setprecision(2) << speedup << "x" << std::endl;
    
    // SIMD optimization should show at least 10% performance improvement
    EXPECT_GT(speedup, 1.1);
    
    // Cleanup
    for (auto entity : entities) {
        entityManager.DestroyEntity(entity);
    }
}

 