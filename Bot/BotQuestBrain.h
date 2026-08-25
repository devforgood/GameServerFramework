#pragma once

#include <string>
#include <unordered_map>
#include <unordered_set>
#include <vector>

#include "BotScenario.h"

namespace bot
{
	//-----------------------------------------------------------------------------------
	// 봇 한 명의 퀘스트 진행 상태와 "지금 무엇을 해야 하는가"(목표).
	//
	// 서버가 알려 주는 것(QuestSync/DialogNode/EnterGate)만 사실로 삼고, 게임 데이터를
	// 곁들여 다음 행동 하나를 정한다. 행동을 실제로 내보내는 것은 BT 노드다 — 여기서는
	// 소켓도 BT 도 모르므로, 시나리오 판단만 따로 떼어 단위 테스트로 고정할 수 있다.
	//
	// 봇마다 하나씩 갖고 그 봇의 워커 스레드에서만 만진다(봇 사이에 공유되는 것이 없다).
	//-----------------------------------------------------------------------------------

	enum class QuestGoalKind
	{
		None = 0,   // 할 일 없음(자유 사냥으로 돌아간다)
		Travel,     // 다른 맵으로 간다. 게이트를 밟는 것까지가 이 목표다
		Interact,   // NPC 에게 간다. 대화가 열리면 정해진 선택지를 누른다
		Hunt,       // 사냥터로 가서 잡는다(처치/수집/레벨 목표)
	};

	struct QuestGoal
	{
		QuestGoalKind kind = QuestGoalKind::None;
		int quest_id = 0;

		int map_id = 0;          // 이 목표를 수행하는 맵
		Vec3 pos;                // 목적지(클라이언트 좌표계 — 서버가 변환해 준다)

		int npc_id = 0;          // Interact
		float reach = 2.0f;      // 이 거리 안에 들어오면 도착으로 본다
		float radius = 10.0f;    // Hunt 반경

		int gate_id = 0;         // Travel: 밟아야 할 게이트

		// 대화에서 실행할 동작. 0 이면 상호작용만 하고 대화는 닫는다(talk 목표).
		int dialog_quest_id = 0;
		bool dialog_complete = false;   // true 면 complete_quest, false 면 accept_quest

		// 선택 보상이 있는 퀘스트의 완료 번호. 대화로는 끝낼 수 없어 QuestComplete 를
		// 직접 보내야 한다(-1 이면 선택 보상이 없다).
		int reward_choice = -1;
	};

	// 서버가 QuestSync 로 알려 준 진행 중 퀘스트 한 건.
	struct QuestProgress
	{
		int state = 0;      // 0 InProgress, 1 ReadyToComplete, 2 Failed, 3 Cooldown
		int stage = 1;      // 1-based
		int progress[3] = { 0, 0, 0 };
	};

	class BotQuestBrain
	{
	public:
		// QuestState(서버 quest_active.state)와 같은 값.
		static constexpr int kStateInProgress = 0;
		static constexpr int kStateReadyToComplete = 1;
		static constexpr int kStateFailed = 2;
		static constexpr int kStateCooldown = 3;

		// scenario 가 nullptr 이거나 계획이 비면 봇은 예전처럼 자유 사냥만 한다.
		void Configure(const BotScenario* scenario, int branch_index);
		bool Enabled() const { return scenario_ != nullptr && !plan_.empty(); }

		const std::vector<int>& Plan() const { return plan_; }
		int BranchIndex() const { return branch_index_; }

		// 재접속. 서버가 다시 알려 줄 것(진행 중 퀘스트/대화)만 버리고, 이번 실행에서
		// 알아낸 것(완료 목록, 수락 실패 횟수)은 남긴다 — 같은 계정으로 다시 붙는 것이라
		// 서버 쪽 진행도는 그대로다.
		void ResetForReconnect();

		void SetMapId(int map_id) { map_id_ = map_id; goal_dirty_ = true; }
		int MapId() const { return map_id_; }

		// ---- 서버 동기화
		void ApplyQuestInfo(int quest_id, int state, int stage, const int* progress, int progress_count);
		void ApplyQuestRemoved(int quest_id);
		void ApplyQuestCompleted(int quest_id);

		bool IsActive(int quest_id) const { return active_.find(quest_id) != active_.end(); }
		bool IsCompleted(int quest_id) const { return completed_.find(quest_id) != completed_.end(); }
		const QuestProgress* FindProgress(int quest_id) const;

		// ---- 대화
		// 서버가 보낸 선택지의 text_id 를 받은 순서 그대로 넘긴다(조건에 걸러진 목록이다).
		void OnDialogOpened(int npc_id, int node_id, std::vector<std::string> choice_text_ids);
		void OnDialogClosed();
		bool DialogOpen() const { return dialog_node_id_ != 0; }
		int DialogNodeId() const { return dialog_node_id_; }
		int DialogNpcId() const { return dialog_npc_id_; }

