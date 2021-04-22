#include "pch.h"
#include "BehaviorOptimized.h"
#include <algorithm>

extern std::_Binder<std::_Unforced, std::uniform_int_distribution<>&, std::default_random_engine&> dice;




using namespace BTOptimized;

EStatus Behavior::Tick()
{
	if (Status != EStatus::Running)
	{
		OnInitialize();
	}

	Status = Update();

	if (Status != EStatus::Running)
	{
		OnTerminate(Status);
	}

	return Status;
}

EStatus Repeat::Update()
{
	while (true)
	{
		Child->Tick();
		if (Child->IsRunning())return EStatus::Success;
		if (Child->IsFailuer())return EStatus::Failure;
		if (++Count == Limited)return EStatus::Success;
		Child->Reset();
	}
	return EStatus::Invalid;
}

void Composite::RemoveChild(size_t InChild)
{
	Children[InChild] = 0;
}

EStatus Sequence::Update()
{
	while (true)
	{
		EStatus s = GetChild(CurrChild)->Tick();
		if (s != EStatus::Success)
			return s;
		if (++CurrChild == ChildrenCount)
			return EStatus::Success;
	}
	return EStatus::Invalid;
}

EStatus Selector::Update()
{
	while (true)
	{
		EStatus s = GetChild(CurrChild)->Tick();
		if (s != EStatus::Failure)
			return s;
		if (++CurrChild == ChildrenCount)
			return EStatus::Failure;
	}
	return EStatus::Invalid;
}

EStatus Parallel::Update()
{
	int SuccessCount = 0, FailureCount = 0;
	for (int i = 0; i < ChildrenCount;++i)
	{
		if (!GetChild(i)->IsTerminate())
			GetChild(i)->Tick();

		if (GetChild(i)->IsSuccess())
		{
			++SuccessCount;
			if (SucessPolicy == EPolicy::RequireOne)
			{
				GetChild(i)->Reset();
				return EStatus::Success;
			}

		}

		if (GetChild(i)->IsFailuer())
		{
			++FailureCount;
			if (FailurePolicy == EPolicy::RequireOne)
			{
				GetChild(i)->Reset();
				return EStatus::Failure;
			}
		}
	}

	if (FailurePolicy == EPolicy::RequireAll&&FailureCount == ChildrenCount)
	{
		for (int i = 0; i < ChildrenCount;++i)
		{
			GetChild(i)->Reset();
		}

		return EStatus::Failure;
	}
	if (SucessPolicy == EPolicy::RequireAll&&SuccessCount == ChildrenCount)
	{
		for (int i = 0; i < ChildrenCount; ++i)
		{
			GetChild(i)->Reset();
		}
		return EStatus::Success;
	}

	return EStatus::Running;
}

void Parallel::OnTerminate(EStatus InStatus)
{
	for (int i = 0; i < ChildrenCount; ++i)
	{
		if (GetChild(i)->IsRunning())
			GetChild(i)->Abort();
	}
}

EStatus ActiveSelector::Update()
{
	uint16_t Previous = CurrChild;
	Selector::OnInitialize();
	EStatus result = Selector::Update();
	if (Previous != ChildrenCount&&CurrChild != Previous)
	{
		GetChild(Previous)->Abort();
	}

	return result;
}

EStatus Condition_IsSeeEnemy::Update()
{
	if (dice() > 50)
	{
		std::cout << "See enemy!" << std::endl;
		return !IsNegation ? EStatus::Success : EStatus::Failure;
	}

	else
	{
		std::cout << "Not see enemy" << std::endl;
		return !IsNegation ? EStatus::Failure : EStatus::Success;
	}

}

EStatus Condition_IsHealthLow::Update()
{
	if (dice() > 80)
	{
		std::cout << "Health is low" << std::endl;
		return !IsNegation ? EStatus::Success : EStatus::Failure;
	}

	else
	{
		std::cout << "Health is not low" << std::endl;
		return !IsNegation ? EStatus::Failure : EStatus::Success;
	}
}

EStatus Condition_IsEnemyDead::Update()
{
	if (dice() > 50)
	{
		std::cout << "Enemy is Dead" << std::endl;
		return !IsNegation ? EStatus::Success : EStatus::Failure;
	}

	else
	{
		std::cout << "Enemy is not Dead" << std::endl;
		return !IsNegation ? EStatus::Failure : EStatus::Success;
	}
}

EStatus Action_Attack::Update()
{
	std::cout << "Action_Attack " << std::endl;
	return EStatus::Success;
}

EStatus Action_Patrol::Update()
{
	std::cout << "Action_Patrol" << std::endl;
	return EStatus::Success;
}

EStatus Action_Runaway::Update()
{
	std::cout << "Action_Runaway" << std::endl;
	return EStatus::Success;
}