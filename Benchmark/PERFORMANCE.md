# 성능 측정 기록 — 월드 틱(World::update)

`BM_WorldTick` 이 무거워 병목을 찾고 개선한 기록이다. 수치는 전부 실측이고, 재현 명령을 함께 적는다.

## 측정 환경

- 빌드: `Benchmark.vcxproj` **Release / x64** (Debug 수치는 의미 없음 — `ENABLE_BT_DEBUG` 가 켜지고 최적화가 꺼진다)
- 서버 틱: `kTickDt = 1/30` 초. **30Hz 기준 한 틱 예산은 33ms**
- 몬스터는 `Starting Village`(primary map)에 스폰. 월드에는 맵 4개가 살아 있다
- 명령:

```bash
cd Benchmark/x64/Release
./Benchmark.exe --benchmark_filter="BM_WorldTick/" --benchmark_min_time=0.5
./Benchmark.exe --benchmark_filter="BM_WorldTickPhases/crowd" --benchmark_min_time=0.5
./Benchmark.exe --benchmark_filter="BM_DetectEnemyOnly|BM_BT" --benchmark_min_time=0.5
```

## 1. 어디가 느린가 — 단계별 분해

`Map::update` 는 네 단계(actors → movement → systems → send)로 나뉘어 있고
[`BM_WorldTickPhases`](WorldBenchmark.cpp) 가 각 단계를 개별 계측한다.

**개선 전 (primary 맵 기준, 이동 전략 `crowd` 강제)**

> ⚠️ 1차 측정은 벤치마크가 `crowd` 를 강제 주입한 조건이었다. 프로덕션 필드 맵은
> `GameMode.json` 의 Field(id 1)가 지정한 **`waypoint`** 로 이미 돌고 있었으므로,
> 아래 movement 수치는 crowd 를 쓰는 맵에만 해당한다(§3 에서 바로잡았다).

| 몬스터 | 틱 총합 | actors(BT) | movement(dtCrowd) | systems(ECS) | send |
|---|---|---|---|---|---|
| 1 | 0.044 ms | 1.9 us (4%) | **41.3 us (94%)** | 0.4 us | 0.3 us |
| 1,000 | 4.59 ms | **2.93 ms (64%)** | 1.42 ms (31%) | 0.23 ms (5%) | 0.009 ms |
| 10,000 | 86.8 ms | **62.1 ms (71%)** | 16.3 ms (19%) | 8.35 ms (10%) | 0.08 ms |

한 마리당 한계비용은 actors 6.2us / movement 1.6us / systems 0.84us 였다.
스냅샷 생성·브로드캐스트(send)는 0.1% 미만으로 사실상 공짜다.

### 1-1. actors 안에서는 '탐지'가 아니라 'BT 프레임워크'가 범인

| 항목 | 10,000마리 | actors(62ms) 대비 |
|---|---|---|
| `BM_DetectEnemyOnly` (그리드 쿼리) | 5.31 ms | 8.6% |
| `BM_BTFrameworkTick_BTCpp` (1틱) | 1,582 ns | — |
| `BM_BTFrameworkTick_CodeBase` (1틱) | **58.4 ns** | — |

흔히 의심하는 적 탐지(그리드 스캔)는 actors 의 8.6% 뿐이었다. 같은 로직·같은 트리 구조인데
프레임워크만 다른 두 구현의 **단일 틱 비용이 27배** 차이가 났다.

### 1-2. 빈 맵도 매 틱 값을 치른다

몬스터 1마리인데 crowd 업데이트가 41us 다. [`CrowdNavMovement::MAX_AGENTS`](../Engine/World/CrowdNavMovement.h) 가
16384 이고, `dtCrowd::update` 는 [`getActiveAgents`](../recastnavigation/DetourCrowd.cpp) 등
**활성 여부와 무관하게 16384 슬롯을 훑는 루프**를 매 틱 여러 번 돈다.

