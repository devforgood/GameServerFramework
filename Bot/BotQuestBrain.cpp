#include "BotQuestBrain.h"

#include <algorithm>

namespace bot
{
	namespace
	{
		// 목표 재계산 주기. 매 틱 다시 세울 필요는 없다 — 상태가 바뀌면 곧바로 다시 세우고
		// (goal_dirty_), 그렇지 않으면 이 간격으로만 확인한다.
		constexpr double kReplanIntervalSeconds = 0.5;
	}

	void BotQuestBrain::Configure(const BotScenario* scenario, int branch_index)
	{
		scenario_ = scenario;
		branch_index_ = branch_index < 0 ? -branch_index : branch_index;

		plan_.clear();
		if (scenario_ != nullptr && scenario_->Loaded())
			plan_ = scenario_->BuildMainQuestPlan(branch_index_);

		goal_ = QuestGoal{};
		goal_dirty_ = true;
	}

	void BotQuestBrain::ResetForReconnect()
	{
		active_.clear();
		OnDialogClosed();

		map_id_ = 0;
		goal_ = QuestGoal{};
		goal_dirty_ = true;
		next_plan_at_ = 0.0;
		next_interact_at_ = 0.0;
		next_dialog_at_ = 0.0;
		next_gate_at_ = 0.0;
		next_complete_at_ = 0.0;
	}

	bool BotQuestBrain::ApplyQuestInfo(int quest_id, int state, int stage,
		const int* progress, int progress_count)
	{
		const bool first_time = seen_.insert(quest_id).second;

		QuestProgress& entry = active_[quest_id];
		entry.state = state;
		entry.stage = stage > 0 ? stage : 1;
		for (int i = 0; i < 3; ++i)
			entry.progress[i] = (progress != nullptr && i < progress_count) ? progress[i] : 0;

		// 받아 냈으니 수락 시도 기록은 의미가 없다.
		accept_attempts_.erase(quest_id);
		accept_retry_at_.erase(quest_id);
		goal_dirty_ = true;
		return first_time;
	}

	void BotQuestBrain::ApplyQuestRemoved(int quest_id)
	{
		active_.erase(quest_id);
		goal_dirty_ = true;
	}

	void BotQuestBrain::ApplyQuestCompleted(int quest_id)
	{
		completed_.insert(quest_id);
		active_.erase(quest_id);
		goal_dirty_ = true;
	}

	const QuestProgress* BotQuestBrain::FindProgress(int quest_id) const
	{
		auto it = active_.find(quest_id);
		return it != active_.end() ? &it->second : nullptr;
	}

	void BotQuestBrain::OnDialogOpened(int npc_id, int node_id, std::vector<std::string> choice_text_ids)
	{
		// 같은 대화가 이어지는 동안에만 누른 횟수를 센다. 새 NPC 와 열면 다시 0 부터다.
		if (dialog_npc_id_ != npc_id)
			dialog_steps_ = 0;

		dialog_npc_id_ = npc_id;
		dialog_node_id_ = node_id;
		dialog_choice_text_ids_ = std::move(choice_text_ids);
	}

	void BotQuestBrain::OnDialogClosed()
	{
		dialog_node_id_ = 0;
		dialog_npc_id_ = 0;
		dialog_steps_ = 0;
		dialog_choice_text_ids_.clear();
	}

