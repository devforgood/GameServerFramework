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

// AoE 마스크 상세 테스트 - 거리 기반 필터링
TEST_F(GridManagerTest, AoEMaskDistanceFiltering) {
    float centerX = 50.0f;
    float centerY = 50.0f;
    
    // 다양한 거리에 타겟들 배치 (GridManager의 NEGATIVE_VALUE_OFFSET을 고려하여 조정)
    auto target1 = std::make_unique<MockGridActor>(1, 55.0f, 50.0f, false); // 거리 5
    auto target2 = std::make_unique<MockGridActor>(2, 60.0f, 50.0f, false); // 거리 10
    auto target3 = std::make_unique<MockGridActor>(3, 65.0f, 50.0f, false); // 거리 15
    auto target4 = std::make_unique<MockGridActor>(4, 70.0f, 50.0f, false); // 거리 20
    auto target5 = std::make_unique<MockGridActor>(5, 75.0f, 50.0f, false); // 거리 25
    
    gridManager->add(target1.get());
    gridManager->add(target2.get());
    gridManager->add(target3.get());
    gridManager->add(target4.get());
    gridManager->add(target5.get());
    
    // 범위 12로 설정하여 거리 기반 필터링 테스트
    auto entities = gridManager->getEntitiesInAoEMask(centerX, centerY, 12.0f, 0.0f, 360.0f);
    
    // 거리 12 이내의 타겟만 검색되어야 함 (실제로는 2개만 검색됨)
    EXPECT_EQ(entities.size(), 2);
    
    std::vector<int> foundIds;
    for (auto entity : entities) {
        foundIds.push_back(entity->getAgentID());
    }
    
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 1) != foundIds.end()); // 거리 5
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 2) != foundIds.end()); // 거리 10
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 3) == foundIds.end()); // 거리 15 (범위 밖)
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 4) == foundIds.end()); // 거리 20 (범위 밖)
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 5) == foundIds.end()); // 거리 25 (범위 밖)
}

// AoE 마스크 상세 테스트 - 경계 케이스
TEST_F(GridManagerTest, AoEMaskEdgeCases) {
    float centerX = 50.0f;
    float centerY = 50.0f;
    
    // 경계 케이스들
    auto target1 = std::make_unique<MockGridActor>(1, 50.0f, 50.0f, false); // 정확히 같은 위치
    auto target2 = std::make_unique<MockGridActor>(2, 50.0f, 50.1f, false); // 매우 가까운 위치
    auto target3 = std::make_unique<MockGridActor>(3, 49.9f, 50.0f, false); // 매우 가까운 위치
    
    gridManager->add(target1.get());
    gridManager->add(target2.get());
    gridManager->add(target3.get());
    
    // 매우 작은 범위로 테스트
    auto entities = gridManager->getEntitiesInAoEMask(centerX, centerY, 0.1f, 0.0f, 360.0f);
    
    // 매우 가까운 타겟들만 검색되어야 함
    EXPECT_GE(entities.size(), 1);
    
    // 0도 각도 테스트 (angle <= 0.0f이면 완전한 원형으로 처리됨)
    auto entities2 = gridManager->getEntitiesInAoEMask(centerX, centerY, 10.0f, 0.0f, 0.0f);
    EXPECT_EQ(entities2.size(), 3); // 0도는 완전한 원형으로 처리되어 모든 타겟이 검색됨
}

// AoE 마스크 성능 테스트 - 많은 타겟
TEST_F(GridManagerTest, AoEMaskPerformanceManyTargets) {
    float centerX = 50.0f;
    float centerY = 50.0f;
    
    // 많은 타겟을 원형으로 배치
    std::vector<std::unique_ptr<MockGridActor>> targets;
    for (int i = 0; i < 100; ++i) {
        float angle = (i * 3.6f) * 3.14159f / 180.0f; // 360도를 100개로 나눔
        float distance = 10.0f + (i % 3) * 5.0f; // 거리 변화
        float x = centerX + distance * std::cos(angle);
        float y = centerY + distance * std::sin(angle);
        
        targets.push_back(std::make_unique<MockGridActor>(i, x, y, false));
        gridManager->add(targets.back().get());
    }
    
    // 성능 측정
    auto start = std::chrono::high_resolution_clock::now();
    auto entities = gridManager->getEntitiesInAoEMask(centerX, centerY, 20.0f, 0.0f, 90.0f);
    auto end = std::chrono::high_resolution_clock::now();
    
    auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
    
    // 성능 기준: 1ms 이내
    EXPECT_LT(duration.count(), 1000);
    
    // 일부 타겟이 검색되어야 함
    EXPECT_GT(entities.size(), 0);
    
    std::cout << "  - AoE search time with 100 targets: " << duration.count() << " microseconds" << std::endl;
    std::cout << "  - Number of targets found: " << entities.size() << std::endl;
}

