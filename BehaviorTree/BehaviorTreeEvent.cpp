#include "pch.h"
#include "BehaviorTreeEvent.h"
#include<assert.h>

using namespace BTEvent;

void BehaviorTree::Tick()
{
	Behaviors.push_back(nullptr);
	while (Step())
	{
	}
}

bool BehaviorTree :: Step()
{
	Behavior* Current = Behaviors.front();
	Behaviors.pop_front();
	if (Current == nullptr)
		return false;
	Current->Tick();
	if (Current->IsTerminate() && Current->Observer)
	{
		Current->Observer(Current->GetStatus());
	}
	else
	{
		Behaviors.push_back(Current);
	}
	return true;
}

void BehaviorTree::Start(Behavior* Bh, BehaviorObserver* Observe)
{
	if (Observe)
	{
		Bh->Observer = *Observe;
	}
	Behaviors.push_front(Bh);
}
void BehaviorTree::Stop(Behavior* Bh, EStatus Result)
{
	assert(Result != EStatus::Running);
	Bh->SetStatus(Result);
	if (Bh->Observer)
	{
		Bh->Observer(Result);
	}
}





