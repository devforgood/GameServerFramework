#pragma once
#include<vector>
#include<string>
#include<iostream>
#include<assert.h>
#include <random>
#include <functional>

namespace BTOptimized
{
	enum class EStatus :uint8_t
	{
		Invalid,
		Success,
		Failure,
		Running,
		Aborted,
	};

	enum class EPolicy :uint8_t
	{
		RequireOne,
		RequireAll,
	};

	class Behavior
	{
	public:
		friend class BehaviorTree;
		EStatus Tick();

		EStatus GetStatus() { return Status; }

		void Reset() { Status = EStatus::Invalid; }
		void Abort() { OnTerminate(EStatus::Aborted); Status = EStatus::Aborted; }

		bool IsTerminate() { return Status == EStatus::Success || Status == EStatus::Failure; }
		bool IsRunning() { return Status == EStatus::Running; }
		bool IsSuccess() { return Status == EStatus::Success; }
		bool IsFailuer() { return Status == EStatus::Failure; }

		virtual std::string Name() = 0;
		virtual void AddChild(Behavior* Child) {};

	protected:
		Behavior() :Status(EStatus::Invalid) {}
		virtual ~Behavior() {}

		virtual void OnInitialize() {};
		virtual EStatus Update() = 0;
		virtual void OnTerminate(EStatus Status) {};

	protected:
		EStatus Status;
	};

	class Decorator :public Behavior
	{
	public:
		friend class BehaviorTree;
		virtual void AddChild(Behavior* InChild) { Child = InChild; }
	protected:
		Decorator() {}
		virtual ~Decorator() {}
		Behavior* Child;
	};

	class Repeat :public Decorator
	{
	public:
		friend class BehaviorTree;
		static Behavior* Create(int InLimited) { return new Repeat(InLimited); }
		virtual std::string Name() override { return "Repeat"; }

	protected:
		Repeat(int InLimited) :Limited(InLimited) {}
		virtual ~Repeat() {}
		virtual void OnInitialize() { Count = 0; }
		virtual EStatus Update()override;
		virtual Behavior* Create() { return nullptr; }
	protected:
		int Limited = 3;
		int Count = 0;
	};

	const size_t MaxChildrenPerComposite = 7;

	class Composite :public Behavior
	{
	public:
		friend class BehaviorTree;
		virtual void AddChild(Behavior* InChild) override
		{ 
			assert(ChildrenCount < MaxChildrenPerComposite);
			ptrdiff_t p = (uintptr_t)InChild - (uintptr_t)this;
			assert(p < std::numeric_limits<uint16_t>::max());
			Children[ChildrenCount++] = static_cast<uint16_t>(p);
		}	

		Behavior* GetChild(size_t index)
		{
			assert(index < MaxChildrenPerComposite);
			return (Behavior*)((uintptr_t)this + Children[index]);
		}

		size_t GetChildrenCount()
		{
			return ChildrenCount;
		}

		void RemoveChild(size_t InChild);
		void ClearChild() 
		{ 
			for (int i = 0; i < MaxChildrenPerComposite; ++i)
			{
				Children[i] = 0;
			}
		}

	protected:
		Composite() {}
		virtual ~Composite() {}
		uint16_t Children[MaxChildrenPerComposite];
		uint16_t ChildrenCount = 0;
	};

	class Sequence :public Composite
	{
	public:
		friend class BehaviorTree;
		virtual std::string Name() override { return "Sequence"; }
		static Behavior* Create() { return new Sequence(); }
	protected:
		Sequence() {}
		virtual ~Sequence() {}
		virtual void OnInitialize() override { CurrChild = 0; }
		virtual EStatus Update() override;

	protected:
		uint16_t CurrChild=0;
	};

	
	class Selector :public Composite
	{
	public:
		friend class BehaviorTree;
		static Behavior* Create() { return new Selector(); }
		virtual std::string Name() override { return "Selector"; }

	protected:
		Selector() {}
		virtual ~Selector() {}
		virtual void OnInitialize() override { CurrChild = 0; }
		virtual EStatus Update() override;

