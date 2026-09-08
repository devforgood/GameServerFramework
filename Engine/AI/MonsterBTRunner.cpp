#include "MonsterBTRunner.h"

#include "Monster.h"
#include "MonsterAISystem.h"
#include "MonsterBT.h"
#include "MonsterCodeBaseBT.h"

void MonsterBTRunner::Create(Monster* monster, Backend backend)
{
	Destroy(); // 재생성 시 이전 백엔드를 흘리지 않는다(ECS 는 여기서 슬롯을 반납한다).

	// 백엔드 → 전략 테이블 변환은 여기 한 곳뿐이다.
	// 백엔드가 늘어나면 이 스위치에 한 줄 추가한다.
	switch (backend)
	{
	case Backend::BTCpp:
		ops_ = &MonsterBT::Ops();
		break;
	case Backend::CodeBase:
		ops_ = &MonsterCodeBaseBT::Ops();
		break;
	case Backend::Ecs:
	default:
		ops_ = &MonsterEcsBT::Ops();
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