`BM_WorldTick/1`(모든 맵) 268us vs `BM_WorldTickPhases/crowd/1`(primary 맵만) 44us —
차액 ~224us 는 **몬스터가 하나도 없는 나머지 3개 맵**이 쓰는 비용이다. 맵당 약 70us, 맵 수에 비례해 늘어난다.

### 1-3. 이동 전략 차이

10,000마리 movement: **crowd 18.4ms vs waypoint 0.65ms (28배)**. 군집 회피를 포기할 수 있는
맵이라면 전략 선택만으로 큰 차이가 난다.

## 2. 조치 #1 — 몬스터 BT 백엔드를 인하우스로 교체

두 백엔드는 [MonsterBT.cpp](../Engine/AI/MonsterBT.cpp)(behaviortree_cpp, `GameData/Monster.xml`)와
[MonsterCodeBaseBT.cpp](../Engine/AI/MonsterCodeBaseBT.cpp)(인하우스 `../BehaviorTree`)로,
트리 구조와 노드 로직이 1:1 로 같다. 바꾼 내용은 두 가지다.

1. **기본 백엔드를 `CodeBase` 로 변경** ([Monster.cpp](../Engine/Actor/Monster.cpp))
2. **선택된 백엔드의 트리만 생성**. 예전에는 몬스터마다 두 트리를 모두 만들고 하나만 틱했다 —
   쓰지도 않는 트리를 위해 스폰마다 `Monster.xml` 을 읽어 파싱했고, 메모리·캐시도 그만큼 낭비했다.

부수 작업: 인하우스 노드에 없던 `Monster dead` / `Monster destoryed` 로그를 맞춰 넣었고,
백엔드 전환이 AI 동작을 바꾸지 않음을 [MonsterBTTest.cpp](../UnitTest/MonsterBTTest.cpp)
(탐지→공격 / 배회 / 사망 분기를 두 백엔드로 각각 검증)로 고정했다.

> 대가: BT 디버그 뷰어(`BTDebugManager`)는 behaviortree_cpp 트리에만 붙는다.
> 뷰어가 필요하면 몬스터 스폰 **전에** `Monster::btBackend_ = BTBackend::BTCpp` 로 되돌린다
> (트리는 스폰 시점에 결정된다).

### 결과

**월드 틱 전체 (`BM_WorldTick`, 맵 4개 포함)**

| 몬스터 | 개선 전 | 개선 후 | 변화 |
|---|---|---|---|
| 1 | 268 us | 282 us | ±0 (전부 고정비) |
| 64 | 449 us | 318 us | -29% |
| 128 | 617 us | 334 us | -46% |
| 256 | 950 us | 371 us | -61% |
| 512 | 2,235 us | 850 us | -62% |
| 1,000 | 4,646 us | **1,744 us** | **-62%** |

**단계별 (primary 맵)**

| | 개선 전 | 개선 후 | 변화 |
|---|---|---|---|
| 1,000 총합 | 4.59 ms | 1.69 ms | -63% |
| 1,000 actors | 2.93 ms | **0.25 ms** | **-91%** |
| 10,000 총합 | 86.8 ms | 23.2 ms | -73% |
| 10,000 actors | 62.1 ms | **8.06 ms** | **-87%** |
| 10,000 movement | 16.3 ms | 10.7 ms | -34% |
| 10,000 systems | 8.35 ms | 4.32 ms | -48% |

actors 외 단계까지 줄어든 것은 몬스터마다 들고 있던 behaviortree_cpp 트리가 사라져
메모리·캐시 압력이 낮아진 효과다. 같은 이유로 `BM_BTWorldTickActors/codebase/10000` 도
12.2ms → **4.37ms** 로 떨어졌다(트리를 하나만 만들게 된 뒤 재측정).

**스폰 비용**: `BM_BTCreate` 는 몬스터 1마리분 트리 생성 비용이다 —
behaviortree_cpp **316 us** vs 인하우스 **1.19 us (265배)**. 스폰마다 XML 을 파싱하던 비용이 사라졌다
(`BM_WorldSpawnMonsters/1000` = 9.5 ms).

