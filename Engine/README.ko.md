# Engine - 고성능 게임 서버 엔진

모듈식 고성능 게임 서버 엔진입니다. SIMD로 최적화된 `GridManager` 공간 분할 코어를 중심으로, ECS, 이벤트 브로커, 액터/AI 시스템, 그리고 SQL 영속화를 기반으로 한 플레이어 성장(아이템 · 스킬 · 퀘스트 · 레벨) 시스템을 제공합니다. 코드는 도메인별 모듈 폴더로 정리되어 있으며, Visual Studio 프로젝트 필터 구조와 일치합니다.

## 🌍 언어 선택

- [English](README.md) (기본)
- [한국어](README.ko.md)

## 🎯 주요 기능

### 공간 분할 및 관리
- **2D 그리드 기반 공간 분할**: 월드를 셀 단위로 분할하여 효율적인 공간 쿼리
- **적응형 그리드 구조**: 엔티티 수에 따라 Vector 기반 또는 HashMap 기반 그리드 자동 선택
- **타입별 엔티티 분리**: 캐릭터와 몬스터를 별도로 관리하여 성능 최적화

### 고급 공간 쿼리
- **시야 범위 검색**: 특정 엔티티 주변의 다른 엔티티들을 효율적으로 검색
- **AoE(Area of Effect) 마스크**: 원형 및 부채꼴 범위 내 엔티티 검색
- **실시간 위치 업데이트**: 엔티티 이동 시 자동으로 그리드 셀 업데이트

### 성능 최적화
- **SIMD 벡터화**: AVX2 명령어를 사용한 8개 거리 동시 계산
- **삼각함수 Look-up 테이블**: 미리 계산된 삼각함수 값으로 빠른 각도 계산
- **배치 처리**: 대량의 엔티티를 배치 단위로 처리하여 캐시 효율성 향상

### 게임 시스템
- **ECS**: 캐시 최적화 시스템과 시스템 매니저를 갖춘 Entity-Component-System
- **이벤트 기반 아키텍처**: 스레드 안전 및 락프리(Boost/Moodycamel) 큐를 지원하는 이벤트 브로커/버스
- **액터 & AI**: 비헤이비어 트리로 구동되는 Actor/Character/Monster 엔티티 (BT 디버깅 및 Lua 스크립팅 포함)
- **플레이어 성장**: 데이터 로드/세이브를 갖춘 아이템 · 스킬 · 퀘스트 · **레벨** 시스템
- **영속화**: 생성된 DAO/VO와 변경 추적을 지원하는 SQL 클라이언트

## 🏗️ 아키텍처

### 핵심 클래스 구조

```cpp
class GridManager {
    // 메인 그리드 관리자
    // - 공간 분할 및 엔티티 관리
    // - 고성능 공간 쿼리 제공
};

class IGrid {
    // 그리드 인터페이스
    // - Vector 기반: 밀집된 데이터에 최적화
    // - HashMap 기반: 희소 데이터에 최적화
};

struct Cell {
    std::unordered_set<IGridActor*> characters;  // 캐릭터 엔티티
    std::unordered_set<IGridActor*> monsters;    // 몬스터 엔티티
};
```

### 적응형 그리드 선택 로직

```cpp
GridManager::GridManager(int width, int height, int cellSize) {
    if (width * height > 100000) {
        grid_ = new GridHashMap(width, height, cellSize);  // 대용량 맵
    } else {
        grid_ = new GridVector(width, height, cellSize);   // 소용량 맵
    }
}
```

## 🚀 성능 최적화 기술

### 1. SIMD 벡터화 (AVX2)
```cpp
inline void calculateDistancesSIMD(const float* x_coords, const float* y_coords, 
                                 float center_x, float center_y, float* distances_sq, 
                                 int count) {
    // 8개의 거리를 동시에 계산
    __m256 centerX = _mm256_set1_ps(center_x);
    __m256 centerY = _mm256_set1_ps(center_y);
    
    for (int i = 0; i < count; i += 8) {
        __m256 x = _mm256_loadu_ps(&x_coords[i]);
        __m256 y = _mm256_loadu_ps(&y_coords[i]);
        
        __m256 dx = _mm256_sub_ps(x, centerX);
        __m256 dy = _mm256_sub_ps(y, centerY);
        
        __m256 distSq = _mm256_add_ps(
            _mm256_mul_ps(dx, dx),
            _mm256_mul_ps(dy, dy)
        );
        
        _mm256_storeu_ps(&distances_sq[i], distSq);
    }
}
```

### 2. 삼각함수 Look-up 테이블
```cpp
class TrigLookupTable {
private:
    static constexpr size_t SAMPLES_PER_QUADRANT = 64;  // 90도당 64개 샘플
    static constexpr size_t TABLE_SIZE = SAMPLES_PER_QUADRANT * 4;  // 전체 360도
    alignas(32) std::array<float, TABLE_SIZE> sin_table;
    alignas(32) std::array<float, TABLE_SIZE> cos_table;
    
public:
    float sin(float angle) const;  // 선형 보간을 사용한 빠른 sine 계산
    float cos(float angle) const;  // 선형 보간을 사용한 빠른 cosine 계산
};
```

