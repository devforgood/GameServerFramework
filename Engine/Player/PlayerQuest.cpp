#include "PlayerQuest.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "PlayerEventBroker.h"
#include "PlayerLevel.h"
#include "PlayerItem.h"
#include "PlayerSkill.h"
#include "PlayerWallet.h"
#include "GameObject.h"
#include "QuestRegistry.h"
#include "QuestPolicy.h"
#include "GameData/gamedata.h"
#include <algorithm>

namespace
{
// 목표 진행도를 검사하는 주기(초). 제한 시간 퀘스트만 시계를 봐야 하므로
// 매 틱 system_clock 을 읽을 이유가 없다.
constexpr float kExpireCheckInterval = 1.0f;
}

void PlayerQuest::Start()
{
	auto eventBroker = game_object->GetComponent<PlayerEventBroker>();
	if (eventBroker == nullptr)
		return;

	eventBroker->subscribe<PlayerQuest, EventActorDead, &PlayerQuest::OnEventActorDead>(this);
	eventBroker->subscribe<PlayerQuest, EventItemAcquired, &PlayerQuest::OnEventItemAcquired>(this);
	eventBroker->subscribe<PlayerQuest, EventItemUsed, &PlayerQuest::OnEventItemUsed>(this);
	eventBroker->subscribe<PlayerQuest, EventSkillUsed, &PlayerQuest::OnEventSkillUsed>(this);
	eventBroker->subscribe<PlayerQuest, EventLevelUp, &PlayerQuest::OnEventLevelUp>(this);
	eventBroker->subscribe<PlayerQuest, EventAreaEntered, &PlayerQuest::OnEventAreaEntered>(this);
	eventBroker->subscribe<PlayerQuest, EventNpcInteracted, &PlayerQuest::OnEventNpcInteracted>(this);
	eventBroker->subscribe<PlayerQuest, EventObjectInteracted, &PlayerQuest::OnEventObjectInteracted>(this);
	eventBroker->subscribe<PlayerQuest, EventPlayerJoined, &PlayerQuest::OnEventPlayerJoined>(this);
}

void PlayerQuest::Update(float dt)
{
	if (activeQuests_.Size() == 0)
		return;

	expireCheckAcc_ += dt;
	if (expireCheckAcc_ < kExpireCheckInterval)
		return;
	expireCheckAcc_ = 0.0f;

	expireTimedOutQuests();
}

std::chrono::system_clock::time_point PlayerQuest::now() const
{
	return clock_ ? clock_() : std::chrono::system_clock::now();
}

// ============================================================
// 이벤트 핸들러 — 이벤트를 목표 종류로 옮기기만 한다.
// ============================================================

void PlayerQuest::OnEventActorDead(const EventActorDead& message)
{
	// 처치한 대상의 종류를 모르면(플레이어 등) 처치 목표로 셀 수 없다.
	if (message.victim_data_id == 0)
		return;
	ReportProgress(QuestObjectiveType::Kill, message.victim_data_id, 1);
}

void PlayerQuest::OnEventItemAcquired(const EventItemAcquired& message)
{
	ReportProgress(QuestObjectiveType::Collect, message.item_id, message.count);
}

void PlayerQuest::OnEventItemUsed(const EventItemUsed& message)
{
	ReportProgress(QuestObjectiveType::UseItem, message.item_id, message.count);
}

void PlayerQuest::OnEventSkillUsed(const EventSkillUsed& message)
{
	ReportProgress(QuestObjectiveType::UseSkill, message.skill_id, 1);
}

void PlayerQuest::OnEventLevelUp(const EventLevelUp& message)
{
	if (message.new_level > cachedLevel_)
		cachedLevel_ = message.new_level;

	// 레벨 달성 목표는 도달한 레벨 자체가 진행도다(누적이 아니라 최고값).
	ReportProgress(QuestObjectiveType::Level, 0, message.new_level);
}

void PlayerQuest::OnEventAreaEntered(const EventAreaEntered& message)
{
	ReportProgress(QuestObjectiveType::Reach, message.area_id, 1);
}

