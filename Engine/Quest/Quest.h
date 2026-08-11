#pragma once
#include <chrono>
#include <vector>
#include <string>
#include "QuestObjective.h"

namespace gamedata
{
	struct Quest;
	struct QuestStage;
	struct QuestStageObjective;
	struct QuestRewards;
}

// 수락 거절 사유. 클라이언트에 그대로 돌려줄 수 있도록 실패 원인을 구분한다.
enum class QuestAcceptResult
{
	Ok = 0,
	NotFound,           // 데이터에 없는 퀘스트 id
	AlreadyActive,      // 이미 진행 중
	AlreadyCompleted,   // 이미 완료했고 반복 불가
	OnCooldown,         // 반복 가능하지만 아직 쿨타임
	Disabled,           // 운영이 내려둔 퀘스트(데이터의 disabled)
	LevelTooLow,
	LevelTooHigh,
	PrerequisiteQuest,  // 선행 퀘스트 미완료
	BlockedQuest,       // 완료하면 안 되는 퀘스트를 이미 완료함(분기)
	MissingItem,
	MissingSkill,
};

// 진행 중 퀘스트의 상태. quest_active.state 컬럼에 그대로 저장된다.
// 값을 바꾸면 저장된 행의 의미가 바뀌므로 새 값은 뒤에 붙인다.
enum class QuestState : int
{
	InProgress = 0,
	ReadyToComplete = 1,  // 모든 스테이지 완료, 완료 NPC 를 찾아가면 끝
	Failed = 2,           // 제한 시간 초과 등

	// 반복 퀘스트를 완료한 뒤 다음 수락까지 기다리는 상태. 이 행에서는
	// accept_time 이 "마지막 완료 시각"이다(쿨타임/일일 리셋의 기준).
	// 반복 불가 퀘스트는 완료와 동시에 행이 지워지므로 이 상태를 갖지 않는다.
	Cooldown = 3,
};

enum class QuestResetType
{
	None = 0,
	Daily,
	Weekly,
};

// 퀘스트 조건을 판정하는 데 필요한 플레이어 상태 조회 창구.
// Quest 정의는 플레이어를 직접 모르고, 이 인터페이스로만 물어본다.
class IQuestOwner
{
public:
	virtual ~IQuestOwner() = default;

	virtual int GetLevel() const = 0;
	virtual bool IsQuestActive(int quest_id) const = 0;
	virtual bool IsQuestCompleted(int quest_id) const = 0;
	virtual bool HasItem(int item_id) const = 0;
	virtual bool HasSkill(int skill_id) const = 0;
};

// 한 스테이지 안에서 이벤트에 반응해야 하는 목표의 위치.
// slot 은 스테이지 안 목표의 0-based 순서이며, 그대로 quest_active.progress1~3 의
// 칸 번호가 된다.
struct QuestObjectiveSlot
{
	int slot = 0;
	const gamedata::QuestStageObjective* data = nullptr;
};

// 퀘스트 "정의". 무상태이며 QuestRegistry 가 id 당 하나만 만들어 모든 플레이어가 공유한다.
// 플레이어별 진행 상태는 PlayerQuest 가 들고 있다.
class Quest
{
public:
	// quest_active 의 진행도 칸 수. 한 스테이지의 목표는 이 수를 넘을 수 없다
	// (GameDataFlow 의 데이터 검증이 같은 값으로 막는다).
	static constexpr int kMaxObjectivesPerStage = 3;

	const gamedata::Quest* gamedata = nullptr; // Pointer to gamedata for Quest information

	virtual ~Quest() = default;

	int GetId() const;
	int StageCount() const;

	// step 은 1-based(데이터의 stages[].step 과 같다). 범위를 벗어나면 nullptr.
	const gamedata::QuestStage* GetStage(int step) const;
	const gamedata::QuestRewards* GetRewards() const;

	// ---- 타입별 정책. 기본은 데이터를 그대로 따르고, 파생 클래스가 필요한 것만 덮어쓴다.
	virtual bool IsAbandonable() const { return true; }
	virtual bool IsRepeatable() const;
	virtual int GetTimeLimitSeconds() const;
	virtual int GetCooldownSeconds() const;
	virtual QuestResetType GetResetType() const;

	bool IsAutoComplete() const;

	// 운영이 내려둔 퀘스트인가(데이터의 disabled). 내려도 이미 진행 중인 퀘스트는
	// 그대로 끝낼 수 있고, 새로 받는 것만 막는다 — 진행분을 빼앗지 않기 위해서다.
	bool IsEnabled() const;

	// 수락 가능 여부. 레벨/선행 퀘스트/보유 아이템·스킬을 모두 확인한다.
	QuestAcceptResult CanAccept(const IQuestOwner& owner) const;

	// 이벤트(목표 종류 + 대상)에 반응해야 하는 슬롯을 out 에 채우고 개수를 돌려준다.
	// 목표의 target_id 가 0 이면 대상을 가리지 않는 와일드카드다(예: 아무 몬스터나 10마리).
	// 처치마다 도는 경로라 할당하지 않는다 — 호출자가 스택 배열을 넘긴다.
	int MatchObjectives(int step, QuestObjectiveType type, int target_id,
		QuestObjectiveSlot* out, int out_size) const;

	// 위와 같지만 결과를 벡터로 돌려준다(도구/테스트용 편의 오버로드).
	std::vector<QuestObjectiveSlot> MatchObjectives(
		int step, QuestObjectiveType type, int target_id) const;

	// 주어진 진행도로 이 스테이지가 끝났는지 판정한다.
	// logic 이 "or" 면 목표 하나만 채워도 되고, 그 외("and")는 전부 채워야 한다.
	bool IsStageComplete(int step, const int* progress, int progress_size) const;
};

// last 와 now 사이에 리셋 경계(일일 = 로컬 자정, 주간 = 로컬 월요일 자정)를 지났는지.
// 리셋 주기가 없으면 항상 false — 쿨타임만으로 재수락을 판정한다.
bool QuestResetBoundaryPassed(
	QuestResetType type,
	std::chrono::system_clock::time_point last,
	std::chrono::system_clock::time_point now);
