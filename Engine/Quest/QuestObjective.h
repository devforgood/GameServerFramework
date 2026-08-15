#pragma once
#include <string_view>

// 퀘스트 목표의 종류.
// 데이터(quest.json 의 stages[].objectives[].type)와 1:1로 대응하며,
// 값을 늘릴 때는 GameDataFlow/validate_data.py 의 OBJECTIVE_TARGET_TABLE 도 같이 늘려야
// 디자이너가 적은 오타를 코드 생성 단계에서 막을 수 있다.
enum class QuestObjectiveType
{
	None = 0,
	Kill,      // target_id = monster.json 의 몬스터 종류
	Collect,   // target_id = item.json 의 아이템
	UseItem,   // target_id = item.json 의 아이템
	UseSkill,  // target_id = skill.json 의 스킬
	Reach,     // target_id = Map.json 의 맵
	Talk,      // target_id = NPC (전용 테이블이 아직 없다)
	Interact,  // target_id = Map.json 의 오브젝트
	Level,     // target_id 를 쓰지 않고 count 가 도달 목표 레벨이다

	// target_id = npc.json 의 NPC. 목적지는 그 NPC 의 escort_dest_id 가 정한다
	// (목표에 또 적으면 둘이 어긋날 수 있고, NPC 는 어차피 목적지를 하나만 갖는다).
	Escort,

	// target_id = npc.json 의 NPC, count = 지켜야 하는 시간(초).
	// 대상이 죽으면 진행도와 무관하게 퀘스트가 실패한다.
	Protect,
};

// 진행도를 쌓는 방식. 대부분은 이벤트마다 더하지만, 레벨처럼 "현재 값"이
// 그대로 진행도인 목표는 더하면 안 되고 가장 높은 값을 남겨야 한다
// (레벨 3에서 5로 두 번 오르면 3+5=8 이 아니라 5 다).
enum class QuestProgressMode
{
	Accumulate,
	Highest,
};

inline QuestObjectiveType ParseObjectiveType(std::string_view type)
{
	if (type == "kill")      return QuestObjectiveType::Kill;
	if (type == "collect")   return QuestObjectiveType::Collect;
	if (type == "use_item")  return QuestObjectiveType::UseItem;
	if (type == "use_skill") return QuestObjectiveType::UseSkill;
	if (type == "reach")     return QuestObjectiveType::Reach;
	if (type == "talk")      return QuestObjectiveType::Talk;
	if (type == "interact")  return QuestObjectiveType::Interact;
	if (type == "level")     return QuestObjectiveType::Level;
	if (type == "escort")    return QuestObjectiveType::Escort;
	if (type == "protect")   return QuestObjectiveType::Protect;
	return QuestObjectiveType::None;
}

// 대상 NPC 가 죽으면 실패하는 목표인가. 지키던 대상이 죽었는데 되살아난 NPC 로 이어서
// 하게 두면 "지켰다"는 말이 성립하지 않는다.
inline bool ObjectiveFailsOnNpcDeath(QuestObjectiveType type)
{
	return type == QuestObjectiveType::Escort || type == QuestObjectiveType::Protect;
}

inline QuestProgressMode ProgressModeOf(QuestObjectiveType type)
{
	return type == QuestObjectiveType::Level
		? QuestProgressMode::Highest
		: QuestProgressMode::Accumulate;
}
