#pragma once

#include "MonsterBTRunner.h"

namespace BT
{
	class Tree;
}
class Monster;

// behaviortree_cpp 백엔드. 노드 로직은 MonsterBTNodes.h 를 공유하고,
// 트리 구조는 GameData/Monster.xml 이 정한다. BT 디버그 뷰어를 지원한다.
class MonsterBT
{
public:
	static BT::Tree* createTree(Monster* monster);

	// MonsterBTRunner 가 쓰는 전략 테이블(생성/틱/해제).
	static const MonsterBTRunner::Ops& Ops();
};