void PlayerQuest::OnEventNpcInteracted(const EventNpcInteracted& message)
{
	ReportProgress(QuestObjectiveType::Talk, message.npc_id, 1);

	// 대화 목표를 올린 뒤에 완료 접수를 본다. 마지막 스테이지가 "보고하기"인 퀘스트는
	// 같은 대화 한 번으로 목표 달성과 완료가 함께 끝나야 한다.
	tryCompleteByNpc(message.npc_id);
}

void PlayerQuest::OnEventObjectInteracted(const EventObjectInteracted& message)
{
	ReportProgress(QuestObjectiveType::Interact, message.object_id, 1);
}

void PlayerQuest::OnEventPlayerJoined(const EventPlayerJoined& message)
{
	// 접속하지 않은 사이에 지난 시간을 여기서 정산한다.
	expireTimedOutQuests();
	clearFinishedCooldowns();
}

// ============================================================
// 진행도
// ============================================================

void PlayerQuest::ReportProgress(QuestObjectiveType type, int target_id, int amount)
{
	if (type == QuestObjectiveType::None || amount <= 0)
		return;

	std::vector<int> quest_ids;
	activeQuests_.CollectKeys(quest_ids);

	// 자동 완료 대상은 순회가 끝난 뒤에 처리한다. 완료는 행을 지우므로
	// 순회 중에 부르면 방금 잡고 있던 참조가 무효가 된다.
	std::vector<int> auto_complete;

	for (int quest_id : quest_ids)
	{
		const Quest* quest = QuestRegistry::Instance().Get(quest_id);
		if (quest == nullptr)
			continue;

		const QuestActiveVO* current = activeQuests_.Find(quest_id);
		if (current == nullptr || static_cast<QuestState>(current->state) != QuestState::InProgress)
			continue;

		QuestObjectiveSlot matched[Quest::kMaxObjectivesPerStage];
		const int matched_count = quest->MatchObjectives(
			current->stage, type, target_id, matched, Quest::kMaxObjectivesPerStage);
		if (matched_count == 0)
			continue;

		// 실제로 반응할 목표가 있을 때만 행을 수정 상태로 만든다
		// (아니면 관계없는 이벤트마다 UPDATE 가 쌓인다).
		QuestActiveVO* vo = activeQuests_.Modify(quest_id);
		if (vo == nullptr)
			continue;

		applyMatched(*quest, *vo, type, matched, matched_count, amount);

		if (static_cast<QuestState>(vo->state) == QuestState::ReadyToComplete &&
			quest->IsAutoComplete())
			auto_complete.push_back(quest_id);
	}

	for (int quest_id : auto_complete)
	{
		// 자동 완료 퀘스트는 선택 보상을 둘 수 없다(고를 사람이 없다).
		// 데이터 검증이 막으므로 여기서는 선택 없음으로 완료한다.
		CompleteQuest(quest_id, -1);
	}
}

void PlayerQuest::applyProgress(const Quest& quest, QuestActiveVO& vo,
	QuestObjectiveType type, int target_id, int amount)
{
	QuestObjectiveSlot matched[Quest::kMaxObjectivesPerStage];
	const int matched_count = quest.MatchObjectives(
		vo.stage, type, target_id, matched, Quest::kMaxObjectivesPerStage);
	applyMatched(quest, vo, type, matched, matched_count, amount);
}

