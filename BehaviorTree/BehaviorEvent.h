#pragma once
#include<vector>
#include<string>
#include<iostream>
#include<functional>



namespace BTEvent
{
	enum class EStatus :uint8_t
	{
		Invalid,
		Success,
		Failure,
		Running,
		Aborted,
	};

	using BehaviorObserver = std::function<void(EStatus)>;


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
		void SetStatus(EStatus InStatus) { Status = InStatus; }
		void Reset() { Status = EStatus::Invalid; }
		void Abort() { OnTerminate(EStatus::Aborted); Status = EStatus::Aborted; }

		bool IsTerminate() { return Status == EStatus::Success || Status == EStatus::Failure; }
		bool IsRunning() { return Status == EStatus::Running; }
		bool IsSuccess() { return Status == EStatus::Success; }
		bool IsFailuer() { return Status == EStatus::Failure; }

		virtual std::string Name() = 0;
		virtual void AddChild(Behavior* Child) {};
	public:
		BehaviorObserver Observer;
	protected:
		Behavior() :Status(EStatus::Invalid) {}
		virtual ~Behavior() {}

		virtual void OnInitialize() {};
		virtual EStatus Update() = 0;
		virtual void OnTerminate(EStatus Status) {};

	protected:
		EStatus Status;
		class BehaviorTree* Tree;

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
		void OnChildComplete(EStatus Status);
	protected:
		Sequence() {}
		virtual ~Sequence() {}
		virtual void OnInitialize() override;
		virtual EStatus Update() override;

	protected:
		Behaviors::iterator CurrChild;
		BehaviorTree* m_pBehaviorTree;
	};

}


