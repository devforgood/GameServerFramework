# Map JSON Updater

Unity 씬과 `GameData/Map.json`을 양방향으로 연동하는 맵 디자인 툴입니다.
씬에 배치한 마커(게이트, 스폰 포인트, 맵 오브젝트)를 JSON으로 기록하고,
반대로 JSON 데이터를 씬에 마커 오브젝트로 생성할 수 있습니다.

## 실행

Unity 에디터에서 `Tools > Map JSON Updater` 메뉴를 선택합니다.

## 자동 동기화 (Auto Sync)

Map.json을 **원본(source of truth)** 으로 씬과 자동 동기화합니다.
기본으로 켜져 있으며, 창 상단의 **Auto Sync** 토글로 끄고 켤 수 있습니다.

- **씬을 열면 (JSON → 씬)**: Map.json 기준으로 마커를 생성/갱신/삭제합니다.
  - 게이트는 `id`, 오브젝트는 `objectId` 로 매칭하여 기존 오브젝트(프리팹 인스턴스 포함)를
    보존한 채 위치/필드만 갱신합니다. 스폰은 타입별 이름순 인덱스로 매칭합니다.
  - JSON에 없는 마커는 씬에서 제거됩니다 (경고 로그 출력).
  - 게이트 생성 시 `Assets/Prefabs/Gate.prefab` 이 있으면 프리팹으로 생성합니다.
- **씬을 편집하면 (씬 → JSON)**: 마커 추가/삭제/이동/필드 변경을 감지해
  약 1초 뒤 Map.json을 자동 저장합니다. (씬 전환·플레이 진입 시에는 즉시 저장)
- **방향 규칙**: 씬을 여는 순간에만 JSON이 이기고, 그 이후의 편집은 씬 → JSON으로 기록됩니다.
- Map.json에 항목이 없는 씬은 어느 방향으로도 건드리지 않습니다.

씬이 JSON과 크게 어긋난 상태에서 **씬을 기준으로** JSON을 맞추고 싶다면:
Auto Sync를 끄고 씬을 연 뒤 **Scan Scene → JSON** + **Save Map JSON**을 실행하고 다시 켜세요.
(자동 저장은 undo 내용도 그대로 JSON에 기록하므로, 작업 전 git 커밋을 권장합니다)

## 마커 컴포넌트

스캔은 **컴포넌트 기반**으로 동작합니다 (태그는 필수가 아닙니다).

| 컴포넌트 | 기록 위치 | 주요 필드 |
| --- | --- | --- |
| `Gate` | `gates` | id, gateName, targetMapId, targetGateId, requiredLevel |
| `SpawnPoint` | `spawn_points.player/monster/boss_spawn` | spawnType, monsterId, spawnInterval, bossId, spawnDelay |
| `MapObjectMarker` | `objects.static_objects` / `objects.movable_objects` | kind, objectType, collision, damage, lootTableId, movementRange, movementSpeed, patrolPath |

- Static 오브젝트의 **크기는 transform.localScale**, 위치는 transform.position으로 기록됩니다.
- `MapObjectMarker.objectId`가 0이면 저장 시 전체 맵에서 유일한 id가 자동 할당되고 마커에 다시 기록됩니다.
- `damage`, `lootTableId`가 0이면 JSON에 기록하지 않습니다.

## 창 구성

### Current Scene 섹션

현재 열린 씬과 연결된 맵(`scene` 필드 기준, 없으면 `name` 기준)을 찾아 표시합니다.
연결된 맵이 없으면 **Create Map Entry for This Scene** 버튼으로 새 맵 항목을 만들 수 있습니다.

- **맵 메타데이터 편집**: name, scene, name_id, desc_id, game_mode_id, size
  - **From Scene Bounds**: 씬의 Renderer 경계로 맵 크기를 자동 계산
- **Scan Scene → JSON**: 씬의 마커를 스캔하여 맵 데이터 갱신
- **Build Scene ← JSON**: JSON 데이터로 씬에 마커 오브젝트 생성
  (기존 마커가 있으면 교체 여부를 물어봅니다. 생성물은 `MapDesign/Gates|SpawnPoints|Objects` 아래에 정리됩니다)
- **Place Markers**: 게이트/스폰/오브젝트 마커를 씬 뷰 중심 위치에 바로 생성
- **NavMesh**: `Assets/GeneratedNavMeshes/{씬이름}_navmesh.bin`과 `GameData/` 복사본의 존재/최신 여부를 표시하고,
  **Copy NavMesh → GameData** 버튼으로 복사와 함께 `navmesh_path`를 갱신합니다.
  (GameData에 navmesh가 없으면 서버가 solo_navmesh로 폴백하여 씬과 어긋납니다)

