# TODO — 맵 툴 / 맵 데이터 정리

2026-07-05 기준. Map JSON Updater(Auto Sync) 도입 후 남은 작업 목록.

메인 퀘스트/대화 쪽 작업은 [doc/TODO_story.md](doc/TODO_story.md) 에 따로 있다.
그쪽 A 항목(게이트·스폰)이 여기와 겹친다 — 1막 후반을 열려면 잿빛 숲 입구 게이트와
몬스터 스폰 마커가 필요하고, 그것이 A3 의 씬 없는 맵 문제와도 이어진다.

## A. 데이터 정합

- [x] **A1. Starting Village / Dark Forest 씬 ↔ Map.json 불일치**
  - 씬이 실제 지형(navmesh 원본)이므로 씬 기준으로 map 1/2 JSON을 재작성함.
  - Starting Village = Field1 복사본 (게이트 x=23.71 → map 14), Dark Forest = Field2 복사본 (게이트 x=-23.39 → map 13).
  - map 1 player_spawn을 씬에 맞는 위치(-13.7,0,0)로 수정 (서버 primary 맵 로그인 스폰이 여기서 나옴 — 기존 (100,0,100)은 씬 밖이었음).
  - 씬 밖 좌표의 샘플 monster_spawn / objects 제거.
- [x] **A2. 깨진 게이트 참조 제거**
  - 존재하지 않는 map 3(Shop) / map 4(Cave)를 가리키던 게이트 삭제 (map 1/2 재작성에 포함).
  - Dragon's Lair → map 2 gate 3, Ancient Dungeon → map 2 gate 2, Battle Arena → map 1 gate 3 참조를 실제 존재하는 gate 1로 수정.
- [ ] **A3. 씬 없는 샘플 맵 3개 처리 결정** — Dragon's Lair(6), Ancient Dungeon(9), Battle Arena(12)
  - 씬 없음. game_mode_id 2/3/4라 field 월드에는 로드되지 않아 당장 무해.
  - 각 게임 모드(레이드/던전/아레나) 개발 시작할 때 씬을 만들거나, 불필요하면 삭제.
  - **2026-08-23 갱신**: Ancient Dungeon(9) / Battle Arena(12)는 Map.json 에서 이미 사라졌고 남은 것은
    Dragon's Lair(6)뿐이다. navmesh 는 없는 게 아니라 아르카디아 평원 것을 공유하고 있다
    (`navmesh_path: "Arcadia Plains_navmesh.bin"`, size 도 501.92×501.96 으로 동일 —
    평원 씬의 한 구역을 좌표로만 떼어 둔 상태). 서버 길찾기는 되지만 클라가 로드할 씬이 없다.
    메인 스토리 5막 무대라 삭제 대상이 아니다 → [doc/TODO_story.md](doc/TODO_story.md) D3.
  - **2026-08-31 갱신**: 이제 툴이 이것을 잡는다 — 맵의 `scene` 이 실제 씬이 아니거나
    Build Settings 에 없으면 저장할 때 경고한다(`MapJsonUpdater.ValidateMapReferences`).
    "서버는 멀쩡한데 클라만 못 들어가는" 상태가 조용히 남지 않게 하려는 것이다.
- [x] **A4. Map.json 사본 배포 자동화 (JSON만)**
  - Save/자동 저장 시 `GameData/Map.json`을 Client(Resources)/Game/UnitTest GameData로 verbatim 복사하도록 툴에 추가.
  - [ ] 코드젠(Gamedata.cs / gamedata.h)과 navmesh 바이너리는 여전히 GameDataFlow.py 수동 실행 필요 — 스키마 변경 시 잊지 말 것.

## B. 툴 개선

