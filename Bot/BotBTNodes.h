#pragma once

#include "BotBlackboard.h"

//---------------------------------------------------------------------------------------
// 봇 BT 의 노드 로직.
//
// Engine/AI/MonsterBTNodes.h 와 같은 방식이다 — 노드는 BT 라이브러리를 모르는 구조체이고,
// BotBehaviorTree.cpp 의 어댑터가 이것을 BT::Condition/BT::Action 으로 감싼다. 덕분에
// 시나리오 판단을 소켓도 BT 도 없이 블랙보드만 채워서 단위 테스트로 고정할 수 있다.
//
// 노드는 블랙보드만 읽고 쓰며, 바깥 세계에는 BotActions 를 통해서만 손을 댄다.
//---------------------------------------------------------------------------------------

namespace botbt
{
	enum class NodeKind
	{
		Condition,
		Action,
	};

	enum class Status
	{
		Success,
		Failure,
		Running,
	};

	using Blackboard = bot::BotBlackboard;

	// 내 캐릭터가 죽었나. 죽은 동안에는 서버가 부활시켜 줄 때까지 아무 명령도 보내지 않는다
	// (부활은 Map 의 pendingRespawns_ 가 처리한다 — 클라가 요청하는 절차가 없다).
	struct IsSelfDead
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "IsSelfDead";

		Status Tick(Blackboard& bb) const
		{
			return bb.self_dead ? Status::Success : Status::Failure;
		}
	};

	// 주의: Running 을 돌려주는 노드는 자기 전제조건이 깨지면 반드시 Failure 로 끝내야 한다.
	// 이 BT 는 Running 인 Sequence 를 다음 틱에 재초기화하지 않는다(앞의 조건 노드를 건너뛰고
	// 진행 중이던 자식부터 다시 틱한다). 그래서 여기서 스스로 끝내지 않으면, 부활한 뒤에도
	// 사망 분기에 갇혀 봇이 영원히 아무것도 하지 않는다.
	struct WaitRespawn
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "WaitRespawn";

		Status Tick(Blackboard& bb) const
		{
			if (!bb.self_dead)
				return Status::Failure;

			// 부활하면 위치가 스폰 지점으로 순간이동한다. 죽기 전 대상은 의미가 없다.
			bb.target_actor_id = 0;
			return Status::Running;
		}
	};

	// 지금 쫓고 있는 대상이 아직 쓸모 있나(시야 안 + 생존 + 탐색 반경 안).
	// 대상이 시야에서 사라지거나 죽으면 여기서 실패해 다시 탐색으로 내려간다.
	struct HasTarget
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "HasTarget";

		Status Tick(Blackboard& bb) const
		{
			if (bb.target_actor_id == 0)
				return Status::Failure;

			const bot::ActorSnapshot* target = bb.view.Find(bb.target_actor_id);
			if (target == nullptr || target->IsDead())
			{
				bb.target_actor_id = 0;
				return Status::Failure;
			}

			// 추격 중에는 탐색 반경보다 조금 더 멀어져도 놓지 않는다. 경계에서
			// 붙었다 떨어졌다 하면 대상이 매 틱 바뀌어 이동 명령만 쏟아진다.
			const float leash = bb.ai.search_radius * 1.5f;
			if (bot::DistanceSqXZ(bb.self_pos, target->pos) > leash * leash)
			{
				bb.target_actor_id = 0;
				return Status::Failure;
			}

			return Status::Success;
		}
	};

	// 가장 가까운 살아 있는 몬스터를 고른다. 없으면 실패 → 배회로 넘어간다.
	struct AcquireTarget
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "AcquireTarget";

		Status Tick(Blackboard& bb) const
		{
			const int found = bb.view.FindNearestMonster(bb.self_pos, bb.ai.search_radius);
			if (found == 0)
				return Status::Failure;

			bb.target_actor_id = found;

			// 새 대상으로 갈아탔으니 이동 명령을 다음 틱에 곧바로 보낸다.
			bb.next_move_at = 0.0;
			return Status::Success;
		}
	};

	struct IsTargetInAttackRange
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "IsTargetInAttackRange";

		Status Tick(Blackboard& bb) const
		{
			const bot::ActorSnapshot* target = bb.view.Find(bb.target_actor_id);
			if (target == nullptr)
				return Status::Failure;

			const float range = bb.ai.attack_range;
			return bot::DistanceSqXZ(bb.self_pos, target->pos) <= range * range
				? Status::Success : Status::Failure;
		}
	};

	// 대상 쪽으로 이동. 대상이 움직이므로 주기적으로 목표를 갱신한다.
	// 이동 자체는 서버가 내비메시로 처리하므로 봇은 목적지만 알려주면 된다.
	struct MoveToTarget
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "MoveToTarget";

		Status Tick(Blackboard& bb) const
		{
			// 죽은 대상도 시야에는 한동안 남아 있다. 여기서 끝내지 않으면 교전 분기가
			// Running 으로 잠겨 HasTarget 이 다시 검사되지 않고, 시체를 계속 쫓는다.
			const bot::ActorSnapshot* target = bb.view.Find(bb.target_actor_id);
			if (target == nullptr || target->IsDead())
				return Status::Failure;

			if (bb.now < bb.next_move_at)
				return Status::Running;

			bb.next_move_at = bb.now + bb.ai.move_repath_ms / 1000.0;
			if (bb.actions != nullptr)
				bb.actions->MoveTo(target->pos);

			return Status::Running;
		}
	};

	// 사거리 안이면 공격 간격을 지키며 스킬을 쓴다. 간격은 skill.json 의 쿨다운보다
	// 짧으면 서버가 거부하므로(그 자체도 측정 대상이다) 설정으로 조절한다.
	struct AttackTarget
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "AttackTarget";

		Status Tick(Blackboard& bb) const
		{
			const bot::ActorSnapshot* target = bb.view.Find(bb.target_actor_id);
			if (target == nullptr || target->IsDead())
				return Status::Failure;

			if (bb.now < bb.next_attack_at)
				return Status::Running;

			bb.next_attack_at = bb.now + bb.ai.attack_interval_ms / 1000.0;
			if (bb.actions != nullptr)
				bb.actions->Attack(bb.target_actor_id, target->pos);

			return Status::Running;
		}
	};

	// 사냥감이 없을 때 스폰 지점 주변을 돌아다닌다. 봇이 한 지점에 멈춰 있으면
	// 관심영역(AoI) 갱신이 거의 발생하지 않아 브로드캐스트 부하를 과소평가하게 된다.
	struct Wander
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "Wander";

		Status Tick(Blackboard& bb) const
		{
			const bool arrived = bot::DistanceSqXZ(bb.self_pos, bb.wander_target)
				<= bb.ai.arrive_epsilon * bb.ai.arrive_epsilon;

			if (!arrived && bb.now < bb.next_wander_at)
				return Status::Running;

			const float radius = bb.ai.wander_radius;
			bb.wander_target = bot::Vec3(
				bb.spawn_pos.x + bb.RandomFloat(-radius, radius),
				bb.spawn_pos.y,
				bb.spawn_pos.z + bb.RandomFloat(-radius, radius));

			bb.next_wander_at = bb.now + bb.ai.wander_interval_ms / 1000.0;
			if (bb.actions != nullptr)
				bb.actions->MoveTo(bb.wander_target);

			return Status::Running;
		}
	};
}
