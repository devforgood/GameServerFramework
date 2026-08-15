# 퀘스트 시스템

데이터(`quest.json`)가 퀘스트의 모든 것을 정하고, 서버는 이벤트를 받아 진행도만 옮긴다.
새 퀘스트를 넣는 데 C++ 코드는 필요 없다 — 새로운 **목표 종류**나 **퀘스트 타입 정책**을
만들 때만 코드를 건드린다.

## 구성

```text
QuestDefinition (데이터)          PlayerQuestState (플레이어별)
  quest.json                        quest_active  / quest_state 테이블
      |                                   ^
      v                                   |
  ResourceLoader                      PlayerQuest  (컴포넌트)
      |                                   ^
      v                                   |
  QuestFactory -> QuestRegistry ------>   +--- PlayerEventBroker
   (code_name 으로 클래스 선택)                (전투/이동/상호작용이 발행)
```

| 파일 | 역할 |
| --- | --- |
| `Quest.h/.cpp` | 퀘스트 정의 래퍼. 스테이지 조회, 수락 조건 판정, 목표 매칭, 스테이지 완료 판정 |
| `QuestObjective.h` | 목표 종류(`QuestObjectiveType`)와 진행도 누적 방식 |
| `QuestRegistry.h/.cpp` | 정의 인스턴스의 공유 캐시(퀘스트 id 당 1개). `SkillRegistry` 와 같은 구조 |
| `QuestFactory.h/.cpp` | **자동 생성**. `code_name` 으로 파생 클래스를 고른다 |
| `MainQuest` / `SubQuest` / `RepeatedQuest` / `LimitedTimeQuest` | 타입별 정책만 덮어쓴다 |
| `../Player/PlayerQuest.h/.cpp` | 플레이어별 진행 상태. 이벤트 구독, 수락/완료/포기, 보상, DB 왕복 |

정의(`Quest`)는 무상태다. 모든 플레이어가 같은 인스턴스를 읽기만 하고,
진행 상태는 전부 `PlayerQuest` 가 들고 있다.

## 데이터 스키마 (`quest.json`)

```jsonc
{
  "id": 1001,
  "code_name": "MainQuest",        // QuestFactory 가 고를 클래스
  "category": "kill",              // 분류(사냥/수집/탐색/보스 …). 서버 로직에는 쓰지 않는다
  "name_id": "...", "desc_id": "...",
  "level": 3, "min_level": 3, "max_level": 0,   // max_level 0 = 상한 없음
  "priority": 100,                 // 노출 우선순위
  "map_id": 2,                     // 이 퀘스트가 속한 지역
  "start_npc_id": 2001,
  "end_npc_id": 2001,              // auto_complete 가 false 면 필수
  "shareable": true,
  "recommended_party_size": 1,
  "auto_complete": false,          // true = 목표 달성 즉시 완료(완료 NPC 불필요)
  "chain_id": 10, "chain_step": 1, // 같은 체인 안에서 chain_step 은 유일해야 한다

  "prerequisites": {
    "completed_quest_ids": [],     // 전부 완료해야 수락 가능
    "blocked_quest_ids": [],       // 하나라도 완료했으면 수락 불가(분기)
    "item_ids": [], "skill_ids": []
  },

  "stages": [                      // 순서대로 진행한다
    {
      "step": 1,                   // 배열 순서와 같아야 한다(DB 의 stage 값)
      "logic": "and",              // "and" = 전부, "or" = 하나만
      "desc_id": "...",
      "objectives": [              // 스테이지당 최대 3개 (quest_active.progress1~3)
        { "type": "kill", "target_id": 3, "count": 10, "desc_id": "..." }
      ]
    }
  ],

  "time": {
    "reset_type": "none",          // none | daily | weekly
    "limit_seconds": 0,            // 0 = 제한 없음
    "cooldown_seconds": 0,
    "repeatable": false            // reset_type 이 none 이 아니면 반드시 true
  },

  "rewards": {
    "exp": 500, "gold": 200,
    "items": [ { "item_id": 1, "count": 5 } ],
    "choice_items": [ ... ],       // 이 중 하나를 고른다(0-based 인덱스). 0개 또는 2개 이상
    "skill_ids": []
  }
}
```

### 목표 종류

