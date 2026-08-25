# Bot — 부하/성능 테스트 클라이언트

실제 게임 클라이언트와 같은 프로토콜로 서버에 붙어, 월드에 입장해 BehaviorTree 로 몬스터를
사냥하는 봇을 다수 돌린다. 목적은 두 가지다.

* **패킷 처리 검증** — 로그인 / 스폰 / 이동 / 스킬 / 관심영역(AoI) 동기화, 그리고 상호작용 /
  대화 / 퀘스트 / 게이트 이동이 부하 상태에서도 정상 동작하는지.
* **성능·부하 측정** — 동시 접속 수를 올리면서 왕복 지연(p50/p95/p99), 초당 패킷/바이트,
  거부·끊김 수가 어떻게 변하는지.

봇은 접속해서 사냥만 하는 것이 아니라 **메인 퀘스트 시나리오를 처음부터 진행한다**.
게임 데이터를 읽어 시작 NPC 를 찾아가고, 대화로 퀘스트를 받고, 목표(처치·수집·도달·보고)를
스스로 수행하며, 필요하면 게이트로 맵을 옮긴다.

Unity 클라이언트는 필요 없다. `Bot.exe` 하나가 N명의 봇을 돌린다.

---

## 실행

```powershell
# 서버 (별도 콘솔)
cd Game
..\x64\Debug\Game.exe 65001

# 봇
cd x64\Debug
.\Bot.exe --bots 200 --threads 8 --duration 120 --rampup 20
```

설정 파일은 실행 디렉터리의 `bot_config.json` 을 읽는다. 없으면 기본값으로 뜬다.
`bot_config.example.json` 을 복사해 쓰면 된다(빌드 시 출력 폴더로 자동 복사된다).

커맨드라인 인자가 파일보다 우선한다.

| 인자 | 뜻 |
| --- | --- |
| `--config <path>` | 설정 파일 경로 |
| `--host <ip>` / `--port <n>` | 게임 서버 주소 (기본 127.0.0.1:65001) |
| `--bots <n>` | 봇 수 |
| `--threads <n>` | 워커 스레드 수 |
| `--duration <sec>` | 실행 시간(0 = 무한, Ctrl+C 로 종료) |
| `--rampup <n>` | 초당 접속 시도 수 |
| `--prefix <str>` | 계정 id 접두어 (기본 `bot_`) |
| `--token <str>` | 인증 토큰(서버 `auth.mode` 가 `db_token` 일 때) |
| `--log <level>` | `trace`\|`debug`\|`info`\|`warn`\|`error` |
| `--csv <path>` | 주기 리포트를 CSV 로 기록 |
| `--quest on\|off` | 메인 퀘스트 시나리오 진행 (기본 on) |
| `--branch <n>` | 가지 번호 시작값. 봇 번호에 더해 체인/분기를 고른다 |
| `--gamedata <path>` | 게임 데이터 폴더 (기본: 리포의 통합 폴더를 자동 탐색) |
| `--reuse-accounts` | 실행마다 새 계정을 만들지 않는다(이전 진행을 이어받는다) |

### 출력

```text
t=  15.1s play=  30 conn=  0 dead=  0 down=  0 | tx 38.8/s rx 334.6/s
   | in 274.9KB/s out 1.9KB/s | ping p50 16 p95 31 p99 35 max 35 ms
   | mdeath 15 skill 6 rej 0 | quest 12/4 map 6 | view 32.0
```

`quest a/c` 는 지금까지 수락한 퀘스트 수와 완료한 수, `map` 은 게이트로 맵을 옮긴 횟수다.
이 값이 늘지 않으면 봇은 패킷은 만들고 있지만 시나리오는 한 발짝도 못 나가고 있다는 뜻인데,
나머지 수치만 봐서는 알아챌 수 없다.

`in/out` 은 봇 전체 합이다. `view` 는 봇 하나가 보고 있는 액터 수의 평균으로,
관심영역 브로드캐스트가 얼마나 커지는지를 나타낸다. 종료 시 누적 요약을 출력한다.

