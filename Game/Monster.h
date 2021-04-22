#pragma once

namespace BT
{
	class BehaviorTree;
}
class Monster
{
private:
	BT::BehaviorTree * bt_;

public:
	Monster();
	void Update();
};

