# 퀘스트 시스템

데이터(`quest.json`)가 퀘스트의 모든 것을 정하고, 서버는 이벤트를 받아 진행도만 옮긴다.
새 퀘스트를 넣는 데 C++ 코드는 필요 없다 — 새로운 **목표 종류**나 **퀘스트 타입 정책**을
만들 때만 코드를 건드린다.

## 구성

```
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
| `kill` | monster.json 의 종류 | 누적 | `EventActorDead.victim_data_id` |
| `level` | (쓰지 않음) | 최고값 — `count` 가 도달 목표 레벨 | `EventLevelUp` |
| `reach` | Map.json 의 맵 | 누적 | `EventAreaEntered` (`Map::OnAddAgent`) |
| `collect` | item.json 의 아이템 | 누적 | `EventItemAcquired` — **인벤토리 시스템 대기** |
| `use_item` | item.json 의 아이템 | 누적 | `EventItemUsed` — **대기** |
| `use_skill` | skill.json 의 스킬 | 누적 | `EventSkillUsed` — **대기** |
| `talk` | NPC id | 누적 | `EventNpcInteracted` — **NPC 시스템 대기** |
| `interact` | Map.json 의 오브젝트 | 누적 | `EventObjectInteracted` — **대기** |

"대기" 로 표시된 종류는 서버가 이미 처리할 수 있지만 아직 그 이벤트를 발행하는 시스템이
없다. 해당 시스템이 생기면 이벤트 한 줄만 발행하면 되고, 퀘스트 쪽은 손대지 않는다.
그 전까지는 `PlayerQuest::ReportProgress` 로 직접 밀어 넣을 수 있다(GM 도구/테스트).

`target_id` 가 0 이면 대상을 가리지 않는 와일드카드다(예: 아무 몬스터나 10마리).

### 목표 종류를 추가하려면

1. `QuestObjective.h` 의 `QuestObjectiveType` 과 `ParseObjectiveType` 에 추가
2. `GameDataFlow/validate_data.py` 의 `OBJECTIVE_TARGET_TABLE` 에 추가
   (여기에 없으면 데이터 검증이 오타로 보고 코드 생성을 막는다)
3. 그 목표를 진행시킬 이벤트를 `EventMessage.h` 에 만들고 `PlayerQuest::Start` 에서 구독

## 상태 기계

```
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

`stage` 컬럼이 새로 생겼다. 이미 돌고 있는 DB 에는 다음을 적용한다.

```sql
ALTER TABLE `quest_active` ADD COLUMN `stage` TINYINT NOT NULL DEFAULT 1 AFTER `state`;
```

컬럼이 없던 시절의 행은 `stage = 0` 으로 읽히는데, `PlayerQuest::Load` 가 1로 올린다.

## 보상

경험치는 `PlayerLevel::GainExp` 로 바로 들어간다. 골드/아이템/스킬은 아직 받아 줄
컴포넌트가 없어서 `PlayerQuest::TakePendingRewards()` 대기열에 쌓인다. 인벤토리와 지갑이
생기면 그쪽이 이 대기열을 꺼내 가면 된다 — 없는 시스템을 있는 척 호출해 보상이 조용히
사라지는 것보다 낫다.

선택 보상은 `choice_items` 의 0-based 인덱스로 고른다(`CompleteQuest(id, index)`).
선택 보상이 있는 퀘스트는 완료 NPC 와 대화하는 것만으로 끝나지 않는다 — 무엇을 줄지
정할 수 없기 때문이다. 클라이언트가 선택을 담아 완료를 요청해야 한다.

## 아직 없는 것

이 단계는 데이터 스키마와 서버 코어까지다. 다음이 남아 있다.

- 네트워크 프로토콜(`syncnet.fbs` 에 퀘스트 메시지 없음)과 클라이언트 UI
- 대화(Dialog) 시스템, 선택지에 따른 분기
- 파티 공유(킬 크레딧 공유, 진행 동기화)
- 호위/보호 목표와 그에 따른 실패 조건
- GM 도구(강제 완료/진행도 수정/퀘스트 on-off)
- 인벤토리·NPC 시스템 연동(위 "대기" 목표들)