	int BotQuestBrain::ChooseDialogChoice()
	{
		if (!DialogOpen() || scenario_ == nullptr)
			return -1;

		// 데이터가 이상해 goto 를 오가더라도 대화 안에서 맴돌지 않게 한다.
		if (dialog_steps_ >= kMaxDialogSteps)
			return -1;

		// 이 대화로 할 일이 없으면 닫는다. talk 목표는 상호작용 자체로 이미 올라갔다.
		if (goal_.kind != QuestGoalKind::Interact || goal_.dialog_quest_id == 0)
			return -1;
		if (goal_.npc_id != dialog_npc_id_)
			return -1;

		const std::string action = goal_.dialog_complete ? "complete_quest" : "accept_quest";
		const int data_index = scenario_->NextDialogChoice(dialog_node_id_, action, goal_.dialog_quest_id);

		const ScenarioDialogNode* node = scenario_->FindDialog(dialog_node_id_);
		if (data_index < 0 || node == nullptr
			|| data_index >= static_cast<int>(node->choices.size()))
		{
			NoteAcceptBlocked();
			return -1;
		}

		// 서버는 조건(show_if)에 걸러진 목록만 보내므로 번호가 데이터와 다르다.
		// text_id 로 짝을 지어 되짚는다(한 노드 안에서 text_id 는 유일하다 — 데이터 검증이 막는다).
		const std::string& text_id = node->choices[data_index].text_id;
		for (int i = 0; i < static_cast<int>(dialog_choice_text_ids_.size()); ++i)
		{
			if (dialog_choice_text_ids_[i] != text_id)
				continue;

			++dialog_steps_;
			if (!goal_.dialog_complete)
			{
				// 수락은 눌렀다고 되는 것이 아니다(레벨/선행 조건은 서버가 다시 본다).
				// 성공하면 QuestSync 가 와서 이 기록을 지운다.
				++accept_attempts_[goal_.quest_id];
			}
			return i;
		}

		// 데이터에는 있는데 서버가 내보내지 않았다 = 지금은 그 선택지의 조건이 맞지 않는다.
		NoteAcceptBlocked();
		return -1;
	}

	void BotQuestBrain::NoteAcceptBlocked()
	{
		// 완료 선택지가 없는 것은 곧 상태가 바뀔 일이라(보고 대화 한 번으로 자동 완료된다)
		// 세지 않는다. 진행을 막는 것은 언제나 '받지 못하는' 쪽이다.
		if (goal_.dialog_complete || goal_.quest_id == 0)
			return;

		const int attempts = ++accept_attempts_[goal_.quest_id];
		if (attempts >= kAcceptGiveUpAttempts)
		{
			// 이번 실행에서는 이 퀘스트를 받을 수 없다고 본다(이전 실행에서 이미 끝냈거나,
			// 데이터가 요구하는 조건을 봇이 채울 수 없다). 계획의 다음 칸으로 넘어간다.
			skipped_.insert(goal_.quest_id);
		}
		else if (attempts >= kAcceptBackoffAttempts)
		{
			// 대개는 레벨이 모자란 것이다. NPC 를 계속 두드리는 대신 잡으러 간다.
			accept_retry_at_[goal_.quest_id] = now_ + kAcceptRetryDelaySeconds;
		}

		goal_dirty_ = true;
	}

	void BotQuestBrain::OnDialogActionFailed()
	{
		NoteAcceptBlocked();
		OnDialogClosed();
	}

	void BotQuestBrain::Update(double now)
	{
		now_ = now;

		if (!goal_dirty_ && now < next_plan_at_)
			return;

		goal_dirty_ = false;
		next_plan_at_ = now + kReplanIntervalSeconds;
		BuildGoal(now);
	}

	bool BotQuestBrain::HuntAnchor(Vec3& out_pos, float& out_radius) const
	{
		if (goal_.kind != QuestGoalKind::Hunt)
			return false;

		out_pos = goal_.pos;
		out_radius = goal_.radius;
		return true;
	}

	QuestGoal BotQuestBrain::MakeInteractGoal(const ScenarioNpc& npc) const
	{
		QuestGoal goal;
		goal.kind = QuestGoalKind::Interact;
		goal.npc_id = npc.id;
		goal.map_id = npc.map_id;
		goal.pos = npc.pos;

		// 서버가 상호작용 거리를 다시 재므로 여유를 두고 다가간다. 경계에 서면
		// 위치 오차 한 번에 거절당한다.
		goal.reach = std::max(1.0f, npc.interact_range * 0.6f);
		return goal;
	}

	QuestGoal BotQuestBrain::MakeHuntGoal(const ScenarioSpawn& spawn) const
	{
		QuestGoal goal;
		goal.kind = QuestGoalKind::Hunt;
		goal.map_id = spawn.map_id;
		goal.pos = spawn.pos;

		// 스폰 반경보다 조금 넓게 잡는다. 몬스터는 스폰 지점 주위를 돌아다니고,
		// 딱 맞춰 두면 봇이 경계에서 들락거리며 이동 명령만 쏟아 낸다.
		goal.radius = std::max(spawn.radius + 6.0f, 10.0f);
		goal.reach = goal.radius;
		return goal;
	}