void PlayerQuest::applyMatched(const Quest& quest, QuestActiveVO& vo,
	QuestObjectiveType type, const QuestObjectiveSlot* matched, int matched_count,
	int amount)
{
	if (matched_count <= 0)
		return;

	bool changed = false;
	for (int i = 0; i < matched_count; ++i)
	{
		const QuestObjectiveSlot& objective = matched[i];
		const int required = objective.data->count;
		const int before = readProgress(vo, objective.slot);
		if (before >= required)
			continue;

		const int updated = (ProgressModeOf(type) == QuestProgressMode::Highest)
			? std::max(before, amount)
			: before + amount;

		// 목표치를 넘겨 저장하지 않는다. 클라이언트가 "12/10" 을 보게 되고,
		// 진행도 칸이 TINYINT 계열로 좁아지면 넘칠 수도 있다.
		const int clamped = std::min(updated, required);
		if (clamped == before)
			continue;

		writeProgress(vo, objective.slot, clamped);
		changed = true;
	}

	if (!changed)
		return;

	markDirty();
	markSync(quest.GetId());

	const int progress[Quest::kMaxObjectivesPerStage] = {
		vo.progress1, vo.progress2, vo.progress3
	};
	if (quest.IsStageComplete(vo.stage, progress, Quest::kMaxObjectivesPerStage))
		advanceStage(quest, vo);
}

void PlayerQuest::advanceStage(const Quest& quest, QuestActiveVO& vo)
{
	if (vo.stage < quest.StageCount())
	{
		++vo.stage;
		vo.progress1 = 0;
		vo.progress2 = 0;
		vo.progress3 = 0;
		markDirty();

		// 새 스테이지에 레벨 목표가 있으면 지금 레벨을 바로 반영한다.
		// 레벨은 "올랐을 때"만 이벤트가 오므로, 이미 조건을 넘긴 채 스테이지에
		// 들어오면 영영 진행되지 않는다.
		applyProgress(quest, vo, QuestObjectiveType::Level, 0, GetLevel());
		return;
	}

	// 마지막 스테이지까지 끝났다. 완료 NPC 를 찾아가면 보상을 받는다.
	vo.state = static_cast<int>(QuestState::ReadyToComplete);
	markDirty();
	markSync(quest.GetId());
}

void PlayerQuest::tryCompleteByNpc(int npc_id)
{
	if (npc_id == 0)
		return;

	std::vector<int> quest_ids;
	activeQuests_.CollectKeys(quest_ids);

	for (int quest_id : quest_ids)
	{
		const QuestActiveVO* vo = activeQuests_.Find(quest_id);
		if (vo == nullptr || static_cast<QuestState>(vo->state) != QuestState::ReadyToComplete)
			continue;

		const Quest* quest = QuestRegistry::Instance().Get(quest_id);
		if (quest == nullptr || quest->gamedata == nullptr)
			continue;
		if (quest->gamedata->end_npc_id != npc_id)
			continue;

		// 선택 보상이 있는 퀘스트는 플레이어가 무엇을 고를지 정해야 하므로
		// 대화만으로 끝내지 않는다. 클라이언트가 선택을 담아 완료를 요청한다.
		if (!quest->gamedata->rewards.choice_items.empty())
			continue;

		CompleteQuest(quest_id, -1);
	}
}

// ============================================================
// 수락 / 완료 / 포기
// ============================================================

