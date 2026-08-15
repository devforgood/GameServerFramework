# 대화(Dialog)

NPC 와의 대화와 선택지 분기. 퀘스트를 주고받는 표현 계층이며, 대화 자체는 아무 상태도
저장하지 않는다 — 창을 닫으면 사라진다.

## 구성

| 파일 | 역할 |
| --- | --- |
| `dialog.json` | 대화 노드와 선택지. 노드 id 는 전역 유일 |
| `DialogAction.h` | 선택지가 하는 일(`close`/`goto`/`accept_quest`/`complete_quest`)과 처리 결과 |
| `Dialog.h` | 정의 래퍼(생성물) |
| `../Player/PlayerDialog.h/.cpp` | 플레이어가 지금 보고 있는 노드와 그 전이 |

## 데이터

```jsonc
{
  "id": 3001,              // 전역 유일 노드 id
  "npc_id": 2001,          // 이 노드가 속한 NPC
  "text_id": "dlg_...",    // NPC 가 하는 말(로컬라이즈 키)
  "choices": [
    { "text_id": "dlg_...", "action": "goto",         "param": 0,    "next_id": 3002 },
    { "text_id": "dlg_...", "action": "accept_quest", "param": 1001, "next_id": 0 },
    { "text_id": "dlg_...", "action": "close",        "param": 0,    "next_id": 0 }
  ]
}
```

NPC 의 **시작 노드**는 `npc.json` 의 `dialog_id` 가 가리킨다. 0 이면 대화가 없는 NPC 다
(호위 대상처럼 상호작용만 하는 NPC).

| action | 하는 일 | 쓰는 필드 |
| --- | --- | --- |
| `close` | 대화를 끝낸다 | — |
| `goto` | `next_id` 노드로 넘어간다 | `next_id` |
| `accept_quest` | `param` 퀘스트를 수락한다 | `param`, (`next_id`) |
| `complete_quest` | `param` 퀘스트를 완료 접수한다 | `param`, (`next_id`) |

**대화로 받는다고 조건이 면제되지 않는다.** `accept_quest` 는 평소와 같은
`Quest::CanAccept` 를 타고, `complete_quest` 는 완료 대기(ReadyToComplete)가 아니면 실패한다.

## 상태는 서버가 들고 있다

클라는 받은 노드를 그리고 고른 번호를 되돌려 보낼 뿐이다. "3번 선택지"가 어느 노드의
3번인지는 서버가 안다.

그래도 클라가 **보고 있던 노드 id 를 함께 보내고**, 서버가 아는 것과 다르면 거절한다
(`DialogResult::StaleNode`). 그러지 않으면 창을 두 번 눌렀을 때 지난 화면의 번호로 지금
노드의 동작이 실행된다 — "되돌아가기"를 눌렀는데 퀘스트가 수락되는 식이다.

동작이 실패하면(레벨 미달 등) **대화는 그 자리에 남는다**. 창을 닫아 버리면 왜 안 됐는지
보여줄 자리가 사라진다.

## 네트워크 프로토콜

| 메시지 | 방향 | 내용 |
| --- | --- | --- |
| `Interact` | C→S | 대화를 여는 입구. 별도의 여는 메시지가 없다 |
| `DialogNode` | S→C | 지금 노드의 텍스트와 선택지. `nodeId == 0` 이면 대화가 끝났다 |
| `DialogSelect` | C→S | 보고 있던 `nodeId` + 고른 `choiceIndex`. 음수면 "창 닫기" |

대화를 여는 메시지를 따로 두지 않는 이유는 **NPC 를 누르는 동작 하나로 족하기** 때문이다.
`Interact` 는 원래 거리와 맵을 검증하고 퀘스트의 `talk` 목표를 진행시키는데, 대화는 그 위에
얹힌다 — 상호작용 응답을 먼저 보내고 이어서 `DialogNode` 를 보낸다(클라가 상호작용 성공을
확인한 뒤 창을 연다).

## 데이터 검증

`validate_data.py` 가 생성 단계에서 막는 것들:

- `npc_id` 가 Npc 테이블에 있는가
- `goto` 의 `next_id` 에 해당하는 노드가 있는가 (끊어진 참조는 런타임에 "대화가 그냥
  닫힌다"로만 나타나서 단서가 남지 않는다)
- `close` 에 `next_id` 를 적지 않았는가
- `accept_quest`/`complete_quest` 의 `param` 이 Quest 테이블에 있는가
- 선택지가 비어 있지 않은가 (없으면 플레이어가 대화를 닫을 방법이 없다)
- NPC 의 `dialog_id` 가 실제 노드를 가리키는가

## 아직 없는 것

- **조건부 선택지** — 퀘스트 진행 상태나 레벨에 따라 선택지를 보이거나 숨기는 규칙이 없다.
  지금은 모든 선택지가 항상 보이고, 안 되는 것은 눌렀을 때 실패한다
- **상점/수리 같은 기능 연결** — action 이 퀘스트 두 가지뿐이다
- **대화 이력/플래그** — "이 NPC 와 처음 대화하는가" 같은 분기를 위한 저장이 없다
- **클라이언트 대화 UI** (프로토콜과 생성 코드는 준비돼 있다)
