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
	//  ├─ Sequence
	//  │   ├─ HasTarget
	//  │   └─ ActiveSelector
	//  │       ├─ Sequence [IsTargetInAttackRange, AttackTarget]
	//  │       └─ MoveToTarget
	//  ├─ AcquireTarget
	//  └─ Wander
	//-----------------------------------------------------------------------------------
	BT::BehaviorTree* CreateBotTree(BotBlackboard* blackboard);

	// 노드까지 해제하고 트리 객체를 지운다.
	void DestroyBotTree(BT::BehaviorTree* tree);
}
