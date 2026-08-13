좋은 방법은 **서버의 BehaviorTree.CPP 트리를 “디버그 스냅샷/이벤트 스트림”으로 직렬화해서 Unity 디버그 UI가 재구성**하는 구조입니다.

## 추천 구조

```text
Game Server
 └─ BehaviorTree.CPP
     ├─ Tick 실행
     ├─ Node 상태 변화 감지
     └─ Debug Snapshot / Event 생성
              ↓ WebSocket / TCP / gRPC stream
Unity Client
 └─ BT Debug Viewer
     ├─ Tree 구조 표시
     ├─ 각 Node 상태 색상 표시
     ├─ 현재 실행 경로 하이라이트
     └─ Blackboard 값 표시
```

## 서버에서 보내면 좋은 데이터

### 1. 트리 구조 정보

처음 1회 또는 트리 변경 시 전송합니다.

```json
{
  "treeId": "boss_ai_001",
  "nodes": [
    {
      "id": 1,
      "parentId": null,
      "name": "Root",
      "type": "Sequence"
    },
    {
      "id": 2,
      "parentId": 1,
      "name": "CheckTarget",
      "type": "Condition"
    },
    {
      "id": 3,
      "parentId": 1,
      "name": "Attack",
      "type": "Action"
    }
  ]
}
```

Unity에서는 이걸 기반으로 노드 그래프를 만듭니다.

---

### 2. Tick별 상태 정보

매 tick 또는 상태 변경 시 전송합니다.

```json
{
  "treeId": "boss_ai_001",
  "tick": 15322,
  "nodeStates": [
    {
      "id": 1,
      "status": "RUNNING"
    },
    {
      "id": 2,
      "status": "SUCCESS"
    },
    {
      "id": 3,
      "status": "RUNNING"
    }
  ]
}
```

상태는 BehaviorTree.CPP 기준으로 보통 다음 정도면 충분합니다.

```text
IDLE
RUNNING
SUCCESS
FAILURE
SKIPPED
```

Unity 표시 예:

```text
IDLE     회색
RUNNING  노랑/파랑
SUCCESS  초록
FAILURE  빨강
SKIPPED  어두운 회색
```

---

## 가장 좋은 방식: “상태 변경 이벤트” 방식

매 tick마다 전체 트리 상태를 보내면 데이터가 커집니다. 개발 디버깅용이라도 트리가 커지면 부담됩니다.

그래서 추천은:

```text
초기 1회: 전체 Tree 구조 전송
이후: 상태가 바뀐 Node만 전송
```

예:

```json
{
  "treeId": "boss_ai_001",
  "tick": 15323,
  "changes": [
    {
      "id": 3,
      "from": "RUNNING",
      "to": "SUCCESS"
    },
    {
      "id": 4,
      "from": "IDLE",
      "to": "RUNNING"
    }
  ]
}
```

이 방식이 Unity에서 실시간 시각화하기 가장 좋습니다.

---

## BehaviorTree.CPP 쪽 구현 포인트

BehaviorTree.CPP는 노드 상태 변화를 관찰할 수 있는 방식이 있습니다. 서버 쪽에서 각 노드의 `NodeStatus` 변화를 기록해서 디버그 메시지로 만들면 됩니다.

개념적으로는 이런 형태입니다.

```cpp
tree.subscribeToStatusChange(
    [](BT::TreeNode& node,
       BT::NodeStatus previous,
       BT::NodeStatus current)
    {
        // node.UID(), node.name(), previous, current 수집
        // Debug Event Queue에 push
    }
);
```

그리고 별도 디버그 송신 루프에서 Unity로 전송합니다.

```cpp
while (debugEnabled)
{
    auto events = debugEventQueue.PopAll();
    SendToUnity(events);
}
```

중요한 점은 **BT tick 로직 안에서 바로 네트워크 송신하지 않는 것**입니다.

```text
BT Tick Thread
  → 상태 변화 이벤트를 lock-free queue 또는 mutex queue에 적재

Debug Network Thread
  → 큐에서 꺼내 Unity로 송신
```

이렇게 해야 AI 실행이 디버그 송신 때문에 느려지지 않습니다.

