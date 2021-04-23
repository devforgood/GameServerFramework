#pragma once

namespace BT
{
	class BehaviorTree;
}
class Monster
{
private:
	int agent_id_;
	BT::BehaviorTree * bt_;

public:
	Monster(int agent_id);
	void Update();
};