30Hz 예산(33ms) 기준으로 한 스레드가 감당하는 몬스터 수는 **약 1,100마리 → 약 14,000마리** 수준이 됐다.

## 3. 조치 #2 — 이동 전략 기본값을 waypoint(경로 추종)로 통일

먼저 정정할 것: **필드 맵은 이미 waypoint 로 돌고 있었다.** `GameMode.json` 의 Field(id 1)가
`"movement": "waypoint"` 이고 [World::Init](../Engine/World/World.cpp) 이 그 값을 그대로 쓴다.
§1 의 movement 수치(10,000마리 16.3ms)는 벤치마크가 `crowd` 를 강제 주입한 값이라
프로덕션에는 해당하지 않았다. 그래서 "movement 가 최대 병목" 이라는 §1 의 결론은 crowd 를 쓰는 맵에만 유효하다.

남아 있던 crowd 경로를 정리했다.

1. [`NavMovementFactory::ParseType`](../Engine/World/NavMovementFactory.cpp) 의 **폴백을 crowd → waypoint**.
   데이터에 `movement` 가 없거나 오타면 예전에는 조용히 crowd 로 떨어졌다.
2. `GameMode.json` 의 Raid(id 2) `movement` 를 **crowd → waypoint** (데이터상 마지막 crowd 사용처).
3. 벤치마크/유닛테스트의 기준 전략을 waypoint 로 맞춤. 그동안 벤치는 crowd 를 강제해
   프로덕션과 다른 값을 냈다. crowd 는 `BM_WorldTick10000/crowd`, `BM_WorldTickPhases/crowd` 로 비교용만 남긴다.

두 전략의 차이는 비용만이 아니다. **waypoint 는 군집 회피/분리(separation)가 없어 에이전트가 겹칠 수 있다.**
회피가 꼭 필요한 맵은 데이터에서 `"movement": "crowd"` 로 지정하면 그대로 동작한다(구현은 유지).

### 결과 (조치 #1 + #2 누적)

**월드 틱 전체 (`BM_WorldTick`, 맵 4개 포함)**

| 몬스터 | 최초(crowd+BT.CPP) | 조치 #1 후 | 조치 #2 후 | 누적 변화 |
|---|---|---|---|---|
| 1 | 268 us | 282 us | **1.34 us** | **-99.5%** |
| 64 | 449 us | 318 us | 18.6 us | -96% |
| 128 | 617 us | 334 us | 38.3 us | -94% |
| 256 | 950 us | 371 us | 85.5 us | -91% |
| 512 | 2,235 us | 850 us | 181 us | -92% |
| 1,000 | 4,646 us | 1,744 us | **608 us** | **-87%** |

1마리 틱이 268us → 1.34us 가 된 것이 §1-2 에서 지적한 **유휴 맵 고정비**다.
waypoint 는 활성 에이전트만 순회하므로 빈 맵은 사실상 공짜다.

**단계별 (primary 맵, waypoint)**

| 몬스터 | 틱 총합 | actors(BT) | movement | systems(ECS) | send |
|---|---|---|---|---|---|
| 1 | 0.001 ms | 0.16 us | 0.02 us | 0.16 us | 0.18 us |
| 1,000 | 0.602 ms | 0.44 ms (72%) | 0.005 ms (0.8%) | 0.15 ms (25%) | 0.008 ms |
| 10,000 | **14.0 ms** | 8.22 ms (59%) | 0.24 ms (1.7%) | 5.46 ms (39%) | 0.06 ms |

같은 조건의 crowd 는 25.8 ms (movement 12.1 ms) — **이동 전략만으로 1.8배** 차이다.
스폰도 `BM_WorldSpawnMonsters/1000` 이 9.5 ms → **4.5 ms** 로 줄었다(crowd 의 addAgent 가 더 비싸다).

**최초 대비: 10,000마리 틱 86.8 ms → 14.0 ms (-84%).** 30Hz 예산(33ms) 안에 10,000마리가 들어온다.

## 4. 수용량 — 10Hz(틱당 100ms)에 몇 마리까지