- [x] **B1. 참조 검증 기능** — `MapJsonUpdater.ValidateMapReferences()` 추가. 수동 저장/스캔/자동 저장 시 dangling `target_map_id`/`target_gate_id`, 맵 id 중복, 맵 내 게이트 id 중복을 경고 로그로 알림(저장은 막지 않음).
- [x] **B2. "Sync Scene ← JSON" 수동 reconcile 버튼** — 툴 창에 버튼 추가. 디스크에서 Map.json을 재로드한 뒤 AutoSync의 id 매칭 reconcile(`ApplyJsonToScene`)을 실행 — 비파괴, 기존 오브젝트 보존.
- [x] **B3. 스폰 포인트 안정 id 도입** — `SpawnPoint.id` 필드 + `Map.json` spawn `id` 추가(맵 내 유일, 타입 공통 네임스페이스). Scan 시 0이면 자동 발급, AutoSync는 id 매칭(id 없는 항목은 종전 이름순 인덱스 매칭으로 폴백 → 기존 씬 무파괴 마이그레이션). 정규화 시 기존 스폰에 id 일괄 부여함.
- [x] **B4. 자동 저장 백업** — 수동/자동 저장 모두 쓰기 직전 기존 파일을 `GameData/Map.json.bak` 1벌로 보존 (`BackupBeforeWrite`). `.bak`은 gitignore 처리.
- [x] **B5. int→double 표기 정규화 커밋** — Map.json 4벌(원본+사본 3)을 툴 직렬화 형태로 정규화(int→double, 스폰 id 부여, CRLF). GameDataFlow 재실행으로 코드젠 갱신 — 오브젝트 position/size/patrol/movement_range가 int→double로 바뀌었으나 해당 필드를 쓰는 서버/클라 코드가 아직 없어 영향 없음(빌드로 확인).

## C. 런타임 / 서버

- [x] **C1. `required_level` 검증 구현(서버)** — `handle(EnterGate)`에서 출발 게이트의 `required_level`을 `PlayerLevel` 컴포넌트 레벨과 비교해 미달 시 거부.
  - [x] **클라 선제 체크 + 피드백** — `PlayerStatSync`(S→C, 레벨/누적 경험치)를 추가했다.
    서버는 로그인 직후와 레벨이 오를 때만 보낸다(경험치만 오른 것은 보내지 않는다 —
    클라가 이 값으로 하는 일은 레벨이 바뀔 때만 달라지고, 처치마다 보내면 사냥 내내 나간다).
    `Gate.OnTriggerEnter` 가 밟기 전에 `required_level` 을 보고, 모자라면 `ScreenNotice` 로
    이유를 띄운다. 판정은 서버가 그대로 다시 하므로 클라를 고쳐도 뚫리지 않는다.
    부하 테스트 봇도 같은 메시지를 받아 못 지나갈 게이트로는 가지 않는다(경로 중간
    게이트의 제한까지 본다 — 첫 게이트만 보면 절반쯤 가서 막힌다).
- [x] **C2. 서버 EnterGate 요청 검증 강화** — 현재 맵 게이트 중 (target_map_id, target_gate_id)가 요청과 일치하는 출발 게이트를 역추적(`FindGateTo`), 없으면 거부. 캐릭터-게이트 xz 거리 5m 초과 시 거부(좌표계 x반전 변환 적용). 임의 맵 순간이동 차단.
- [x] **C3. 씬-navmesh 정합 검증** — `Map::ValidateMapDataOnNavMesh()` 추가: 맵 로드 시 Map.json 게이트/스폰 좌표(클라 좌표계 → x반전)가 navmesh 위에 있는지 findNearestPoly로 검사(수평 허용 2m), 어긋나면 경고 로그. `MapNavMeshTest` 단위 테스트로도 상시 검증 — 현재 맵 1/2/13/14 모두 정합 확인됨.
  - 씬 지형 전체와 navmesh의 일치까지 보장하지는 않음(마커 지점 표본 검증). 씬 수정 후 navmesh 재생성을 잊으면 테스트/로드 로그에서 잡힌다.

## D. 확인 작업 (Unity/실행 필요)

- [ ] **D1. Unity 컴파일 + Auto Sync 실동작 확인** — Field1/Field2를 열었을 때 변경 0건으로 지나가는지, 게이트 이동 시 1초 뒤 자동 저장되는지.
  - 스폰 id 도입으로 첫 씬 오픈 시 스폰 컴포넌트에 id가 스탬프되어 "n건 변경"이 뜨는 것은 정상(1회성 마이그레이션). 이후 재오픈부터 0건이어야 함.
- [ ] **D2. 게이트 id 오버라이드 확인** — 4개 씬 게이트 인스펙터에서 `Id: 1` 표시 확인.
- [ ] **D3. 로그인 스폰 확인** — map 1 player_spawn 수정 후 접속 시 (-13.7,0,0) 부근에 정상 스폰되는지.
- [ ] **D4. EnterGate 서버 검증 실동작 확인** — 정상 게이트 이동이 여전히 되는지(거리 5m 허용치가 충분한지), 조작 요청(존재하지 않는 목적지/원거리 요청)이 거부 로그와 함께 막히는지.
