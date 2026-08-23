#pragma once
#include <string_view>

// 선택지를 보여 줄 조건.
// 데이터(dialog.json 의 choices[].show_if.state)와 1:1로 대응하며, 값을 늘릴 때는
// GameDataFlow/validate_data.py 의 DIALOG_CONDITION_STATES 도 같이 늘려야 디자이너가
// 적은 오타를 코드 생성 단계에서 막을 수 있다.
//
// 조건은 전부 "퀘스트 하나가 어떤 상태인가"를 묻는다. 대화가 실제로 감추고 싶어 하는
// 것이 그것뿐이어서다 — 아직 받지도 않은 퀘스트의 완료 선택지, 이미 끝낸 퀘스트의
// 수락 선택지처럼. 레벨이나 아이템 조건이 필요해지면 여기가 아니라 show_if 에 필드를
// 더한다(그때는 조건이 "퀘스트 상태"가 아니게 되므로 이 enum 이름부터 바뀌어야 한다).
enum class DialogConditionType
{
	// 조건이 없다. 언제나 보인다.
	None = 0,

	Acceptable,       // 지금 수락할 수 있다(Quest::CanAccept 가 Ok)
	InProgress,       // 진행 중이다(아직 목표가 남았다)
	ReadyToComplete,  // 목표를 다 채웠고 완료 접수만 남았다
	Completed,        // 완료했다
	NotCompleted,     // 아직 완료하지 않았다(받지 않았거나 진행 중이거나)
	Failed,           // 제한 시간 초과 등으로 실패했다
};

inline DialogConditionType ParseDialogCondition(std::string_view state)
{
	if (state == "acceptable")        return DialogConditionType::Acceptable;
	if (state == "in_progress")       return DialogConditionType::InProgress;
	if (state == "ready_to_complete") return DialogConditionType::ReadyToComplete;
	if (state == "completed")         return DialogConditionType::Completed;
	if (state == "not_completed")     return DialogConditionType::NotCompleted;
	if (state == "failed")            return DialogConditionType::Failed;
	return DialogConditionType::None;
}