### All Maps 섹션

모든 맵의 요약(게이트/스폰/오브젝트 수, NavMesh, 게이트 연결 정보)을 표시합니다.

- **Open Scene**: 해당 맵의 씬 에셋을 찾아 엽니다.
- **Delete Map**: Map.json에서 맵 항목을 삭제합니다 (다른 맵 게이트가 참조 중이면 경고 로그).

## 게이트 스키마 (Map.json 기준)

`Gate` 컴포넌트는 Map.json의 `gates[]` 항목과 1:1로 대응합니다.

- `id` — 게이트 고유 id(`gates[].id`). 0이면 Scan 시 자동 발급되어 컴포넌트에 기록됩니다(맵 내부에서 유일).
- `gateName` — `gates[].name`. 비우면 GameObject 이름을 사용합니다.
- `targetMapId` / `targetGateId` — 목적지 맵/게이트의 **id**(`target_map_id` / `target_gate_id`).
  이름이 아니라 id를 직접 지정하므로 스캔 순서·이름 변경과 무관하게 참조가 안정적입니다.
- `requiredLevel` — `required_level`.

두 맵을 서로 연결할 때는:

1. 맵 A, B를 각각 스캔/저장 → 각 게이트에 id가 발급됩니다.
2. 연결하려는 게이트의 인스펙터에 상대 맵/게이트의 **id**를 `Target Map Id` / `Target Gate Id`로 입력합니다.
   (id는 **All Maps** 섹션 또는 Map.json에서 확인)

## 저장과 배포

**Save Map JSON**은 `GameData/Map.json`에만 기록합니다.
Client(Resources)/Game/UnitTest로 배포하려면 `GameDataFlow/GameDataFlow.py`를 실행하세요.
(navmesh 바이너리도 함께 배포됩니다)

## 시각적 표시 (Gizmos)

- **Gate**: 파란색 와이어 큐브 (BoxCollider 크기)
- **Player Spawn**: 초록색 와이어 구
- **Monster Spawn**: 빨간색 와이어 구
- **Boss Spawn**: 노란색 와이어 구
- **Static Object**: 주황(충돌) / 청록(비충돌) 와이어 큐브 (localScale 크기)
- **Movable Object**: 자홍색 와이어 구 + 이동 범위 + 순찰 경로 라인

## Map.json 구조

```json
{
  "id": 1,
  "name": "Starting Village",
  "scene": "Starting Village",
  "name_id": "map_starting_village_name",
  "desc_id": "map_starting_village_desc",
  "game_mode_id": 1,
  "size": { "width": 1000.0, "height": 1000.0 },
  "gates": [
    {
      "id": 1,
      "name": "Village to Forest Gate",
      "position": { "x": 950.0, "y": 0.0, "z": 500.0 },
      "target_map_id": 2,
      "target_gate_id": 1,
      "required_level": 1
    }
  ],
  "spawn_points": {
    "player_spawn": [ { "position": { "x": 100.0, "y": 0.0, "z": 100.0 }, "monster_id": 0, "spawn_interval": 0, "boss_id": 0, "spawn_delay": 0 } ],
    "monster_spawn": [ { "position": { "x": 300.0, "y": 0.0, "z": 300.0 }, "monster_id": 1, "spawn_interval": 30, "boss_id": 0, "spawn_delay": 0 } ],
    "boss_spawn": []
  },
  "objects": {
    "static_objects": [
      { "id": 1, "type": "building", "name": "Village Hall",
        "position": { "x": 500.0, "y": 0.0, "z": 500.0 },
        "size": { "x": 50.0, "y": 30.0, "z": 50.0 },
        "collision": true }
    ],
    "movable_objects": [
      { "id": 3, "type": "npc", "name": "Village Elder",
        "position": { "x": 520.0, "y": 0.0, "z": 520.0 },
        "movement_range": 20.0, "movement_speed": 1.0 }
    ]
  },
  "navmesh_path": "Starting Village_navmesh.bin"
}
```

## 주의사항

1. **알 수 없는 필드 보존**: 툴이 모르는 JSON 필드는 로드/저장 시 그대로 보존됩니다 (JsonExtensionData).
2. **씬 매칭**: 맵의 `scene` 필드와 씬 이름이 일치해야 합니다 (없으면 `name`으로 폴백).
3. **숫자 표기**: 저장 시 좌표가 double로 기록되므로(예: `500` → `500.0`),
   이후 GameDataFlow 코드 생성 시 정수 필드가 double로 바뀔 수 있습니다. GameDataFlow 실행 후 빌드로 확인하세요.
4. **백업**: 중요한 데이터는 git 커밋 후 사용하세요.
