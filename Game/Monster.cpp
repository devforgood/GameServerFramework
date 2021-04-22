#include "Monster.h"
#include "../BehaviorTree/BehaviorTree.h"


Monster::Monster()
{
	BT::BehaviorTreeBuilder* Builder = new BT::BehaviorTreeBuilder();
	bt_ = Builder
		->ActiveSelector()
			->Sequence()
				->Condition(BT::Condition_IsSeeEnemy::Create(false))
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
	bt_->Tick();
}