---

## Unity 쪽 구현 방법

Unity에서는 일반 게임 UI가 아니라 **EditorWindow 또는 Runtime Debug Overlay**로 만드는 것을 추천합니다.

개발 단계라면 우선순위는:

```text
1순위: Unity EditorWindow
2순위: Runtime IMGUI / UI Toolkit Overlay
3순위: 실제 인게임 디버그 패널
```

구성은 다음 정도면 충분합니다.

```text
BTDebugWindow
 ├─ TreeGraphView
 │   ├─ NodeBox
 │   ├─ Parent-Child Line
 │   └─ Current Status Color
 ├─ BlackboardView
 ├─ SelectedNodeDetailView
 └─ Tick Timeline / Pause / Step
```

Unity에서 노드 그래프는 다음 중 하나를 쓰면 됩니다.

| 방법                     | 추천도 | 설명               |
| ---------------------- | --: | ---------------- |
| UI Toolkit + GraphView |  높음 | 에디터용 디버거에 적합     |
| Runtime UI Toolkit     |  중간 | 런타임 표시 가능        |
| IMGUI                  |  중간 | 빠른 프로토타입용        |
| 직접 RectTransform 노드 배치 |  높음 | 런타임/에디터 모두 제어 쉬움 |
| 외부 Graph 라이브러리         |  중간 | 빠르지만 의존성 증가      |

개발 초기에는 **직접 RectTransform으로 트리 레이아웃을 그리는 방식**이 제일 단순합니다.

---

## 추가로 보내면 좋은 정보

트리 상태만으로는 디버깅이 부족할 수 있으므로 다음도 같이 보내는 게 좋습니다.

```json
{
  "treeId": "boss_ai_001",
  "blackboard": {
    "targetId": 10023,
    "distanceToTarget": 7.4,
    "hpRate": 0.35,
    "phase": 2
  }
}
```

추천 디버그 항목:

```text
현재 Tick 번호
현재 실행 중인 Node
최근 실패한 Condition
Blackboard 주요 값
Action 실행 시간
Node별 누적 실행 횟수
Node별 평균 실행 시간
마지막 Failure reason
```

특히 실전에서는 **Condition 실패 이유**가 중요합니다.

예:

```json
{
  "nodeId": 12,
  "status": "FAILURE",
  "reason": "distanceToTarget > attackRange"
}
```

---

## 네트워크 방식 추천

개발 디버그 용도라면 다음 순서로 추천합니다.

| 방식             | 추천도 | 이유                             |
| -------------- | --: | ------------------------------ |
| WebSocket      |  높음 | Unity/C++ 양쪽 구현 쉬움, 실시간 스트림 적합 |
| TCP Raw Socket |  높음 | 단순하고 빠름                        |
| gRPC Stream    |  중간 | 구조적이지만 세팅 무거움                  |
| UDP            |  낮음 | 상태 누락 가능, 디버깅용으로 불편            |
| HTTP Polling   |  낮음 | 실시간성이 떨어짐                      |

개발용이면 **WebSocket + JSON**으로 시작하는 게 가장 빠릅니다.
안정화되면 **Protobuf over WebSocket/TCP**로 바꾸면 됩니다.

---

## 결론

추천 설계는 이겁니다.

```text
BehaviorTree.CPP 서버
  1. 트리 구조를 JSON/Proto로 직렬화
  2. subscribeToStatusChange로 노드 상태 변화 수집
  3. Blackboard 주요 값도 주기적으로 전송
  4. WebSocket/TCP로 Unity에 스트리밍

Unity 클라이언트
  1. 트리 구조를 받아 노드 그래프 생성
  2. 상태 변경 이벤트를 받아 색상 갱신
  3. 현재 실행 경로, 실패 노드, Blackboard 값 표시
```

가장 중요한 원칙은:

```text
게임 로직용 BT 실행과 디버그 전송을 분리한다.
전체 스냅샷보다 상태 변경 이벤트 중심으로 보낸다.
Unity는 서버의 BT를 실행하지 않고, 시각화만 한다.
```