// AoE 마스크 상세 테스트 - 완전한 원형 공격
TEST_F(GridManagerTest, AoEMaskFullCircle) {
    // 중앙에 공격자 배치
    float centerX = 50.0f;
    float centerY = 50.0f;
    
    // 원형으로 여러 타겟 배치
    auto target1 = std::make_unique<MockGridActor>(1, 60.0f, 50.0f, false); // 동쪽
    auto target2 = std::make_unique<MockGridActor>(2, 40.0f, 50.0f, false); // 서쪽
    auto target3 = std::make_unique<MockGridActor>(3, 50.0f, 60.0f, false); // 북쪽
    auto target4 = std::make_unique<MockGridActor>(4, 50.0f, 40.0f, false); // 남쪽
    auto target5 = std::make_unique<MockGridActor>(5, 200.0f, 200.0f, false); // 범위 밖
    
    gridManager->add(target1.get());
    gridManager->add(target2.get());
    gridManager->add(target3.get());
    gridManager->add(target4.get());
    gridManager->add(target5.get());
    
    // 완전한 원형 공격 (360도)
    auto entities = gridManager->getEntitiesInAoEMask(centerX, centerY, 15.0f, 0.0f, 360.0f);
    
    // 범위 내의 모든 타겟이 검색되어야 함
    EXPECT_EQ(entities.size(), 4);
    
    // 각 방향의 타겟이 모두 포함되어야 함
    std::vector<int> foundIds;
    for (auto entity : entities) {
        foundIds.push_back(entity->getAgentID());
    }
    
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 1) != foundIds.end());
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 2) != foundIds.end());
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 3) != foundIds.end());
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 4) != foundIds.end());
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 5) == foundIds.end()); // 범위 밖
}

// AoE 마스크 상세 테스트 - 90도 부채꼴 공격 (동쪽)
TEST_F(GridManagerTest, AoEMask90DegreeEast) {
    float centerX = 50.0f;
    float centerY = 50.0f;
    
    // 동쪽 방향에 타겟들 배치
    auto target1 = std::make_unique<MockGridActor>(1, 60.0f, 50.0f, false); // 정동쪽
    auto target2 = std::make_unique<MockGridActor>(2, 55.0f, 55.0f, false); // 동북쪽 (90도 내)
    auto target3 = std::make_unique<MockGridActor>(3, 55.0f, 45.0f, false); // 동남쪽 (90도 내)
    auto target4 = std::make_unique<MockGridActor>(4, 40.0f, 50.0f, false); // 서쪽 (90도 밖)
    auto target5 = std::make_unique<MockGridActor>(5, 50.0f, 60.0f, false); // 북쪽 (90도 밖)
    
    gridManager->add(target1.get());
    gridManager->add(target2.get());
    gridManager->add(target3.get());
    gridManager->add(target4.get());
    gridManager->add(target5.get());
    
    // 90도 부채꼴 공격 (동쪽 방향, 0도)
    auto entities = gridManager->getEntitiesInAoEMask(centerX, centerY, 15.0f, 0.0f, 90.0f);
    
    // 90도 범위 내의 타겟만 검색되어야 함
    EXPECT_EQ(entities.size(), 3);
    
    std::vector<int> foundIds;
    for (auto entity : entities) {
        foundIds.push_back(entity->getAgentID());
    }
    
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 1) != foundIds.end()); // 정동쪽
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 2) != foundIds.end()); // 동북쪽
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 3) != foundIds.end()); // 동남쪽
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 4) == foundIds.end()); // 서쪽 (범위 밖)
    EXPECT_TRUE(std::find(foundIds.begin(), foundIds.end(), 5) == foundIds.end()); // 북쪽 (범위 밖)
}

// 새로운 테스트: 다양한 방향의 부채꼴 공격
TEST_F(GridManagerTest, AoEMaskVariousDirections) {
    float centerX = 50.0f;
    float centerY = 50.0f;
    
    // 8방향에 타겟 배치
    auto target1 = std::make_unique<MockGridActor>(1, 60.0f, 50.0f, false); // 동쪽 (0도)
    auto target2 = std::make_unique<MockGridActor>(2, 60.0f, 60.0f, false); // 북동쪽 (45도)
    auto target3 = std::make_unique<MockGridActor>(3, 50.0f, 60.0f, false); // 북쪽 (90도)
    auto target4 = std::make_unique<MockGridActor>(4, 40.0f, 60.0f, false); // 북서쪽 (135도)
    auto target5 = std::make_unique<MockGridActor>(5, 40.0f, 50.0f, false); // 서쪽 (180도)
    auto target6 = std::make_unique<MockGridActor>(6, 40.0f, 40.0f, false); // 남서쪽 (225도)
    auto target7 = std::make_unique<MockGridActor>(7, 50.0f, 40.0f, false); // 남쪽 (270도)
    auto target8 = std::make_unique<MockGridActor>(8, 60.0f, 40.0f, false); // 남동쪽 (315도)
    
    gridManager->add(target1.get());
    gridManager->add(target2.get());
    gridManager->add(target3.get());
    gridManager->add(target4.get());
    gridManager->add(target5.get());
    gridManager->add(target6.get());
    gridManager->add(target7.get());
    gridManager->add(target8.get());
    
    // 북쪽 방향 90도 부채꼴 공격
    auto entitiesNorth = gridManager->getEntitiesInAoEMask(centerX, centerY, 15.0f, 90.0f, 90.0f);
    EXPECT_EQ(entitiesNorth.size(), 3); // 북쪽, 북동쪽, 북서쪽
    
    // 서쪽 방향 90도 부채꼴 공격
    auto entitiesWest = gridManager->getEntitiesInAoEMask(centerX, centerY, 15.0f, 180.0f, 90.0f);
    EXPECT_EQ(entitiesWest.size(), 3); // 서쪽, 북서쪽, 남서쪽
    
    // 남동쪽 방향 45도 부채꼴 공격
    auto entitiesSoutheast = gridManager->getEntitiesInAoEMask(centerX, centerY, 15.0f, 315.0f, 45.0f);
    EXPECT_EQ(entitiesSoutheast.size(), 1); // 남동쪽만
}

// 메인 함수 (gtest 실행용)
int main(int argc, char** argv) {
    ::testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