---

## 스레드 모델 — 봇 사이에 공유 상태가 없다

```text
main 스레드                워커 0            워커 1            워커 N
  BotRunner        ┌── io_context ──┐  ┌── io_context ──┐   ...
   리포트/종료 조율 │  BotClient …   │  │  BotClient …   │
                   │  (소켓/시야/BT) │  │  (소켓/시야/BT) │
                   └── std::thread ─┘  └── std::thread ─┘
```

* 워커 하나 = `io_context` 하나 = 스레드 하나. 그 워커가 맡은 봇의 소켓 콜백, AI 틱,
  계측치 갱신이 **전부 그 스레드에서만** 실행된다. 그래서 락도 strand 도 없다.
* 봇은 소켓, 수신 버퍼, 시야(`WorldView`), 블랙보드, BT 인스턴스, 난수원, 계측치를
  각자 소유한다. 봇끼리 참조하는 것이 하나도 없으므로 한 봇이 끊기거나 느려져도
  옆 봇의 진행에 영향을 주지 않는다.
* 난수 시드는 봇마다 다르다. 같은 시드면 전원이 같은 좌표로 몰려가 한 셀에만 부하가 실린다.
* 리포터(main)는 워커의 상태를 직접 읽지 않는다. `io_context` 에 작업을 post 해서
  **워커 스레드가 스스로 스냅샷을 만들게** 하고 그 결과만 받아 합친다. 측정 때문에
  부하 발생 경로에 락이 끼어드는 일이 없다.
* 접속은 램프업(`connects_per_second`)으로 나눠 시작한다. 한꺼번에 붙이면 수락 큐와
  로그인 DB 왕복이 먼저 막혀서, 재려던 정상 부하가 아니라 접속 폭주 구간만 측정하게 된다.
* 봇마다 토큰 버킷으로 송신을 제한한다(`limits.max_packets_per_second`). 서버의
  `network.max_packets_per_second` 보다 낮게 두어야 테스트가 레이트리밋 강제 종료로 끝나지 않는다.

---

## 시나리오

접속 → `Login` → `AddAgent`(스폰) → 메인 퀘스트 진행. 서버 응답에 따라 상태가 바뀐다.

```text
ActiveSelector
├─ Sequence [IsSelfDead, WaitRespawn]                 사망 중에는 아무 명령도 보내지 않는다
├─ Sequence [IsDialogOpen, AdvanceDialog]             열린 대화를 먼저 끝낸다
├─ Sequence [IsQuestTravelGoal, TravelToGate]         게이트까지 가서 EnterGate
├─ Sequence [IsQuestNpcGoal, ApproachQuestNpc,        NPC 앞까지 가서 Interact
│            InteractQuestNpc]
├─ Sequence [IsQuestHuntGoal, ReachHuntArea]          사냥터까지 이동(도착하면 전투로 넘긴다)
├─ Sequence
│   ├─ IsCombatAllowed                                사냥 목표일 때만 싸운다
│   ├─ HasTarget                                      대상이 살아 있고 시야 안인가
│   └─ ActiveSelector
│       ├─ Sequence [IsTargetInAttackRange, Attack]   UseSkill(기본 공격)
│       └─ MoveToTarget                               SetMoveTarget 으로 추격
├─ Sequence [IsCombatAllowed, AcquireTarget]          가장 가까운 살아 있는 몬스터
└─ Wander                                             사냥감이 없으면 사냥터(없으면 스폰) 주변 배회
```

`--quest off` 로 끄면 퀘스트 노드가 전부 실패로 떨어져 예전과 똑같은 사냥 트리가 된다.

### 무엇을 할지 정하는 곳과 실행하는 곳이 나뉘어 있다

`BotQuestBrain` 이 **목표 하나**(어디로 가서 무엇을 한다)를 정하고, BT 노드는 그것을 명령으로
옮기기만 한다. 덕분에 "다음에 무엇을 해야 하는가" 를 소켓 없이 단위 테스트로 고정할 수 있다.