QuestAcceptResult PlayerQuest::AcceptQuest(int quest_id)
{
	Quest* quest = QuestRegistry::Instance().Get(quest_id);
	if (quest == nullptr || quest->gamedata == nullptr)
		return QuestAcceptResult::NotFound;

	if (const QuestActiveVO* existing = activeQuests_.Find(quest_id))
	{
		const QuestState state = static_cast<QuestState>(existing->state);
		if (state == QuestState::InProgress || state == QuestState::ReadyToComplete)
			return QuestAcceptResult::AlreadyActive;

		if (state == QuestState::Cooldown)
		{
			// 이 행에서는 accept_time 이 마지막 완료 시각이다.
			const auto last = existing->accept_time;
			const auto current = now();

			const int cooldown = quest->GetCooldownSeconds();
			if (cooldown > 0 && current < last + std::chrono::seconds(cooldown))
				return QuestAcceptResult::OnCooldown;

			const QuestResetType reset = quest->GetResetType();
			if (reset != QuestResetType::None &&
				!QuestResetBoundaryPassed(reset, last, current))
				return QuestAcceptResult::OnCooldown;
		}
		// Failed 행은 그대로 재도전할 수 있다.
	}

	const QuestAcceptResult result = quest->CanAccept(*this);
	if (result != QuestAcceptResult::Ok)
		return result;

	if (QuestActiveVO* existing = activeQuests_.Modify(quest_id))
	{
		resetActiveRow(*existing);
	}
	else
	{
		QuestActiveVO vo{};
		vo.character_id = characterId_;
		vo.quest_id = quest_id;
		resetActiveRow(vo);
		if (!activeQuests_.Add(quest_id, vo))
			return QuestAcceptResult::AlreadyActive;
	}

	markDirty();
	markSync(quest_id);
	publish(EventQuestAccepted{ characterId_, quest_id });

	// 수락 시점에 이미 만족한 레벨 목표를 반영한다(수락 후 레벨이 오를 때까지
	// 기다리게 두면 조건을 넘긴 플레이어가 진행할 방법이 없다).
	if (QuestActiveVO* vo = activeQuests_.Modify(quest_id))
	{
		applyProgress(*quest, *vo, QuestObjectiveType::Level, 0, GetLevel());

		if (static_cast<QuestState>(vo->state) == QuestState::ReadyToComplete &&
			quest->IsAutoComplete())
			CompleteQuest(quest_id, -1);
	}

	return QuestAcceptResult::Ok;
}

bool PlayerQuest::CompleteQuest(int quest_id, int reward_choice)
{
	Quest* quest = QuestRegistry::Instance().Get(quest_id);
	if (quest == nullptr || quest->gamedata == nullptr)
		return false;

	const QuestActiveVO* current = activeQuests_.Find(quest_id);
	if (current == nullptr ||
		static_cast<QuestState>(current->state) != QuestState::ReadyToComplete)
		return false;

	const auto& choices = quest->gamedata->rewards.choice_items;
	if (!choices.empty() &&
		(reward_choice < 0 || reward_choice >= static_cast<int>(choices.size())))
		return false;

	// 회수를 먼저 한다. 보상 지급이 EventItemAcquired 를 발행하는데, 그 전에 비워야
	// 방금 받은 보상 아이템이 퀘스트 아이템 회수에 휩쓸리지 않는다.
	discardQuestItems(quest_id);

	grantRewards(*quest, reward_choice);
	setCompleted(quest_id);

	if (quest->IsRepeatable())
	{
		// 반복 퀘스트는 행을 남겨 마지막 완료 시각을 기억한다(쿨타임/일일 리셋 기준).
		if (QuestActiveVO* vo = activeQuests_.Modify(quest_id))
		{
			vo->state = static_cast<int>(QuestState::Cooldown);
			vo->stage = 1;
			vo->progress1 = 0;
			vo->progress2 = 0;
			vo->progress3 = 0;
			vo->accept_time = now();
		}
	}
	else
	{
		activeQuests_.Remove(quest_id);
	}

	markDirty();
	pushUnique(syncCompleted_, quest_id);
	markRemovedFromLog(quest_id);
	publish(EventQuestCompleted{ characterId_, quest_id });
	return true;
}

bool PlayerQuest::AbandonQuest(int quest_id)
{
	const Quest* quest = QuestRegistry::Instance().Get(quest_id);
	if (quest == nullptr || !quest->IsAbandonable())
		return false;

	const QuestActiveVO* vo = activeQuests_.Find(quest_id);
	if (vo == nullptr)
		return false;

	const QuestState state = static_cast<QuestState>(vo->state);
	if (state == QuestState::Cooldown)
		return false; // 진행 중이 아니라 다음 수락을 기다리는 행이다

	activeQuests_.Remove(quest_id);
	discardQuestItems(quest_id);
	markDirty();
	markRemovedFromLog(quest_id);
	return true;
}

// ============================================================
// 운영(GM) 조작 — 정상 경로의 조건 검사를 건너뛴다.
// ============================================================

