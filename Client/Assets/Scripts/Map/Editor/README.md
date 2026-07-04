# Map JSON Updater

Unity 씬과 `GameData/Map.json`을 양방향으로 연동하는 맵 디자인 툴입니다.
씬에 배치한 마커(게이트, 스폰 포인트, 맵 오브젝트)를 JSON으로 기록하고,
반대로 JSON 데이터를 씬에 마커 오브젝트로 생성할 수 있습니다.

## 실행

Unity 에디터에서 `Tools > Map JSON Updater` 메뉴를 선택합니다.

## 마커 컴포넌트

스캔은 **컴포넌트 기반**으로 동작합니다 (태그는 필수가 아닙니다).

| 컴포넌트 | 기록 위치 | 주요 필드 |
| --- | --- | --- |
| `Gate` | `gates` | gateName, destinationMapName, destinationGateName, requiredLevel |
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

## 게이트 ID 안정성

스캔 시 기존 게이트는 **이름으로 매칭하여 id를 유지**합니다.
다른 맵의 `target_gate_id`가 이 id를 참조하므로, 게이트 이름을 바꾸면 새 id가 발급되어
참조가 깨질 수 있습니다. 이름 변경 후에는 이 게이트를 목적지로 쓰는 맵들을 다시 스캔하세요.

새로 만든 두 맵을 서로 연결할 때는:
1. 맵 A, B를 각각 스캔/저장 (게이트 id 발급)
2. 게이트의 destinationMapName/GateName을 채우고 다시 스캔/저장 (참조 해석)

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
