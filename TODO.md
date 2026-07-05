# TODO — 맵 툴 / 맵 데이터 정리

2026-07-05 기준. Map JSON Updater(Auto Sync) 도입 후 남은 작업 목록.

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
  - 씬/navmesh 없음. game_mode_id 2/3/4라 field 월드에는 로드되지 않아 당장 무해.
  - 각 게임 모드(레이드/던전/아레나) 개발 시작할 때 씬을 만들거나, 불필요하면 삭제.
- [x] **A4. Map.json 사본 배포 자동화 (JSON만)**
  - Save/자동 저장 시 `GameData/Map.json`을 Client(Resources)/Game/UnitTest GameData로 verbatim 복사하도록 툴에 추가.
  - [ ] 코드젠(Gamedata.cs / gamedata.h)과 navmesh 바이너리는 여전히 GameDataFlow.py 수동 실행 필요 — 스키마 변경 시 잊지 말 것.

## B. 툴 개선

- [x] **B1. 참조 검증 기능** — `MapJsonUpdater.ValidateMapReferences()` 추가. 수동 저장/스캔/자동 저장 시 dangling `target_map_id`/`target_gate_id`, 맵 id 중복, 맵 내 게이트 id 중복을 경고 로그로 알림(저장은 막지 않음).
- [ ] **B2. "Sync Scene ← JSON" 수동 reconcile 버튼** — 씬을 연 채 JSON을 외부 편집했을 때 재오픈 없이 반영 (기존 Build 버튼은 전체 삭제 후 재생성이라 파괴적).
- [ ] **B3. 스폰 포인트 안정 id 도입** — 현재 이름순 인덱스 매칭이라 이름 변경 시 페어링이 밀림. 게이트/오브젝트처럼 id 보유 방식 검토.
- [x] **B4. 자동 저장 백업** — 수동/자동 저장 모두 쓰기 직전 기존 파일을 `GameData/Map.json.bak` 1벌로 보존 (`BackupBeforeWrite`). `.bak`은 gitignore 처리.
- [ ] **B5. int→double 표기 정규화 커밋** — 첫 자동 저장 시 Map.json 전체가 `500`→`500.0`으로 재포맷되어 큰 diff 발생. 미리 한 번 저장→커밋으로 정규화하고 GameDataFlow 코드젠 타입 영향 확인.

## C. 런타임 / 서버

- [x] **C1. `required_level` 검증 구현(서버)** — `handle(EnterGate)`에서 출발 게이트의 `required_level`을 `PlayerLevel` 컴포넌트 레벨과 비교해 미달 시 거부.
  - [ ] 클라 `Gate.OnTriggerEnter` 선제 체크는 보류 — 클라에 레벨 동기화가 아직 없음(레벨/경험치를 클라로 내려주는 프로토콜 추가 필요). 서버 거부 시 UX 피드백도 이때 함께.
- [x] **C2. 서버 EnterGate 요청 검증 강화** — 현재 맵 게이트 중 (target_map_id, target_gate_id)가 요청과 일치하는 출발 게이트를 역추적(`FindGateTo`), 없으면 거부. 캐릭터-게이트 xz 거리 5m 초과 시 거부(좌표계 x반전 변환 적용). 임의 맵 순간이동 차단.
- [ ] **C3. 씬-navmesh 정합 런타임 검증** — 클라 씬 지형과 서버 navmesh 좌표/스케일 일치 확인 (기존 TODO).

## D. 확인 작업

- [ ] **D1. Unity 컴파일 + Auto Sync 실동작 확인** — Field1/Field2를 열었을 때 변경 0건으로 지나가는지, 게이트 이동 시 1초 뒤 자동 저장되는지.
- [ ] **D2. 게이트 id 오버라이드 확인** — 4개 씬 게이트 인스펙터에서 `Id: 1` 표시 확인.
- [ ] **D3. 로그인 스폰 확인** — map 1 player_spawn 수정 후 접속 시 (-13.7,0,0) 부근에 정상 스폰되는지.
- [ ] **D4. EnterGate 서버 검증 실동작 확인** — 정상 게이트 이동이 여전히 되는지(거리 5m 허용치가 충분한지), 조작 요청(존재하지 않는 목적지/원거리 요청)이 거부 로그와 함께 막히는지.