기존 패킷 송수신이 이미 있다면, 핵심은 **BT 실행 상태를 외부에서 억지로 읽는 구조가 아니라, BT tick 과정에서 “디버그 동기화용 상태 모델”을 같이 갱신하는 구조**로 만드는 것입니다.

## 추천 디자인

현재 코드처럼 각 노드가 `Monster* monster_`를 들고 있고, `tick()` 안에서 `monster_->SetState()`, `Attack()`, `patrol()`, `setMoveTarget()` 같은 실제 게임 로직을 실행하는 구조입니다. 예를 들어 `ConditionDetectEnemy`는 적 탐지 후 `target_agent_id_`와 AI 상태를 갱신하고, `ActionAttack`은 `AIState_Attack`으로 바꾼 뒤 공격을 실행합니다. 

따라서 디버그 동기화는 다음처럼 분리하는 게 좋습니다.

```text
Monster BT 실행
  ├─ 실제 게임 상태 변경
  │   ├─ SetState()
  │   ├─ Attack()
  │   ├─ patrol()
  │   └─ setMoveTarget()
  │
  └─ Debug Mirror 상태 기록
      ├─ 현재 tick 번호
      ├─ 실행된 node id
      ├─ node status
      ├─ monster ai state
      ├─ target_agent_id
      ├─ position / destination
      └─ 실패 reason
```

즉, Unity로 보내는 값은 **BT 자체의 원본 상태**가 아니라, 서버가 만든 **BT Debug Mirror**입니다.

---

## 1. Monster 단위로 DebugContext를 둔다

`Monster`마다 디버그용 상태를 하나 붙이는 방식이 가장 단순합니다.

```cpp
struct BTNodeDebugState
{
    uint16_t node_id;
    std::string node_name;
    BT::NodeStatus status;
    uint64_t last_tick;
    uint32_t enter_count;
    uint32_t success_count;
    uint32_t failure_count;
    uint32_t running_count;
    std::string reason;
};

struct MonsterBTDebugContext
{
    int64_t monster_id;
    uint64_t bt_tick = 0;

    syncnet::AIState ai_state;
    int64_t target_agent_id = -1;

    std::unordered_map<uint16_t, BTNodeDebugState> nodes;

    std::vector<uint16_t> executed_nodes_this_tick;
    bool dirty = false;
};
```

그리고 `Monster`에 붙입니다.

```cpp
class Monster
{
public:
    MonsterBTDebugContext* bt_debug() { return &bt_debug_; }

private:
    MonsterBTDebugContext bt_debug_;
};
```

---

## 2. 노드 tick 안에서 DebugContext를 갱신한다

현재 노드들은 전부 `Monster* monster_`를 가지고 있으므로, 가장 현실적인 방식은 각 노드의 `tick()`에서 디버그 상태를 기록하는 것입니다.

예를 들어 `ConditionDetectEnemy`는 이렇게 바꿀 수 있습니다.

```cpp
BT::NodeStatus tick() override
{
    auto* debug = monster_->bt_debug();

    debug->executed_nodes_this_tick.push_back(NodeId::ConditionDetectEnemy);

    monster_->target_agent_id_ = monster_->map()->DetectEnemy(monster_);

    BT::NodeStatus result;

    if (monster_->target_agent_id_ >= 0)
    {
        monster_->SetState(syncnet::AIState_Detect);
        result = BT::NodeStatus::SUCCESS;

        debug->nodes[NodeId::ConditionDetectEnemy].reason =
            "enemy detected";
    }
    else
    {
        monster_->SetState(syncnet::AIState_Patrol);
        result = BT::NodeStatus::FAILURE;

        debug->nodes[NodeId::ConditionDetectEnemy].reason =
            "enemy not found";
    }

    debug->target_agent_id = monster_->target_agent_id_;
    debug->ai_state = monster_->state();
    debug->nodes[NodeId::ConditionDetectEnemy].status = result;
    debug->nodes[NodeId::ConditionDetectEnemy].last_tick = debug->bt_tick;
    debug->dirty = true;

    return result;
}
```

이 방식의 장점은 **실제 로직 결과와 디버그 상태가 어긋날 가능성이 낮다**는 점입니다.

