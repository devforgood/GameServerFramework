#include "MonsterBTRunner.h"

#include "Monster.h"
#include "MonsterBT.h"
#include "MonsterCodeBaseBT.h"

void MonsterBTRunner::Create(Monster* monster)
{
	Destroy(); // 재생성 시 이전 트리를 흘리지 않는다.

	// 백엔드 선택은 여기 한 곳뿐이다. 백엔드가 늘어나면 이 스위치에 한 줄 추가한다.
	switch (Monster::btBackend_)
	{
	case Monster::BTBackend::BTCpp:
		ops_ = &MonsterBT::Ops();
		break;
	case Monster::BTBackend::CodeBase:
	default:
		ops_ = &MonsterCodeBaseBT::Ops();
		break;
	}

	tree_ = ops_->create(monster);
}

void MonsterBTRunner::Destroy()
{
	if (tree_ != nullptr)
	{
		ops_->destroy(tree_);
		tree_ = nullptr;
	}
	ops_ = nullptr;
}
