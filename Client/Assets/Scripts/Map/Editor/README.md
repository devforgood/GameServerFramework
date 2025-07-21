# Map JSON Updater

Unity 에디터에서 씬의 spawn 태그와 gate 태그를 찾아 Map.json을 자동으로 업데이트하는 툴입니다.

## 사용법

### 1. 툴 실행
- Unity 에디터에서 `Tools > Map JSON Updater` 메뉴를 선택합니다.

### 2. 씬에서 태그 설정

#### Gate 태그 설정
1. 씬에서 게이트 오브젝트를 생성합니다.
2. 오브젝트의 Tag를 "Gate"로 설정합니다.
3. `GateComponent` 스크립트를 추가합니다.
4. 다음 설정을 입력합니다:
   - **Target Map ID**: 연결할 맵의 ID
   - **Target Gate ID**: 연결할 게이트의 ID
   - **Required Level**: 필요한 레벨

#### Spawn 태그 설정
1. 씬에서 스폰 포인트 오브젝트를 생성합니다.
2. 오브젝트의 Tag를 "Spawn"으로 설정합니다.
3. `SpawnComponent` 스크립트를 추가합니다.
4. 다음 설정을 입력합니다:
   - **Spawn Type**: Player, Monster, Boss 중 선택
   - **Monster ID**: 몬스터 ID (Monster 타입인 경우)
   - **Spawn Interval**: 스폰 간격 (초)
   - **Boss ID**: 보스 ID (Boss 타입인 경우)
   - **Spawn Delay**: 스폰 지연 시간 (초)

### 3. 씬 스캔
1. "Scan Scene for Tags" 버튼을 클릭합니다.
2. 씬 이름과 맵 이름이 일치하면 해당 맵에 업데이트됩니다.
3. 일치하지 않으면 새로운 맵 ID가 생성됩니다.

### 4. 저장
1. "Save Map JSON" 버튼을 클릭하여 변경사항을 저장합니다.

## 시각적 표시

### Gizmos
- **Gate**: 파란색 와이어프레임 큐브
- **Player Spawn**: 초록색 와이어프레임 구
- **Monster Spawn**: 빨간색 와이어프레임 구
- **Boss Spawn**: 노란색 와이어프레임 구

### 에디터 창
- 현재 씬과 일치하는 맵은 녹색으로 표시됩니다.
- 각 맵의 게이트, 스폰 포인트 개수가 표시됩니다.
- 디버그 정보 토글로 추가 정보를 확인할 수 있습니다.

## 예시

### Gate 오브젝트 설정
```
GameObject: "VillageToForestGate"
Tag: "Gate"
GateComponent:
  - Target Map ID: 2
  - Target Gate ID: 1
  - Required Level: 1
```

### Spawn 오브젝트 설정
```
GameObject: "PlayerSpawn1"
Tag: "Spawn"
SpawnComponent:
  - Spawn Type: Player
  - Monster ID: 0
  - Spawn Interval: 0
  - Boss ID: 0
  - Spawn Delay: 0
```

## 주의사항

1. **태그 설정**: 반드시 "Gate" 또는 "Spawn" 태그를 설정해야 합니다.
2. **컴포넌트 추가**: `GateComponent`와 `SpawnComponent`를 추가해야 설정이 적용됩니다.
3. **씬 이름**: 씬 이름과 맵 이름이 일치해야 자동으로 매핑됩니다.
4. **백업**: 중요한 데이터는 반드시 백업 후 사용하세요.

## 자동 생성되는 Map.json 구조

```json
{
  "id": 1,
  "name": "SceneName",
  "name_id": "map_scenename_name",
  "desc_id": "map_scenename_desc",
  "game_mode_id": 1,
  "size": {
    "width": 1000,
    "height": 1000
  },
  "gates": [
    {
      "id": 1,
      "name": "GateName",
      "position": {"x": 100, "y": 0, "z": 100},
      "target_map_id": 2,
      "target_gate_id": 1,
      "required_level": 1
    }
  ],
  "spawn_points": {
    "player_spawn": [
      {"position": {"x": 0, "y": 0, "z": 0}}
    ],
    "monster_spawn": [
      {"position": {"x": 50, "y": 0, "z": 50}, "monster_id": 1, "spawn_interval": 30}
    ],
    "boss_spawn": [
      {"position": {"x": 100, "y": 0, "z": 100}, "boss_id": 1, "spawn_delay": 10}
    ]
  }
}
``` 