bool PlayerQuest::GmForceAccept(int quest_id)
{
	Quest* quest = QuestRegistry::Instance().Get(quest_id);
	if (quest == nullptr || quest->gamedata == nullptr)
		return false;

	if (QuestActiveVO* existing = activeQuests_.Modify(quest_id))
	{
		resetActiveRow(*existing);
	}
	else
	{
		QuestActiveVO vo{};
		vo.character_id = characterId_;
		vo.quest_id = quest_id;
		resetActiveRow(vo);
		if (!activeQuests_.Add(quest_id, vo))
			return false;
	}

	markDirty();
	markSync(quest_id);
	publish(EventQuestAccepted{ characterId_, quest_id });
	return true;
}

bool PlayerQuest::GmForceComplete(int quest_id, int reward_choice)
{
	Quest* quest = QuestRegistry::Instance().Get(quest_id);
	if (quest == nullptr || quest->gamedata == nullptr)
		return false;

	// 진행 중이 아니면 진행 중으로 만든 뒤 끝낸다(문의 대응에서 흔한 경우다).
	if (activeQuests_.Find(quest_id) == nullptr && !GmForceAccept(quest_id))
		return false;

	QuestActiveVO* vo = activeQuests_.Modify(quest_id);
	if (vo == nullptr)
		return false;

	vo->stage = quest->StageCount();
	vo->state = static_cast<int>(QuestState::ReadyToComplete);
	markDirty();

	// 선택 보상이 있는데 고르지 않았으면 첫 번째로 준다(GM 이 지정하면 그 값을 쓴다).
	const auto& choices = quest->gamedata->rewards.choice_items;
	const int choice = (!choices.empty() && reward_choice < 0) ? 0 : reward_choice;

	return CompleteQuest(quest_id, choice);
}

bool PlayerQuest::GmSetProgress(int quest_id, int stage, int progress1, int progress2, int progress3)
{
	const Quest* quest = QuestRegistry::Instance().Get(quest_id);
	if (quest == nullptr)
		return false;

	if (stage < 1 || stage > quest->StageCount())
		return false;

	QuestActiveVO* vo = activeQuests_.Modify(quest_id);
	if (vo == nullptr)
		return false;

	vo->stage = stage;
	vo->progress1 = progress1;
	vo->progress2 = progress2;
	vo->progress3 = progress3;
	vo->state = static_cast<int>(QuestState::InProgress);
	markDirty();
	markSync(quest_id);

	// 세운 값만으로 스테이지가 끝나 있으면 정상 경로와 같게 넘어가야 한다.
	const int progress[Quest::kMaxObjectivesPerStage] = { progress1, progress2, progress3 };
	if (quest->IsStageComplete(stage, progress, Quest::kMaxObjectivesPerStage))
		advanceStage(*quest, *vo);

	return true;
}

bool PlayerQuest::GmResetQuest(int quest_id)
{
	const bool had_row = activeQuests_.Find(quest_id) != nullptr;

	if (had_row)
	{
		activeQuests_.Remove(quest_id);
		markRemovedFromLog(quest_id);
	}

	discardQuestItems(quest_id);
	clearCompleted(quest_id);
	markDirty();
	return true;
}

void PlayerQuest::discardQuestItems(int quest_id)
{
	if (game_object == nullptr)
		return;
	// 퀘스트 전용 아이템은 그 퀘스트가 끝나면 쓸 데가 없다. 남겨 두면 인벤토리만
	// 채우고, 다시 수락했을 때 목표가 이미 채워진 채로 시작한다.
	if (auto* inventory = game_object->GetComponent<PlayerItem>())
		inventory->RemoveQuestItems(quest_id);
}

// ============================================================
// 시간 정책
// ============================================================

