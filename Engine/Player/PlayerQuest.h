#pragma once
#include "Component.h"
#include "DbChangeTracker.h"
#include "EventMessage.h"
#include "Quest.h"
#include "QuestObjective.h"
#include "./SQL/generated/vo.h"
#include <chrono>
#include <cstdint>
#include <functional>
#include <utility>
#include <vector>

// 퀘스트 완료로 지급이 확정된 보상 한 건.
// 경험치는 PlayerLevel 로 바로 넘기지만, 골드/아이템은 아직 받아 줄 컴포넌트가 없다.
// 여기 쌓아 두고 인벤토리/지갑이 생기면 그쪽이 꺼내 가면 된다 — 없는 시스템을
// 있는 척 호출해 보상이 사라지는 것보다 낫다.
struct QuestRewardGrant
{
	int quest_id = 0;
	int exp = 0;
	int gold = 0;
	std::vector<std::pair<int, int>> items; // (item_id, count)
	std::vector<int> skill_ids;
};

// 플레이어의 퀘스트 진행 상태.
// 게임 시스템과는 PlayerEventBroker 로만 이어져 있다: 전투/이동/상호작용이 발행한
// 이벤트를 받아 목표 진행도로 옮기고, 수락/완료/실패를 다시 이벤트로 알린다.
class PlayerQuest : public ComponentBase<PlayerQuest>, public IQuestOwner
{
public:
	virtual void Start() override;
	virtual void Update(float dt) override;
	virtual void Load(std::any data) override;
	virtual void Save(std::any data) override;

	// ---- 이벤트 핸들러 (구독은 Start 에서)
	void OnEventActorDead(const EventActorDead& message);
	void OnEventItemAcquired(const EventItemAcquired& message);
	void OnEventItemUsed(const EventItemUsed& message);
	void OnEventSkillUsed(const EventSkillUsed& message);
	void OnEventLevelUp(const EventLevelUp& message);
	void OnEventAreaEntered(const EventAreaEntered& message);
	void OnEventNpcInteracted(const EventNpcInteracted& message);
	void OnEventObjectInteracted(const EventObjectInteracted& message);
	void OnEventNpcDead(const EventNpcDead& message);
	void OnEventNpcEscorted(const EventNpcEscorted& message);
	void OnEventPlayerJoined(const EventPlayerJoined& message);

	// ---- 퀘스트 조작
	// 수락. 실패 사유를 그대로 돌려주므로 클라이언트에 이유를 보여줄 수 있다.
	QuestAcceptResult AcceptQuest(int quest_id);

	// 완료. 모든 스테이지를 끝낸(ReadyToComplete) 퀘스트만 완료할 수 있다.
	// 선택 보상이 있는 퀘스트는 reward_choice 가 choice_items 의 유효한 인덱스여야 한다.
	bool CompleteQuest(int quest_id, int reward_choice = -1);

	// 포기. MainQuest 처럼 포기할 수 없는 퀘스트는 false.
	bool AbandonQuest(int quest_id);

	// 이벤트를 거치지 않고 직접 진행도를 올린다(GM 도구, 아직 이벤트가 없는 시스템용).
	void ReportProgress(QuestObjectiveType type, int target_id, int amount);

	// ---- 운영(GM) 조작
	// 모두 정상 경로의 조건 검사를 건너뛴다. 문의 대응/QA 용이며 호출한 쪽이 권한을 확인해야 한다.

	// 조건과 상관없이 진행 중으로 만든다(진행도는 초기화). 데이터에 없는 퀘스트면 false.
	bool GmForceAccept(int quest_id);

	// 남은 스테이지를 건너뛰고 완료 처리한다. 보상은 정상 완료와 같게 지급된다.
	bool GmForceComplete(int quest_id, int reward_choice = -1);

	// 스테이지와 진행도를 직접 세운다. 채워지면 스테이지 전환/완료 판정도 그대로 탄다.
	bool GmSetProgress(int quest_id, int stage, int progress1, int progress2 = 0, int progress3 = 0);

	// 진행 기록과 완료 플래그를 모두 지워 "한 번도 안 받은" 상태로 되돌린다.
	bool GmResetQuest(int quest_id);

	// ---- 조회
	bool IsActive(int quest_id) const;
	bool IsCompleted(int quest_id) const;
	QuestState GetState(int quest_id) const;
	int GetStage(int quest_id) const;
	int GetProgress(int quest_id, int slot) const;
	const QuestActiveVO* GetActiveQuest(int quest_id) const;

	// 지급 확정된 보상을 꺼낸다(꺼내면 목록은 비워진다).
	std::vector<QuestRewardGrant> TakePendingRewards();
	const std::vector<QuestRewardGrant>& GetPendingRewards() const { return pendingRewards_; }

	// ---- 클라 동기화
	// 매 이벤트마다 메시지를 보내면 처치 한 번에 한 통씩 나간다. 바뀐 퀘스트 id 만 모아
	// 두고 Update 끝에 한 번 내보낸다(sendSync).

