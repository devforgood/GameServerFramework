#pragma once
#include "Quest.h"

// 메인 스토리 퀘스트. 체인의 뼈대라 중간에 버리면 뒤 챕터가 통째로 막히므로
// 포기할 수 없다.
class MainQuest : public Quest
{
public:
	bool IsAbandonable() const override { return false; }
};
