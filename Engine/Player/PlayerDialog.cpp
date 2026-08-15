#include "PlayerDialog.h"
#include "GameObject.h"
#include "PlayerQuest.h"
#include "GameData/ResourceLoader.h"
#include "gamedata.h"

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
	return true;
}

void PlayerDialog::Close()
{
	npcId_ = 0;
	currentNodeId_ = 0;
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

	if (choice_index < 0 || choice_index >= static_cast<int>(node->choices.size()))
		return DialogResult::InvalidChoice;

	bool failed = false;
	const int next_id = applyAction(node->choices[choice_index], failed);

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
