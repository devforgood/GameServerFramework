#include <gtest/gtest.h>
#include "../Engine/SystemManager.h"
#include "../Engine/Components.h"
#include "../Engine/Systems.h"
#include "../Engine/CacheOptimizedSystem.h"
#include <memory>
#include <vector>
#include <chrono>
#include <iomanip>
#include <algorithm>
#include <random>

using namespace Engine;

// ECS 테스트 클래스
class ECSTest : public ::testing::Test {
protected:
    void SetUp() override {
        // 각 테스트 전에 실행되는 설정
        systemManager = std::make_unique<SystemManager>();
        auto& entityManager = systemManager->GetEntityManager();
        
        // 모든 컴포넌트 타입 등록
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
        // 각 테스트 후에 실행되는 정리
        systemManager.reset();
    }

    std::unique_ptr<SystemManager> systemManager;
};

// 기본 ECS 생성자 테스트
TEST_F(ECSTest, Constructor) {
    EXPECT_NO_THROW({
        SystemManager sm;
    });
}

// 엔티티 생성 테스트
TEST_F(ECSTest, CreateEntity) {
    auto& entityManager = systemManager->GetEntityManager();
    
    EntityID entity1 = entityManager.CreateEntity();
    EntityID entity2 = entityManager.CreateEntity();
    
    EXPECT_NE(entity1, entity2);
    EXPECT_GT(entity2, entity1);
}

// 컴포넌트 추가 테스트
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

// 컴포넌트 제거 테스트
TEST_F(ECSTest, RemoveComponent) {
    auto& entityManager = systemManager->GetEntityManager();
    EntityID entity = entityManager.CreateEntity();
    
    PositionComponent pos{10.0f, 20.0f, 30.0f};
    entityManager.AddComponent(entity, pos);
    
    EXPECT_TRUE(entityManager.HasComponent<PositionComponent>(entity));
    
    entityManager.RemoveComponent<PositionComponent>(entity);
    
    EXPECT_FALSE(entityManager.HasComponent<PositionComponent>(entity));
}

// 엔티티 제거 테스트
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

// 컴포넌트 배열 접근 테스트
TEST_F(ECSTest, ComponentArrayAccess) {
    auto& entityManager = systemManager->GetEntityManager();
    
    // 여러 엔티티 생성
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
    
    // 직접 배열 접근 테스트
    PositionComponent* positions = positionArray->GetArray();
    VelocityComponent* velocities = velocityArray->GetArray();
    
    for (size_t i = 0; i < 5; ++i) {
        EXPECT_FLOAT_EQ(positions[i].x, static_cast<float>(i));
        EXPECT_FLOAT_EQ(velocities[i].vx, static_cast<float>(i));
        }
}

// 캐시 미스 패턴 분석 테스트
TEST_F(ECSTest, CacheMissPatternAnalysis) {
    auto& entityManager = systemManager->GetEntityManager();
    
    const int entityCount = 50000;
    std::vector<EntityID> entities;
    
    // 엔티티 생성
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
    
    // 1. 순차 접근 (캐시 친화적)
    auto start = std::chrono::high_resolution_clock::now();
    
    for (int iter = 0; iter < iterations; ++iter) {
        for (size_t i = 0; i < entityCount; ++i) {
            positions[i].x += velocities[i].vx * 0.016f;
        }
    }
    
    auto end = std::chrono::high_resolution_clock::now();
    auto sequentialTime = std::chrono::duration_cast<std::chrono::nanoseconds>(end - start);
    
    // 2. 랜덤 접근 (캐시 미스 유발)
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
    
    // 3. 스트라이드 접근 (부분적 캐시 미스)
    const size_t stride = 16; // 캐시 라인을 넘나드는 접근
    start = std::chrono::high_resolution_clock::now();
    
    for (int iter = 0; iter < iterations; ++iter) {
        for (size_t i = 0; i < entityCount; i += stride) {
            positions[i].x += velocities[i].vx * 0.016f;
        }
    }
    
    end = std::chrono::high_resolution_clock::now();
    auto strideTime = std::chrono::duration_cast<std::chrono::nanoseconds>(end - start);
    
    double sequentialAvg = static_cast<double>(sequentialTime.count()) / iterations;
    double randomAvg = static_cast<double>(randomTime.count()) / iterations;
    double strideAvg = static_cast<double>(strideTime.count()) / iterations;
    
    double randomSlowdown = randomAvg / sequentialAvg;
    double strideSlowdown = strideAvg / sequentialAvg;
    
    std::cout << "\n=== Cache Miss Pattern Analysis ===" << std::endl;
    std::cout << "Entity count: " << entityCount << std::endl;
    std::cout << "Sequential access: " << std::fixed << std::setprecision(2) << sequentialAvg << " ns/iter" << std::endl;
    std::cout << "Random access: " << std::fixed << std::setprecision(2) << randomAvg << " ns/iter (slowdown: " << randomSlowdown << "x)" << std::endl;
    std::cout << "Stride access: " << std::fixed << std::setprecision(2) << strideAvg << " ns/iter (slowdown: " << strideSlowdown << "x)" << std::endl;
    
    // 캐시 친화적 접근이 랜덤 접근보다 최소 2배 빨라야 함
    EXPECT_GT(randomSlowdown, 2.0);
    
    // 정리
    for (auto entity : entities) {
        entityManager.DestroyEntity(entity);
    }
}

