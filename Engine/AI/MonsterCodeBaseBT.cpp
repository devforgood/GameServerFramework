#include "MonsterCodeBaseBT.h"

#include "../BehaviorTree/BehaviorTree.h"

#include <chrono>       // steady_clock
#include <string>
#include <type_traits>  // std::conditional_t

#include "MonsterBTNodes.h"

//---------------------------------------------------------------------------------------
// 인하우스 BT(../BehaviorTree) 백엔드 어댑터.
//
// 게임 로직은 MonsterBTNodes.h 의 로직 구조체 하나뿐이고, 여기서는 그것을 인하우스 BT 의
// Behavior 파생 클래스로 감싸는 템플릿과 트리 구조(빌더)만 둔다.
// 트리 구조는 GameData/Monster.xml(behaviortree_cpp 백엔드) 과 1:1 로 같아야 하며,
// 두 백엔드가 같은 결정을 내리는지는 UnitTest/MonsterBTTest.cpp 가 고정한다.
//
//  Fallback(Selector)
//  ├─ Sequence                       (생존 분기)
//  │  ├─ ConditionCheckHealth
//  │  └─ Fallback
//  │     ├─ Sequence
//  │     │  ├─ ConditionDetectEnemy
//  │     │  └─ Fallback
//  │     │     ├─ Sequence [ConditionAttackRange, ActionAttack]
//  │     │     └─ ActionChase
//  │     └─ ActionPatrol
//  └─ Sequence                       (사망 분기)
//     ├─ ActionDead
//     └─ Delay(2000ms) → ActionDestroyed
//---------------------------------------------------------------------------------------

namespace
{
	BT::EStatus ToEStatus(monsterbt::Status status)
	{
		switch (status)
		{
		case monsterbt::Status::Success: return BT::EStatus::Success;
		case monsterbt::Status::Failure: return BT::EStatus::Failure;
		default:                         return BT::EStatus::Running;
		}
	}

	// 조건 노드는 BT::Condition, 액션 노드는 BT::Action 으로 붙는다.
	template <class Logic>
	using NodeBase = std::conditional_t<
		Logic::kKind == monsterbt::NodeKind::Condition, BT::Condition, BT::Action>;

	// 로직 구조체 하나를 인하우스 BT 노드로 만드는 어댑터.
	// Update() 안에서 Logic::Tick 이 그대로 인라인되므로 직접 구현했을 때와 비용이 같다.
	template <class Logic>
	class MonsterNode : public NodeBase<Logic>
	{
	public:
		static BT::Behavior* Create(Monster* monster) { return new MonsterNode(monster); }
		std::string Name() override { return Logic::kName; }

	protected:
		explicit MonsterNode(Monster* monster) : monster_(monster) {}
		~MonsterNode() override = default;

		BT::EStatus Update() override
		{
			// reason 은 이 백엔드에서 쓰지 않는다(디버그 뷰어는 behaviortree_cpp 전용).
			return ToEStatus(logic_.Tick(monster_).status);
		}

	private:
		Monster* monster_;
		Logic logic_;
	};

	// behaviortree_cpp 의 <Delay delay_msec="..."> 에 해당하는 데코레이터.
	// 최초 틱에 타이머를 시작해 경과 전까지 Running, 경과 후 자식을 틱하고 그 결과를 반환한다.
	class Decorator_Delay : public BT::Decorator
	{
	public:
		static BT::Behavior* Create(int delayMs) { return new Decorator_Delay(delayMs); }
		std::string Name() override { return "Delay"; }
		void Release() override
		{
			if (Child != nullptr)
				Child->Release();
			delete this;
		}

	protected:
		explicit Decorator_Delay(int delayMs) : delay_(delayMs) {}
		~Decorator_Delay() override = default;

		void OnInitialize() override
		{
			until_ = std::chrono::steady_clock::now() + delay_;
		}

		BT::EStatus Update() override
		{
			if (std::chrono::steady_clock::now() < until_)
				return BT::EStatus::Running;
			return Child->Tick();
		}

	private:
		std::chrono::milliseconds delay_;
		std::chrono::steady_clock::time_point until_;
	};

	//--- MonsterBTRunner 전략 구현 ---

	void* CreateTree(Monster* monster)
	{
		return MonsterCodeBaseBT::createTree(monster);
	}

	void TickTree(void* tree, Monster* monster)
	{
		(void)monster;
		static_cast<BT::BehaviorTree*>(tree)->Tick();
	}

	void DestroyTree(void* tree)
	{
		BT::BehaviorTree* behaviorTree = static_cast<BT::BehaviorTree*>(tree);
		behaviorTree->Release(); // 노드들을 해제한다(트리 객체는 delete 로).
		delete behaviorTree;
	}
}

BT::BehaviorTree* MonsterCodeBaseBT::createTree(Monster* monster)
{
	using namespace monsterbt;

	BT::BehaviorTreeBuilder builder;
	return builder
		.Selector()                                                          // Fallback (root)
			->Sequence()                                                     // 생존 분기
				->Condition(MonsterNode<CheckHealth>::Create(monster))->Back()
				->Selector()                                                 // Fallback
					->Sequence()
						->Condition(MonsterNode<DetectEnemy>::Create(monster))->Back()
						->Selector()                                         // Fallback
							->Sequence()
								->Condition(MonsterNode<AttackRange>::Create(monster))->Back()
								->Action(MonsterNode<Attack>::Create(monster))->Back()
							->Back()
							->Action(MonsterNode<Chase>::Create(monster))->Back()
						->Back()
					->Back()
					->Action(MonsterNode<Patrol>::Create(monster))->Back()
				->Back()
			->Back()
			->Sequence()                                                     // 사망 분기
				->Action(MonsterNode<Dead>::Create(monster))->Back()
				->Action(Decorator_Delay::Create(2000))
					->Action(MonsterNode<Destroyed>::Create(monster))->Back()
				->Back()
			->Back()
		->End();
}

const MonsterBTRunner::Ops& MonsterCodeBaseBT::Ops()
{
	static const MonsterBTRunner::Ops ops{ &CreateTree, &TickTree, &DestroyTree };
	return ops;
}
