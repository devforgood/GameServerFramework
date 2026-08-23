#include "PlayerDialog.h"
#include "DialogCondition.h"
#include "GameObject.h"
#include "PlayerQuest.h"
#include "QuestRegistry.h"
#include "GameData/ResourceLoader.h"
#include "gamedata.h"
#include <algorithm>

namespace
{
	// 이 선택지를 지금 보여 줄 것인가.
	//
	// 조건이 없으면 언제나 보인다 — 대부분의 선택지가 그렇고, 무엇보다 노드마다
	// 조건 없는 선택지가 하나는 있어야 대화를 닫을 수 있다(데이터 검증이 강제한다).
	//
	// 조건이 있는데 퀘스트를 볼 수 없으면(컴포넌트가 없으면) 감춘다. 판정할 수 없는
	// 조건을 통과시키면 눌러도 반드시 실패하는 선택지가 화면에 남는다.
	bool IsChoiceShown(const gamedata::DialogChoice& choice, const PlayerQuest* quests)
	{
		const DialogConditionType type = ParseDialogCondition(choice.show_if.state);
		if (type == DialogConditionType::None || choice.show_if.quest_id == 0)
			return true;

		if (quests == nullptr)
			return false;

		const int quest_id = choice.show_if.quest_id;

		// PlayerQuest::GetState 는 진행 중이 아닌 퀘스트에도 InProgress 를 돌려주므로
		// 행이 실제로 있는지부터 본다.
		auto has_state = [quests, quest_id](QuestState want)
		{
			const VOQuestActive* vo = quests->GetActiveQuest(quest_id);
			return vo != nullptr && static_cast<QuestState>(vo->state) == want;
		};

		switch (type)
		{
		case DialogConditionType::Acceptable:
		{
			// 대화에서 받는다고 조건이 면제되지 않으므로, 보여 줄지도 같은 잣대로 정한다.
			// 운영이 내려둔(disabled) 퀘스트가 여기서 함께 걸러지는 것도 이 때문이다.
			const Quest* quest = QuestRegistry::Instance().Get(quest_id);
			return quest != nullptr && quest->CanAccept(*quests) == QuestAcceptResult::Ok;
		}

		case DialogConditionType::InProgress:      return has_state(QuestState::InProgress);
		case DialogConditionType::ReadyToComplete: return has_state(QuestState::ReadyToComplete);
		case DialogConditionType::Failed:          return has_state(QuestState::Failed);
		case DialogConditionType::Completed:       return quests->IsCompleted(quest_id);
		case DialogConditionType::NotCompleted:    return !quests->IsCompleted(quest_id);

		case DialogConditionType::None:
		default:
			return true;
		}
	}
}

bool PlayerDialog::Open(int npc_id)
{
	const gamedata::Npc* npc = ResourceLoader::Instance().GetNpc(npc_id);
	if (npc == nullptr || npc->dialog_id == 0)
		return false;

	const gamedata::Dialog* root = ResourceLoader::Instance().GetDialog(npc->dialog_id);
	if (root == nullptr)
		return false;

	npcId_ = npc_id;
	currentNodeId_ = root->id;
	refreshVisibleChoices();
	return true;
}

void PlayerDialog::Close()
{
	npcId_ = 0;
	currentNodeId_ = 0;
	visibleMask_ = 0;
}

void PlayerDialog::refreshVisibleChoices()
{
	visibleMask_ = 0;

	const gamedata::Dialog* node = GetCurrentNode();
	if (node == nullptr)
		return;

	const PlayerQuest* quests =
		game_object != nullptr ? game_object->GetComponent<PlayerQuest>() : nullptr;

	const int count = std::min(static_cast<int>(node->choices.size()), kMaxChoices);
	for (int i = 0; i < count; ++i)
	{
		if (IsChoiceShown(node->choices[i], quests))
			visibleMask_ |= (1u << i);
	}
}

bool PlayerDialog::IsChoiceVisible(int choice_index) const
{
	if (choice_index < 0 || choice_index >= kMaxChoices)
		return false;
	return (visibleMask_ & (1u << choice_index)) != 0;
}