// 메모리 레이아웃 최적화 검증 테스트
TEST_F(ECSTest, MemoryLayoutOptimization) {
    auto& entityManager = systemManager->GetEntityManager();
    
    const int entityCount = 10000;
    
    std::cout << "\n=== Memory Layout Analysis ===" << std::endl;
    
    // 컴포넌트 크기 정보
    std::cout << "Component sizes:" << std::endl;
    std::cout << "  PositionComponent: " << sizeof(PositionComponent) << " bytes" << std::endl;
    std::cout << "  VelocityComponent: " << sizeof(VelocityComponent) << " bytes" << std::endl;
    std::cout << "  HealthComponent: " << sizeof(HealthComponent) << " bytes" << std::endl;
    
    // 캐시 라인 크기 (일반적으로 64바이트)
    const size_t cacheLineSize = 64;
    size_t positionsPerCacheLine = cacheLineSize / sizeof(PositionComponent);
    size_t velocitiesPerCacheLine = cacheLineSize / sizeof(VelocityComponent);
    
    std::cout << "Cache line optimization:" << std::endl;
    std::cout << "  Cache line size: " << cacheLineSize << " bytes" << std::endl;
    std::cout << "  Positions per cache line: " << positionsPerCacheLine << std::endl;
    std::cout << "  Velocities per cache line: " << velocitiesPerCacheLine << std::endl;
    
    // 엔티티 생성 및 메모리 주소 분석
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
    
    // 메모리 연속성 검증
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
    
    // 메모리 정렬 검증
    uintptr_t positionAlignment = reinterpret_cast<uintptr_t>(positions) % alignof(PositionComponent);
    uintptr_t velocityAlignment = reinterpret_cast<uintptr_t>(velocities) % alignof(VelocityComponent);
    
    std::cout << "Memory alignment:" << std::endl;
    std::cout << "  Position array aligned: " << (positionAlignment == 0 ? "Yes" : "No") << std::endl;
    std::cout << "  Velocity array aligned: " << (velocityAlignment == 0 ? "Yes" : "No") << std::endl;
    
    // 메모리 사용량
    size_t totalMemory = entityCount * (sizeof(PositionComponent) + sizeof(VelocityComponent));
    size_t cacheLines = (totalMemory + cacheLineSize - 1) / cacheLineSize;
    
    std::cout << "Memory usage:" << std::endl;
    std::cout << "  Total memory: " << totalMemory << " bytes (" << (totalMemory / 1024) << " KB)" << std::endl;
    std::cout << "  Cache lines used: " << cacheLines << std::endl;
    
    // 검증
    EXPECT_TRUE(isPositionContiguous);
    EXPECT_TRUE(isVelocityContiguous);
    EXPECT_EQ(positionAlignment, 0);
    EXPECT_EQ(velocityAlignment, 0);
    
    // 정리
    for (auto entity : entities) {
        entityManager.DestroyEntity(entity);
    }
}

  // 시스템 등록 및 실행 테스트
TEST_F(ECSTest, SystemRegistrationAndExecution) {
    auto& entityManager = systemManager->GetEntityManager();
    
    // 테스트용 엔티티 생성
    EntityID entity = entityManager.CreateEntity();
    entityManager.AddComponent(entity, PositionComponent{0.0f, 0.0f, 0.0f});
    entityManager.AddComponent(entity, VelocityComponent{1.0f, 2.0f, 3.0f});
    
    // 시스템 등록
    bool systemExecuted = false;
    systemManager->RegisterSystem<PositionComponent, VelocityComponent>(
        [&systemExecuted](float deltaTime, PositionComponent& position, VelocityComponent& velocity) {
            systemExecuted = true;
            // 간단한 이동 업데이트
            position.x += velocity.vx * deltaTime;
            position.y += velocity.vy * deltaTime;
            position.z += velocity.vz * deltaTime;
        });
    
    // 시스템 실행
    systemManager->Update(1.0f);
    
    EXPECT_TRUE(systemExecuted);
    
    // 위치가 업데이트되었는지 확인
    auto& position = entityManager.GetComponent<PositionComponent>(entity);
    EXPECT_FLOAT_EQ(position.x, 1.0f);
    EXPECT_FLOAT_EQ(position.y, 2.0f);
    EXPECT_FLOAT_EQ(position.z, 3.0f);
}

