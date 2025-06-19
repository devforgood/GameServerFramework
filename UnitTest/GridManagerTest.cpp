#include <gtest/gtest.h>
#include "../Engine/GridManager.h"
#include <memory>
#include <vector>
#include <chrono>

// 테스트용 Mock IGridActor 클래스
class MockGridActor : public IGridActor {
private:
    int agentId;
    float x, y;
    int gridX, gridY;
    bool isCharacterType;

public:
    MockGridActor(int id, float posX, float posY, bool isChar = false) 
        : agentId(id), x(posX), y(posY), gridX(-1), gridY(-1), isCharacterType(isChar) {}

    virtual ~MockGridActor() = default;

    // IGridActor 인터페이스 구현
    virtual bool isCharacter() const override { return isCharacterType; }
    virtual void setGridX(int gx) override { gridX = gx; }
    virtual void setGridY(int gy) override { gridY = gy; }
    virtual int getGridX() const override { return gridX; }
    virtual int getGridY() const override { return gridY; }
    virtual float getVector2X() const override { return x; }
    virtual float getVector2Y() const override { return y; }
    virtual int getAgentID() const override { return agentId; }
	virtual void decrementHealth(int amount) override {} 

    // 위치 설정 메서드
    void setPosition(float newX, float newY) {
        x = newX;
        y = newY;
    }

    int getId() const { return agentId; }
};

// GridManager 테스트 클래스
class GridManagerTest : public ::testing::Test {
protected:
    void SetUp() override {
        // 각 테스트 전에 실행되는 설정
        gridManager = std::make_unique<GridManager>(100, 100, 10);
    }

    void TearDown() override {
        // 각 테스트 후에 실행되는 정리
        gridManager.reset();
    }

    std::unique_ptr<GridManager> gridManager;
};

// 기본 생성자 테스트
TEST_F(GridManagerTest, Constructor) {
    EXPECT_NO_THROW({
        GridManager gm(100, 100, 10);
    });
}

// Actor 추가 테스트
TEST_F(GridManagerTest, AddActor) {
    auto actor = std::make_unique<MockGridActor>(1, 50.0f, 50.0f, true);
    
    EXPECT_NO_THROW(gridManager->add(actor.get()));
    
    // 그리드 좌표가 올바르게 설정되었는지 확인
    // NEGATIVE_VALUE_OFFSET = 100 * 10 / 2 = 500
    // (50 + 500) / 10 = 55
    EXPECT_EQ(actor->getGridX(), 55);
    EXPECT_EQ(actor->getGridY(), 55);
}

// Actor 이동 테스트
TEST_F(GridManagerTest, MoveActor) {
    auto actor = std::make_unique<MockGridActor>(1, 50.0f, 50.0f, true);
    
    gridManager->add(actor.get());
    int originalGridX = actor->getGridX();
    int originalGridY = actor->getGridY();
    
    // 다른 셀로 이동
    actor->setPosition(150.0f, 150.0f);
    gridManager->move(actor.get(), 150.0f, 150.0f);
    
    // 그리드 좌표가 변경되었는지 확인
    EXPECT_TRUE(actor->getGridX() != originalGridX || actor->getGridY() != originalGridY);
}

// Actor 제거 테스트
TEST_F(GridManagerTest, RemoveActor) {
    auto actor = std::make_unique<MockGridActor>(1, 50.0f, 50.0f, true);
    
    gridManager->add(actor.get());
    EXPECT_NO_THROW(gridManager->remove(actor.get()));
}

// 시야 범위 내 엔티티 검색 테스트
TEST_F(GridManagerTest, GetEntitiesInViewRange) {
    // 중앙에 viewer 배치
    auto viewer = std::make_unique<MockGridActor>(1, 50.0f, 50.0f, true);
    gridManager->add(viewer.get());
    
    // 시야 범위 내에 actor 배치
    auto nearbyActor = std::make_unique<MockGridActor>(2, 55.0f, 55.0f, false);
    gridManager->add(nearbyActor.get());
    
    // 시야 범위 밖에 actor 배치
    auto farActor = std::make_unique<MockGridActor>(3, 200.0f, 200.0f, false);
    gridManager->add(farActor.get());
    
    auto entities = gridManager->getEntitiesInViewRange(viewer.get(), 20.0f);
    
    // 최소한 nearbyActor는 포함되어야 함
    EXPECT_GE(entities.size(), 1);
    
    // farActor는 포함되지 않아야 함
    bool foundFarActor = false;
    for (auto entity : entities) {
        if (entity->getAgentID() == 3) {
            foundFarActor = true;
            break;
        }
    }
    EXPECT_FALSE(foundFarActor);
}

