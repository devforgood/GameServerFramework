#include "Quest.h"
#include "GameData/gamedata.h"
#include <ctime>

namespace
{
// 리셋 경계를 세는 기준 번호. 두 시각의 번호가 다르면 그 사이에 경계를 지난 것이다.
// 일일은 로컬 날짜(연/일), 주간은 로컬 기준 그 날짜가 속한 주의 월요일 날짜로 센다.
long long LocalResetIndex(QuestResetType type, std::chrono::system_clock::time_point tp)
{
	const std::time_t raw = std::chrono::system_clock::to_time_t(tp);
	std::tm local{};
#if defined(_WIN32)
	localtime_s(&local, &raw);
#else
	localtime_r(&raw, &local);
#endif

	// 로컬 자정 기준 일 번호. mktime 이 서머타임 보정까지 해 준다.
	std::tm midnight = local;
	midnight.tm_hour = 0;
	midnight.tm_min = 0;
	midnight.tm_sec = 0;
	midnight.tm_isdst = -1;

	if (type == QuestResetType::Weekly)
	{
		// tm_wday 는 일요일이 0 이다. 주의 시작을 월요일로 맞춘다.
		const int days_since_monday = (local.tm_wday + 6) % 7;
		midnight.tm_mday -= days_since_monday;
	}

	const std::time_t normalized = std::mktime(&midnight);
	if (normalized == static_cast<std::time_t>(-1))
		return 0;

	return static_cast<long long>(normalized);
}
}

int Quest::GetId() const
{
	return gamedata != nullptr ? gamedata->id : 0;
}

int Quest::StageCount() const
{
	return gamedata != nullptr ? static_cast<int>(gamedata->stages.size()) : 0;
}

const gamedata::QuestStage* Quest::GetStage(int step) const
{
	if (gamedata == nullptr || step < 1 || step > StageCount())
		return nullptr;
	return &gamedata->stages[static_cast<size_t>(step) - 1];
}

const gamedata::QuestRewards* Quest::GetRewards() const
{
	return gamedata != nullptr ? &gamedata->rewards : nullptr;
}

bool Quest::IsRepeatable() const
{
	return gamedata != nullptr && gamedata->time.repeatable;
}

int Quest::GetTimeLimitSeconds() const
{
	return gamedata != nullptr ? gamedata->time.limit_seconds : 0;
}

int Quest::GetCooldownSeconds() const
{
	return gamedata != nullptr ? gamedata->time.cooldown_seconds : 0;
}

QuestResetType Quest::GetResetType() const
{
	if (gamedata == nullptr)
		return QuestResetType::None;
	if (gamedata->time.reset_type == "daily")
		return QuestResetType::Daily;
	if (gamedata->time.reset_type == "weekly")
		return QuestResetType::Weekly;
	return QuestResetType::None;
}

bool Quest::IsAutoComplete() const
{
	return gamedata != nullptr && gamedata->auto_complete;
}

bool Quest::IsEnabled() const
{
	return gamedata != nullptr && !gamedata->disabled;
}