// MovementSystem 테스트
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
    
    // 위치 업데이트 확인
    EXPECT_FLOAT_EQ(position.x, 5.0f);
    EXPECT_FLOAT_EQ(position.y, 10.0f);
    EXPECT_FLOAT_EQ(position.z, 15.0f);
    
    // 마찰 적용 확인 (0.95f)
    EXPECT_FLOAT_EQ(velocity.vx, 5.0f * 0.95f);
    EXPECT_FLOAT_EQ(velocity.vy, 10.0f * 0.95f);
    EXPECT_FLOAT_EQ(velocity.vz, 15.0f * 0.95f);
}

// PhysicsSystem 테스트
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
    
    // 중력 적용 테스트
    systemManager->Update(1.0f);
    
    auto& position = entityManager.GetComponent<PositionComponent>(entity);
    auto& velocity = entityManager.GetComponent<VelocityComponent>(entity);
    
    // 중력이 적용되었는지 확인 (g = -9.81)
    EXPECT_FLOAT_EQ(velocity.vy, -9.81f);
    EXPECT_FLOAT_EQ(position.y, 10.0f - 9.81f);
}

// HealthSystem 테스트
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
    
    // 체력 재생 확인 (5 HP per second)
    EXPECT_FLOAT_EQ(health.currentHealth, 55.0f);
    EXPECT_TRUE(health.isAlive);
}

// AISystem 테스트
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
    
    // AI 상태가 변경되었는지 확인
    EXPECT_NE(ai.aiState, initialState);
    EXPECT_FLOAT_EQ(ai.aiTimer, 0.0f);
}

// CollisionSystem 테스트
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
    
    // 지면 충돌 처리 확인
    EXPECT_FLOAT_EQ(position.y, 2.0f); // radius 값으로 조정됨
}

// 컴포넌트 조합 쿼리 테스트
TEST_F(ECSTest, ComponentQuery) {
    auto& entityManager = systemManager->GetEntityManager();
    
    // 다양한 컴포넌트 조합을 가진 엔티티들 생성
    EntityID entity1 = entityManager.CreateEntity();
    entityManager.AddComponent(entity1, PositionComponent{0.0f, 0.0f, 0.0f});
    entityManager.AddComponent(entity1, VelocityComponent{1.0f, 0.0f, 0.0f});
    entityManager.AddComponent(entity1, HealthComponent{100.0f, 100.0f, true});
    
    EntityID entity2 = entityManager.CreateEntity();
    entityManager.AddComponent(entity2, PositionComponent{10.0f, 0.0f, 0.0f});
    entityManager.AddComponent(entity2, VelocityComponent{0.0f, 1.0f, 0.0f});
    
    EntityID entity3 = entityManager.CreateEntity();
    entityManager.AddComponent(entity3, HealthComponent{50.0f, 100.0f, true});
    
    // Position과 Velocity를 모두 가진 엔티티들 조회
    auto movingEntities = entityManager.GetEntitiesWithComponents<PositionComponent, VelocityComponent>();
    EXPECT_EQ(movingEntities.size(), 2);
    
    // Position, Velocity, Health를 모두 가진 엔티티들 조회
    auto fullEntities = entityManager.GetEntitiesWithComponents<PositionComponent, VelocityComponent, HealthComponent>();
    EXPECT_EQ(fullEntities.size(), 1);
    EXPECT_EQ(fullEntities[0], entity1);
}

// 성능 테스트
TEST_F(ECSTest, Performance) {
    auto& entityManager = systemManager->GetEntityManager();
    
    // 대량의 엔티티 생성
    const int entityCount = 10000;
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
    
    // 성능 측정
    auto start = std::chrono::high_resolution_clock::now();
    
    for (int i = 0; i < 100; ++i) {
        systemManager->Update(0.016f); // 60 FPS 시뮬레이션
    }
    
    auto end = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
    
    // 성능 결과 출력
    std::cout << "Performance test: " << entityCount << " entities, 100 frames in " 
              << duration.count() << " microseconds" << std::endl;
    std::cout << "Average frame time: " << (duration.count() / 100) << " microseconds" << std::endl;
    
    // 성능 검증 (10,000 엔티티, 100 프레임이 1초 이내에 완료되어야 함)
    EXPECT_LT(duration.count(), 1000000); // 1초 미만
}