서버 시뮬레이션은 10Hz([`GameServer::UpdateGameLogic`](../Engine/Server.cpp) 의 `SIM_RATE = 10`)이므로
한 틱 예산은 **100ms** 다. `BM_WorldTickCapacity`(몬스터만) 와 `BM_WorldTickCapacityEngaged`(플레이어 50명)로
`World::update` 를 직접 재고 `budget_pct = 틱 시간 / 100ms` 를 출력한다.

**배회만 (플레이어 0명)**

| 몬스터 | 틱 | 예산 대비 |
|---|---|---|
| 10,000 | 11.8 ms | 12% |
| 20,000 | 32.0 ms | 32% |
| 32,000 | 61.5 ms | 62% |
| **40,000** | **94.7 ms** | **95%** |
| 60,000 | 194 ms | 194% |

**교전 포함 (플레이어 50명이 스폰 좌표 전역에 분산)**

| 몬스터 | 틱 | 예산 대비 |
|---|---|---|
| 10,000 | 28.1 ms | 28% |
| 20,000 | 63.0 ms | 63% |
| **28,000** | **94.0 ms** | **94%** |
| 30,000 | 101.9 ms | 102% |
| 32,000 | 108.7 ms | 109% |

> 40,000 / 28,000 / 30,000 / 32,000 은 `--benchmark_repetitions=3` 중앙값이며 표준편차는 1~3ms 다.

**결론: 배회만이면 약 41,000마리, 플레이어 50명이 교전 중이면 약 29,000마리**에서 10Hz 예산이 찬다.
교전이 붙으면 같은 몬스터 수의 틱 비용이 **1.5~2.4배**로 뛴다 — 탐지가 매 틱 성공하면서
스태거링(`kDetectInterval`)이 무력화되고, `ActionChase` 가 매 틱 `SetMoveTarget` 을 불러
waypoint 전략이 그때마다 경로를 다시 계산하기 때문이다.

한 마리당 비용도 규모에 따라 커진다(10k 1.18us → 40k 2.37us). 액터 리스트/ECS 풀을 훑는
캐시 미스가 늘어난 영향으로, actors 와 systems 가 거의 같은 비율로 함께 증가한다.

### 측정 조건(그대로 믿으면 안 되는 것들)

- **단일 스레드** 기준이다(월드 로직은 한 스레드에서 돈다).
- 몬스터가 유효 스폰 좌표(수천 개)에 **겹쳐 쌓인다**. 실제 맵처럼 흩어지면 그리드 밀도가 달라진다.
- **실제 네트워크 송신이 없다.** 세션이 없어 `SendBroadcast` 가 직렬화까지만 하고 끝난다
  (send 단계가 0.1% 미만인 이유). 실서버는 접속자 수만큼 송신 비용이 더 붙는다.
- 벤치의 `dt` 는 `1/30` 인데 예산은 10Hz 기준으로 계산했다. 비용 자체는 dt 에 거의 무관하지만
  10Hz 로 돌리면 한 틱당 이동 거리가 3배라 경로 재계산 빈도가 달라질 수 있다.

## 5. 남은 병목과 다음 순서

10,000마리 기준 현재 비중이다.

| 단계 | 시간 | 비중 |
|---|---|---|
| actors (BT) | 8.22 ms | 59% |
| systems (ECS) | 5.46 ms | 39% |
| movement (waypoint) | 0.24 ms | 1.7% |
| send | 0.06 ms | 0.4% |

1. **actors** — 이제 프레임워크 오버헤드가 아니라 노드 안의 실제 작업이다.
   `BM_DetectEnemyOnly` 로 탐지 비중을 다시 재고, 스태거링 간격(`kDetectInterval`)이나
   그리드 쿼리 자체를 손볼지 판단한다.
2. **systems** — 위치 변경 감지/ActorInfo 누적이 39%까지 올라왔다. 변경 없는 액터를 빨리 걸러내는 쪽.
3. **movement / send** — 손댈 이유 없음.
