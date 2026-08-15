#pragma once
#include <string_view>

// 선택지를 골랐을 때 서버가 하는 일.
// 데이터(dialog.json 의 choices[].action)와 1:1로 대응하며, 값을 늘릴 때는
// GameDataFlow/validate_data.py 의 DIALOG_ACTIONS 도 같이 늘려야 디자이너의 오타를
// 코드 생성 단계에서 막을 수 있다.
enum class DialogActionType
{
	None = 0,

	// 대화를 끝낸다. next_id 를 쓰지 않는다.
	Close,

	// next_id 노드로 넘어간다. 대화 분기의 기본형이다.
	Goto,

	// param 퀘스트를 수락한다. 조건은 평소와 같이 검사한다 — 대화로 받는다고
	// 레벨이나 선행 조건이 면제되지는 않는다.
	AcceptQuest,

	// param 퀘스트를 완료 접수한다. 완료 대기(ReadyToComplete)가 아니면 실패한다.
	CompleteQuest,
};

inline DialogActionType ParseDialogAction(std::string_view action)
{
	if (action == "close")          return DialogActionType::Close;
	if (action == "goto")           return DialogActionType::Goto;
	if (action == "accept_quest")   return DialogActionType::AcceptQuest;
	if (action == "complete_quest") return DialogActionType::CompleteQuest;
	return DialogActionType::None;
}

// 선택지를 처리한 결과. 클라는 이 값으로 창을 닫을지 다음 노드를 그릴지 정한다.
enum class DialogResult
{
	Ok,             // 처리했고 대화가 이어진다(다음 노드가 있다)
	Closed,         // 대화가 끝났다
	NoDialog,       // 대화 중이 아니다
	StaleNode,      // 클라가 보낸 노드가 지금 열려 있는 노드가 아니다(중복 클릭 등)
	InvalidChoice,  // 선택지 번호가 범위를 벗어났다
	ActionFailed,   // 선택은 유효했지만 그 동작이 실패했다(퀘스트 조건 미달 등)
};