		// 지금 열린 대화에서 목표를 이루려면 몇 번을 눌러야 하는가(서버가 보낸 목록 기준).
		// -1 이면 여기서 할 일이 없다 — 창을 닫는다.
		//
		// 목표가 원하는 동작이 목록에 없으면 그것도 정보다(레벨이 모자라거나 이미 끝낸
		// 퀘스트라 서버가 선택지를 내보내지 않은 것이다). 그 경우를 세어 두었다가
		// 물러설지 건너뛸지 정한다.
		int ChooseDialogChoice();

		// ---- 목표
		void Update(double now);
		const QuestGoal& Goal() const { return goal_; }
		void MarkGoalStale() { goal_dirty_ = true; }

		// 배회할 때 중심으로 삼을 지점(사냥 목표가 있으면 사냥터, 없으면 false).
		bool HuntAnchor(Vec3& out_pos, float& out_radius) const;

		// ---- 송신 간격. 서버가 거절해도 봇이 같은 패킷을 계속 두드리지 않게 한다.
		bool CanSendInteract(double now) const { return now >= next_interact_at_; }
		void NoteInteractSent(double now) { next_interact_at_ = now + kInteractIntervalSeconds; }

		bool CanSendDialog(double now) const { return now >= next_dialog_at_; }
		void NoteDialogSent(double now) { next_dialog_at_ = now + kDialogIntervalSeconds; }

		bool CanSendGate(double now) const { return now >= next_gate_at_; }
		void NoteGateSent(double now) { next_gate_at_ = now + kGateIntervalSeconds; }

		// 완료 요청(선택 보상)은 한 번만 보내고 응답을 기다린다.
		bool CanSendComplete(double now) const { return now >= next_complete_at_; }
		void NoteCompleteSent(double now) { next_complete_at_ = now + kCompleteIntervalSeconds; }

	private:
		// 서버 게이트 쿨타임이 1초다. 그보다 촘촘히 보내면 거절만 쌓인다.
		static constexpr double kGateIntervalSeconds = 1.5;
		static constexpr double kInteractIntervalSeconds = 1.0;
		static constexpr double kDialogIntervalSeconds = 0.4;
		static constexpr double kCompleteIntervalSeconds = 2.0;

		// 한 번 연 대화에서 누를 수 있는 횟수. 데이터가 이상해 goto 를 오가더라도
		// 봇이 대화 안에서 맴돌지 않게 막는다.
		static constexpr int kMaxDialogSteps = 6;

		// 수락 선택지가 없을 때: 이만큼 시도하면 잠시 물러나 레벨을 올리고,
		// 이만큼 되면 이 퀘스트를 포기하고 계획의 다음 칸으로 넘어간다.
		static constexpr int kAcceptBackoffAttempts = 2;
		static constexpr int kAcceptGiveUpAttempts = 8;
		static constexpr double kAcceptRetryDelaySeconds = 15.0;

		// 목표가 원하는 대화 선택지가 지금 없다는 것을 기록한다(물러서기/건너뛰기 판단).
		void NoteDialogActionMissing();

		void BuildGoal(double now);
		bool BuildObjectiveGoal(const ScenarioQuest& quest, const QuestProgress& progress,
			double now, QuestGoal& out) const;

		// 목적지가 다른 맵이면 Travel 로 바꾼다. 길이 없으면 false.
		bool RouteTo(int map_id, QuestGoal& goal) const;

		QuestGoal MakeInteractGoal(const ScenarioNpc& npc) const;
		QuestGoal MakeHuntGoal(const ScenarioSpawn& spawn) const;

		// 레벨이 모자라 진행이 막혔을 때 쓰는 목표(지금 맵에서 아무거나 잡는다).
		bool BuildLevelUpGoal(int prefer_map_id, QuestGoal& out) const;

		const BotScenario* scenario_ = nullptr;
		int branch_index_ = 0;
		std::vector<int> plan_;

		int map_id_ = 0;

		std::unordered_map<int, QuestProgress> active_;
		std::unordered_set<int> completed_;

		// 계획에서 빼기로 한 퀘스트(이미 끝냈거나, 봇이 스스로 진행할 수 없는 목표다).
		std::unordered_set<int> skipped_;

		std::unordered_map<int, int> accept_attempts_;
		std::unordered_map<int, double> accept_retry_at_;

		int dialog_node_id_ = 0;
		int dialog_npc_id_ = 0;
		int dialog_steps_ = 0;
		std::vector<std::string> dialog_choice_text_ids_;

		QuestGoal goal_;
		bool goal_dirty_ = true;
		double now_ = 0.0;
		double next_plan_at_ = 0.0;

		double next_interact_at_ = 0.0;
		double next_dialog_at_ = 0.0;
		double next_gate_at_ = 0.0;
		double next_complete_at_ = 0.0;
	};
}
