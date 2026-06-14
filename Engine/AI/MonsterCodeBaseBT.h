#pragma once

namespace BT
{
	class BehaviorTree;
}
class Monster;

class MonsterCodeBaseBT
{
public:
	static BT::BehaviorTree* createTree(Monster* monster);
};