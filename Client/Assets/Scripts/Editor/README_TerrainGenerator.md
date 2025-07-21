# Unified Terrain Generator

Unity 에디터에서 다양한 지형을 생성할 수 있는 통합 툴입니다. Simple 모드와 Advanced 모드를 지원합니다.

## 사용법

### 1. 툴 실행
- Unity 에디터에서 `Tools > Terrain Generator` 메뉴를 선택합니다.

### 2. Generator Mode 선택

#### Simple Mode
- 기본적인 지형 생성 기능
- 장애물, 경사로, 물 생성
- 빠르고 간단한 설정

#### Advanced Mode
- 고급 지형 생성 기능
- 고도, 터널, 다리 생성
- 성능 최적화 옵션
- 경로찾기 테스트 설정

### 3. Terrain Type 선택

#### SimplePlane
- 기본 평면 지형
- 장애물과 경사로 추가 가능

#### Maze
- 미로 형태의 지형
- 벽과 통로로 구성

#### City
- 도시 형태의 지형
- 건물과 도로로 구성

#### Forest
- 숲 형태의 지형
- 나무와 자연 요소로 구성

#### Mountain
- 산악 지형
- 고도 변화와 지형 특징

#### Battlefield
- 전장 형태의 지형
- 전투에 최적화된 구조

#### Dungeon
- 던전 형태의 지형
- 복잡한 내부 구조

#### Custom
- 사용자 정의 지형
- 기본 평면에 추가 기능

### 4. Basic Settings

#### Terrain Size
- 지형의 크기 (X, Y, Z)
- 기본값: (50, 10, 50)

#### Grid Size
- 그리드 크기
- 기본값: 10

#### Wall Height
- 벽의 높이
- 기본값: 3

#### Wall Thickness
- 벽의 두께
- 기본값: 0.5

### 5. Simple Mode Settings

#### Obstacles
- **Generate Obstacles**: 장애물 생성 여부
- **Obstacle Count**: 장애물 개수
- **Obstacle Size**: 장애물 크기

#### Ramps
- **Generate Ramps**: 경사로 생성 여부
- **Ramp Count**: 경사로 개수
- **Ramp Height**: 경사로 높이

#### Water
- **Generate Water**: 물 생성 여부
- **Water Level**: 물 높이

### 6. Advanced Mode Settings

#### Advanced Features
- **Generate Elevation**: 고도 생성
- **Max Elevation**: 최대 고도
- **Generate Tunnels**: 터널 생성
- **Tunnel Count**: 터널 개수
- **Generate Bridges**: 다리 생성
- **Bridge Count**: 다리 개수

#### Performance Settings
- **Optimize for Performance**: 성능 최적화
- **Generate LOD**: LOD 생성
- **Use Static Batching**: 정적 배칭 사용

#### Visualization Settings
- **Show Pathfinding Nodes**: 경로찾기 노드 표시
- **Show Grid**: 그리드 표시
- **Grid Color**: 그리드 색상
- **Node Color**: 노드 색상

### 7. Material Settings

#### Required Materials
- **Terrain Material**: 지형 재질
- **Obstacle Material**: 장애물 재질
- **Water Material**: 물 재질

#### Advanced Materials (Advanced Mode)
- **Bridge Material**: 다리 재질
- **Tunnel Material**: 터널 재질

### 8. 지형 생성
1. 원하는 설정을 모두 완료
2. "Generate Simple Terrain" 또는 "Generate Advanced Terrain" 버튼 클릭
3. 생성된 지형이 씬에 추가되고 선택됨

## 특징

### Simple Mode
- 빠른 지형 생성
- 기본적인 기능만 제공
- 초보자에게 적합
- 성능 최적화

### Advanced Mode
- 복잡한 지형 생성
- 고급 기능 제공
- 경로찾기 테스트 지원
- 성능 최적화 옵션

## 주의사항

1. **Material 설정**: 필요한 재질을 미리 설정하거나 자동 생성됨
2. **성능**: Advanced 모드는 더 많은 리소스를 사용
3. **저장**: 생성된 지형은 씬에 저장해야 함
4. **수정**: 생성 후 개별 오브젝트 수정 가능

## 예시

### Simple Mode 예시
```
Mode: Simple
Terrain Type: Maze
Terrain Size: (50, 10, 50)
Generate Obstacles: true
Obstacle Count: 20
Generate Ramps: true
Ramp Count: 5
```

### Advanced Mode 예시
```
Mode: Advanced
Terrain Type: City
Terrain Size: (100, 20, 100)
Generate Elevation: true
Max Elevation: 10
Generate Tunnels: true
Tunnel Count: 3
Optimize for Performance: true
```

## 레거시 툴

기존의 개별 툴들은 `Tools > Terrain Generator (Legacy)` 메뉴에서 접근할 수 있습니다:
- Sample Terrain Generator (Legacy)
- Advanced Terrain Generator (Legacy) 