| 목표 | 언제 | 봇이 하는 일 |
| --- | --- | --- |
| `Interact` | 수락/보고할 NPC 가 있다 | NPC 앞까지 가서 `Interact` → 대화 선택지를 누른다 |
| `Hunt` | 처치·수집·레벨 목표가 남았다 | 그 몬스터의 스폰 지점으로 가서 평소처럼 싸운다 |
| `Travel` | 목적지가 다른 맵이다 | 게이트까지 가서 `EnterGate` (여러 번 갈아타기도 한다) |
| `None` | 계획을 다 했다 | 자유 사냥으로 부하만 유지한다 |

목표는 서버가 보내는 것(`QuestSync`, `DialogNode`, `EnterGate` 응답)만 사실로 삼아 다시 세운다.
봇이 자기 상태를 추측하지 않으므로, 파티 공유·GM 조작처럼 서버 쪽에서 상태가 바뀌어도
다음 계산에서 자연스럽게 따라간다.

### 목표를 게임 데이터에서 끌어낸다

서버가 읽는 것과 같은 폴더(`Client/Assets/Resources/GameData/`)를 그대로 읽는다. 봇 전용
사본을 두면 데이터가 바뀔 때마다 어긋나고, 그 어긋남은 "봇이 아무 데도 안 간다" 로만 나타난다.

| 데이터 | 봇이 쓰는 것 |
| --- | --- |
| `quest.json` | 체인/분기 순서, 스테이지 목표, 시작·완료 NPC, 선택 보상 개수 |
| `npc.json` | NPC 위치·맵·상호작용 거리·대화 시작 노드 |
| `dialog.json` | 어느 선택지를 눌러야 원하는 수락/완료에 닿는지 |
| `Map.json` | 게이트(맵 사이 길찾기), 몬스터 스폰 지점(사냥터) |
| `monster.json` | 어떤 몬스터가 어떤 아이템을 떨구는지(수집 목표) |

### 분기는 봇마다 갈린다

가지 번호는 **봇 번호**다(`--branch` 로 시작값을 옮긴다). 무작위로 고르면 한쪽으로 쏠리고
재현도 안 되지만, 번호로 가르면 봇을 늘릴수록 모든 가지가 골고루 진행된다.

* **어느 메인 체인을 탈지** — `봇 번호 % 체인 수`
* **같은 자리의 분기 중 무엇을 할지** — `봇 번호 / 체인 수`
  (같은 `chain_step` 에 놓이고 서로를 `blocked_quest_ids` 로 막는 퀘스트들. 한 봇은 반드시
  한쪽만 진행한다 — 서버가 반대쪽을 `BlockedQuest` 로 거절한다)
* **선택 보상** — `봇 번호 % 선택지 수`

### 대화를 어떻게 따라가는가

서버는 **조건(`show_if`)에 걸러진 목록만** 보내므로 클라가 세는 번호와 데이터의 번호가 다르다.
봇은 받은 선택지의 `text_id` 로 데이터의 선택지를 되짚고, 거기서 원하는 동작(`accept_quest` /
`complete_quest`)까지 가는 길을 대화 그래프에서 찾아 그 방향의 `goto` 를 누른다.
(한 노드 안에서 `text_id` 는 유일하다 — 데이터 검증이 막는다.)

원하는 선택지가 목록에 없으면 그것도 정보다. 대개 **레벨이 모자라** 서버가 수락 선택지를
내보내지 않은 것이므로, NPC 를 계속 두드리는 대신 잠시 물러나 몬스터를 잡는다. 그래도 계속
안 되면(이전 실행에서 이미 끝낸 퀘스트 등) 그 퀘스트를 건너뛰고 계획의 다음 칸으로 간다.

### 계정은 실행마다 새로 만든다

메인 퀘스트는 반복할 수 없다. 지난 실행의 계정으로 다시 붙으면 이미 끝낸 퀘스트를 받지 못해
"처음부터" 가 성립하지 않으므로, 계정 id 에 실행마다 다른 꼬리표를 붙인다
(`bot_a1b2c_000001`). 이어서 진행하려면 `--reuse-accounts` 를 쓴다.

