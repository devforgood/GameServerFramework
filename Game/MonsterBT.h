#pragma once



namespace BT
{
	class Tree;
}
class Monster;

class MonsterBT
{
public:
	static BT::Tree* createTree(Monster* monster);
};