void PlayerQuest::expireTimedOutQuests()
{
	std::vector<int> quest_ids;
	activeQuests_.CollectKeys(quest_ids);

	const auto current = now();

	for (int quest_id : quest_ids)
	{
		const QuestActiveVO* found = activeQuests_.Find(quest_id);
		if (found == nullptr)
			continue;

		const QuestState state = static_cast<QuestState>(found->state);
		if (state != QuestState::InProgress && state != QuestState::ReadyToComplete)
			continue;

		const Quest* quest = QuestRegistry::Instance().Get(quest_id);
		if (quest == nullptr)
			continue;

		const int limit = quest->GetTimeLimitSeconds();
		if (limit <= 0)
			continue;

		if (current < found->accept_time + std::chrono::seconds(limit))
			continue;

		QuestActiveVO* vo = activeQuests_.Modify(quest_id);
		if (vo == nullptr)
			continue;

		vo->state = static_cast<int>(QuestState::Failed);
		markDirty();
		markSync(quest_id);
		publish(EventQuestFailed{ characterId_, quest_id });
	}
}

void PlayerQuest::clearFinishedCooldowns()
{
	std::vector<int> quest_ids;
	activeQuests_.CollectKeys(quest_ids);

	const auto current = now();

	for (int quest_id : quest_ids)
	{
		const QuestActiveVO* vo = activeQuests_.Find(quest_id);
		if (vo == nullptr || static_cast<QuestState>(vo->state) != QuestState::Cooldown)
			continue;

		const Quest* quest = QuestRegistry::Instance().Get(quest_id);
		if (quest == nullptr)
			continue;

		const int cooldown = quest->GetCooldownSeconds();
		if (cooldown > 0 && current < vo->accept_time + std::chrono::seconds(cooldown))
			continue;

		const QuestResetType reset = quest->GetResetType();
		if (reset != QuestResetType::None &&
			!QuestResetBoundaryPassed(reset, vo->accept_time, current))
			continue;

		// 다시 받을 수 있게 된 행은 지운다. 완료 사실은 완료 비트가 기억한다.
		activeQuests_.Remove(quest_id);
		markDirty();
		markRemovedFromLog(quest_id);
	}
}

// ============================================================
// 보상
// ============================================================

void PlayerQuest::grantRewards(const Quest& quest, int reward_choice)
{
	const gamedata::QuestRewards* rewards = quest.GetRewards();
	if (rewards == nullptr)
		return;

	QuestRewardGrant grant;
	grant.quest_id = quest.GetId();
	// 이벤트 기간 배율은 지급 시점에 곱한다(데이터를 다시 배포하지 않아도 되도록).
	grant.exp = QuestPolicy::Instance().ApplyExp(rewards->exp);
	grant.gold = QuestPolicy::Instance().ApplyGold(rewards->gold);

	for (const auto& item : rewards->items)
		grant.items.emplace_back(item.item_id, item.count);

	if (reward_choice >= 0 && reward_choice < static_cast<int>(rewards->choice_items.size()))
	{
		const auto& chosen = rewards->choice_items[static_cast<size_t>(reward_choice)];
		grant.items.emplace_back(chosen.item_id, chosen.count);
	}

	grant.skill_ids = rewards->skill_ids;

	if (game_object != nullptr)
	{
		if (grant.exp > 0)
		{
			if (auto* level = game_object->GetComponent<PlayerLevel>())
				level->GainExp(grant.exp);
		}

		if (grant.gold > 0)
		{
			if (auto* wallet = game_object->GetComponent<PlayerWallet>())
				wallet->AddGold(grant.gold);
		}

		if (auto* inventory = game_object->GetComponent<PlayerItem>())
		{
			// 아이템 지급은 EventItemAcquired 를 발행한다. 다른 퀘스트의 수집 목표가
			// 여기서 함께 올라갈 수 있는데, 실제로 인벤토리에 들어갔으므로 맞는 동작이다.
			for (const auto& [item_id, count] : grant.items)
				inventory->AddItem(item_id, count);
		}

		if (auto* skills = game_object->GetComponent<PlayerSkill>())
		{
			for (int skill_id : grant.skill_ids)
				skills->LearnSkill(skill_id);
		}
	}

	// 무엇을 줬는지는 따로 남긴다 — 클라이언트에 보여줄 보상 목록이자,
	// 컴포넌트가 아직 없는 보상 종류를 잃지 않기 위한 기록이다.
	pendingRewards_.push_back(std::move(grant));
}

