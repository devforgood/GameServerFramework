#pragma once

namespace BT
{
	class BehaviorTree;
}

namespace bot
{
	struct BotBlackboard;

	//-----------------------------------------------------------------------------------
	// 봇 시나리오 트리를 만든다. 트리는 봇마다 하나씩 새로 만들고 봇이 소유한다
	// (노드가 블랙보드 포인터를 들고 있으므로 절대 공유하지 않는다 — 공유하면 봇끼리
	// 상태가 섞이고, 여러 워커 스레드가 같은 노드를 동시에 틱하게 된다).
	//
	//  ActiveSelector
	//  ├─ Sequence [IsSelfDead, WaitRespawn]
	//  ├─ Sequence [IsDialogOpen, AdvanceDialog]
	//  ├─ Sequence [IsQuestTravelGoal, TravelToGate]
	//  ├─ Sequence [IsQuestNpcGoal, ApproachQuestNpc, InteractQuestNpc]
	//  ├─ Sequence [IsQuestHuntGoal, ReachHuntArea]
	//  ├─ Sequence
	//  │   ├─ IsCombatAllowed
	//  │   ├─ HasTarget
	//  │   └─ ActiveSelector
	//  │       ├─ Sequence [IsTargetInAttackRange, AttackTarget]
	//  │       └─ MoveToTarget
	//  ├─ Sequence [IsCombatAllowed, AcquireTarget]
	//  └─ Wander
	//
	// 시나리오(퀘스트) 노드는 목표가 없으면 전부 실패로 떨어지므로, 설정에서 시나리오를
	// 끄면 예전과 똑같은 사냥 트리가 된다.
	//-----------------------------------------------------------------------------------
	BT::BehaviorTree* CreateBotTree(BotBlackboard* blackboard);

	// 노드까지 해제하고 트리 객체를 지운다.
	void DestroyBotTree(BT::BehaviorTree* tree);
}
