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
				->Sequence()                                                            // 대화 마무리
					->Condition(BotNode<IsDialogOpen>::Create(blackboard))->Back()
					->Action(BotNode<AdvanceDialog>::Create(blackboard))->Back()
				->Back()
				->Sequence()                                                            // 맵 이동
					->Condition(BotNode<IsQuestTravelGoal>::Create(blackboard))->Back()
					->Action(BotNode<TravelToGate>::Create(blackboard))->Back()
				->Back()
				->Sequence()                                                            // NPC 상호작용
					->Condition(BotNode<IsQuestNpcGoal>::Create(blackboard))->Back()
					->Action(BotNode<ApproachQuestNpc>::Create(blackboard))->Back()
					->Action(BotNode<InteractQuestNpc>::Create(blackboard))->Back()
				->Back()
				->Sequence()                                                            // 사냥터 이동
					->Condition(BotNode<IsQuestHuntGoal>::Create(blackboard))->Back()
					->Action(BotNode<ReachHuntArea>::Create(blackboard))->Back()
				->Back()
				->Sequence()                                                            // 교전
					->Condition(BotNode<IsCombatAllowed>::Create(blackboard))->Back()
					->Condition(BotNode<HasTarget>::Create(blackboard))->Back()
					->ActiveSelector()
						->Sequence()
							->Condition(BotNode<IsTargetInAttackRange>::Create(blackboard))->Back()
							->Action(BotNode<AttackTarget>::Create(blackboard))->Back()
						->Back()
						->Action(BotNode<MoveToTarget>::Create(blackboard))->Back()
					->Back()
				->Back()
				->Sequence()                                                            // 대상 탐색
					->Condition(BotNode<IsCombatAllowed>::Create(blackboard))->Back()
					->Action(BotNode<AcquireTarget>::Create(blackboard))->Back()
				->Back()
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