---

## 3. 반복 코드를 줄이려면 헬퍼를 둔다

각 노드마다 위 코드를 다 쓰면 지저분해지므로, 헬퍼를 두는 게 좋습니다.

```cpp
class BTDebugRecorder
{
public:
    static void Record(
        Monster* monster,
        uint16_t node_id,
        std::string_view node_name,
        BT::NodeStatus status,
        std::string_view reason = "")
    {
        auto* debug = monster->bt_debug();

        auto& node = debug->nodes[node_id];
        node.node_id = node_id;
        node.node_name = std::string(node_name);
        node.status = status;
        node.last_tick = debug->bt_tick;
        node.reason = std::string(reason);

        switch (status)
        {
        case BT::NodeStatus::SUCCESS:
            node.success_count++;
            break;
        case BT::NodeStatus::FAILURE:
            node.failure_count++;
            break;
        case BT::NodeStatus::RUNNING:
            node.running_count++;
            break;
        default:
            break;
        }

        debug->executed_nodes_this_tick.push_back(node_id);
        debug->ai_state = monster->state();
        debug->target_agent_id = monster->target_agent_id_;
        debug->dirty = true;
    }
};
```

그러면 노드는 이렇게 단순해집니다.

```cpp
BT::NodeStatus tick() override
{
    monster_->target_agent_id_ = monster_->map()->DetectEnemy(monster_);

    if (monster_->target_agent_id_ >= 0)
    {
        monster_->SetState(syncnet::AIState_Detect);

        BTDebugRecorder::Record(
            monster_,
            NodeId::ConditionDetectEnemy,
            "ConditionDetectEnemy",
            BT::NodeStatus::SUCCESS,
            "enemy detected");

        return BT::NodeStatus::SUCCESS;
    }

    monster_->SetState(syncnet::AIState_Patrol);

    BTDebugRecorder::Record(
        monster_,
        NodeId::ConditionDetectEnemy,
        "ConditionDetectEnemy",
        BT::NodeStatus::FAILURE,
        "enemy not found");

    return BT::NodeStatus::FAILURE;
}
```

---

## 4. 노드 ID는 직접 고정하는 것이 좋다

Unity에서 트리를 시각화하려면 노드 식별자가 안정적이어야 합니다.

현재 XML에서 `ConditionDetectEnemy`, `ActionPatrol`, `ActionChase`, `ConditionAttackRange`, `ActionAttack`, `ConditionCheckHealth`, `ActionDead`, `ActionDestroyed` 같은 노드를 등록하고 있습니다. 

이 노드들에 고정 ID를 부여하는 게 좋습니다.

```cpp
namespace NodeId
{
    constexpr uint16_t ConditionDetectEnemy = 1;
    constexpr uint16_t ActionPatrol = 2;
    constexpr uint16_t ActionChase = 3;
    constexpr uint16_t ConditionAttackRange = 4;
    constexpr uint16_t ActionAttack = 5;
    constexpr uint16_t ConditionCheckHealth = 6;
    constexpr uint16_t ActionDead = 7;
    constexpr uint16_t ActionDestroyed = 8;
}
```

BehaviorTree.CPP 내부 UID에 의존해도 되지만, 개발 중 XML이 바뀌거나 트리 생성 방식이 바뀌면 Unity 쪽 매핑이 흔들릴 수 있습니다.
디버그 뷰어 목적이면 **서버가 정의한 고정 NodeId**가 더 안전합니다.

---

## 5. Tick 시작/종료 지점을 명확히 잡는다

동기화는 각 노드가 아니라 **BT tick 단위 프레임**으로 묶어야 합니다.

```cpp
void Monster::TickAI()
{
    auto* debug = bt_debug();

    debug->bt_tick++;
    debug->executed_nodes_this_tick.clear();
    debug->dirty = false;

    tree_->tickOnce();

    if (debug->dirty)
    {
        BuildAndSendBTDebugFrame(*debug);
    }
}
```

Unity에서는 이 단위로 보면 됩니다.