	protected:
		uint16_t CurrChild=0;
	};

	class Parallel :public Composite
	{
	public:
		friend class BehaviorTree;
		static Behavior* Create(EPolicy InSucess, EPolicy InFailure) { return new Parallel(InSucess, InFailure); }
		virtual std::string Name() override { return "Parallel"; }

	protected:
		Parallel(EPolicy InSucess, EPolicy InFailure) :SucessPolicy(InSucess), FailurePolicy(InFailure) {}
		virtual ~Parallel() {}
		virtual EStatus Update() override;
		virtual void OnTerminate(EStatus InStatus) override;

	protected:
		EPolicy SucessPolicy;
		EPolicy FailurePolicy;
	};

	class ActiveSelector :public Selector
	{
	public:
		friend class BehaviorTree;
		static Behavior* Create() { return new ActiveSelector(); }
		virtual void OnInitialize() override { CurrChild = ChildrenCount; }
		virtual std::string Name() override { return "ActiveSelector"; }
	protected:
		ActiveSelector() {}
		virtual ~ActiveSelector() {}
		virtual EStatus Update() override;
	};

	class Condition :public Behavior
	{
	public:
		friend class BehaviorTree;
	protected:
		Condition(bool InIsNegation) :IsNegation(InIsNegation) {}
		virtual ~Condition() {}

	protected:
		bool  IsNegation = false;
	};

	class Action :public Behavior
	{
	public:
		friend class BehaviorTree;
	protected:
		Action() {}
		virtual ~Action() {}
	};

	class Condition_IsSeeEnemy :public Condition
	{
	public:
		friend class BehaviorTree;
		static Behavior* Create(bool InIsNegation) { return new Condition_IsSeeEnemy(InIsNegation); }
		virtual std::string Name() override { return "Condtion_IsSeeEnemy"; }

	protected:
		Condition_IsSeeEnemy(bool InIsNegation) :Condition(InIsNegation) {}
		virtual ~Condition_IsSeeEnemy() {}
		virtual EStatus Update() override;
	};

	class Condition_IsHealthLow :public Condition
	{
	public:
		friend class BehaviorTree;
		static Behavior* Create(bool InIsNegation) { return new Condition_IsHealthLow(InIsNegation); }
		virtual std::string Name() override { return "Condition_IsHealthLow"; }

	protected:
		Condition_IsHealthLow(bool InIsNegation) :Condition(InIsNegation) {}
		virtual ~Condition_IsHealthLow() {}
		virtual EStatus Update() override;

	};

	class Condition_IsEnemyDead :public Condition
	{
	public:
		friend class BehaviorTree;
		static Behavior* Create(bool InIsNegation) { return new Condition_IsEnemyDead(InIsNegation); }
		virtual std::string Name() override { return "Condition_IsHealthLow"; }

	protected:
		Condition_IsEnemyDead(bool InIsNegation) :Condition(InIsNegation) {}
		virtual ~Condition_IsEnemyDead() {}
		virtual EStatus Update() override;

	};

	class Action_Attack :public Action
	{
	public:
		friend class BehaviorTree;
		static Behavior* Create() { return new Action_Attack(); }
		virtual std::string Name() override { return "Action_Attack"; }

	protected:
		Action_Attack() {}
		virtual ~Action_Attack() {}
		virtual EStatus Update() override;
	};

	class Action_Runaway :public Action
	{
	public:
		friend class BehaviorTree;
		static Behavior* Create() { return new Action_Runaway(); }
		virtual std::string Name() override { return "Action_Runaway"; }

	protected:
		Action_Runaway() {}
		virtual ~Action_Runaway() {}
		virtual EStatus Update() override;
	};

	class Action_Patrol :public Action
	{
	public:
		friend class BehaviorTree;
		static Behavior* Create() { return new Action_Patrol(); }
		virtual std::string Name() override { return "Action_Patrol"; }

	protected:
		Action_Patrol() {}
		virtual ~Action_Patrol() {}
		virtual EStatus Update() override;
	};
}



