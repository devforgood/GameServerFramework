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