std::vector<QuestRewardGrant> PlayerQuest::TakePendingRewards()
{
	return std::move(pendingRewards_);
}

void PlayerQuest::publish(const EventMessage& message)
{
	if (game_object == nullptr)
		return;
	if (auto* broker = game_object->GetComponent<PlayerEventBroker>())
		broker->publish(message);
}

// ============================================================
// 조회
// ============================================================

bool PlayerQuest::IsActive(int quest_id) const
{
	const QuestActiveVO* vo = activeQuests_.Find(quest_id);
	if (vo == nullptr)
		return false;

	const QuestState state = static_cast<QuestState>(vo->state);
	return state == QuestState::InProgress || state == QuestState::ReadyToComplete;
}

bool PlayerQuest::IsCompleted(int quest_id) const
{
	size_t byte_idx = static_cast<size_t>(quest_id) / 8;
	if (byte_idx >= completedBits_.size())
		return false;
	return (completedBits_[byte_idx] >> (quest_id % 8)) & 1;
}

QuestState PlayerQuest::GetState(int quest_id) const
{
	const QuestActiveVO* vo = activeQuests_.Find(quest_id);
	return vo != nullptr ? static_cast<QuestState>(vo->state) : QuestState::InProgress;
}

int PlayerQuest::GetStage(int quest_id) const
{
	const QuestActiveVO* vo = activeQuests_.Find(quest_id);
	return vo != nullptr ? vo->stage : 0;
}

int PlayerQuest::GetProgress(int quest_id, int slot) const
{
	const QuestActiveVO* vo = activeQuests_.Find(quest_id);
	return vo != nullptr ? readProgress(*vo, slot) : 0;
}

const QuestActiveVO* PlayerQuest::GetActiveQuest(int quest_id) const
{
	return activeQuests_.Find(quest_id);
}

int PlayerQuest::GetLevel() const
{
	if (game_object != nullptr)
	{
		if (auto* level = game_object->GetComponent<PlayerLevel>())
			return level->GetLevel();
	}
	return cachedLevel_;
}

bool PlayerQuest::HasItem(int item_id) const
{
	if (game_object == nullptr)
		return false;
	auto* inventory = game_object->GetComponent<PlayerItem>();
	// 인벤토리가 없으면 보유 여부를 확인할 방법이 없다. 확인할 수 없는 조건을
	// 통과시키면 조건이 있으나 마나 하므로 막는다.
	return inventory != nullptr && inventory->Has(item_id);
}

bool PlayerQuest::HasSkill(int skill_id) const
{
	if (game_object == nullptr)
		return false;
	auto* skills = game_object->GetComponent<PlayerSkill>();
	return skills != nullptr && skills->Has(skill_id);
}

// ============================================================
// 저장 / 로드
// ============================================================

void PlayerQuest::Load(std::any data)
{
	const auto& load_data = std::any_cast<const PlayerLoadData&>(data);

	characterId_ = static_cast<int>(load_data.player.id);
	cachedLevel_ = load_data.player.level > 0 ? load_data.player.level : 1;

	// 진행 중 퀘스트를 기존(Persisted) 상태로 로드
	activeQuests_.Clear();
	for (const auto& vo : load_data.quest_actives)
	{
		QuestActiveVO row = vo;
		// stage 컬럼이 없던 시절에 저장된 행은 0 으로 들어온다. 1 스테이지로 본다.
		if (row.stage < 1)
			row.stage = 1;
		activeQuests_.AddPersisted(row.quest_id, row);
	}

	// 완료 퀘스트 플래그를 DB 의 raw 바이트 문자열에서 복원
	const std::string& flags = load_data.quest_state.flags;
	completedBits_.assign(flags.begin(), flags.end());

	// character_id 가 0 이면 행이 아직 없음 -> 첫 저장 시 INSERT
	questState_.SetPersisted(load_data.quest_state.character_id != 0);
}