	bool BotQuestBrain::RouteTo(int map_id, QuestGoal& goal) const
	{
		if (map_id == 0 || map_id == map_id_)
			return true;

		if (map_id_ == 0 || scenario_ == nullptr)
			return false;   // 아직 어느 맵인지 모른다(로그인 전)

		const ScenarioGate* gate = scenario_->NextGate(map_id_, map_id);
		if (gate == nullptr)
			return false;

		const int quest_id = goal.quest_id;
		goal = QuestGoal{};
		goal.kind = QuestGoalKind::Travel;
		goal.quest_id = quest_id;
		goal.map_id = map_id;
		goal.gate_id = gate->id;
		goal.pos = gate->pos;

		// 서버는 게이트에서 5m 안에 있어야 받아 준다. 그보다 가까이 붙어서 보낸다.
		goal.reach = 2.5f;
		return true;
	}

	bool BotQuestBrain::BuildLevelUpGoal(int prefer_map_id, QuestGoal& out) const
	{
		if (scenario_ == nullptr)
			return false;

		// 지금 맵에서 잡을 수 있으면 맵을 옮기지 않는다. 레벨을 올리는 것이 목적이라
		// 어느 몬스터인지는 상관없다.
		const ScenarioSpawn* spawn = scenario_->FindAnySpot(map_id_);
		if (spawn == nullptr && prefer_map_id != 0)
			spawn = scenario_->FindAnySpot(prefer_map_id);
		if (spawn == nullptr)
			return false;

		const int quest_id = out.quest_id;
		out = MakeHuntGoal(*spawn);
		out.quest_id = quest_id;
		return RouteTo(out.map_id, out);
	}

	bool BotQuestBrain::BuildObjectiveGoal(const ScenarioQuest& quest, const QuestProgress& progress,
		double now, QuestGoal& out) const
	{
		const int stage_index = progress.stage - 1;
		if (stage_index < 0 || stage_index >= static_cast<int>(quest.stages.size()))
			return false;

		const ScenarioStage& stage = quest.stages[stage_index];

		for (int i = 0; i < static_cast<int>(stage.objectives.size()); ++i)
		{
			const ScenarioObjective& objective = stage.objectives[i];
			if (i < 3 && progress.progress[i] >= objective.count)
				continue;   // 이 목표는 이미 채웠다

			QuestGoal goal;
			goal.quest_id = quest.id;

			switch (objective.kind)
			{
			case ObjectiveKind::Talk:
			{
				const ScenarioNpc* npc = scenario_->FindNpc(objective.target_id);
				if (npc == nullptr)
					continue;

				// 상호작용 자체가 대화 목표를 올린다. 여기서는 대화로 할 일이 없다.
				const int quest_id = goal.quest_id;
				goal = MakeInteractGoal(*npc);
				goal.quest_id = quest_id;
				break;
			}

			case ObjectiveKind::Kill:
			{
				const ScenarioSpawn* spawn = scenario_->FindHuntSpot(objective.target_id, quest.map_id);
				if (spawn == nullptr)
					continue;

				const int quest_id = goal.quest_id;
				goal = MakeHuntGoal(*spawn);
				goal.quest_id = quest_id;
				break;
			}

			case ObjectiveKind::Collect:
			{
				// 수집 아이템은 몬스터가 떨군다. 그것을 떨구는 몬스터의 사냥터로 간다.
				const ScenarioSpawn* spawn = scenario_->FindItemSource(objective.target_id, quest.map_id);
				if (spawn == nullptr)
					continue;

				const int quest_id = goal.quest_id;
				goal = MakeHuntGoal(*spawn);
				goal.quest_id = quest_id;
				break;
			}

			case ObjectiveKind::Level:
			{
				if (!BuildLevelUpGoal(quest.map_id, goal))
					continue;

				out = goal;
				return true;   // BuildLevelUpGoal 이 이미 길찾기까지 마쳤다
			}

			case ObjectiveKind::Reach:
			{
				// 서버는 맵에 '들어오는 순간'에만 세어 준다. 이미 그 맵에 있다면
				// 한 번 나갔다 와야 한다.
				int destination = objective.target_id;
				if (destination == map_id_)
				{
					const ScenarioGate* gate = scenario_->FindAnyGate(map_id_);
					if (gate == nullptr)
						continue;

					goal.kind = QuestGoalKind::Travel;
					goal.map_id = map_id_;
					goal.gate_id = gate->id;
					goal.pos = gate->pos;
					goal.reach = 2.5f;
					out = goal;
					return true;
				}

				goal.map_id = destination;
				if (!RouteTo(destination, goal))
					continue;

				out = goal;
				return true;
			}

			case ObjectiveKind::Escort:
			case ObjectiveKind::Protect:
			{
				// 호위/보호는 대상 곁에 붙어 있는 것이 핵심이다. 그 자리를 사냥터로 삼으면
				// 봇이 근처를 지키며 달려드는 것들을 잡는다.
				const ScenarioNpc* npc = scenario_->FindNpc(objective.target_id);
				if (npc == nullptr)
					continue;

				goal.kind = QuestGoalKind::Hunt;
				goal.map_id = npc->map_id;
				goal.pos = npc->pos;
				goal.radius = 12.0f;
				goal.reach = goal.radius;
				break;
			}

			default:
				// interact/use_item/use_skill 은 봇이 스스로 진행할 방법이 없다.
				// 다음 목표를 본다(같은 스테이지의 다른 목표는 할 수 있을지 모른다).
				continue;
			}

			if (!RouteTo(goal.map_id, goal))
				continue;

			out = goal;
			return true;
		}

		(void)now;
		return false;
	}