```text
BT Tick 1001
  실행된 노드:
    ConditionCheckHealth: SUCCESS
    ConditionDetectEnemy: SUCCESS
    ConditionAttackRange: FAILURE
    ActionChase: SUCCESS

  Monster 상태:
    AIState_Detect
    target_agent_id = 1234
```

이렇게 해야 Unity에서 “이번 tick에 실제로 어떤 경로를 탔는지”를 정확하게 보여줄 수 있습니다.

---

## 6. 전체 상태가 아니라 변경분 중심으로 보낸다

디버그 동기화는 두 종류로 나누는 게 좋습니다.

```text
TreeDefinition
  - 트리 구조
  - node_id
  - node_name
  - parent_id
  - node_type

TreeRuntimeFrame
  - monster_id
  - bt_tick
  - 이번 tick에 실행된 node list
  - 변경된 node status
  - monster ai state
  - target_agent_id
  - reason
```

서버 내부 디자인은 이렇게 잡으면 됩니다.

```cpp
struct BTDebugRuntimeFrame
{
    int64_t monster_id;
    uint64_t tick;

    syncnet::AIState ai_state;
    int64_t target_agent_id;

    std::vector<BTNodeDebugState> changed_nodes;
    std::vector<uint16_t> executed_path;
};
```

Unity는 `executed_path`를 받아서 이번 tick 실행 경로를 하이라이트하고, `changed_nodes`로 색상을 갱신하면 됩니다.

---

## 7. Condition 실패 이유를 반드시 넣는 것이 좋다

BT 디버깅에서 제일 중요한 건 “왜 이 경로로 가지 않았는가”입니다.

현재 샘플 기준으로는 이런 reason을 넣으면 유용합니다.

```text
ConditionCheckHealth
  SUCCESS: health > 0
  FAILURE: health <= 0

ConditionDetectEnemy
  SUCCESS: enemy detected
  FAILURE: enemy not found

ConditionAttackRange
  SUCCESS: target in attack range
  FAILURE: target out of range

ActionPatrol
  SUCCESS: patrol command issued

ActionChase
  SUCCESS: chase target position updated

ActionAttack
  SUCCESS: attack command issued
```

예를 들어 `ConditionAttackRange`는 현재 `monster_->AttackRange() >= 0`이면 성공하는 구조입니다. 
이 경우 디버그 reason을 이렇게 넣으면 됩니다.

```cpp
BT::NodeStatus tick() override
{
    int range_result = monster_->AttackRange();

    if (range_result >= 0)
    {
        BTDebugRecorder::Record(
            monster_,
            NodeId::ConditionAttackRange,
            "ConditionAttackRange",
            BT::NodeStatus::SUCCESS,
            "target in attack range");

        return BT::NodeStatus::SUCCESS;
    }

    BTDebugRecorder::Record(
        monster_,
        NodeId::ConditionAttackRange,
        "ConditionAttackRange",
        BT::NodeStatus::FAILURE,
        "target out of range");

    return BT::NodeStatus::FAILURE;
}
```

---

## 8. Monster 상태와 BT 상태를 분리해서 보여줘야 한다

현재 노드에서 `monster_->SetState(syncnet::AIState_Detect)`, `AIState_Attack`, `AIState_Dead`, `AIState_Destroyed` 같은 게임 AI 상태를 직접 변경하고 있습니다.  

이때 Unity에서는 두 상태를 구분해야 합니다.

```text
BT NodeStatus
  - SUCCESS
  - FAILURE
  - RUNNING
  - IDLE

Monster AIState
  - Patrol
  - Detect
  - Attack
  - Dead
  - Destroyed
```

즉, 노드 색상은 `NodeStatus`로 표시하고, 몬스터 머리 위나 디테일 패널에는 `AIState`를 표시하는 방식이 좋습니다.

```text
[ConditionDetectEnemy] SUCCESS
[ConditionAttackRange] FAILURE
[ActionChase] SUCCESS

Monster State: Detect
Target: 1234
```

---

## 9. 코드 침투를 줄이는 대안: Decorator 방식

노드마다 `BTDebugRecorder::Record()`를 넣기 싫다면, XML에서 디버그 데코레이터를 감싸는 방식도 가능합니다.

개념은 이렇습니다.