// AoE 마스크 테스트
TEST_F(GridManagerTest, GetEntitiesInAoEMask) {
    // 중앙에 AoE 중심점 설정
    float centerX = 50.0f;
    float centerY = 50.0f;
    
    // AoE 범위 내에 actor 배치
    auto actor1 = std::make_unique<MockGridActor>(1, 55.0f, 55.0f, false);
    auto actor2 = std::make_unique<MockGridActor>(2, 45.0f, 45.0f, false);
    gridManager->add(actor1.get());
    gridManager->add(actor2.get());
    
    // AoE 범위 밖에 actor 배치
    auto actor3 = std::make_unique<MockGridActor>(3, 200.0f, 200.0f, false);
    gridManager->add(actor3.get());
    
    auto entities = gridManager->getEntitiesInAoEMask(centerX, centerY, 20.0f, 90.0f);
    
    // 최소한 actor1 또는 actor2는 포함되어야 함
    EXPECT_GE(entities.size(), 1);
    
    // actor3는 포함되지 않아야 함
    bool foundActor3 = false;
    for (auto entity : entities) {
        if (entity->getAgentID() == 3) {
            foundActor3 = true;
            break;
        }
    }
    EXPECT_FALSE(foundActor3);
}

// 캐릭터와 몬스터 분리 저장 테스트
TEST_F(GridManagerTest, CharacterMonsterSeparation) {
    // 같은 위치에 캐릭터와 몬스터 배치
    auto character = std::make_unique<MockGridActor>(1, 50.0f, 50.0f, true);
    auto monster = std::make_unique<MockGridActor>(2, 50.0f, 50.0f, false);
    
    gridManager->add(character.get());
    gridManager->add(monster.get());
    
    // 시야 범위 검색으로 두 타입이 모두 검색되는지 확인
    // getEntitiesInViewRange는 셀 기반이므로 같은 셀에 있으면 모두 검색됨
    auto entities = gridManager->getEntitiesInViewRange(character.get(), 20.0f);
    
    // character는 자신을 제외하므로 monster만 검색되어야 함
    EXPECT_EQ(entities.size(), 1);
    EXPECT_FALSE(entities[0]->isCharacter()); // 몬스터여야 함
    
    // monster의 관점에서 검색하면 character가 검색되어야 함
    auto entities2 = gridManager->getEntitiesInViewRange(monster.get(), 20.0f);
    EXPECT_EQ(entities2.size(), 1);
    EXPECT_TRUE(entities2[0]->isCharacter()); // 캐릭터여야 함
}

// 경계 조건 테스트
TEST_F(GridManagerTest, BoundaryConditions) {
    GridManager smallGrid(10, 10, 10); // 작은 그리드
    
    // 경계 근처에 actor 배치
    auto actor = std::make_unique<MockGridActor>(1, 95.0f, 95.0f, true);
    
    EXPECT_NO_THROW(smallGrid.add(actor.get()));
    EXPECT_GE(actor->getGridX(), 0);
    EXPECT_GE(actor->getGridY(), 0);
}

