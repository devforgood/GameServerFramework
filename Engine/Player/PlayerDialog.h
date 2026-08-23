#pragma once
#include "Component.h"
#include "DialogAction.h"
#include <cstdint>

namespace gamedata
{
	struct Dialog;
	struct DialogChoice;
}

class PlayerQuest;

// 플레이어가 지금 보고 있는 대화.
//
// 대화는 **서버가 상태를 들고 있다**. 클라가 "3번 선택지"만 보내면 그것이 어느 노드의
// 3번인지는 서버가 안다. 클라가 노드 id 까지 보내게 해서 서로 아는 것이 같은지 확인하고,
// 다르면(창을 두 번 눌렀거나 오래된 화면이면) 거절한다 — 그러지 않으면 지난 화면의
// 선택지로 지금 노드의 동작이 실행된다.
//
// 대화 자체는 데이터(dialog.json)일 뿐이고, 선택의 결과는 다른 컴포넌트가 맡는다
// (퀘스트 수락/완료는 PlayerQuest). 여기서는 "지금 어느 노드인가"와 그 전이만 다룬다.
//
// 선택지에는 조건(show_if)이 걸릴 수 있어서 **노드의 선택지 전부가 나가지는 않는다**.
// 무엇이 나갔는지는 노드가 현재 노드가 되는 순간 한 번 정해 두고(visibleMask_) 그대로
// 들고 있는다. 클라가 보낸 번호는 "그때 내보낸 목록"에서의 번호이므로, 그 사이 퀘스트
// 상태가 바뀌어도 플레이어가 실제로 누른 선택지가 실행된다 — 매번 다시 계산하면 화면은
// 그대로인데 번호의 뜻만 조용히 바뀐다.
class PlayerDialog : public ComponentBase<PlayerDialog>
{
public:
	// NPC 와의 대화를 연다. 그 NPC 에 대화가 없으면 false(그냥 상호작용으로 끝난다).
	// 이미 다른 대화가 열려 있으면 새 대화로 갈아탄다 — 다른 NPC 를 눌렀다는 뜻이다.
	bool Open(int npc_id);

	// 선택지를 고른다. node_id 는 클라가 보고 있던 노드,
	// choice_index 는 **내보낸 목록**에서의 번호다(조건에 걸려 빠진 선택지는 세지 않는다).
	// out_next_node 에 이어서 보여줄 노드를 채운다(끝났으면 nullptr).
	DialogResult Select(int node_id, int choice_index, const gamedata::Dialog** out_next_node = nullptr);

	// 대화를 닫는다(창을 닫았거나, 멀어졌거나, 맵을 옮겼을 때).
	void Close();

	// ---- 조회
	bool IsOpen() const { return currentNodeId_ != 0; }
	int GetCurrentNodeId() const { return currentNodeId_; }
	int GetNpcId() const { return npcId_; }

	// 지금 열려 있는 노드 데이터. 닫혀 있으면 nullptr.
	const gamedata::Dialog* GetCurrentNode() const;

	// 원본 번호(node->choices 의 인덱스) 기준으로, 이 선택지가 내보낸 목록에 들어 있는가.
	// 노드를 직렬화하는 쪽이 이것으로 거른다.
	bool IsChoiceVisible(int choice_index) const;

	// 내보낸 목록에서의 번호 -> node->choices 의 원본 번호. 범위를 벗어나면 -1.
	int ResolveVisibleChoice(int visible_index) const;

private:
	// 선택지의 동작을 수행한다. 대화를 이어갈 다음 노드 id 를 반환하며, 0 이면 종료다.
	// 동작이 실패하면 out_failed 를 세운다(선택 자체는 유효했다는 뜻으로 구분한다).
	int applyAction(const gamedata::DialogChoice& choice, bool& out_failed);

	// 현재 노드로 옮겨 갈 때 한 번만 부른다. 지금 상태로 조건을 판정해 visibleMask_ 를 세운다.
	void refreshVisibleChoices();

	int npcId_ = 0;

	// 0 이면 대화 중이 아니다. 노드 id 는 dialog.json 전역에서 유일하다.
	int currentNodeId_ = 0;

	// 내보낸 목록에 들어 있는 선택지(원본 번호 기준 비트). 플레이어마다 하나씩 붙는
	// 컴포넌트라 벡터 대신 비트로 들고 있는다 — 노드당 선택지는 몇 개뿐이다.
	uint32_t visibleMask_ = 0;

	// visibleMask_ 가 담을 수 있는 선택지 수. 데이터 검증이 같은 값으로 막는다.
	static constexpr int kMaxChoices = 32;
};