QuestAcceptResult Quest::CanAccept(const IQuestOwner& owner) const
{
	if (gamedata == nullptr)
		return QuestAcceptResult::NotFound;

	const int quest_id = gamedata->id;

	if (owner.IsQuestActive(quest_id))
		return QuestAcceptResult::AlreadyActive;

	if (!IsEnabled())
		return QuestAcceptResult::Disabled;

	// 반복 퀘스트만 완료 후 다시 받을 수 있다.
	if (owner.IsQuestCompleted(quest_id) && !IsRepeatable())
		return QuestAcceptResult::AlreadyCompleted;

	const int level = owner.GetLevel();
	if (gamedata->min_level > 0 && level < gamedata->min_level)
		return QuestAcceptResult::LevelTooLow;
	// max_level 0 은 상한 없음.
	if (gamedata->max_level > 0 && level > gamedata->max_level)
		return QuestAcceptResult::LevelTooHigh;

	const auto& prerequisites = gamedata->prerequisites;

	for (int required : prerequisites.completed_quest_ids)
	{
		if (!owner.IsQuestCompleted(required))
			return QuestAcceptResult::PrerequisiteQuest;
	}

	// 갈림길이 다시 합쳐지는 자리. 어느 가지를 탔든 하나만 끝냈으면 된다 —
	// completed_quest_ids 는 전부 만족해야 하므로 "둘 중 하나"를 적을 수 없다.
	const auto& any_of = prerequisites.completed_any_quest_ids;
	if (!any_of.empty())
	{
		bool satisfied = false;
		for (int required : any_of)
		{
			if (owner.IsQuestCompleted(required))
			{
				satisfied = true;
				break;
			}
		}

		if (!satisfied)
			return QuestAcceptResult::PrerequisiteQuest;
	}

	// 분기 퀘스트: 반대편 가지를 이미 골랐으면 이쪽은 받을 수 없다.
	for (int blocked : prerequisites.blocked_quest_ids)
	{
		if (owner.IsQuestCompleted(blocked))
			return QuestAcceptResult::BlockedQuest;
	}

	for (int item_id : prerequisites.item_ids)
	{
		if (!owner.HasItem(item_id))
			return QuestAcceptResult::MissingItem;
	}

	for (int skill_id : prerequisites.skill_ids)
	{
		if (!owner.HasSkill(skill_id))
			return QuestAcceptResult::MissingSkill;
	}

	return QuestAcceptResult::Ok;
}

int Quest::MatchObjectives(int step, QuestObjectiveType type, int target_id,
	QuestObjectiveSlot* out, int out_size) const
{
	const gamedata::QuestStage* stage = GetStage(step);
	if (stage == nullptr || out == nullptr || type == QuestObjectiveType::None)
		return 0;

	int matched = 0;
	const int count = static_cast<int>(stage->objectives.size());
	for (int slot = 0; slot < count && slot < kMaxObjectivesPerStage; ++slot)
	{
		if (matched >= out_size)
			break;

		const gamedata::QuestStageObjective& objective = stage->objectives[slot];
		if (ParseObjectiveType(objective.type) != type)
			continue;

		// 레벨 목표는 대상이 없다. 그 외에는 target_id 0 을 와일드카드로 본다.
		if (type != QuestObjectiveType::Level &&
			objective.target_id != 0 &&
			objective.target_id != target_id)
			continue;

		out[matched++] = QuestObjectiveSlot{ slot, &objective };
	}

	return matched;
}

std::vector<QuestObjectiveSlot> Quest::MatchObjectives(
	int step, QuestObjectiveType type, int target_id) const
{
	QuestObjectiveSlot buffer[kMaxObjectivesPerStage];
	const int count = MatchObjectives(step, type, target_id, buffer, kMaxObjectivesPerStage);
	return std::vector<QuestObjectiveSlot>(buffer, buffer + count);
}

bool Quest::IsStageComplete(int step, const int* progress, int progress_size) const
{
	const gamedata::QuestStage* stage = GetStage(step);
	if (stage == nullptr || progress == nullptr)
		return false;

	const int count = static_cast<int>(stage->objectives.size());
	if (count == 0)
		return false;

	const bool any_of = (stage->logic == "or");

	for (int slot = 0; slot < count && slot < kMaxObjectivesPerStage; ++slot)
	{
		// 진행도 칸이 모자라면 채울 방법이 없으므로 미완료로 본다.
		// (데이터 검증이 막지만, 검증을 건너뛴 데이터로도 조용히 완료되지는 않게 한다)
		const bool done = slot < progress_size &&
			progress[slot] >= stage->objectives[slot].count;

		if (any_of && done)
			return true;
		if (!any_of && !done)
			return false;
	}

	return !any_of;
}

bool QuestResetBoundaryPassed(
	QuestResetType type,
	std::chrono::system_clock::time_point last,
	std::chrono::system_clock::time_point now)
{
	if (type == QuestResetType::None)
		return false;
	if (now <= last)
		return false;

	return LocalResetIndex(type, now) != LocalResetIndex(type, last);
}
