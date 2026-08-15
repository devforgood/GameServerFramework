#pragma once
#include "Component.h"
#include "DialogAction.h"

namespace gamedata
{
	struct Dialog;
	struct DialogChoice;
}

// 플레이어가 지금 보고 있는 대화.
//
// 대화는 **서버가 상태를 들고 있다**. 클라가 "3번 선택지"만 보내면 그것이 어느 노드의
// 3번인지는 서버가 안다. 클라가 노드 id 까지 보내게 해서 서로 아는 것이 같은지 확인하고,
// 다르면(창을 두 번 눌렀거나 오래된 화면이면) 거절한다 — 그러지 않으면 지난 화면의
// 선택지로 지금 노드의 동작이 실행된다.
//
// 대화 자체는 데이터(dialog.json)일 뿐이고, 선택의 결과는 다른 컴포넌트가 맡는다
// (퀘스트 수락/완료는 PlayerQuest). 여기서는 "지금 어느 노드인가"와 그 전이만 다룬다.
class PlayerDialog : public ComponentBase<PlayerDialog>
{
public:
	// NPC 와의 대화를 연다. 그 NPC 에 대화가 없으면 false(그냥 상호작용으로 끝난다).
	// 이미 다른 대화가 열려 있으면 새 대화로 갈아탄다 — 다른 NPC 를 눌렀다는 뜻이다.
	bool Open(int npc_id);

	// 선택지를 고른다. node_id 는 클라가 보고 있던 노드다.
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

private:
	// 선택지의 동작을 수행한다. 대화를 이어갈 다음 노드 id 를 반환하며, 0 이면 종료다.
	// 동작이 실패하면 out_failed 를 세운다(선택 자체는 유효했다는 뜻으로 구분한다).
	int applyAction(const gamedata::DialogChoice& choice, bool& out_failed);

	int npcId_ = 0;

	// 0 이면 대화 중이 아니다. 노드 id 는 dialog.json 전역에서 유일하다.
	int currentNodeId_ = 0;
};
