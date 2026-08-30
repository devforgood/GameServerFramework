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

			// 사냥 목표가 있으면 그 사냥터를 중심으로 돈다. 스폰 지점 주변을 돌면
			// 목표 몬스터가 없는 곳에서 배회하다 진행이 멈춘다.
			bot::Vec3 center = bb.spawn_pos;
			float radius = bb.ai.wander_radius;
			bot::Vec3 hunt_center;
			float hunt_radius = 0.0f;
			if (bb.quest.HuntAnchor(hunt_center, hunt_radius))
			{
				center = hunt_center;
				radius = hunt_radius;
			}

			bb.wander_target = bot::Vec3(
				center.x + bb.RandomFloat(-radius, radius),
				center.y,
				center.z + bb.RandomFloat(-radius, radius));

			bb.next_wander_at = bb.now + bb.ai.wander_interval_ms / 1000.0;
			if (bb.actions != nullptr)
				bb.actions->MoveTo(bb.wander_target);

			return Status::Running;
		}
	};
	//-----------------------------------------------------------------------------------
	// 시나리오(메인 퀘스트) 노드.
	//
	// 무엇을 할지는 BotQuestBrain 이 목표 하나로 정해 두고, 여기서는 그 목표를 실제 명령으로
	// 옮기기만 한다. 판단과 실행을 나눠 두면 목표 계산을 소켓 없이 테스트할 수 있고,
	// 노드는 "가까이 가서 보낸다" 만 하므로 짧게 유지된다.
	//
	// 주의: 여기 Running 을 돌려주는 노드도 전제조건이 깨지면 반드시 Failure 로 끝낸다
	// (이 BT 는 Running 인 Sequence 를 다음 틱에 재초기화하지 않는다).
	//-----------------------------------------------------------------------------------

	// 대화 창이 열려 있는가. 열려 있으면 다른 무엇보다 먼저 끝낸다 — 서버가 대화 상태를
	// 들고 있어서, 열어 둔 채 돌아다니면 다음 상호작용이 지난 노드로 돌아온다.
	struct IsDialogOpen
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "IsDialogOpen";

		Status Tick(Blackboard& bb) const
		{
			return bb.quest.DialogOpen() ? Status::Success : Status::Failure;
		}
	};

	// 목표가 시키는 선택지를 누른다. 지금 목표로 할 일이 없으면 창을 닫는다.
	struct AdvanceDialog
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "AdvanceDialog";

		Status Tick(Blackboard& bb) const
		{
			if (!bb.quest.DialogOpen())
				return Status::Failure;

			if (!bb.quest.CanSendDialog(bb.now))
				return Status::Running;

			const int node_id = bb.quest.DialogNodeId();
			const int choice = bb.quest.ChooseDialogChoice();

			bb.quest.NoteDialogSent(bb.now);
			if (bb.actions != nullptr)
				bb.actions->SelectDialog(node_id, choice);

			// 닫기를 보냈으면 응답을 기다리지 않고 이쪽도 닫는다. 서버도 곧 빈 노드를
			// 보내지만, 그때까지 이 분기에 머물면 목표를 향해 움직이지 못한다.
			if (choice < 0)
				bb.quest.OnDialogClosed();

			return Status::Running;
		}
	};

	struct IsQuestTravelGoal
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "IsQuestTravelGoal";

		Status Tick(Blackboard& bb) const
		{
			return bb.quest.Goal().kind == bot::QuestGoalKind::Travel
				? Status::Success : Status::Failure;
		}
	};

	// 게이트까지 걸어가 밟는다. 도착 맵은 게이트가 정하므로 봇이 고를 수 없다.
	struct TravelToGate
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "TravelToGate";

		Status Tick(Blackboard& bb) const
		{
			const bot::QuestGoal& goal = bb.quest.Goal();
			if (goal.kind != bot::QuestGoalKind::Travel)
				return Status::Failure;

			if (bot::DistanceSqXZ(bb.self_pos, goal.pos) > goal.reach * goal.reach)
			{
				if (bb.now >= bb.next_move_at)
				{
					bb.next_move_at = bb.now + bb.ai.move_repath_ms / 1000.0;
					if (bb.actions != nullptr)
						bb.actions->MoveTo(goal.pos);
				}
				return Status::Running;
			}

			if (!bb.quest.CanSendGate(bb.now))
				return Status::Running;

			bb.quest.NoteGateSent(bb.now);
			if (bb.actions != nullptr)
				bb.actions->EnterGate(goal.gate_id);

			return Status::Running;
		}
	};

	struct IsQuestNpcGoal
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "IsQuestNpcGoal";

		Status Tick(Blackboard& bb) const
		{
			return bb.quest.Goal().kind == bot::QuestGoalKind::Interact
				? Status::Success : Status::Failure;
		}
	};

	// NPC 에게 다가간다. 서버가 상호작용 거리를 다시 재므로 여유를 두고 붙는다.
	struct ApproachQuestNpc
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "ApproachQuestNpc";

		Status Tick(Blackboard& bb) const
		{
			const bot::QuestGoal& goal = bb.quest.Goal();
			if (goal.kind != bot::QuestGoalKind::Interact)
				return Status::Failure;

			if (bot::DistanceSqXZ(bb.self_pos, goal.pos) <= goal.reach * goal.reach)
				return Status::Success;

			if (bb.now >= bb.next_move_at)
			{
				bb.next_move_at = bb.now + bb.ai.move_repath_ms / 1000.0;
				if (bb.actions != nullptr)
					bb.actions->MoveTo(goal.pos);
			}
			return Status::Running;
		}
	};

	// 도착했으면 말을 건다. 상호작용 하나로 대화 목표가 오르고, 완료 대기 중이던 퀘스트는
	// 대화에서 완료 선택지를 눌러 접수된다(선택 보상도 선택지가 정한다).
	struct InteractQuestNpc
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "InteractQuestNpc";

		Status Tick(Blackboard& bb) const
		{
			const bot::QuestGoal& goal = bb.quest.Goal();
			if (goal.kind != bot::QuestGoalKind::Interact)
				return Status::Failure;

			// 걸어오는 사이에 밀려났으면 다시 접근부터 한다.
			if (bot::DistanceSqXZ(bb.self_pos, goal.pos) > goal.reach * goal.reach)
				return Status::Failure;

			if (!bb.quest.CanSendInteract(bb.now))
				return Status::Running;

			bb.quest.NoteInteractSent(bb.now);
			if (bb.actions != nullptr)
				bb.actions->Interact(goal.npc_id);

			return Status::Running;
		}
	};

	struct IsQuestHuntGoal
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "IsQuestHuntGoal";

		Status Tick(Blackboard& bb) const
		{
			return bb.quest.Goal().kind == bot::QuestGoalKind::Hunt
				? Status::Success : Status::Failure;
		}
	};

	// 사냥터로 이동한다. 도착하면 Failure 로 끝내 전투 분기에 자리를 넘긴다
	// (여기서 Running 을 유지하면 사냥터 한가운데서 아무것도 잡지 않는다).
	struct ReachHuntArea
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "ReachHuntArea";

		Status Tick(Blackboard& bb) const
		{
			const bot::QuestGoal& goal = bb.quest.Goal();
			if (goal.kind != bot::QuestGoalKind::Hunt)
				return Status::Failure;

			if (bot::DistanceSqXZ(bb.self_pos, goal.pos) <= goal.radius * goal.radius)
				return Status::Failure;

			if (bb.now >= bb.next_move_at)
			{
				bb.next_move_at = bb.now + bb.ai.move_repath_ms / 1000.0;
				if (bb.actions != nullptr)
					bb.actions->MoveTo(goal.pos);
			}
			return Status::Running;
		}
	};

	// 지금 싸워도 되는가. 시나리오를 끄면 언제나 되고(예전 동작), 켜면 사냥 목표일 때만
	// 싸운다 — NPC 에게 가는 길이나 게이트로 가는 길에 눈에 띈 것을 쫓아가면 목적지에
	// 영영 닿지 못한다.
	struct IsCombatAllowed
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "IsCombatAllowed";

		Status Tick(Blackboard& bb) const
		{
			if (!bb.quest.Enabled())
				return Status::Success;

			const bot::QuestGoalKind kind = bb.quest.Goal().kind;
			return (kind == bot::QuestGoalKind::Hunt || kind == bot::QuestGoalKind::None)
				? Status::Success : Status::Failure;
		}
	};
}