| type | target_id | 진행 방식 | 발행하는 곳 |
| --- | --- | --- | --- |
| `kill` | monster.json 의 종류 | 누적 | `EventActorDead.victim_data_id` (`Monster::NotifyKilledBy`) |
| `level` | (쓰지 않음) | 최고값 — `count` 가 도달 목표 레벨 | `EventLevelUp` (`PlayerLevel::GainExp`) |
| `reach` | Map.json 의 맵 | 누적 | `EventAreaEntered` (`Map::OnAddAgent`) |
| `collect` | item.json 의 아이템 | 누적 | `EventItemAcquired` (`PlayerItem::AddItem`) |
| `use_item` | item.json 의 아이템 | 누적 | `EventItemUsed` (`PlayerItem::UseItem`) |
| `use_skill` | skill.json 의 스킬 | 누적 | `EventSkillUsed` (`PlayerController::handle(UseSkill)`) |
| `talk` | npc.json 의 NPC | 누적 | `EventNpcInteracted` (`PlayerController::handle(Interact)`) |
| `interact` | Map.json 의 오브젝트 | 누적 | `EventObjectInteracted` (같은 `Interact` 핸들러) |

`target_id` 가 0 이면 대상을 가리지 않는 와일드카드다(예: 아무 몬스터나 10마리).
이벤트를 거치지 않고 직접 밀어 넣으려면 `PlayerQuest::ReportProgress` 를 쓴다(테스트/도구).

### 목표 종류를 추가하려면

1. `QuestObjective.h` 의 `QuestObjectiveType` 과 `ParseObjectiveType` 에 추가
2. `GameDataFlow/validate_data.py` 의 `OBJECTIVE_TARGET_TABLE` 에 추가
   (여기에 없으면 데이터 검증이 오타로 보고 코드 생성을 막는다)
3. 그 목표를 진행시킬 이벤트를 `EventMessage.h` 에 만들고 `PlayerQuest::Start` 에서 구독

## 상태 기계

```text
                 AcceptQuest
  (없음) ───────────────────────> InProgress
                                    │  목표 달성 → 다음 스테이지
                                    │  마지막 스테이지 달성
                                    v
                                ReadyToComplete
                                    │  CompleteQuest / 완료 NPC 와 대화
                    ┌───────────────┴───────────────┐
       반복 불가 ↓                                    ↓ 반복 가능
     (행 삭제, 완료 비트만 남음)                    Cooldown
                                                     │ 쿨타임·리셋 경계 통과
                                                     v
                                                 다시 수락 가능

  InProgress ──제한 시간 초과──> Failed ──AcceptQuest──> InProgress (재도전)
```

`QuestState` 값은 `quest_active.state` 컬럼에 그대로 저장되므로 새 값은 뒤에 붙인다.

## 저장

| 테이블 | 내용 |
| --- | --- |
| `quest_active` | 진행 중 퀘스트 한 줄씩. `state`, `stage`, `progress1~3`, `accept_time` |
| `quest_state` | 완료 퀘스트 비트셋(`flags` BLOB). 퀘스트 id 당 1비트 |

- 진행도는 **스테이지 안의 슬롯 순서**로 `progress1~3` 에 들어간다. 그래서 한 스테이지의
  목표는 3개까지고, 데이터 검증이 같은 값으로 막는다. 더 필요하면 스테이지를 나눈다.
- `state = Cooldown` 인 행에서는 `accept_time` 이 **마지막 완료 시각**이다.
  일일/주간 리셋과 쿨타임을 여기서 계산한다.
- 완료 퀘스트는 비트 하나로만 남는다. 완료 시각은 남기지 않으므로, 시각이 필요한 정책
  (쿨타임·리셋)은 반복 퀘스트의 `Cooldown` 행이 담당한다.

### 기존 DB 마이그레이션

`stage` 컬럼이 새로 생겼지만 **손으로 할 일은 없다**. 서버 기동 때
`SqlClientManager::create_tables` 가 두 단계를 밟는다.

1. `create_tables.sql` 의 `CREATE TABLE IF NOT EXISTS` 를 한 문장씩 실행한다. 커넥션에
   `allowMultiQueries` 가 없어 `;` 로 이어 붙인 스크립트를 통째로 넘기면 드라이버가
   거부하므로 `sql_script::Split` 으로 잘라 보낸다.
2. `information_schema.COLUMNS` 를 읽어 실제 컬럼과 생성된 목록(`schema_columns.h`)을
   대조하고, **빠진 컬럼만** `ALTER TABLE ... ADD COLUMN` 한다.

`CREATE TABLE IF NOT EXISTS` 는 이미 있는 테이블에 아무것도 하지 않으므로 2단계가 없으면
**기존 DB 는 영영 갱신되지 않는다**.

