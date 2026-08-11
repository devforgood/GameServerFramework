#include "QuestRegistry.h"
#include "Quest.h"
#include "QuestFactory.h"
#include "GameData/ResourceLoader.h"

QuestRegistry& QuestRegistry::Instance()
{
	static QuestRegistry instance;
	return instance;
}

Quest* QuestRegistry::Get(int questId)
{
	std::lock_guard<std::mutex> lock(mutex_);

	// 캐시된 Quest 는 gamedata 를 ResourceLoader 저장소를 가리키는 포인터로 들고 있다.
	// 리소스를 다시 로드하면 그 저장소가 통째로 교체되므로 죽은 포인터를 읽지 않도록
	// 여기서 다시 묶는다(SkillRegistry 와 같은 이유).
	const gamedata::Quest* current = ResourceLoader::Instance().GetQuest(questId);

	auto itr = quests_.find(questId);
	if (itr != quests_.end())
	{
		Quest* cached = itr->second.get();
		if (cached != nullptr && cached->gamedata != current)
			cached->gamedata = current;
		return cached;
	}

	if (current == nullptr)
		return nullptr;

	Quest* quest = QuestFactory::Create(questId);
	if (quest == nullptr)
		return nullptr;

	quests_[questId] = std::unique_ptr<Quest>(quest);
	return quest;
}

void QuestRegistry::Clear()
{
	std::lock_guard<std::mutex> lock(mutex_);
	quests_.clear();
}
