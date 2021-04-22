#pragma once
#include<vector>
#include<string>
#include<iostream>


namespace BT
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
		virtual void Release() = 0;
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
		virtual void AddChild(Behavior* InChild) { Child = InChild; }
	protected:
		Decorator():Child(nullptr) {}
		virtual ~Decorator() {}
		Behavior* Child;
	};

	class Repeat :public Decorator
	{
	public:

		static Behavior* Create(int InLimited) { return new Repeat(InLimited); }
		virtual void Release() { Child->Release(); delete this; }
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

	class Composite :public Behavior
	{
	public:
		virtual void AddChild(Behavior* InChild) override { Children.push_back(InChild); }
		void RemoveChild(Behavior* InChild);
		void ClearChild() { Children.clear(); }
		virtual void Release()
		{
			for (auto it : Children)
			{
				it->Release();
			}

			delete this;
		}

	protected:
		Composite() {}
		virtual ~Composite() {}
		using Behaviors = std::vector<Behavior*>;
		Behaviors Children;
	};

	class Sequence :public Composite
	{
	public:
		virtual std::string Name() override { return "Sequence"; }
		static Behavior* Create() { return new Sequence(); }
	protected:
		Sequence() {}
		virtual ~Sequence() {}
		virtual void OnInitialize() override { CurrChild = Children.begin(); }
		virtual EStatus Update() override;

	protected:
		Behaviors::iterator CurrChild;
	};

	class Filter :public Sequence
	{
	public:
		static Behavior* Create() { return new Filter(); }
		void AddCondition(Behavior* Condition) { Children.insert(Children.begin(), Condition); }
		void AddAction(Behavior* Action) { Children.push_back(Action); }
		virtual std::string Name() override { return "Fliter"; }

	protected:
		Filter() {}
		virtual ~Filter() {}
	};

	class Selector :public Composite
	{
	public:
		static Behavior* Create() { return new Selector(); }
		virtual std::string Name() override { return "Selector"; }

	protected:
		Selector() {}
		virtual ~Selector() {}
		virtual void OnInitialize() override { CurrChild = Children.begin(); }
		virtual EStatus Update() override;

	protected:
		Behaviors::iterator CurrChild;
	};

	class Parallel :public Composite
	{
	public:
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

	class Monitor :public Parallel
	{
	public:
		static Behavior* Create(EPolicy InSucess, EPolicy InFailure) { return new Monitor(InSucess, InFailure); }
		void AddCondition(Behavior* Condition) { Children.insert(Children.begin(), Condition); }
		void AddAction(Behavior* Action) { Children.push_back(Action); }
		virtual std::string Name() override { return "Monitor"; }

	protected:
		Monitor(EPolicy InSucess, EPolicy InFailure) :Parallel(InSucess, InFailure) {}
		virtual ~Monitor() {}
	};

	class ActiveSelector :public Selector
	{
	public:
		static Behavior* Create() { return new ActiveSelector(); }
		virtual void OnInitialize() override { CurrChild = Children.end(); }
		virtual std::string Name() override { return "ActiveSelector"; }
	protected:
		ActiveSelector() {}
		virtual ~ActiveSelector() {}
		virtual EStatus Update() override;
	};

	class Condition :public Behavior
	{
	public:
		virtual void Release() { delete this; }

	protected:
		Condition(bool InIsNegation) :IsNegation(InIsNegation) {}
		virtual ~Condition() {}

	protected:
		bool  IsNegation = false;
	};

	class Action :public Behavior
	{
	public:
		virtual void Release() { delete this; }

	protected:
		Action() {}
		virtual ~Action() {}
	};

	class Condition_IsSeeEnemy :public Condition
	{
	public:
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
		static Behavior* Create() { return new Action_Patrol(); }
		virtual std::string Name() override { return "Action_Patrol"; }

	protected:
		Action_Patrol() {}
		virtual ~Action_Patrol() {}
		virtual EStatus Update() override;
	};
}



