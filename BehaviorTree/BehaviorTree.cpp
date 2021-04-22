#include "pch.h"
#include "BehaviorTree.h"
#include<assert.h>

using namespace BT;

void BehaviorTree::Tick()
{	
	Root->Tick();
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Sequence()
{
	Behavior* Sq=Sequence::Create();
	AddBehavior(Sq);
	return this;
}
BehaviorTreeBuilder* BehaviorTreeBuilder::Action(EActionMode ActionModes)
{
	Behavior* Ac;
	switch (ActionModes)
	{
	case EActionMode::Attack:
		Ac=Action_Attack::Create();
		break;

	case  EActionMode::Patrol:
		Ac= Action_Patrol::Create();
		break;

	case EActionMode::Runaway:
		Ac=Action_Runaway::Create();
		break;

	default:
		Ac = nullptr;
		break;
	}
	
	AddBehavior(Ac);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Condition(EConditionMode ConditionMode,bool IsNegation)
{
	Behavior* Cd;
	switch (ConditionMode)
	{
	case EConditionMode::IsSeeEnemy:
		Cd=Condition_IsSeeEnemy::Create(IsNegation);
		break;

	case  EConditionMode::IsHealthLow:
		Cd=Condition_IsHealthLow::Create(IsNegation);
		break;

	case EConditionMode::IsEnemyDead:
		Cd = Condition_IsEnemyDead::Create(IsNegation);
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
	Behavior* St=Selector::Create();

	AddBehavior(St);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Repeat(int RepeatNum)
{
	Behavior* Rp=Repeat::Create(RepeatNum);

	AddBehavior(Rp);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::ActiveSelector()
{
	Behavior* Ast = ActiveSelector::Create();

	AddBehavior(Ast);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Filter()
{
	Behavior* Ft = Filter::Create();

	AddBehavior(Ft);

	return this;
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Parallel(EPolicy InSucess, EPolicy InFailure)
{
	Behavior* Pl=Parallel::Create(InSucess,InFailure);

	AddBehavior(Pl);

	return this;
}
BehaviorTreeBuilder* BehaviorTreeBuilder::Monitor(EPolicy InSucess, EPolicy InFailure)
{
	Behavior* Mt=Monitor::Create(InSucess,InFailure);

	AddBehavior(Mt);

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
	BehaviorTree* Tmp= new BehaviorTree(TreeRoot);
	TreeRoot = nullptr;
	return Tmp;
}

void BehaviorTreeBuilder::AddBehavior(Behavior* NewBehavior)
{
	assert(NewBehavior);
	if (!TreeRoot)
	{
		TreeRoot=NewBehavior;
	}
	else
	{
		NodeStack.top()->AddChild(NewBehavior);
	}

	NodeStack.push(NewBehavior);
}

BehaviorTreeBuilder* BehaviorTreeBuilder::Action(Behavior* NewBehavior)
{
	AddBehavior(NewBehavior);
	return this;
}
BehaviorTreeBuilder* BehaviorTreeBuilder::Condition(Behavior* NewBehavior)
{
	AddBehavior(NewBehavior);
	return this;
}