```xml
<Debug node_id="1" name="ConditionDetectEnemy">
    <ConditionDetectEnemy/>
</Debug>
```

`DebugDecorator`가 자식 노드 tick 전후 상태를 기록합니다.

장점:

```text
노드 코드 수정이 적다
공통적으로 SUCCESS/FAILURE/RUNNING 기록 가능
```

단점:

```text
Condition 실패 reason 같은 도메인 정보는 알기 어렵다
target_agent_id, attack range 같은 상세 값은 별도 기록 필요
XML이 지저분해질 수 있다
```

그래서 추천은 혼합 방식입니다.

```text
공통 NodeStatus 기록:
  DebugDecorator 또는 subscribeToStatusChange

도메인 디버그 정보:
  각 노드 tick 안에서 BTDebugRecorder로 기록
```

---

## 결론

현재 코드 구조에서는 이 디자인이 가장 적합합니다.

```text
1. Monster마다 MonsterBTDebugContext를 둔다.
2. BT tick 시작 시 debug frame을 초기화한다.
3. 각 노드 tick에서 실제 게임 로직 수행 후 BTDebugRecorder에 결과를 기록한다.
4. BT tick 종료 시 dirty 상태이면 RuntimeFrame을 만들어 Unity로 보낸다.
5. Unity는 서버 BT를 재실행하지 않고, RuntimeFrame만 반영한다.
```

정리하면 핵심은 이것입니다.

```text
BehaviorTree.CPP의 실행 흐름을 Unity와 직접 동기화하려 하지 말고,
서버 tick마다 "실행 결과 프레임"을 만들어 Unity가 따라 그리게 한다.
```

이렇게 하면 서버의 실제 AI 실행과 Unity 디버그 표시가 가장 덜 어긋납니다.

---

아니요. 앞선 설계는 **디버그 기능이 항상 코드에 존재한다는 전제**가 조금 섞여 있었습니다.
개발 단계 전용이라면 구조를 더 명확히 분리하는 게 맞습니다.

## 권장 설계

```text
Release Build
  BehaviorTree.CPP
  Monster AI Logic
  Packet Sync
  Debug 코드 없음

Dev / Debug Build
  BehaviorTree.CPP
  Monster AI Logic
  Packet Sync
  BT Debug Instrumentation
  BT Debug Frame Builder
  Unity BT Viewer
```

핵심은 **릴리즈 빌드에서 디버그 코드가 컴파일되지 않게 하는 것**입니다.

---

## 1. 컴파일 플래그로 완전 분리

예를 들어:

```cpp
#if defined(ENABLE_BT_DEBUG)
    BTDebugRecorder::Record(...);
#endif
```

또는 매크로로 감싸는 것이 좋습니다.

```cpp
#if defined(ENABLE_BT_DEBUG)
#define BT_DEBUG_RECORD(monster, node_id, node_name, status, reason) \
    BTDebugRecorder::Record(monster, node_id, node_name, status, reason)
#else
#define BT_DEBUG_RECORD(monster, node_id, node_name, status, reason) \
    do {} while (0)
#endif
```

노드에서는 이렇게 씁니다.

```cpp
BT_DEBUG_RECORD(
    monster_,
    NodeId::ConditionDetectEnemy,
    "ConditionDetectEnemy",
    BT::NodeStatus::SUCCESS,
    "enemy detected");
```

릴리즈 빌드에서는 이 코드가 사실상 사라집니다.

---

## 2. Monster에 DebugContext를 직접 넣지 않는 편이 좋다

릴리즈 빌드 제외가 목적이면 이전처럼 `Monster`에 항상 `MonsterBTDebugContext`를 두는 건 별로입니다.

대신 이렇게 분리합니다.

```cpp
class Monster
{
public:
    // 실제 게임 상태만 유지
    int64_t target_agent_id_;
    syncnet::AIState state_;
};
```

디버그 컨텍스트는 별도 매니저가 관리합니다.

```cpp
#if defined(ENABLE_BT_DEBUG)

class BTDebugManager
{
public:
    MonsterBTDebugContext& GetOrCreate(Monster* monster);
    void BeginTick(Monster* monster);
    void Record(...);
    void EndTick(Monster* monster);
};

#endif
```