2단계를 SQL 로만 하려던 시도(`ADD COLUMN IF NOT EXISTS` 를 컬럼마다 붙이기)는 실패했다.
그 문법은 **MariaDB 전용**이라 MySQL 서버에서는 문장 전체가 문법 오류로 거부된다 —
마이그레이션이 도는 것처럼 보이면서 실제로는 아무것도 하지 않고, 기동할 때마다 컬럼 수만큼
에러 로그만 쌓였다. 지금 방식은 어느 서버에서든 같게 동작하고, 바꿀 것이 없으면 조용하다.

**스키마 정비는 프로세스에 한 번만 돈다**(`std::call_once`). `SqlClientManager::init` 은 DB
스레드마다 불리는데, 스키마까지 매번 돌리면 모두가 같은 시점에 "컬럼이 없다"고 판단하고
같은 `ALTER` 를 동시에 던진다. 하나만 성공하고 나머지는 `Duplicate column name` 으로 실패해,
스키마는 멀쩡한데 기동이 실패한 것처럼 보인다. 별도 서버 프로세스와의 경합까지 고려해
`Duplicate column name`(1060)은 실패로 세지 않는다 — 원하는 상태는 이미 이뤄졌기 때문이다.

컬럼이 없던 시절의 행은 `stage = 0` 으로 읽히는데, `PlayerQuest::Load` 가 1로 올린다.

## 보상

지급은 완료 시점에 실제 컴포넌트로 들어간다.

| 보상 | 받는 곳 |
| --- | --- |
| exp | `PlayerLevel::GainExp` |
| gold | `PlayerWallet::AddGold` |
| items / choice_items | `PlayerItem::AddItem` |
| skill_ids | `PlayerSkill::LearnSkill` |

`PlayerQuest::TakePendingRewards()` 는 "무엇을 줬는지"의 기록이다 — 클라이언트에 보여 줄
보상 목록이자, 아직 받아 줄 컴포넌트가 없는 보상 종류가 조용히 사라지지 않게 하는 장치다.

보상 아이템 지급은 `EventItemAcquired` 를 발행하므로 다른 퀘스트의 수집 목표가 함께
올라갈 수 있다. 실제로 인벤토리에 들어갔으니 맞는 동작이다.

선택 보상은 `choice_items` 의 0-based 인덱스로 고른다(`CompleteQuest(id, index)`).
선택 보상이 있는 퀘스트는 완료 NPC 와 대화하는 것만으로 끝나지 않는다 — 무엇을 줄지
정할 수 없기 때문이다. 클라이언트가 선택을 담아 완료를 요청해야 한다.

### 퀘스트 아이템

`item.json` 에서 `type: "quest"` + `quest_id` 로 표시한다(`no_trade` / `no_sell` 도 함께).
퀘스트를 완료하거나 포기하면 `PlayerItem::RemoveQuestItems` 로 회수된다 — 남겨 두면
인벤토리만 채우고, 다시 수락했을 때 목표가 이미 채워진 채로 시작한다.

회수는 보상 지급보다 **먼저** 한다. 순서가 반대면 방금 받은 보상 아이템이 회수에 휩쓸린다.

## 네트워크 프로토콜

| 메시지 | 방향 | 내용 |
| --- | --- | --- |
| `Interact` | C→S / 응답 | NPC·오브젝트 상호작용. 서버가 같은 맵 + 거리(`interact_range`)를 검증하고 `EventNpcInteracted` / `EventObjectInteracted` 를 발행한다 |
| `QuestAccept` | C→S / 응답 | 수락. 실패 사유는 `QuestAcceptResult` → `StatusCode` 로 좁혀 돌려준다 |
| `QuestComplete` | C→S / 응답 | 완료. `rewardChoice` 로 선택 보상을 고른다 |
| `QuestAbandon` | C→S / 응답 | 포기 |
| `QuestSync` | S→C | 바뀐 퀘스트(`quests`), 목록에서 빠진 것(`removed`), 이번에 완료된 것(`completed`) |

`QuestSync` 는 매 이벤트마다 보내지 않는다. 바뀐 퀘스트 id 만 모아 두고 `PlayerQuest::Update`
끝에서 한 통으로 내보낸다(처치 한 번에 메시지 한 통이 나가지 않도록). 로그인 직후에는
`MarkAllForSync()` 로 진행 중인 전체가 한 번 나간다.

