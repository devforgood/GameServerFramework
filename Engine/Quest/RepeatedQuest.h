#pragma once
#include "Quest.h"

// 일일/주간 반복 퀘스트. 리셋 주기는 데이터(time.reset_type)가 정하지만,
// 반복 가능 여부는 클래스가 보장한다 — 반복 퀘스트인데 데이터에 repeatable 을
// 빠뜨리면 첫 완료 후 영영 잠긴다.
class RepeatedQuest : public Quest
{
public:
	bool IsRepeatable() const override { return true; }
};
