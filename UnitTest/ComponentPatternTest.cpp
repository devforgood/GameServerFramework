#include <gtest/gtest.h>
#include "../Engine/GameObject.h"
#include "../Engine/Component.h"
#include "../Engine/ComponentTypeId.h"

// 테스트용 컴포넌트 정의
class PositionComponent : public ComponentBase<PositionComponent> {
public:
    float x, y, z;
    PositionComponent() : x(0), y(0), z(0) {}
    PositionComponent(float x, float y, float z) : x(x), y(y), z(z) {}
};

class VelocityComponent : public ComponentBase<VelocityComponent> {
public:
    float vx, vy, vz;
    VelocityComponent() : vx(0), vy(0), vz(0) {}
    VelocityComponent(float vx, float vy, float vz) : vx(vx), vy(vy), vz(vz) {}
};

class HealthComponent : public ComponentBase<HealthComponent> {
public:
    int health;
    HealthComponent(int hp = 100) : health(hp) {}
};

class MockUpdateComponent : public ComponentBase<MockUpdateComponent> {
public:
    int updateCount = 0;
    int startCount = 0;
    
    void Start() override {
        startCount++;
    }

    void Update() override {
        updateCount++;
    }
};

// ComponentTypeId 테스트
TEST(ComponentPattern, ComponentTypeId_IsUniquePerType) {
    uint32_t posId1 = PositionComponent::StaticTypeId();
    uint32_t posId2 = PositionComponent::StaticTypeId();
    uint32_t velId1 = VelocityComponent::StaticTypeId();
    uint32_t healthId = HealthComponent::StaticTypeId();

    EXPECT_EQ(posId1, posId2);
    EXPECT_NE(posId1, velId1);
    EXPECT_NE(posId1, healthId);
    EXPECT_NE(velId1, healthId);
}

// GameObject AddComponent 및 GetComponent 테스트
TEST(ComponentPattern, GameObject_AddAndGetComponent) {
    GameObject obj("TestObject");

    EXPECT_EQ(obj.GetName(), "TestObject");
    EXPECT_EQ(obj.ComponentCount(), 0);
    EXPECT_FALSE(obj.HasComponent<PositionComponent>());

    auto* pos = obj.AddComponent<PositionComponent>(1.0f, 2.0f, 3.0f);
    EXPECT_NE(pos, nullptr);
    EXPECT_EQ(obj.ComponentCount(), 1);
    EXPECT_TRUE(obj.HasComponent<PositionComponent>());

    auto* retrievedPos = obj.GetComponent<PositionComponent>();
    EXPECT_EQ(pos, retrievedPos);
    EXPECT_EQ(retrievedPos->x, 1.0f);
    EXPECT_EQ(retrievedPos->y, 2.0f);
    EXPECT_EQ(retrievedPos->z, 3.0f);
    EXPECT_EQ(retrievedPos->game_object, &obj);
}

// GameObject RemoveComponent 테스트
TEST(ComponentPattern, GameObject_RemoveComponent) {
    GameObject obj;
    obj.AddComponent<PositionComponent>();
    obj.AddComponent<VelocityComponent>();

    EXPECT_EQ(obj.ComponentCount(), 2);
    EXPECT_TRUE(obj.HasComponent<PositionComponent>());
    EXPECT_TRUE(obj.HasComponent<VelocityComponent>());

    EXPECT_TRUE(obj.RemoveComponent<PositionComponent>());
    EXPECT_EQ(obj.ComponentCount(), 1);
    EXPECT_FALSE(obj.HasComponent<PositionComponent>());
    EXPECT_TRUE(obj.HasComponent<VelocityComponent>());

    EXPECT_FALSE(obj.RemoveComponent<PositionComponent>()); // 이미 삭제된 경우 false 반환
}

// GameObject ForEachComponent 테스트
TEST(ComponentPattern, GameObject_ForEachComponent) {
    GameObject obj;
    obj.AddComponent<PositionComponent>(1.0f, 0.0f, 0.0f);
    obj.AddComponent<VelocityComponent>(0.0f, 1.0f, 0.0f);
    obj.AddComponent<HealthComponent>(100);

    int count = 0;
    obj.ForEachComponent([&count](Component& comp) {
        count++;
    });

    EXPECT_EQ(count, 3);
}

// GameObject Update 호출 시 Component Update 호출 테스트
TEST(ComponentPattern, GameObject_UpdateCallsComponentUpdate) {
    GameObject obj;
    auto* mock = obj.AddComponent<MockUpdateComponent>();

    EXPECT_EQ(mock->startCount, 1); // AddComponent 시 Start 호출 확인
    EXPECT_EQ(mock->updateCount, 0);
    
    obj.update(0.16f);
    EXPECT_EQ(mock->updateCount, 1);
    
    obj.update(0.16f);
    EXPECT_EQ(mock->updateCount, 2);
}

// 중복 추가 시 기존 컴포넌트 반환 테스트
TEST(ComponentPattern, GameObject_AddExistingComponentReturnsExisting) {
    GameObject obj;
    auto* pos1 = obj.AddComponent<PositionComponent>(1.0f, 1.0f, 1.0f);
    auto* pos2 = obj.AddComponent<PositionComponent>(2.0f, 2.0f, 2.0f);

    EXPECT_EQ(pos1, pos2);
    EXPECT_EQ(obj.ComponentCount(), 1);
    EXPECT_EQ(pos1->x, 1.0f); // 2.0f로 덮어쓰여지지 않아야 함
}