이렇게 하면 릴리즈 빌드의 `Monster` 메모리 레이아웃과 런타임 비용을 건드리지 않습니다.

---

## 3. 노드 코드는 최소 침투 방식으로 유지

예를 들어 `ConditionDetectEnemy`는 이렇게 됩니다.

```cpp
BT::NodeStatus tick() override
{
    monster_->target_agent_id_ = monster_->map()->DetectEnemy(monster_);

    if (monster_->target_agent_id_ >= 0)
    {
        monster_->SetState(syncnet::AIState_Detect);

        BT_DEBUG_RECORD(
            monster_,
            NodeId::ConditionDetectEnemy,
            "ConditionDetectEnemy",
            BT::NodeStatus::SUCCESS,
            "enemy detected");

        return BT::NodeStatus::SUCCESS;
    }

    monster_->SetState(syncnet::AIState_Patrol);

    BT_DEBUG_RECORD(
        monster_,
        NodeId::ConditionDetectEnemy,
        "ConditionDetectEnemy",
        BT::NodeStatus::FAILURE,
        "enemy not found");

    return BT::NodeStatus::FAILURE;
}
```

릴리즈 빌드에서는 `BT_DEBUG_RECORD(...)`가 빈 코드가 되므로 실제 동작에는 영향이 없습니다.

---

## 4. Tick 시작/종료도 매크로로 감싼다

```cpp
void Monster::TickAI()
{
    BT_DEBUG_BEGIN_TICK(this);

    tree_->tickOnce();

    BT_DEBUG_END_TICK(this);
}
```

매크로 정의:

```cpp
#if defined(ENABLE_BT_DEBUG)

#define BT_DEBUG_BEGIN_TICK(monster) \
    BTDebugManager::Instance().BeginTick(monster)

#define BT_DEBUG_END_TICK(monster) \
    BTDebugManager::Instance().EndTick(monster)

#else

#define BT_DEBUG_BEGIN_TICK(monster) \
    do {} while (0)

#define BT_DEBUG_END_TICK(monster) \
    do {} while (0)

#endif
```

---

## 5. 디버그 전용 파일을 분리한다

권장 파일 구조는 이렇습니다.

```text
AI/
 ├─ MonsterBT.cpp
 ├─ MonsterBTNodes.cpp
 └─ MonsterBTNodes.h

AI/Debug/
 ├─ BTDebugManager.h
 ├─ BTDebugManager.cpp
 ├─ BTDebugRecorder.h
 ├─ BTDebugFrameBuilder.cpp
 └─ BTDebugNodeIds.h
```

빌드 설정에서:

```text
Release:
  AI/Debug/* 제외
  ENABLE_BT_DEBUG 미정의

Dev:
  AI/Debug/* 포함
  ENABLE_BT_DEBUG 정의
```

---

## 6. 권장 최종 구조

```text
BT 노드 코드
  - 실제 게임 로직은 항상 존재
  - 디버그 기록은 BT_DEBUG_RECORD 매크로로만 호출

BTDebugManager
  - ENABLE_BT_DEBUG일 때만 컴파일
  - Monster별 디버그 상태 관리
  - Tick 단위 프레임 생성

BTDebugFrameSender
  - ENABLE_BT_DEBUG일 때만 컴파일
  - Unity 디버그 클라이언트로 전송

Release Build
  - BT_DEBUG_* 매크로가 모두 no-op
  - DebugManager 없음
  - DebugFrame 없음
  - Debug 송신 없음
```

---

## 결론

개발 단계 전용이라는 조건을 반영하면, 설계 원칙은 이겁니다.

```text
1. 릴리즈 빌드에는 디버그 상태 객체를 넣지 않는다.
2. Monster 클래스에 디버그 필드를 상시 추가하지 않는다.
3. 디버그 기록은 매크로 또는 컴파일 플래그로 제거 가능하게 한다.
4. BT 실행 로직과 디버그 프레임 생성 로직은 파일/모듈 단위로 분리한다.
5. Unity 시각화용 동기화는 Dev 빌드에서만 활성화한다.
```