void PlayerQuest::Save(std::any data)
{
	auto* save_data = std::any_cast<PlayerSaveData*>(data);

	// 변경된 진행 퀘스트만 INSERT/UPDATE/DELETE 레코드로
	if (activeQuests_.HasPendingChanges())
		save_data->quest_actives = activeQuests_.Flush();

	// 완료 플래그가 변경되었거나 신규 행이면 quest_state 레코드 생성
	if (auto record = questState_.Flush(buildStateVO()))
		save_data->quest_state = std::move(record);
}

void PlayerQuest::setCompleted(int quest_id)
{
	size_t byte_idx = static_cast<size_t>(quest_id) / 8;
	if (byte_idx >= completedBits_.size())
		completedBits_.resize(byte_idx + 1, 0);
	completedBits_[byte_idx] |= static_cast<uint8_t>(1 << (quest_id % 8));
	questState_.MarkDirty();
	markDirty();
}

void PlayerQuest::clearCompleted(int quest_id)
{
	const size_t byte_idx = static_cast<size_t>(quest_id) / 8;
	if (byte_idx >= completedBits_.size())
		return;

	const uint8_t mask = static_cast<uint8_t>(1 << (quest_id % 8));
	if ((completedBits_[byte_idx] & mask) == 0)
		return;

	completedBits_[byte_idx] &= static_cast<uint8_t>(~mask);
	questState_.MarkDirty();
	markDirty();
}

void PlayerQuest::resetActiveRow(QuestActiveVO& vo)
{
	vo.character_id = characterId_;
	vo.state = static_cast<int>(QuestState::InProgress);
	vo.stage = 1;
	vo.progress1 = 0;
	vo.progress2 = 0;
	vo.progress3 = 0;
	vo.accept_time = now();
}

QuestStateVO PlayerQuest::buildStateVO() const
{
	QuestStateVO vo;
	vo.character_id = characterId_;
	vo.flags.assign(completedBits_.begin(), completedBits_.end());
	return vo;
}

void PlayerQuest::pushUnique(std::vector<int>& list, int quest_id)
{
	if (std::find(list.begin(), list.end(), quest_id) == list.end())
		list.push_back(quest_id);
}

void PlayerQuest::markRemovedFromLog(int quest_id)
{
	pushUnique(syncRemoved_, quest_id);

	// 사라진 퀘스트를 "바뀐 퀘스트"로도 보내면 클라가 지운 뒤 다시 만든다.
	syncChanged_.erase(
		std::remove(syncChanged_.begin(), syncChanged_.end(), quest_id),
		syncChanged_.end());
}

bool PlayerQuest::HasPendingSync() const
{
	return !syncChanged_.empty() || !syncRemoved_.empty() || !syncCompleted_.empty();
}

void PlayerQuest::DrainSync(
	std::vector<int>& changed, std::vector<int>& removed, std::vector<int>& completed)
{
	changed = std::move(syncChanged_);
	removed = std::move(syncRemoved_);
	completed = std::move(syncCompleted_);
	syncChanged_.clear();
	syncRemoved_.clear();
	syncCompleted_.clear();
}

void PlayerQuest::MarkAllForSync()
{
	std::vector<int> quest_ids;
	activeQuests_.CollectKeys(quest_ids);
	for (int quest_id : quest_ids)
	{
		if (IsActive(quest_id))
			markSync(quest_id);
	}
}

int PlayerQuest::readProgress(const QuestActiveVO& vo, int slot)
{
	switch (slot)
	{
	case 0: return vo.progress1;
	case 1: return vo.progress2;
	case 2: return vo.progress3;
	default: return 0;
	}
}

void PlayerQuest::writeProgress(QuestActiveVO& vo, int slot, int value)
{
	switch (slot)
	{
	case 0: vo.progress1 = value; break;
	case 1: vo.progress2 = value; break;
	case 2: vo.progress3 = value; break;
	default: break;
	}
}