트리는 인하우스 BT(`../BehaviorTree`)를 쓰고, 노드 로직은 몬스터 AI와 같은 방식으로
`BotBTNodes.h` 의 구조체에 두고 `BotBehaviorTree.cpp` 가 BT 노드로 감싼다. 덕분에 소켓 없이
가짜 행동 구현으로 시나리오를 단위 테스트할 수 있다(`UnitTest/BotLogicTest.cpp`).

> **주의**: 이 BT 는 `Running` 인 `Sequence` 를 다음 틱에 재초기화하지 않는다(앞의 조건 노드를
> 건너뛰고 진행 중이던 자식부터 다시 틱한다). 그래서 `Running` 을 돌려주는 액션 노드는
> 자기 전제조건이 깨지면 반드시 `Failure` 로 끝내야 한다. 그러지 않으면 부활한 뒤에도 사망
> 분기에 갇히거나, 죽은 대상을 계속 때린다. 회귀 테스트로 고정해 두었다.

### 서버와 맞춰 둔 규칙

* 스폰 위치는 서버가 정한다. 로그인 응답의 좌표를 그대로 `AddAgent` 에 실어 보낸다.
* 재접속 시 로그인 응답의 `uuid`(재접속 토큰)를 되돌려 보낸다. `actorId` 가 0 이 아니면
  핸드오버이므로 `AddAgent` 를 보내지 않고 그 id 를 채택한다.
* `UpdateActorNotify` 는 **변경된 필드만** 온다. 빠진 `state`/`health` 를 기본값으로 덮어쓰면
  살아 있는 몬스터가 매 틱 체력 0 으로 보인다 — 없는 필드는 이전 값을 남긴다.
* `removed` 는 시야 이탈이지 사망이 아니다(처치로 세지 않는다).
* 기본 공격(`skill.json` id 1)은 `pos` 를 조준점으로 삼아 캐스터 → `pos` 방향 부채꼴로
  판정한다. 그래서 자기 위치가 아니라 **대상의 위치**를 실어야 맞는다.
* 이동은 서버가 내비메시로 처리한다. 봇은 목적지만 알려주고 자기 위치는
  `UpdateActorNotify` 로 되받는다.
* 데이터의 좌표는 클라이언트 좌표계다. 서버가 x 축을 뒤집어 비교하므로 봇은 데이터 값을
  그대로 목적지로 쓰면 된다.
* 게이트는 **밟은 게이트 id 만** 보낸다(목적지는 서버가 정한다). 5m 안이어야 받아 주고
  쿨타임이 1초라, 봇도 그보다 촘촘히 보내지 않는다.
* 상호작용 한 번이 `talk` 목표를 올리고, **완료 대기 중이던 퀘스트는 그 자리에서 접수된다**.
  단 선택 보상이 있는 퀘스트는 무엇을 고를지 대화로 전할 수 없어 서버가 거절하므로,
  클라이언트처럼 `QuestComplete` 에 번호를 실어 직접 보낸다.
* `reach`(맵 도달) 목표는 그 맵에 **들어오는 순간**에만 오른다. 이미 그 맵에 있으면
  한 번 나갔다 온다.

---

## 측정치

| 항목 | 의미 |
| --- | --- |
| `ping p50/p95/p99/max` | `Ping` 왕복. 틱 시각이 아니라 실제 시계로 잰다 |
| `login` / `spawn` | 요청 → 응답 지연(요약에만 출력) |
| `tx/rx pps`, `in/out KB/s` | 봇 전체 합계 |
| `mdeath` | 시야 안에서 몬스터가 죽는 것을 본 횟수. **봇별 관측**이라 한 몬스터의 죽음을 그것을 보고 있던 봇 수만큼 센다 |
| `skill` / `rej` | 스킬 전송 수 / 서버가 거부한 수(쿨다운·입력잠금 등) |
| `레이트리밋 보류` | 봇이 스스로 눌러 보내지 않은 패킷 수. 크면 시나리오가 서버 한도에 비해 과격한 것이다 |
| `view` | 봇 하나가 보고 있는 액터 수 평균 |

