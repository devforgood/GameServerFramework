#include <gtest/gtest.h>
#include "../Engine/SystemManager.h"
#include "../Engine/Components.h"
#include "../Engine/Systems.h"
#include <memory>
#include <vector>
#include <chrono>

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

 