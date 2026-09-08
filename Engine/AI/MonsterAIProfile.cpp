#include "MonsterAIProfile.h"

// 표와 조회 함수는 헤더에 있다(AI 의 안쪽 루프가 부르므로 인라인되어야 한다).
// 여기 남는 것은 문자열을 다루는 두 함수뿐이고, 둘 다 스폰/로그 경로에서만 불린다.

monsterai::AIProfile monsterai::ParseProfile(std::string_view name)
{
	if (name == "passive")
		return AIProfile::Passive;
	if (name == "boss")
		return AIProfile::Boss;
	return AIProfile::Aggressive;
}

const char* monsterai::ProfileName(AIProfile profile)
{
	switch (profile)
	{
	case AIProfile::Passive: return "passive";
	case AIProfile::Boss:    return "boss";
	default:                 return "aggressive";
	}
}
