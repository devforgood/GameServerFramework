# Unity Client

Unity 기반 게임 클라이언트입니다. 길찾기 테스트 도구와 다양한 개발 도구를 포함하고 있습니다.

## 🎯 주요 기능

### 길찾기 테스트 도구
- **Sample Terrain Generator**: 기본 지형 생성 도구
- **Advanced Terrain Generator**: 고급 지형 생성 (다양한 테스트 시나리오)
- **Pathfinding Test Manager**: 길찾기 에이전트 관리

### 지형 생성 타입
- **SimplePlane**: 기본 평면 지형
- **Maze**: 미로 형태 지형
- **City**: 도시 형태 지형 (빌딩, 다리)
- **Forest**: 숲 형태 지형 (나무, 고도 변화)
- **Mountain**: 산악 지형 (산, 터널)
- **Battlefield**: 전장 지형 (참호, 벙커)
- **Dungeon**: 던전 지형 (벽, 기둥)
- **Custom**: 사용자 정의 지형

### 길찾기 테스트 시나리오
- **SimplePathfinding**: 기본 길찾기 테스트
- **ComplexMaze**: 복잡한 미로 테스트
- **MultiLevel**: 다층 구조 테스트
- **DynamicObstacles**: 동적 장애물 테스트
- **PerformanceTest**: 성능 테스트

## 🛠️ 개발 도구

### Sample Terrain Generator
```
Tools > Sample Terrain Generator
```
- 6가지 기본 지형 타입
- 장애물, 경사로, 물 생성
- 설정 가능한 파라미터

### Advanced Terrain Generator
```
Tools > Advanced Terrain Generator
```
- 8가지 고급 지형 타입
- 5가지 테스트 시나리오
- 성능 최적화 설정
- 시각화 옵션

### Pathfinding Test Manager
```
Tools > Pathfinding Test Manager
```
- 에이전트 생성 및 관리
- 실시간 경로 시각화
- 타겟 랜덤화
- 성능 모니터링

## 🚀 사용 방법

### 1. 지형 생성
1. Unity 에디터에서 `Tools > Sample Terrain Generator` 또는 `Tools > Advanced Terrain Generator` 선택
2. 원하는 지형 타입과 설정 선택
3. "Generate Sample Terrain" 또는 "Generate Advanced Terrain" 버튼 클릭

### 2. 길찾기 테스트
1. `Tools > Pathfinding Test Manager` 선택
2. 에이전트 수와 설정 조정
3. "Spawn Agents" 버튼으로 에이전트 생성
4. Play 모드에서 자동 길찾기 동작 확인

### 3. 에이전트 관리
- **Spawn Agents**: 새로운 에이전트 생성
- **Clear All Agents**: 모든 에이전트 제거
- **Randomize Targets**: 모든 에이전트의 타겟 랜덤화
- **Stop All Agents**: 모든 에이전트 정지

## 📁 프로젝트 구조

```
Client/
├── Assets/
│   ├── Scripts/
│   │   ├── Editor/
│   │   │   ├── SampleTerrainGenerator.cs      # 기본 지형 생성기
│   │   │   ├── AdvancedTerrainGenerator.cs    # 고급 지형 생성기
│   │   │   └── PathfindingTestManager.cs      # 길찾기 테스트 관리자
│   │   ├── PathfindingAgent.cs                # 길찾기 에이전트
│   │   ├── GameManager.cs                     # 게임 매니저
│   │   ├── Session.cs                         # 네트워크 세션
│   │   └── PacketFactory.cs                   # 패킷 팩토리
│   ├── Resources/
│   │   └── Materials/                         # 머티리얼
│   │       ├── TerrainMaterial.mat            # 지형 머티리얼
│   │       ├── ObstacleMaterial.mat           # 장애물 머티리얼
│   │       └── WaterMaterial.mat              # 물 머티리얼
│   ├── Scenes/                               # Unity 씬
│   ├── Prefabs/                              # 프리팹
│   └── GeneratedNavMeshes/                   # 생성된 네비메시
├── Packages/                                 # Unity 패키지
├── ProjectSettings/                          # 프로젝트 설정
└── Library/                                  # Unity 라이브러리
```

## 🔧 개발 환경

### 요구사항
- Unity 2021.3 LTS 이상
- .NET 4.x 또는 .NET Standard 2.1
- Visual Studio 2019 이상 (C# 스크립트 편집용)

### 의존성
- **RecastNavigation**: 길찾기 시스템
- **SharpNav**: 네비게이션 메시
- **FlatBuffers**: 직렬화
- **gRPC**: 네트워크 통신

## 📊 성능 최적화

### 지형 생성 최적화
- **Static Batching**: 정적 오브젝트 배칭
- **LOD 시스템**: 거리에 따른 레벨 오브 다운
- **메모리 최적화**: 효율적인 메모리 사용

### 길찾기 최적화
- **경로 캐싱**: 자주 사용되는 경로 캐시
- **배치 처리**: 다중 에이전트 동시 처리
- **시각화 최적화**: 경로 표시 성능 개선

## 🎮 게임 기능

### 네트워크 통신
- **Session.cs**: 네트워크 세션 관리
- **PacketFactory.cs**: 패킷 생성 및 파싱
- **gRPC 통신**: 서버와의 실시간 통신

### 게임 데이터
- **GameManager.cs**: 게임 상태 관리
- **ItemFactory.cs**: 아이템 생성
- **SkillFactory.cs**: 스킬 생성

### UI 시스템
- **인게임 UI**: 플레이어 상태, 스킬 바
- **로비 UI**: 매칭, 친구 목록
- **설정 UI**: 그래픽, 사운드 설정

## 🔗 관련 프로젝트

- **[Battle/](../Battle/README.md)** - 배틀 서버 (인게임 동기화)
- **[Lobby/](../Lobby/README.md)** - 로비 서버 (매칭, 인증)
- **[Engine/](../Engine/README.md)** - 게임 엔진 (GridManager)
- **[GameData/](../GameData/README.md)** - 게임 데이터

## 📈 개발 가이드

### 새로운 지형 타입 추가
1. `AdvancedTerrainGenerator.cs`에서 새로운 `TerrainType` 추가
2. 해당 지형 생성 메서드 구현
3. UI에 새로운 옵션 추가

### 새로운 길찾기 테스트 추가
1. `PathfindingTestType` enum에 새로운 타입 추가
2. `ConfigureTerrainForTestType` 메서드에 로직 추가
3. 테스트 시나리오 구현

### 커스텀 에이전트 추가
1. `PathfindingAgent` 클래스 상속
2. 커스텀 행동 로직 구현
3. `PathfindingTestManager`에 통합
