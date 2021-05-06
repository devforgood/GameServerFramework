#include "Monster.h"
#include "../BehaviorTree/BehaviorTree.h"
#include <random>
#include <functional>
#include "World.h"

extern std::_Binder<std::_Unforced, std::uniform_int_distribution<>&, std::default_random_engine&> dice;


class Condition_DetectEnemy : public BT::Condition
{
private:
	Monster* monster_;

public:
	static Behavior* Create(bool InIsNegation, Monster * monster) { return new Condition_DetectEnemy(InIsNegation, monster); }
	virtual std::string Name() override { return "Condition_DetectEnemy"; }

protected:
	Condition_DetectEnemy(bool InIsNegation, Monster* monster)
		: Condition(InIsNegation), monster_(monster)
	{

	}

	virtual ~Condition_DetectEnemy() {}
	virtual BT::EStatus Update() override
	{
		monster_->target_agent_id_ = monster_->world()->DetectEnemy(monster_->agent_id());
		if (monster_->target_agent_id_ >=0)
		{
			monster_->SetState(syncnet::AIState_Detect);
			std::cout << "See enemy!" << std::endl;
			return !IsNegation ? BT::EStatus::Success : BT::EStatus::Failure;
		}
		else
		{
			monster_->SetState(syncnet::AIState_Patrol);
			std::cout << "Not see enemy" << std::endl;
			return !IsNegation ? BT::EStatus::Failure : BT::EStatus::Success;
		}
	}
};

class Action_Chase :public BT::Action
{
private:
	Monster* monster_;

public:
	static Behavior* Create(Monster* monster) { return new Action_Chase(monster); }
	virtual std::string Name() override { return "Action_Follow"; }

protected:
	Action_Chase(Monster* monster) : monster_(monster){}
	virtual ~Action_Chase() {}
	virtual BT::EStatus Update() override
	{
		monster_->world()->map()->setMoveTarget(monster_->world()->map()->getPos(monster_->target_agent_id_), false, monster_->agent_id());

		return BT::EStatus::Success;
	}
};

class Action_Patrol :public BT::Action
{
private:
	Monster* monster_;

public:
	static Behavior* Create(Monster* monster) { return new Action_Patrol(monster); }
	virtual std::string Name() override { return "Action_Patrol"; }

protected:
	Action_Patrol(Monster* monster) : monster_(monster) {}
	virtual ~Action_Patrol() {}
	virtual BT::EStatus Update() override
	{
		monster_->world()->map()->patrol(monster_->agent_id());
		return BT::EStatus::Success;
	}
};

// todo : "Attack" , "Flee"


Monster::Monster(int agent_id, World* world)
	: GameObject(agent_id, world), bt_(nullptr)
{
	BT::BehaviorTreeBuilder* Builder = new BT::BehaviorTreeBuilder();
	bt_ = Builder
		->ActiveSelector()
			->Sequence()
				->Condition(Condition_DetectEnemy::Create(false, this))
					->Back()
				->ActiveSelector()
					->Sequence()
						->Condition(BT::Condition_IsHealthLow::Create(true))
							->Back()
						->Action(Action_Chase::Create(this))
							->Back()
						->Back()
					->Parallel(BT::EPolicy::RequireAll, BT::EPolicy::RequireOne)
						->Condition(BT::Condition_IsEnemyDead::Create(true))
							->Back()
						->Action(BT::Action_Attack::Create())
							->Back()
						->Back()
					->Back()
				->Back()
			->Action(Action_Patrol::Create(this))
		->End();
	delete Builder;
}

Monster::~Monster()
{
	if (bt_ != nullptr)
		delete bt_;
}

void Monster::Update()
{
	bt_->Tick();
}