즉, **BT 실행 결과를 관찰하는 개발 전용 Instrumentation Layer**로 설계하는 것이 맞습니다.
---

## 2026-05-04 구현 기록: BT Debug 동기화 프레임 시스템

이번 구현은 위 설계 중 "BT tick 결과를 서버에서 Debug Mirror/Frame으로 만든 뒤 별도 전송 루프가 가져간다"는 부분을 코드에 반영했다.

### 추가 파일

```text
Game/BTDebugNodeIds.h
  - Monster BT 노드별 고정 debug node id 정의

Game/BTDebugManager.h
  - ENABLE_BT_DEBUG 게이트
  - BT_DEBUG_BEGIN_TICK / BT_DEBUG_RECORD / BT_DEBUG_END_TICK 매크로
  - ConsumeFrames()로 외부 sender가 JSON frame을 가져갈 수 있는 인터페이스

Game/BTDebugManager.cpp
  - Monster별 tick context 관리
  - 실행 경로(executedPath), 변경 노드(changes), AIState, targetActorId를 JSON RuntimeFrame으로 생성
  - TreeDefinition JSON 생성 함수 제공
```

### 동작 흐름

```text
Monster::update()
  -> BT_DEBUG_BEGIN_TICK(this)
  -> tree_->tickOnce()
  -> BT_DEBUG_END_TICK(this)

MonsterBT 노드 tick()
  -> 실제 게임 로직 실행
  -> BT_DEBUG_RECORD(monster, node_id, node_name, status, reason)

BTDebugManager
  -> 상태가 바뀐 노드만 changes에 적재
  -> 이번 tick에 실행된 노드 id는 executedPath에 적재
  -> dirty tick만 JSON frame으로 만들어 queue에 push

BTDebug sender thread 또는 WebSocket/TCP layer
  -> BTDebugManager::Instance().ConsumeFrames()
  -> Unity BT Debug Viewer로 전송
```

### RuntimeFrame 예시

```json
{
  "type": "TreeRuntimeFrame",
  "treeId": "monster_ai",
  "monsterId": 12,
  "tick": 531,
  "aiState": "Detect",
  "targetActorId": 3,
  "executedPath": [6, 1, 4, 3],
  "changes": [
    {
      "id": 4,
      "name": "ConditionAttackRange",
      "status": "FAILURE",
      "reason": "target out of range",
      "successCount": 2,
      "failureCount": 8,
      "runningCount": 0
    }
  ]
}
```

### 현재 기록되는 reason

```text
ConditionCheckHealth
  SUCCESS: health > 0
  FAILURE: health <= 0

ConditionDetectEnemy
  SUCCESS: enemy detected
  FAILURE: enemy not found

ConditionAttackRange
  SUCCESS: target in attack range
  FAILURE: target out of range

ActionPatrol
  SUCCESS: patrol command issued

ActionChase
  SUCCESS: chase target position updated

ActionAttack
  SUCCESS: attack command issued

ActionDead
  SUCCESS: dead state applied

ActionDestroyed
  SUCCESS: destroyed state applied
```

### 빌드/릴리즈 정책

```text
Debug build
  - NDEBUG가 없으면 ENABLE_BT_DEBUG를 자동 활성화
  - BTDebugManager가 tick frame을 생성

Release build
  - ENABLE_BT_DEBUG를 별도로 정의하지 않으면 BT_DEBUG_* 매크로는 no-op
  - Monster 클래스에 debug context 필드를 추가하지 않았으므로 릴리즈 객체 레이아웃 영향 없음
```

### 다음 연결 지점

```text
1. WebSocket 또는 TCP debug sender를 만든다.
2. sender thread는 BT tick thread와 분리한다.
3. sender는 ConsumeFrames()로 frame을 가져와 Unity에 전송한다.
4. Unity는 TreeDefinition을 먼저 받고 RuntimeFrame의 executedPath/changes를 적용한다.
5. flatbuffer 프로토콜에 실을 경우 syncnet.fbs에 BTDebugFrame 계열 table을 추가하고 syncnet_generated.h를 재생성한다.
```