// 성능 테스트
TEST_F(GridManagerTest, Performance) {
    std::vector<std::unique_ptr<MockGridActor>> actors;
    
    // 많은 actor 추가
    for (int i = 0; i < 1000; ++i) {
        float x = static_cast<float>(i % 100) * 10.0f;
        float y = static_cast<float>(i / 100) * 10.0f;
        actors.push_back(std::make_unique<MockGridActor>(i, x, y, i % 2 == 0));
        gridManager->add(actors.back().get());
    }
    
    // 시야 범위 검색 성능 테스트
    auto viewer = std::make_unique<MockGridActor>(9999, 50.0f, 50.0f, true);
    gridManager->add(viewer.get());
    
    auto start = std::chrono::high_resolution_clock::now();
    auto entities = gridManager->getEntitiesInViewRange(viewer.get(), 50.0f);
    auto end = std::chrono::high_resolution_clock::now();
    
    auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
    
    // 성능 기준: 10ms 이내
    EXPECT_LT(duration.count(), 10000);
    
    // 검색된 엔티티가 있어야 함
    EXPECT_GT(entities.size(), 0);
}

// 여러 Actor 동시 처리 테스트
TEST_F(GridManagerTest, MultipleActors) {
    std::vector<std::unique_ptr<MockGridActor>> actors;
    
    // 100개의 actor를 다양한 위치에 배치
    for (int i = 0; i < 100; ++i) {
        float x = static_cast<float>(i % 10) * 10.0f;
        float y = static_cast<float>(i / 10) * 10.0f;
        actors.push_back(std::make_unique<MockGridActor>(i, x, y, i % 2 == 0));
        gridManager->add(actors.back().get());
    }
    
    // 중앙에서 시야 범위 검색
    auto viewer = std::make_unique<MockGridActor>(100, 50.0f, 50.0f, true);
    gridManager->add(viewer.get());
    
    auto entities = gridManager->getEntitiesInViewRange(viewer.get(), 30.0f);
    
    // 중앙 근처에 있는 actor들이 검색되어야 함
    EXPECT_GT(entities.size(), 0);
}

// 동일 위치 Actor 테스트
TEST_F(GridManagerTest, SamePositionActors) {
    // 같은 위치에 여러 actor 배치
    auto actor1 = std::make_unique<MockGridActor>(1, 50.0f, 50.0f, true);
    auto actor2 = std::make_unique<MockGridActor>(2, 50.0f, 50.0f, false);
    auto actor3 = std::make_unique<MockGridActor>(3, 50.0f, 50.0f, true);
    
    gridManager->add(actor1.get());
    gridManager->add(actor2.get());
    gridManager->add(actor3.get());
    
    // actor1(캐릭터)의 관점에서 검색하면 자신을 제외한 2개만 검색됨
    auto entities = gridManager->getEntitiesInViewRange(actor1.get(), 20.0f);
    EXPECT_EQ(entities.size(), 2);
    
    // actor2(몬스터)의 관점에서 검색하면 모든 캐릭터가 검색됨 (몬스터는 자신을 제외하지 않음)
    auto entities2 = gridManager->getEntitiesInViewRange(actor2.get(), 20.0f);
    EXPECT_EQ(entities2.size(), 2); // actor1과 actor3 (캐릭터들)
}

// 이동 후 검색 테스트
TEST_F(GridManagerTest, MoveAndSearch) {
    auto actor = std::make_unique<MockGridActor>(1, 50.0f, 50.0f, true);
    auto target = std::make_unique<MockGridActor>(2, 200.0f, 200.0f, false);
    
    gridManager->add(actor.get());
    gridManager->add(target.get());
    
    // 초기에는 target이 시야 범위 밖에 있음
    auto entities1 = gridManager->getEntitiesInViewRange(actor.get(), 20.0f);
    bool foundTarget1 = false;
    for (auto entity : entities1) {
        if (entity->getAgentID() == 2) {
            foundTarget1 = true;
            break;
        }
    }
    EXPECT_FALSE(foundTarget1);
    
    // actor를 target 근처로 이동
    actor->setPosition(210.0f, 210.0f);
    gridManager->move(actor.get(), 210.0f, 210.0f);
    
    // 이동 후에는 target이 시야 범위 내에 있어야 함
    auto entities2 = gridManager->getEntitiesInViewRange(actor.get(), 20.0f);
    bool foundTarget2 = false;
    for (auto entity : entities2) {
        if (entity->getAgentID() == 2) {
            foundTarget2 = true;
            break;
        }
    }
    EXPECT_TRUE(foundTarget2);
}

// 메인 함수 (gtest 실행용)
int main(int argc, char** argv) {
    ::testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
