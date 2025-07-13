# Recast Navigation

길찾기 시스템입니다. RecastNavigation 라이브러리를 기반으로 3D 환경에서의 효율적인 경로 탐색을 제공합니다.

## 🎯 주요 기능

### 길찾기 시스템
- **NavMesh 생성**: 3D 지형에서 네비게이션 메시 생성
- **경로 탐색**: A* 알고리즘을 사용한 최적 경로 탐색
- **다중 에이전트**: 여러 에이전트의 동시 경로 탐색

### 성능 최적화
- **메모리 효율성**: 효율적인 메모리 사용
- **실시간 업데이트**: 동적 장애물 처리
- **병렬 처리**: 다중 스레드 경로 탐색

## 🏗️ 아키텍처

### 핵심 컴포넌트
- **NavMesh**: 네비게이션 메시 데이터 구조
- **PathFinder**: 경로 탐색 알고리즘
- **Agent**: 경로를 따라 이동하는 에이전트
- **Crowd**: 다중 에이전트 관리

### 길찾기 파이프라인
1. **지형 분석**: 3D 지형 데이터 분석
2. **NavMesh 생성**: 네비게이션 메시 생성
3. **경로 탐색**: 시작점에서 목표점까지 경로 탐색
4. **경로 스무딩**: 부드러운 경로 생성

## 📊 사용 예시

### NavMesh 생성
```cpp
// NavMesh 생성 설정
rcConfig config;
config.cs = 0.3f;           // 셀 크기
config.ch = 0.2f;           // 셀 높이
config.walkableSlopeAngle = 45.0f;
config.walkableHeight = 2.0f;
config.walkableRadius = 0.6f;
config.walkableClimb = 0.9f;
config.minRegionArea = 8;
config.mergeRegionArea = 20;
config.maxEdgeLen = 12;
config.maxSimplificationError = 1.3f;
config.detailSampleDist = 6.0f;
config.detailSampleMaxError = 1.0f;

// NavMesh 생성
rcBuildNavMesh(&config, meshData, navMesh);
```

### 경로 탐색
```cpp
// 경로 탐색
dtPolyRef startRef, endRef;
float startPos[3] = {100.0f, 0.0f, 100.0f};
float endPos[3] = {200.0f, 0.0f, 200.0f};

// 경로 찾기
dtPolyRef path[MAX_POLYS];
int pathCount;
navQuery->findPath(startRef, endRef, startPos, endPos, &filter, path, &pathCount, MAX_POLYS);

// 경로 스무딩
float smoothPath[MAX_POLYS * 3];
int smoothPathCount;
navQuery->findStraightPath(startPos, endPos, path, pathCount, smoothPath, nullptr, nullptr, &smoothPathCount);
```

## 📁 프로젝트 구조

```
recastnavigation/
├── DetourAlloc.cpp          # 메모리 할당 관리
├── DetourAlloc.h            # 메모리 할당 헤더
├── DetourAssert.cpp         # 디버그 어설트
├── DetourAssert.h           # 디버그 어설트 헤더
├── NavMesh.cpp              # 네비게이션 메시 구현
├── NavMesh.h                # 네비게이션 메시 헤더
├── PathFinder.cpp           # 경로 탐색 구현
├── PathFinder.h             # 경로 탐색 헤더
└── x64/                     # 빌드 출력
    └── Debug/
```

## 🔧 시스템 요구사항

### 컴파일러 요구사항
- **C++17** 이상 지원
- **CMake** 빌드 시스템

### 의존성
- RecastNavigation 라이브러리
- 3D 수학 라이브러리

## 📈 성능 최적화

### 메모리 관리
- **메모리 풀링**: 자주 사용되는 객체 재사용
- **압축 저장**: NavMesh 데이터 압축
- **지연 로딩**: 필요할 때만 NavMesh 로드

### 처리 최적화
- **병렬 처리**: 다중 스레드 경로 탐색
- **공간 분할**: 효율적인 공간 쿼리
- **캐시 시스템**: 자주 사용되는 경로 캐시

## 🔗 관련 프로젝트

- **[Client/](../Client/README.md)** - Unity 클라이언트 (길찾기 테스트)
- **[Engine/](../Engine/README.md)** - 게임 엔진 (공간 분할)
- **[BehaviorTree/](../BehaviorTree/README.md)** - AI 행동 트리 (길찾기 사용) 