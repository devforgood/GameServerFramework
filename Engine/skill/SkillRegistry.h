#pragma once
#include <memory>
#include <mutex>
#include <unordered_map>

class Skill;

// 스킬 정의(무상태 Skill 인스턴스)의 공유 저장소.
// SkillFactory 로 스킬 id 당 1개만 생성해 캐싱하고, 모든 캐릭터의 SkillSet 이 공유한다.
// (기존에는 캐릭터마다 전체 스킬 테이블을 인스턴스화했다: 캐릭터 수 × 스킬 수만큼의 객체)
class SkillRegistry
{
public:
	static SkillRegistry& Instance();

	// 캐시에 없으면 SkillFactory 로 생성한다. 데이터에 없는 id 면 nullptr.
	// 리소스가 다시 로드돼 gamedata 포인터가 갈렸으면 여기서 자동으로 다시 묶는다.
	Skill* Get(int skillId);

	// 캐시를 비운다. 이제는 Get 이 gamedata 를 스스로 다시 묶으므로 리로드 후에
	// 반드시 부를 필요는 없다. code_name 이 바뀌어 스킬 클래스 자체를 다시 골라야 할 때만
	// 의미가 있다(같은 id 를 다른 구현으로 바꾼 경우).
	void Clear();

private:
	SkillRegistry() = default;

	std::mutex mutex_;
	std::unordered_map<int, std::unique_ptr<Skill>> skills_;
};
