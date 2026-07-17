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
	Skill* Get(int skillId);

	// 리소스 리로드/테스트용: 캐시를 비운다. (gamedata 포인터가 갈리므로 리로드 후 필수)
	void Clear();

private:
	SkillRegistry() = default;

	std::mutex mutex_;
	std::unordered_map<int, std::unique_ptr<Skill>> skills_;
};
