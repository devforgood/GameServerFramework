#include "PlayerQuest.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "PlayerEventBroker.h"
#include "PlayerLevel.h"
#include "GameObject.h"
#include "QuestRegistry.h"
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
	markDirty();
	return true;
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
	grant.exp = rewards->exp;
	grant.gold = rewards->gold;

	for (const auto& item : rewards->items)
		grant.items.emplace_back(item.item_id, item.count);

	if (reward_choice >= 0 && reward_choice < static_cast<int>(rewards->choice_items.size()))
	{
		const auto& chosen = rewards->choice_items[static_cast<size_t>(reward_choice)];
		grant.items.emplace_back(chosen.item_id, chosen.count);
	}

	grant.skill_ids = rewards->skill_ids;

	// 경험치는 받아 줄 컴포넌트가 이미 있다. 나머지는 대기열에 남긴다.
	if (grant.exp > 0 && game_object != nullptr)
	{
		if (auto* level = game_object->GetComponent<PlayerLevel>())
			level->GainExp(grant.exp);
	}

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
	// 인벤토리 컴포넌트가 아직 보유 조회를 제공하지 않는다. 확인할 수 없는 조건을
	// 통과시키면 조건이 있으나 마나 하므로, 생길 때까지는 막는다.
	return false;
}

bool PlayerQuest::HasSkill(int skill_id) const
{
	// HasItem 과 같은 이유. PlayerSkill 이 보유 스킬 조회를 제공하면 여기서 넘긴다.
	return false;
}

// ============================================================
// 저장 / 로드
// ============================================================

void PlayerQuest::Load(std::any data)
{
	const auto& load_data = std::any_cast<const PlayerLoadData&>(data);

	characterId_ = load_data.player.id;
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