	void BotQuestBrain::BuildGoal(double now)
	{
		goal_ = QuestGoal{};
		if (!Enabled())
			return;

		for (int quest_id : plan_)
		{
			if (completed_.find(quest_id) != completed_.end())
				continue;
			if (skipped_.find(quest_id) != skipped_.end())
				continue;

			const ScenarioQuest* quest = scenario_->FindQuest(quest_id);
			if (quest == nullptr || quest->disabled)
			{
				skipped_.insert(quest_id);
				continue;
			}

			auto active = active_.find(quest_id);
			if (active != active_.end())
			{
				const QuestProgress& progress = active->second;

				if (progress.state == kStateReadyToComplete)
				{
					const ScenarioNpc* npc = scenario_->FindNpc(quest->end_npc_id);
					if (npc == nullptr)
					{
						skipped_.insert(quest_id);
						continue;
					}

					QuestGoal goal = MakeInteractGoal(*npc);
					goal.quest_id = quest_id;
					goal.dialog_quest_id = quest_id;
					goal.dialog_complete = true;
					goal.reward_choice = quest->reward_choice_count > 0
						? branch_index_ % quest->reward_choice_count : -1;

					if (!RouteTo(npc->map_id, goal))
					{
						skipped_.insert(quest_id);
						continue;
					}

					goal_ = goal;
					return;
				}

				if (progress.state == kStateInProgress)
				{
					QuestGoal goal;
					if (BuildObjectiveGoal(*quest, progress, now, goal))
					{
						goal_ = goal;
						return;
					}

					// 남은 목표를 봇이 진행할 수 없다. 여기서 붙잡고 있어 봐야 계획 전체가
					// 멈추므로 다음 칸으로 넘어간다.
					skipped_.insert(quest_id);
					continue;
				}

				// 실패했거나(제한 시간) 쿨타임이면 이번 실행에서는 더 볼 것이 없다.
				skipped_.insert(quest_id);
				continue;
			}

			// 아직 받지 않았다. 수락 창구(시작 NPC)로 간다.
			auto retry = accept_retry_at_.find(quest_id);
			if (retry != accept_retry_at_.end() && now < retry->second)
			{
				// 받지 못하는 이유는 대개 레벨이다. 기다리는 동안 잡으러 간다.
				QuestGoal goal;
				goal.quest_id = quest_id;
				if (BuildLevelUpGoal(quest->map_id, goal))
				{
					goal_ = goal;
					return;
				}
				continue;
			}

			const ScenarioNpc* npc = scenario_->FindNpc(quest->start_npc_id);
			if (npc == nullptr)
			{
				skipped_.insert(quest_id);
				continue;
			}

			QuestGoal goal = MakeInteractGoal(*npc);
			goal.quest_id = quest_id;
			goal.dialog_quest_id = quest_id;
			goal.dialog_complete = false;

			if (!RouteTo(npc->map_id, goal))
			{
				skipped_.insert(quest_id);
				continue;
			}

			goal_ = goal;
			return;
		}

		// 계획을 다 했다(또는 더 진행할 수 없다). 남은 시간은 자유 사냥으로 부하를 만든다.
	}
}