// 캐시 친화적 접근 테스트
TEST_F(ECSTest, CacheFriendlyAccess) {
    auto& entityManager = systemManager->GetEntityManager();
    
    const int entityCount = 1000;
    std::vector<EntityID> entities;
    
    // 엔티티 생성
    for (int i = 0; i < entityCount; ++i) {
        EntityID entity = entityManager.CreateEntity();
        entityManager.AddComponent(entity, PositionComponent{static_cast<float>(i), 0.0f, 0.0f});
        entityManager.AddComponent(entity, VelocityComponent{1.0f, 0.0f, 0.0f});
        entities.push_back(entity);
    }
    
    // 직접 배열 접근으로 성능 측정
    auto* positionArray = entityManager.GetComponentArray<PositionComponent>();
    auto* velocityArray = entityManager.GetComponentArray<VelocityComponent>();
    
    PositionComponent* positions = positionArray->GetArray();
    VelocityComponent* velocities = velocityArray->GetArray();
    
    auto start = std::chrono::high_resolution_clock::now();
    
    // 캐시 친화적인 순차 접근
    for (size_t i = 0; i < positionArray->GetSize(); ++i) {
        positions[i].x += velocities[i].vx * 0.016f;
        positions[i].y += velocities[i].vy * 0.016f;
        positions[i].z += velocities[i].vz * 0.016f;
    }
    
    auto end = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::nanoseconds>(end - start);
    
    std::cout << "Cache-friendly access: " << entityCount << " entities in " 
              << duration.count() << " nanoseconds" << std::endl;
    
    // 결과 검증
    EXPECT_FLOAT_EQ(positions[0].x, 0.016f);
    EXPECT_FLOAT_EQ(positions[entityCount-1].x, static_cast<float>(entityCount-1) + 0.016f);
}

// 대량 컴포넌트 루프 성능 테스트
TEST_F(ECSTest, MassiveComponentLoopPerformance) {
    auto& entityManager = systemManager->GetEntityManager();
    
    // 다양한 엔티티 수로 테스트
    std::vector<int> entityCounts = {1000, 10000, 50000, 100000};
    
    for (int entityCount : entityCounts) {
        std::cout << "\n=== Testing with " << entityCount << " entities ===" << std::endl;
        
        // 엔티티 생성
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
        
        // 1. 기본 시스템 성능 테스트
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
        
        // 2. 직접 배열 접근 성능 테스트
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
        
        // 3. 캐시 최적화된 배치 처리 성능 테스트
        SystemManager batchSystemManager;
        auto& batchEntityManager = batchSystemManager.GetEntityManager();
        batchEntityManager.RegisterComponent<PositionComponent>();
        batchEntityManager.RegisterComponent<VelocityComponent>();
        
        // 엔티티 재생성 (배치 시스템용)
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
        
        // 메모리 사용량 정보
        size_t memoryUsage = entityCount * (sizeof(PositionComponent) + sizeof(VelocityComponent) + sizeof(HealthComponent));
        std::cout << "Memory usage: " << (memoryUsage / 1024) << " KB" << std::endl;
        
        // 정리
        for (auto entity : entities) {
            entityManager.DestroyEntity(entity);
        }
        for (auto entity : batchEntities) {
            batchEntityManager.DestroyEntity(entity);
        }
        
        // 성능 검증 (100,000 엔티티가 1ms 이내에 처리되어야 함)
        if (entityCount == 100000) {
            EXPECT_LT(avgFrameTime, 1000.0); // 1ms 미만
        }
    }
}

// SIMD 최적화 성능 테스트
TEST_F(ECSTest, SIMDOptimizedPerformance) {
    auto& entityManager = systemManager->GetEntityManager();
    
    const int entityCount = 100000;
    std::vector<EntityID> entities;
    
    // 엔티티 생성
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
    
    // 일반 시스템 vs SIMD 최적화 시스템 비교
    const int iterations = 100;
    
    // 1. 일반 시스템
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
    
    // 2. SIMD 최적화 시스템
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
    
    // SIMD 최적화가 최소 10% 성능 향상을 보여야 함
    EXPECT_GT(speedup, 1.1);
    
    // 정리
    for (auto entity : entities) {
        entityManager.DestroyEntity(entity);
    }
}

 