	// 로그인 직후처럼 전체를 다시 내려줘야 할 때, 진행 중인 모든 퀘스트를 동기화 대상으로 올린다.
	void MarkAllForSync();

	// 제한 시간/쿨타임 판정에 쓸 현재 시각. 테스트에서 시간을 흘려보내기 위해 교체할 수 있다.
	void SetClock(std::function<std::chrono::system_clock::time_point()> clock)
	{
		clock_ = std::move(clock);
	}

	// ---- IQuestOwner
	int GetLevel() const override;
	bool IsQuestActive(int quest_id) const override { return IsActive(quest_id); }
	bool IsQuestCompleted(int quest_id) const override { return IsCompleted(quest_id); }
	bool HasItem(int item_id) const override;
	bool HasSkill(int skill_id) const override;

private:
	std::chrono::system_clock::time_point now() const;

	// 이번 틱에 바뀐 퀘스트를 한 통으로 묶어 클라에 내보낸다. Update 끝에서 호출.
	// 보낼 통로(PlayerSender)가 없으면 아무것도 하지 않는다.
	void sendSync();
	bool HasPendingSync() const;
	void DrainSync(std::vector<int>& changed, std::vector<int>& removed, std::vector<int>& completed);

	// 진행도를 반영하고, 스테이지가 끝났으면 다음 스테이지로 넘기거나
	// 완료 대기 상태로 만든다.
	void applyProgress(const Quest& quest, QuestActiveVO& vo,
		QuestObjectiveType type, int target_id, int amount);

	// 이미 골라 둔 목표 슬롯에 진행도를 반영한다(목표 매칭을 두 번 하지 않기 위해).
	void applyMatched(const Quest& quest, QuestActiveVO& vo, QuestObjectiveType type,
		const QuestObjectiveSlot* matched, int matched_count, int amount);

	// 스테이지 전환. 마지막 스테이지였다면 완료 대기(또는 자동 완료)로 간다.
	void advanceStage(const Quest& quest, QuestActiveVO& vo);

	// 완료 NPC 와 대화했을 때 완료 접수를 시도한다.
	void tryCompleteByNpc(int npc_id);

	// 제한 시간이 지난 퀘스트를 Failed 로 돌린다.
	void expireTimedOutQuests();

	// 지키는 중인 목표(protect)에 흐른 시간을 진행도로 넣는다.
	void tickProtectObjectives(int seconds);

	// 퀘스트를 실패 상태로 만든다(사유는 호출 측 로그에 남긴다).
	// 이미 실패했거나 진행 중이 아니면 아무것도 하지 않는다.
	bool failQuest(int quest_id);

	// 쿨타임 대기 중인 행 가운데 리셋 경계를 지난 것을 정리한다.
	void clearFinishedCooldowns();

	void grantRewards(const Quest& quest, int reward_choice);

	// 이 퀘스트 전용 아이템을 인벤토리에서 회수한다(완료/포기 시).
	void discardQuestItems(int quest_id);

	void publish(const EventMessage& message);

	// 동기화 대기 목록에 중복 없이 넣는다.
	static void pushUnique(std::vector<int>& list, int quest_id);
	void markSync(int quest_id) { pushUnique(syncChanged_, quest_id); }
	void markRemovedFromLog(int quest_id);

	void setCompleted(int quest_id);
	void clearCompleted(int quest_id);
	void resetActiveRow(QuestActiveVO& vo);
	QuestStateVO buildStateVO() const;

	static int readProgress(const QuestActiveVO& vo, int slot);
	static void writeProgress(QuestActiveVO& vo, int slot, int value);

	// 진행 중 퀘스트의 DB 변경 추적 (quest_id 기준)
	DbCollectionTracker<int, QuestActiveVO> activeQuests_;

	// quest_state 행(완료 플래그)의 DB 변경 추적
	DbRowTracker<QuestStateVO> questState_;

	// 완료 퀘스트 플래그: quest_id 당 1비트, 바이트 단위로 패킹
	std::vector<uint8_t> completedBits_;

	std::vector<QuestRewardGrant> pendingRewards_;

	// 다음 틱에 클라로 내보낼 변경분.
	std::vector<int> syncChanged_;
	std::vector<int> syncRemoved_;
	std::vector<int> syncCompleted_;

	std::function<std::chrono::system_clock::time_point()> clock_;

	// 제한 시간 검사 누적 시간(초). 매 틱 시계를 읽지 않기 위해 1초에 한 번만 본다.
	float expireCheckAcc_ = 0.0f;

	int characterId_ = 0;

	// PlayerLevel 이 붙어 있지 않을 때 쓰는 레벨(Load 로 받은 값).
	// 레벨 조건과 레벨 달성 목표가 이 값을 본다.
	int cachedLevel_ = 1;
};
