#pragma once

#include "MonsterBTRunner.h"

namespace BT
{
	class BehaviorTree;
}
class Monster;

// 인하우스 BT(../BehaviorTree) 백엔드. 노드 로직은 MonsterBTNodes.h 를 공유하고,
// 트리 구조는 createTree 의 빌더 코드가 정한다. 틱 비용이 낮아 기본 백엔드다.
class MonsterCodeBaseBT
{
public:
	static BT::BehaviorTree* createTree(Monster* monster);

	// MonsterBTRunner 가 쓰는 전략 테이블(생성/틱/해제).
	static const MonsterBTRunner::Ops& Ops();
};