지연은 1ms 버킷 히스토그램(0~1023ms + 초과)으로 모은다. 평균만 보면 서버가 멈칫한 구간이
통째로 지워지므로 분위수를 같이 본다.

---

## 알아 둘 것

* **계정** — 봇마다 계정 id 가 다르고, 시나리오를 켜면 실행마다도 달라진다(`bot_a1b2c_000001`). 같은 id 로 두 세션이 붙으면 서버가
  먼저 붙은 쪽을 쫓아낸다(`EvictExistingLogin`). 로그인 시 계정 행이 없으면 생성되므로
  첫 실행에서는 DB 쓰기가 함께 측정된다.
* **인증** — 서버 `auth.mode` 가 `allow_all` 이면 토큰 없이 통과한다. `db_token` 이면
  `session_token` 에 있는 값을 `--token` 으로 넘겨야 한다.
* **끊긴 봇** — 서버는 세션이 끊겨도 60초 동안 캐릭터를 유지한다(재접속 유예). 테스트를
  반복하면 그 시간 동안 이전 실행의 캐릭터가 월드에 남아 `view` 를 부풀린다.
* **전투 부하의 크기는 맵 데이터가 정한다** — `monster_spawn` 마커 하나가 `count` 마리를
  유지하고, 죽으면 `spawn_interval` 초 뒤에 다시 채워진다(`Engine/Map/MonsterSpawner`).
  기본 맵(Starting Village)은 마커 3곳 × 6마리 = 18마리이고 리스폰 주기는 20초다.
  더 센 전투 부하가 필요하면 `Map.json` 의 `count` 를 올리거나 마커를 늘린다
  (Unity 맵 툴의 `Scatter spawns` → `Monsters per point`).

---

## 구성

| 파일 | 역할 |
| --- | --- |
| `main.cpp` | 설정 로드 → 러너 실행, Ctrl+C 처리 |
| `BotConfig.*` | JSON + 커맨드라인 설정 |
| `BotRunner.*` | 워커 생성/봇 배정, 주기 리포트, 종료 조율 |
| `BotWorker.*` | 스레드 1 + io_context 1 + 봇 N, 램프업과 틱 |
| `BotClient.*` | 봇 한 명의 상태 기계(접속/로그인/스폰/전투)와 송신 레이트리밋 |
| `BotSession.*` | TCP 연결, 길이 프리픽스 프레이밍, 송수신 큐 |
| `BotPacket.*` | flatbuffers 메시지 생성/파싱, 프레임 인코딩 |
| `WorldView.*` | 서버 동기화 상태 병합, 대상 탐색 |
| `BotScenario.*` | 게임 데이터 로드, 체인/분기 계획, 맵 길찾기, 대화 경로 탐색(봇 전원이 공유) |
| `BotQuestBrain.*` | 봇 한 명의 퀘스트 진행 상태와 다음 목표 하나 |
| `BotBlackboard.h` | BT 가 보는 상태와 행동 인터페이스 |
| `BotBTNodes.h` | 노드 로직(BT 라이브러리를 모른다) |
| `BotBehaviorTree.*` | 노드 어댑터 + 트리 구성 |
| `BotMetrics.*` | 지연 히스토그램, 카운터, 리포트 문자열 |
| `BotLog.*` | 레벨 필터 + 출력 직렬화만 하는 최소 로거 |

빌드:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" `
  GameServerFramework.sln /t:Bot /p:Configuration=Debug /p:Platform=x64
```

프로토콜 헤더는 `Engine/flatbuffers/syncnet_generated.h` 를 그대로 쓴다.
`syncnet.fbs` 를 바꾸면 `BUILD_ENVIRONMENT.md` 의 절차대로 두 C++ 사본을 모두 갱신해야 한다.
