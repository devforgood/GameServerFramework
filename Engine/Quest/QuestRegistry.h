#pragma once
#include <memory>
#include <mutex>
#include <unordered_map>

class Quest;

// 퀘스트 정의(무상태 Quest 인스턴스)의 공유 저장소.
// QuestFactory 로 퀘스트 id 당 1개만 생성해 캐싱하고, 모든 플레이어의 PlayerQuest 가
// 이것을 읽기만 한다(SkillRegistry 와 같은 구조).
class QuestRegistry
{
public:
	static QuestRegistry& Instance();

	// 캐시에 없으면 QuestFactory 로 생성한다. 데이터에 없는 id 면 nullptr.
	// 리소스가 다시 로드돼 gamedata 포인터가 갈렸으면 여기서 자동으로 다시 묶는다.
	Quest* Get(int questId);

	// 캐시를 비운다. code_name 이 바뀌어 퀘스트 클래스 자체를 다시 골라야 할 때만
	// 의미가 있다(Get 이 gamedata 는 스스로 다시 묶는다).
	void Clear();

private:
	QuestRegistry() = default;

	std::mutex mutex_;
	std::unordered_map<int, std::unique_ptr<Quest>> quests_;
};