### 3. 배치 처리 시스템
```cpp
static constexpr size_t MAX_BATCH_SIZE = 256;  // 한 번에 처리할 최대 엔티티 수
alignas(32) float x_coords[MAX_BATCH_SIZE];    // 32바이트 정렬된 좌표 배열
alignas(32) float y_coords[MAX_BATCH_SIZE];
alignas(32) float distances_sq[MAX_BATCH_SIZE];
```

## 📊 사용 예시

### 기본 사용법
```cpp
// 그리드 매니저 초기화 (1000x1000 월드, 10x10 셀 크기)
GridManager gridManager(1000, 1000, 10);

// 엔티티 추가
IGridActor* player = new Player(100.0f, 200.0f);
gridManager.add(player);

// 엔티티 이동
gridManager.move(player, 150.0f, 250.0f);

// 시야 범위 내 엔티티 검색
auto nearbyEntities = gridManager.getEntitiesInViewRange(player, 50.0f);
```

### AoE 스킬 구현
```cpp
// 원형 AoE (반지름 30, 중심점 (100, 100))
auto entitiesInCircle = gridManager.getEntitiesInAoEMask(100.0f, 100.0f, 30.0f, 0.0f);

// 부채꼴 AoE (반지름 30, 방향 45도, 각도 90도)
auto entitiesInSector = gridManager.getEntitiesInAoEMask(100.0f, 100.0f, 30.0f, 45.0f, 90.0f);
```

### 브로드캐스트 메시지
```cpp
// 특정 범위 내 모든 엔티티에게 메시지 전송
gridManager.broadcastToNearby(100.0f, 100.0f, 50.0f, "Hello World!");
```

## 🔧 시스템 요구사항

### 컴파일러 요구사항
- **C++17** 이상 지원
- **AVX2** 명령어셋 지원 (Intel Haswell 이상, AMD Excavator 이상)

### 헤더 파일
```cpp
#include <immintrin.h>  // SIMD 연산
#include <array>        // 고정 크기 배열
#include <unordered_set> // 해시셋
#include <vector>       // 동적 배열
```

## 📈 성능 벤치마크

### 처리 성능
- **10,000개 엔티티**: AoE 쿼리 < 1ms
- **100,000개 엔티티**: 시야 범위 검색 < 5ms
- **SIMD 최적화**: 기존 대비 3-8배 성능 향상

### 메모리 사용량
- **Vector 기반**: 밀집 데이터에 최적화, 낮은 메모리 오버헤드
- **HashMap 기반**: 희소 데이터에 최적화, 동적 메모리 할당

## 📁 프로젝트 구조

소스는 도메인별 폴더로 정리되어 있으며 Visual Studio 프로젝트 필터 구조와 일치합니다. 파일은 평면 `#include "Name.h"` 방식을 사용하며, 모든 모듈 폴더(및 프로젝트 루트)가 `AdditionalIncludeDirectories`에 등록되어 있어 물리적 폴더 위치와 무관하게 헤더가 해석됩니다.

```
Engine/
├── Engine.vcxproj / .filters   # Visual Studio 프로젝트 & 필터
├── Grid/                       # 공간 분할 (GridManager, IGridActor)
├── ECS/                        # Entity-Component-System (ECS, Components, Systems, SystemManager)
├── GameObject/                 # GameObject 베이스, Component, ComponentTypeId
├── Actor/                      # Actor / Character / Monster (+ ActorFactory)
├── AI/                         # 비헤이비어 트리 AI (BehaviorTreeCPP, MonsterBT, BTDebug, LuaObject)
├── Controller/                 # PlayerController
├── Player/                     # Player + 데이터 로드/세이브, 아이템, 스킬, 퀘스트, 레벨, 이벤트 브로커
├── Level/                      # 레벨 시스템 (Level, LevelFactory)
├── Quest/                      # 퀘스트 시스템 (Main / Repeated / LimitedTime + QuestFactory)
├── skill/                      # 스킬 시스템 (Skill, Jump/NormalAttack, SkillManager + SkillFactory)
├── MonsterData/                # 몬스터 데이터 테이블 (+ MonsterDataFactory)
├── GameMode/                   # GameMode (+ GameModeFactory)
├── World/                      # World, Map, NavMap
├── Movement/                   # Vector3, Walker
├── EventBroker/                # 이벤트 버스/큐 (스레드 안전 & 락프리: Boost/Moodycamel)
├── EventMessage/               # 이벤트 메시지 정의
├── Message/                    # 게임 메시지 (GameMessage, SendMessage)
├── SQL/                        # DB 영속화 (SqlClient, DbRecord, generated/ DAO & VO)
├── GameData/                   # 게임 데이터 리소스 로딩 (ResourceLoader, gamedata)
├── Random/ · RingBuffer/ · Time/   # 유틸리티 (RandomUtil, RingBuffer, TimeStamp)
├── flatbuffers/                # 생성된 FlatBuffers 스키마 (syncnet_generated)
├── (루트)                      # Common, Server, Item/ItemFactory, MapFactory, 헬퍼
└── x64/Debug/                  # 빌드 출력
```

## 🔗 관련 프로젝트

- **[Battle/](../Battle/README.md)** - 배틀 서버 (GridManager 사용)
- **[Game/](../Game/README.md)** - 게임 로직 (Actor 시스템)
- **[BehaviorTree/](../BehaviorTree/README.md)** - AI 행동 트리 