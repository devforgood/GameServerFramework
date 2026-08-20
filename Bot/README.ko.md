# Bot — 부하/성능 테스트 클라이언트

실제 게임 클라이언트와 같은 프로토콜로 서버에 붙어, 월드에 입장해 BehaviorTree 로 몬스터를
사냥하는 봇을 다수 돌린다. 목적은 두 가지다.

* **패킷 처리 검증** — 로그인 / 스폰 / 이동 / 스킬 / 관심영역(AoI) 동기화가 부하 상태에서도
  정상 동작하는지.
* **성능·부하 측정** — 동시 접속 수를 올리면서 왕복 지연(p50/p95/p99), 초당 패킷/바이트,
  거부·끊김 수가 어떻게 변하는지.

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

### 출력

```text
t=  15.1s play=  30 conn=  0 dead=  0 down=  0 | tx 38.8/s rx 334.6/s
   | in 274.9KB/s out 1.9KB/s | ping p50 16 p95 31 p99 35 max 35 ms
   | mdeath 15 skill 6 rej 0 | view 32.0
```

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

접속 → `Login` → `AddAgent`(스폰) → BT 로 사냥. 서버 응답에 따라 상태가 바뀐다.

```text
ActiveSelector
├─ Sequence [IsSelfDead, WaitRespawn]                 사망 중에는 아무 명령도 보내지 않는다
├─ Sequence
│   ├─ HasTarget                                      대상이 살아 있고 시야 안인가
│   └─ ActiveSelector
│       ├─ Sequence [IsTargetInAttackRange, Attack]   UseSkill(기본 공격)
│       └─ MoveToTarget                               SetMoveTarget 으로 추격
├─ AcquireTarget                                      가장 가까운 살아 있는 몬스터
└─ Wander                                             사냥감이 없으면 스폰 주변 배회
```

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

* **계정** — 봇마다 계정 id 가 다르다(`bot_000001`…). 같은 id 로 두 세션이 붙으면 서버가
  먼저 붙은 쪽을 쫓아낸다(`EvictExistingLogin`). 로그인 시 계정 행이 없으면 생성되므로
  첫 실행에서는 DB 쓰기가 함께 측정된다.
* **인증** — 서버 `auth.mode` 가 `allow_all` 이면 토큰 없이 통과한다. `db_token` 이면
  `session_token` 에 있는 값을 `--token` 으로 넘겨야 한다.
* **끊긴 봇** — 서버는 세션이 끊겨도 60초 동안 캐릭터를 유지한다(재접속 유예). 테스트를
  반복하면 그 시간 동안 이전 실행의 캐릭터가 월드에 남아 `view` 를 부풀린다.
* **몬스터가 부족하면 전투가 측정되지 않는다** — 지금 기본 맵(Starting Village)의
  `monster_spawn` 마커는 3개이고, 마커의 `spawn_interval` 은 서버가 아직 사용하지 않는다
  (죽은 몬스터가 다시 살아나지 않는다). 그래서 서버를 새로 띄운 직후에만 전투가 발생하고,
  이후에는 봇이 배회만 한다. 지속적인 전투 부하가 필요하면 서버에 몬스터 리스폰을 넣거나
  `Map.json` 의 기본 맵에 `monster_spawn` 마커를 늘려야 한다.
  (배회만 해도 이동·AoI·브로드캐스트 부하는 그대로 측정된다.)

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
