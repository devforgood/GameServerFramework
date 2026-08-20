#include "BotBehaviorTree.h"

#include <string>
#include <type_traits>

#include "../BehaviorTree/BehaviorTree.h"
#include "BotBTNodes.h"

namespace bot
{
	namespace
	{
		BT::EStatus ToEStatus(botbt::Status status)
		{
			switch (status)
			{
			case botbt::Status::Success: return BT::EStatus::Success;
			case botbt::Status::Failure: return BT::EStatus::Failure;
			default:                     return BT::EStatus::Running;
			}
		}

		template <class Logic>
		using NodeBase = std::conditional_t<
			Logic::kKind == botbt::NodeKind::Condition, BT::Condition, BT::Action>;

		// 로직 구조체 하나를 인하우스 BT 노드로 감싸는 어댑터.
		template <class Logic>
		class BotNode : public NodeBase<Logic>
		{
		public:
			static BT::Behavior* Create(BotBlackboard* blackboard) { return new BotNode(blackboard); }
			std::string Name() override { return Logic::kName; }

		protected:
			explicit BotNode(BotBlackboard* blackboard) : blackboard_(blackboard) {}
			~BotNode() override = default;

			BT::EStatus Update() override
			{
				return ToEStatus(logic_.Tick(*blackboard_));
			}

		private:
			BotBlackboard* blackboard_;
			Logic logic_;
		};
	}

	BT::BehaviorTree* CreateBotTree(BotBlackboard* blackboard)
	{
		using namespace botbt;

		BT::BehaviorTreeBuilder builder;
		return builder
			.ActiveSelector()
				->Sequence()                                                            // 사망 대기
					->Condition(BotNode<IsSelfDead>::Create(blackboard))->Back()
					->Action(BotNode<WaitRespawn>::Create(blackboard))->Back()
				->Back()
				->Sequence()                                                            // 교전
					->Condition(BotNode<HasTarget>::Create(blackboard))->Back()
					->ActiveSelector()
						->Sequence()
							->Condition(BotNode<IsTargetInAttackRange>::Create(blackboard))->Back()
							->Action(BotNode<AttackTarget>::Create(blackboard))->Back()
						->Back()
						->Action(BotNode<MoveToTarget>::Create(blackboard))->Back()
					->Back()
				->Back()
				->Action(BotNode<AcquireTarget>::Create(blackboard))->Back()            // 대상 탐색
				->Action(BotNode<Wander>::Create(blackboard))->Back()                   // 배회
			->End();
	}

	void DestroyBotTree(BT::BehaviorTree* tree)
	{
		if (tree == nullptr)
			return;

		if (tree->HaveRoot())
			tree->Release();
		delete tree;
	}
}
