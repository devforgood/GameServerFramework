#include "pch.h"
#include "BehaviorTreeOptimized.h"
#include<assert.h>

using namespace BTOptimized;

void BehaviorTree::Tick()
{
	Root->Tick();
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Sequence()
{
	Behavior* Sq = Bt->Allocate<class Sequence>();
	AddBehavior(Sq);
	return this;
}
BehaviorTreeBuilder* BehaviorTreeBuilder::Action(EActionMode ActionModes)
{
	Behavior* Ac;
	switch (ActionModes)
	{
	case EActionMode::Attack:
		Ac = Bt->Allocate<class Action_Attack>();;
		break;

	case  EActionMode::Patrol:
		Ac = Bt->Allocate<class Action_Patrol>();
		break;

	case EActionMode::Runaway:
		Ac = Bt->Allocate<class Action_Runaway>();
		break;

	default:
		Ac = nullptr;
		break;
	}

	AddBehavior(Ac);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Condition(EConditionMode ConditionMode, bool IsNegation)
{
	Behavior* Cd;
	switch (ConditionMode)
	{
	case EConditionMode::IsSeeEnemy:
		Cd = Bt->Allocate<class Condition_IsSeeEnemy>(IsNegation);
		break;

	case  EConditionMode::IsHealthLow:
		Cd = Bt->Allocate<class Condition_IsHealthLow>(IsNegation);
		break;

	case EConditionMode::IsEnemyDead:
		Cd = Bt->Allocate<class Condition_IsEnemyDead>(IsNegation);
		break;

	default:
		Cd = nullptr;
		break;
	}

	AddBehavior(Cd);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Selector()
{
	Behavior* St = Bt->Allocate<class Selector>();

	AddBehavior(St);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Repeat(int RepeatNum)
{
	Behavior* Rp = Bt->Allocate<class Repeat>(RepeatNum);

	AddBehavior(Rp);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::ActiveSelector()
{
	Behavior* Ast = Bt->Allocate<class ActiveSelector>();

	AddBehavior(Ast);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Parallel(EPolicy InSucess, EPolicy InFailure)
{
	Behavior* Pl = Bt->Allocate<class Parallel>(InSucess, InFailure);

	AddBehavior(Pl);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Back()
{
	NodeStack.pop();
	return this;
}

BehaviorTree* BehaviorTreeBuilder::End()
{
	while (!NodeStack.empty())
	{
		NodeStack.pop();
	}
	return Bt;
}

void BehaviorTreeBuilder::AddBehavior(Behavior* NewBehavior)
{
	assert(NewBehavior);
	if (!Bt->HaveRoot())
	{
		Bt->SetRoot(NewBehavior);
	}
	else
	{
		NodeStack.top()->AddChild(NewBehavior);
	}
	NodeStack.push(NewBehavior);
}