# Map Tool

맵 하나를 **씬부터 서버가 읽는 데이터까지** 만들어 내는 통합 창입니다.
Unity 에디터에서 `Tools > Map Tool`.

```
1. 맵 씬      씬 생성/열기 + Build Settings 등록
2. 지형       프리셋(평지/장애물/미로) + 경계 벽 생성
3. NavMesh    씬 전체 → OBJ → Recast 로 굽기
4. Map.json   맵 항목 생성/갱신 + 마커 스캔 + NavMesh 를 GameData 로 복사
```

네 단계는 **현재 씬 이름 하나로 엮입니다.** 씬 `Foo` 라면
`Assets/GeneratedObj/Foo.obj` → `Assets/GeneratedNavMeshes/Foo_navmesh.bin` →
`GameData/Foo_navmesh.bin` → `Map.json` 의 `scene == "Foo"` 항목(`navmesh_path`)으로 이어집니다.
경로를 손으로 맞출 일이 없고, 각 단계는 파일 존재/수정 시각으로 상태를 스스로 판정합니다.

| 표시 | 뜻 |
| --- | --- |
| 해야 함 | 산출물이 아직 없음 |
| 갱신 필요 | 앞 단계가 더 최신 (예: 씬을 고쳤는데 NavMesh 를 다시 굽지 않음) |
| 완료 | 최신 상태 |

**한 번에 실행** 버튼은 지형은 그대로 두고 `NavMesh 굽기 → Map.json 등록`만 다시 맞춥니다.
씬을 편집한 뒤 서버에 반영할 때 쓰는 경로입니다.

## 주의

- 서버가 읽는 것은 **GameData 쪽 NavMesh 사본**입니다. 4단계까지 해야 실제로 반영됩니다.
- Build Settings 에 없는 씬은 게이트로 진입할 수 없습니다(1단계에서 경고/버튼 제공).
- 지형 프리셋은 NavMesh 를 구울 수 있는 최소 지오메트리(평지·장애물·경사로·미로·경계 벽)만 만듭니다.
  더 복잡한 지형은 씬에서 직접 만들고 3단계부터 이어가면 됩니다(OBJ 추출은 씬 전체를 대상으로 합니다).
- 클라/서버 배포는 별도입니다 — `GameDataFlow/GameDataFlow.py` 를 실행하세요.

## NavMesh 크기 한계

Recast 는 맵 하나를 **타일 하나**로 굽습니다(솔로 빌드). 폴리곤 메시의 정점 인덱스가 `unsigned short` 라
컨투어 정점 합계가 **65,534** 를 넘으면 실패합니다. 네이티브 로그에는 이렇게만 남습니다:

```text
Step 5: Building and triangulating contours...
Could not triangulate contours.
```

맵의 **넓이가 아니라 장애물 개수**가 걸립니다. 1000x1000 맵 실측(2m 큐브 장애물, `cellSize` 0.3):

| 장애물 | 컨투어 정점 | 결과 |
| --- | --- | --- |
| 2,000 | 15,136 | OK (1.4MB) |
| 5,000 | 34,026 | OK (3.1MB) |
| 20,000 | 139,372 | **실패** |

오브젝트당 약 7 정점으로 거의 선형이라, 3단계에 `Estimated cost` 로 미리 표시하고 한계를 넘으면 경고합니다.

넘었을 때 방법은 두 가지입니다.

1. **장애물을 줄인다** — 약 6,500개 이하(창이 알려 주는 값)면 안전합니다.
2. **`Cell size` 를 키운다** — 같은 20,000개 맵도 `cellSize` 1.2 에서는 성공했습니다(0.5·0.8 은 실패).
   대신 NavMesh 가 거칠어져 좁은 통로가 막힐 수 있습니다.

`cellSize` 는 굽는 비용도 좌우합니다. 1000x1000 맵은 0.3 에서 하이트필드가 3339x3340(약 1,100만 셀)이라
13초가 걸립니다. 셀 수가 400만을 넘으면 창이 알려 줍니다.