**보내는 주체는 컴포넌트 자신이다.** 무엇이 바뀌었는지는 `PlayerQuest` 가 가장 잘 아는데,
그것을 밖으로 꺼내 `Player` 가 직렬화하게 하면 내부 상태를 오직 그 목적으로만 public 으로
열어 두게 된다. 전송 통로만 [`PlayerSender`](../Player/PlayerSender.h) 로 받아 직접 보낸다 —
통로가 없으면(세션 전, 테스트) 조용히 건너뛰고, 가짜 통로를 끼우면 와이어 포맷을 그대로
검증할 수 있다(`UnitTest/PlayerQuestTest.cpp` 의 `SyncHarness`).

## 파티와의 연결

퀘스트 쪽에서 파티를 아는 곳은 두 군데뿐이다. 자세한 규칙은 [Party/README.md](../Party/README.md).

- **킬 크레딧**: 파티로 잡은 처치는 `party_credit` 이 반경 안의 파티원 전원에게
  `EventActorDead` 를 발행한다. 퀘스트 목표는 **인원과 무관하게 각자 1마리로 센다** —
  나눠 세면 파티 사냥이 퀘스트 진행에는 손해가 된다(경험치만 나눈다).
- **퀘스트 공유**: `quest.json` 의 `shareable` 이 참인 퀘스트만 `PlayerParty::ShareQuest` 로
  파티원에게 제안할 수 있다. 제안을 수락해도 조건 검사(`Quest::CanAccept`)는 그대로 탄다 —
  공유는 조건을 면제해 주지 않는다.

## 운영(GM)

데이터만 바꿔서 라이브 서비스를 조정할 수 있어야 한다.

- **퀘스트 on/off**: `quest.json` 의 `disabled: true`. 새로 받는 것만 막고, **이미 진행 중인
  퀘스트는 그대로 끝낼 수 있다** — 진행분을 빼앗지 않기 위해서다.
- **보상 배율**: `QuestPolicy::Instance().SetExpMultiplier / SetGoldMultiplier`. 지급 시점에
  곱하므로 데이터 재배포가 필요 없다. 배율 때문에 보상이 0 이 되지는 않는다(최소 1).
- **플레이어 조작**: `PlayerQuest::GmForceAccept / GmForceComplete / GmSetProgress / GmResetQuest`.
  모두 정상 경로의 조건 검사를 건너뛴다 — 호출한 쪽이 권한을 확인해야 한다.

## 호위 / 보호

`escort`/`protect` 목표는 대상 NPC 가 월드에 액터로 서 있어야 성립한다. 자세한 것은
[Actor/NonPlayerCharacter.h](../Actor/NonPlayerCharacter.h).

- **`npc.json` 의 hp 가 0 보다 큰 NPC 만** 액터로 스폰된다. 맞을 수 있다는 것이 곧 죽을 수
  있다는 뜻이고, 그때부터 서버가 상태를 들고 있어야 하기 때문이다. 위치만 필요한 NPC
  (퀘스트를 주는 마을 사람)는 데이터로 남는다.
- **목적지는 목표가 아니라 NPC 가 갖는다**(`escort_dest_id`). 양쪽에 적으면 어긋날 수 있고,
  NPC 는 어차피 목적지를 하나만 갖는다.
- **`protect` 의 진행도는 초다.** `count` 가 지켜야 하는 시간이고 `PlayerQuest::Update` 가
  1초 단위로 쌓는다.
- **대상이 죽으면 그 목표가 걸린 퀘스트는 실패한다.** 리스폰한 NPC 로 이어서 하게 두면
  "지켰다"가 아무 의미도 갖지 않는다.

호위 NPC 는 `IsMonsterTarget()` 이 참이라 몬스터가 실제로 공격한다 — 그래야 보호가 위험을
동반한 목표가 된다. `IsCharacter()` 와 나눠 둔 이유는 그쪽이 "`Character*` 로 캐스팅해도
되는가"를 뜻해서다(플레이어 id 조회 등이 그 값을 믿고 캐스팅한다).

## 아직 없는 것

- 조건부 대화 선택지 — 퀘스트 상태에 따라 선택지를 보이거나 숨기는 규칙
  ([Dialog/README.md](../Dialog/README.md) 참고)
- 클라이언트 퀘스트 UI (프로토콜과 생성 코드는 준비돼 있다)
- 보유 스킬과 시전 권한의 연결 — `SkillSet::InitFromResources` 가 아직 모든 스킬을 준다.
  `PlayerSkill` 은 "무엇을 배웠는가"만 기록하며 선행조건/보상에 쓰인다
