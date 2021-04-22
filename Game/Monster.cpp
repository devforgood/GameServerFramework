#include "Monster.h"
#include "../BehaviorTree/BehaviorTree.h"
#include <random>
#include <functional>

extern std::_Binder<std::_Unforced, std::uniform_int_distribution<>&, std::default_random_engine&> dice;


class Condition_DetectEnemy : public BT::Condition
{
private:
	Monster* monster_;

public:
	static Behavior* Create(bool InIsNegation, Monster * monster) { return new Condition_DetectEnemy(InIsNegation, monster); }
	virtual std::string Name() override { return "Condition_IsSeeEnemy_Monster"; }

protected:
	Condition_DetectEnemy(bool InIsNegation, Monster* monster)
		: Condition(InIsNegation), monster_(monster)
	{

	}

	virtual ~Condition_DetectEnemy() {}
	virtual BT::EStatus Update() override
	{
		if (dice() > 50)
		{
			std::cout << "See enemy!" << std::endl;
			return !IsNegation ? BT::EStatus::Success : BT::EStatus::Failure;
		}
		else
		{
			std::cout << "Not see enemy" << std::endl;
			return !IsNegation ? BT::EStatus::Failure : BT::EStatus::Success;
		}
	}
};


Monster::Monster()
{
	BT::BehaviorTreeBuilder* Builder = new BT::BehaviorTreeBuilder();
	bt_ = Builder
		->ActiveSelector()
			->Sequence()
				->Condition(Condition_DetectEnemy::Create(false, this))
					->Back()
				->ActiveSelector()
					->Sequence()
						->Condition(BT::Condition_IsHealthLow::Create(false))
							->Back()
						->Action(BT::EActionMode::Runaway)
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
			->Action(BT::EActionMode::Patrol)
		->End();
	delete Builder;
}

void Monster::Update()
{
	//bt_->Tick();
}