## 스폰 지점 자동 배치 (4단계 `Scatter spawns`)

큰 맵에 스폰을 손으로 찍는 것은 현실적이지 않습니다(500x500 맵에 몬스터 수백~수천).
4단계의 **`Scatter spawns (random, on navmesh)`** 에서 타입/개수를 정하고 누르면
**NavMesh 위에서만** 무작위로 뽑아 마커를 만듭니다.

씬 지오메트리(레이캐스트·경계 상자)가 아니라 **서버가 읽는 바로 그 navmesh 파일**을 읽습니다.
서버는 스폰 좌표를 `findNearestPoly` 로 검증하고(`Map::ValidateMapDataOnNavMesh`),
벗어난 지점은 몬스터 스폰 자체가 실패합니다. 게다가 navmesh 는 에이전트 반경만큼 안쪽으로
좁혀져 있어서 "바닥 위"와 "이동 가능"이 다릅니다.

| 설정 | 뜻 |
| --- | --- |
| `Type` | Player / Monster / Boss |
| `Count` | 요청 개수. **몬스터는 지점 하나당 한 마리** 스폰됩니다 |
| `Monster` | `monster.json` 목록에서 선택. `(random)` 은 지점마다 무작위 |
| `Min spacing` | 점 사이 최소 거리(m). 0 이면 검사하지 않습니다 |
| `Edge margin` | navmesh 가장자리·장애물에서 띄울 거리(m) |
| `Seed` | 0 이면 매번 다른 배치. 같은 값이면 같은 배치가 재현됩니다 |
| `Replace existing` | 같은 타입의 기존 스폰을 지우고 새로 뿌립니다 |

- 점은 **폴리곤 면적에 비례**해 뽑습니다. 삼각형을 균등하게 고르면 잘게 쪼개진 장애물 주변에 몰립니다.
- 최소 간격이 빡빡하면 요청 개수를 다 채우지 못합니다. 창이 미리 알려 주고, 실제로 놓인 개수를 보고합니다.
  기준은 육각 충전이 아니라 **무작위 배치의 포화 한계**(`면적 / 1.44·간격²`)입니다 —
  Arcadia Plains(이동 가능 면적 184,328m²)에서 간격 5m 로 8,000개를 요청했을 때
  육각 기준으로는 8,514개였지만 실제로는 5,287개에서 멈췄습니다.
- 마커는 `MapDesign/SpawnPoints/Scattered_{타입}` 아래에 모입니다. 되돌리기는 한 번이면 됩니다.

## 구성

| 파일 | 역할 |
| --- | --- |
| `MapToolWindow.cs` | 창(UI) — 단계 상태 표시와 실행 버튼만. 표기는 영어 |
| `MapPipeline.cs` | 단계 실행 로직과 경로 규칙(창 상태에 의존하지 않는 정적 API) |
| `TerrainBuilder.cs` | 지형 생성 |
| `NavMeshSampler.cs` | 구워 둔 navmesh 를 읽어 그 위에서 좌표를 뽑는다(스폰 자동 배치) |
| `MapJsonUpdater.cs` | 씬 ⇔ Map.json 변환 라이브러리(아래 문서) |
| `MapJsonAutoSync.cs` | 씬 열기/편집 시 Map.json 자동 동기화 |

## 통합되면서 없어진 창

`Tools` 메뉴에는 이제 **Map Tool 하나만** 있습니다. 아래 창들은 삭제됐습니다.

| 없어진 창 | 대체 |
| --- | --- |
| `Tools > Terrain Generator` (+ Legacy 2종) | Map Tool 2단계 (`TerrainBuilder`) |
| `Tools > RecastNavigation/Open NavMesh Generator` | Map Tool 3단계 (씬 → OBJ → 굽기 자동) |
| `Tools > Map JSON Updater` | Map Tool 4단계 + `Markers` 접기 영역 |