int PlayerDialog::ResolveVisibleChoice(int visible_index) const
{
	if (visible_index < 0)
		return -1;

	for (int i = 0; i < kMaxChoices; ++i)
	{
		if ((visibleMask_ & (1u << i)) == 0)
			continue;
		if (visible_index == 0)
			return i;
		--visible_index;
	}
	return -1;
}

const gamedata::Dialog* PlayerDialog::GetCurrentNode() const
{
	if (currentNodeId_ == 0)
		return nullptr;
	return ResourceLoader::Instance().GetDialog(currentNodeId_);
}

DialogResult PlayerDialog::Select(int node_id, int choice_index, const gamedata::Dialog** out_next_node)
{
	if (out_next_node != nullptr)
		*out_next_node = nullptr;

	if (currentNodeId_ == 0)
		return DialogResult::NoDialog;

	// 클라가 보고 있던 노드와 서버가 아는 노드가 같아야 한다. 다르면 지난 화면의
	// 선택지로 지금 노드의 동작이 실행된다(창을 두 번 누르면 바로 벌어진다).
	if (node_id != currentNodeId_)
		return DialogResult::StaleNode;

	const gamedata::Dialog* node = GetCurrentNode();
	if (node == nullptr)
	{
		Close();
		return DialogResult::NoDialog;
	}

	// 클라가 보낸 번호는 조건에 걸려 빠진 선택지를 뺀 목록에서의 번호다.
	// 그 목록은 이 노드로 옮겨 올 때 정해 뒀으므로, 지금 조건을 다시 재지 않고
	// 그때의 목록으로 되짚는다 — 화면은 그대로인데 번호의 뜻만 바뀌면 안 된다.
	const int actual_index = ResolveVisibleChoice(choice_index);
	if (actual_index < 0 || actual_index >= static_cast<int>(node->choices.size()))
		return DialogResult::InvalidChoice;

	bool failed = false;
	const int next_id = applyAction(node->choices[actual_index], failed);

	if (failed)
	{
		// 동작이 실패했으면 대화는 그 자리에 둔다. 창을 닫아 버리면 왜 안 됐는지
		// 보여줄 자리가 사라진다.
		return DialogResult::ActionFailed;
	}

	if (next_id == 0)
	{
		Close();
		return DialogResult::Closed;
	}

	const gamedata::Dialog* next = ResourceLoader::Instance().GetDialog(next_id);
	if (next == nullptr)
	{
		// 데이터 검증이 막아 주지만, 통과해 들어왔다면 조용히 굴러가지 않게 끝낸다.
		Close();
		return DialogResult::Closed;
	}

	currentNodeId_ = next->id;
	refreshVisibleChoices();
	if (out_next_node != nullptr)
		*out_next_node = next;

	return DialogResult::Ok;
}

int PlayerDialog::applyAction(const gamedata::DialogChoice& choice, bool& out_failed)
{
	out_failed = false;

	switch (ParseDialogAction(choice.action))
	{
	case DialogActionType::Goto:
		return choice.next_id;

	case DialogActionType::AcceptQuest:
	{
		auto* quests = game_object != nullptr ? game_object->GetComponent<PlayerQuest>() : nullptr;
		if (quests == nullptr)
		{
			out_failed = true;
			return 0;
		}

		// 대화로 받는다고 조건이 면제되지는 않는다.
		if (quests->AcceptQuest(choice.param) != QuestAcceptResult::Ok)
		{
			out_failed = true;
			return 0;
		}
		return choice.next_id;
	}

	case DialogActionType::CompleteQuest:
	{
		auto* quests = game_object != nullptr ? game_object->GetComponent<PlayerQuest>() : nullptr;
		if (quests == nullptr || !quests->CompleteQuest(choice.param))
		{
			out_failed = true;
			return 0;
		}
		return choice.next_id;
	}

	case DialogActionType::Close:
	case DialogActionType::None:
	default:
		return 0;
	}
}
