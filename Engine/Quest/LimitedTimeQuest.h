#pragma once
#include "Quest.h"

// 제한 시간 퀘스트. 수락 시각부터 time.limit_seconds 안에 끝내야 하며,
// 넘기면 PlayerQuest 가 Failed 로 돌린다.
class LimitedTimeQuest : public Quest
{
public:
	// 데이터에 제한 시간이 없으면 이 클래스를 고른 의미가 없다. 검증이 막지만,
	// 검증을 건너뛴 데이터로도 무제한이 되지는 않도록 최소값을 둔다.
	int GetTimeLimitSeconds() const override
	{
		const int limit = Quest::GetTimeLimitSeconds();
		return limit > 0 ? limit : kDefaultLimitSeconds;
	}

private:
	static constexpr int kDefaultLimitSeconds = 1800;
};