- 지형 프리셋은 실제로 동작하던 것(평지·장애물·경사로·미로)만 옮겼습니다.
  삭제된 창의 City/Forest/Mountain/Battlefield/Dungeon 은 로그만 출력하는 빈 구현이었습니다.
- OBJ 추출은 더 이상 선택(Selection) 기반이 아니라 **씬 전체 자동**입니다.
- 마커 배치 · `Rebuild scene markers from Map.json` · Auto Sync 토글은 4단계의 `Markers` 안에 있습니다.
- 없어진 기능: 창에서 맵 메타데이터(name/scene/name_id/game_mode_id)를 직접 편집하던 필드와
  **All Maps** 목록입니다. 지금은 `Map.json` 을 직접 편집하세요(게이트 id 연결도 동일).

---

# Map JSON Updater (라이브러리)

> 예전에는 같은 이름의 독립 창이었지만, 지금은 UI 없이 데이터 모델과 변환 로직만 제공합니다.
> 아래 설명 중 창 조작에 해당하는 부분은 Map Tool 로 옮겨졌습니다.

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
| `Gate` | `gates` | id, gateName, gateType, targetId, requiredLevel |
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

- `id` — 게이트 고유 id(`gates[].id`). **데이터 전체에서 유일**합니다.
  0이면 Scan 시 자동 발급(1000번대)되어 컴포넌트에 기록됩니다.
- `gateName` — `gates[].name`. 비우면 GameObject 이름을 사용합니다.
- `gateType` — `gates[].type`. `TwoWay` 는 짝이 되는 게이트가 서로를 가리켜야 하고,
  `OneWay` 는 인스턴스 던전 입구용이라 짝이 필요 없습니다.
- `targetId` — 도착 지점의 **전역 유일 id**(`target_id`). 게이트 id 이거나 player_spawn id 입니다.
  목적지 맵은 그 마커가 속한 맵으로 정해지므로 목적지 맵 id 를 따로 적지 않습니다.
- `requiredLevel` — `required_level`.

두 맵을 서로 연결할 때는:

1. 맵 A, B를 각각 스캔/저장 → 각 게이트에 id가 발급됩니다.
2. 연결하려는 게이트의 인스펙터에 도착 지점의 **id**를 `Target Id` 로 입력합니다.
   (id는 **All Maps** 섹션 또는 Map.json에서 확인)
3. `TwoWay` 면 상대 게이트의 `Target Id` 도 이 게이트의 id 로 맞춰 짝을 만듭니다.
   레이드 등 인스턴스 입구는 `OneWay` 로 두고 목적지의 player_spawn id 를 가리키면 됩니다.

## gate_links (존 그래프 가중치)

`gates[]` 옆에 있는 `gate_links[]` 는 **같은 맵 안에서 이 마커에서 저 마커까지 걸어가는
비용**입니다. 서버의 존 그래프(맵을 넘나드는 경로 탐색)가 쓰는 가중치로,
`{ "from_id": 1002, "to_id": 1003, "cost": 43.81 }` 형태입니다.

이 툴은 `gate_links` 를 만들지도 고치지도 않습니다(그대로 보존만 합니다). 서버가 맵의
navmesh 로 직접 재기 때문에 보통은 **비워 두면 됩니다**. 적어야 하는 경우는 둘 뿐입니다.

- 서버가 기동 시 로드하지 않는 맵(레이드 등 인스턴스) — 잴 navmesh 가 없습니다.
- 실제 거리와 다르게 경로를 유도하고 싶을 때(적힌 값이 실측값을 이깁니다).

적어 둔 뒤 **게이트를 옮기면 값이 낡습니다**. 그 경우 Scan 시 경고가 뜨고, 서버 쪽
단위 테스트(`ZoneGraphNavMeshTest.RecordedGateLinkMatchesMeasuredCost`)가 실측값과
어긋난 것을 잡습니다.

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
      "id": 1001,
      "name": "Village to Forest Gate",
      "type": "two_way",
      "position": { "x": 950.0, "y": 0.0, "z": 500.0 },
      "target_id": 1002,
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
