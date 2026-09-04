#include "MonsterAISystem.h"

#include "ECS.h"
#include "INavMovement.h"
#include "LogHelper.h"
#include "Map.h"
#include "MathHelper.h"
#include "Monster.h"

namespace
{
	// 배회 튜닝. 예전 ActionPatrol 노드의 상수를 그대로 옮겼다(초 단위로).
	constexpr float kPatrolRadius = 40.0f;
	constexpr float kArriveDistSq = 1.5f * 1.5f;
	constexpr float kMoveTimeoutSec = 8.0f;
	constexpr float kRestMinSec = 0.1f;
	constexpr float kRestMaxSec = 1.0f;

	float HorizontalDistSq(const float* a, const float* b)
	{
		const float dx = a[0] - b[0];
		const float dz = a[2] - b[2];
		return dx * dx + dz * dz;
	}
}

void monsterai::RegisterComponents(engine::EntityManager& entityManager)
{
	entityManager.RegisterComponent<AIScheduleComponent>();
	entityManager.RegisterComponent<AIAgentComponent>();
}

void monsterai::MonsterAISystem::Register(Monster* monster)
{
	const engine::EntityID entity = static_cast<engine::EntityID>(monster->GetEntityId());

	AIAgentComponent agent;
	agent.owner = monster;
	agent.actorId = monster->GetActorId();
	agent.targetActorId = monster->targetActorId_;
	const float* spawnPos = monster->GetSpawnPos();
	agent.spawnPos[0] = spawnPos[0];
	agent.spawnPos[1] = spawnPos[1];
	agent.spawnPos[2] = spawnPos[2];
	agent.nextState = static_cast<uint8_t>(monster->GetState());
	entityManager_->AddComponent(entity, agent);

	// 같은 틱에 전부 몰리지 않게 actorId 로 위상을 흩는다.
	// (예전에는 DetectEnemy 노드가 같은 방식으로 스캔만 분산했다.)
	AIScheduleComponent schedule;
	schedule.nextThinkTick = tick_ + (static_cast<uint32_t>(agent.actorId) % kIdleThinkInterval);
	entityManager_->AddComponent(entity, schedule);
}

void monsterai::MonsterAISystem::Wake(Monster* monster)
{
	auto* schedules = entityManager_->GetComponentArray<AIScheduleComponent>();
	const engine::EntityID entity = static_cast<engine::EntityID>(monster->GetEntityId());
	if (!schedules->HasData(entity))
		return; // 다른 백엔드이거나 아직 등록 전이다.

	schedules->GetData(entity).nextThinkTick = tick_;
}

size_t monsterai::MonsterAISystem::AgentCount() const
{
	return entityManager_->GetComponentArray<AIScheduleComponent>()->GetSize();
}

float monsterai::MonsterAISystem::NextRestSeconds()
{
	// xorshift32. 휴식 시간을 흩는 것이 전부라 분포 품질보다 비용이 중요하다
	// (예전에는 도달할 때마다 mt19937 + uniform_int_distribution 을 돌렸다).
	rngState_ ^= rngState_ << 13;
	rngState_ ^= rngState_ >> 17;
	rngState_ ^= rngState_ << 5;

	const float unit = static_cast<float>(rngState_ & 0xFFFFFF) / static_cast<float>(0x1000000);
	return kRestMinSec + unit * (kRestMaxSec - kRestMinSec);
}

void monsterai::MonsterAISystem::Update(float deltaTime)
{
	++tick_;
	worldTime_ += deltaTime;

	auto* scheduleArray = entityManager_->GetComponentArray<AIScheduleComponent>();
	const size_t count = scheduleArray->GetSize();
	if (count == 0)
		return;

	auto* agentArray = entityManager_->GetComponentArray<AIAgentComponent>();
	if (agentArray->GetSize() != count)
	{
		// 두 컴포넌트는 Register 에서 함께 붙고 DestroyEntity 에서 함께 빠지므로
		// 두 배열의 같은 인덱스가 같은 개체를 가리킨다. 어긋났다면 그 규칙이 깨진 것이다.
		LOG.error("MonsterAISystem: schedule/agent 배열 크기 불일치 ({} vs {})",
			count, agentArray->GetSize());
		return;
	}

	AIScheduleComponent* schedules = scheduleArray->GetArray();
	AIAgentComponent* agents = agentArray->GetArray();

	CollectDue(schedules, count);
	if (due_.empty())
		return;

	nav_ = map_->GetNavMap();

	// 조건 평가 — 비쌀수록 더 좁은 무리에만 돈다.
	EvaluateAlive(agents);
	EvaluateDetect(agents);
	EvaluateAttackRange(agents);

	// 결정(표 조회) → 행동별 버킷.
	Decide(agents);

	// 실행 — 버킷마다 한 가지 일만 한다.
	RunPatrol(agents);
	RunChase(agents);
	RunAttack(agents);
	RunDead(agents);

	FlushStates(agents);
	Reschedule(agents, schedules);
}

// 패스 1. 매 틱 도는 유일한 전수 순회. 4바이트씩 순차로 읽어 예측이 잘 선다.
void monsterai::MonsterAISystem::CollectDue(AIScheduleComponent* schedules, size_t count)
{
	due_.clear();
	for (size_t i = 0; i < count; ++i)
	{
		if (tick_ >= schedules[i].nextThinkTick)
			due_.push_back(static_cast<uint32_t>(i));
	}
	thinkCount_ += due_.size();
}

// 패스 2-a. 생존 조건. 여기서부터 개체 레코드를 만진다.
void monsterai::MonsterAISystem::EvaluateAlive(AIAgentComponent* agents)
{
	alive_.clear();
	for (const uint32_t slot : due_)
	{
		AIAgentComponent& agent = agents[slot];
		agent.conditions = 0;

		if (agent.owner->GetHealth() <= 0)
			continue;

		agent.conditions = kAlive;
		agent.flags &= ~static_cast<uint8_t>(kDeadHandled); // 되살아났다면 사망 처리를 다시 할 수 있게 한다.
		alive_.push_back(slot);
	}
}

// 패스 2-b. 적 탐지. 살아 있는 개체에만 돈다.
//
// 교전 중이면 전체 재탐색 대신 그 대상이 아직 유효한지만 본다(거리 1회 + Raycast 1회).
// 전체 스캔은 시야 반경의 모든 셀을 훑고 후보마다 시야 판정을 하므로 훨씬 비싸다.
// 배회 중에는 사고 자체가 kIdleThinkInterval 틱마다 한 번이라, 스캔 간격도 그만큼이다.
void monsterai::MonsterAISystem::EvaluateDetect(AIAgentComponent* agents)
{
	engaged_.clear();
	for (const uint32_t slot : alive_)
	{
		AIAgentComponent& agent = agents[slot];
		Monster* monster = agent.owner;

		if (agent.targetActorId >= 0)
		{
			if (map_->IsTargetVisible(monster, agent.targetActorId))
			{
				agent.conditions |= kHasTarget;
				engaged_.push_back(slot);
				continue;
			}
			agent.targetActorId = -1; // 놓쳤다 — 아래에서 새 대상을 찾는다.
		}

		agent.targetActorId = map_->DetectEnemy(monster);
		monster->targetActorId_ = agent.targetActorId; // 공격/테스트가 보는 값과 맞춘다.
		if (agent.targetActorId >= 0)
		{
			agent.conditions |= kHasTarget;
			engaged_.push_back(slot);
		}
	}
}

// 패스 2-c. 사거리 판정. 교전 중인 개체에만 돈다(Raycast 가 들어가 가장 비싼 조건이다).
void monsterai::MonsterAISystem::EvaluateAttackRange(AIAgentComponent* agents)
{
	for (const uint32_t slot : engaged_)
	{
		AIAgentComponent& agent = agents[slot];

		const float* selfPos = nav_->GetPos(agent.actorId);
		const float* targetPos = nav_->GetPos(agent.targetActorId);
		if (ManhattanDistance(selfPos, targetPos) > Monster::kAttackRange)
			continue;

		float hitPoint[3];
		if (!nav_->Raycast(agent.actorId, targetPos, hitPoint))
			agent.conditions |= kInAttackRange;
	}
}

// 패스 3. 결정. 조건 비트로 표를 찾을 뿐이라 분기가 없다.
void monsterai::MonsterAISystem::Decide(AIAgentComponent* agents)
{
	patrolling_.clear();
	chasing_.clear();
	attacking_.clear();
	dying_.clear();

	for (const uint32_t slot : due_)
	{
		AIAgentComponent& agent = agents[slot];
		const Action action = kDecisionTable[agent.conditions & kConditionMask];
		agent.action = static_cast<uint8_t>(action);

		switch (action)
		{
		case Action::Patrol: patrolling_.push_back(slot); break;
		case Action::Chase:  chasing_.push_back(slot); break;
		case Action::Attack: attacking_.push_back(slot); break;
		case Action::Dead:   dying_.push_back(slot); break;
		}
	}
}

void monsterai::MonsterAISystem::IssuePatrolTarget(AIAgentComponent& agent)
{
	// 임의 지점 선택 + 경로 산출이 한 호출로 묶여 있다(틱 안에서 가장 비싼 작업이라
	// 목적지에 도달하거나 휴식이 끝났을 때만 발생한다).
	if (nav_->Patrol(agent.actorId, agent.spawnPos, kPatrolRadius, agent.dest))
	{
		agent.patrolPhase = kPatrolMoving;
		agent.moveUntil = worldTime_ + kMoveTimeoutSec;
	}
}

// 패스 4-a. 배회. 목적지로 가다가 도착하면 잠시 쉬고 다음 지점을 고른다.
void monsterai::MonsterAISystem::RunPatrol(AIAgentComponent* agents)
{
	for (const uint32_t slot : patrolling_)
	{
		AIAgentComponent& agent = agents[slot];
		agent.nextState = static_cast<uint8_t>(syncnet::AIState_Patrol);

		switch (agent.patrolPhase)
		{
		case kPatrolMoving:
		{
			const float* pos = nav_->GetPos(agent.actorId);
			if (HorizontalDistSq(pos, agent.dest) <= kArriveDistSq || worldTime_ >= agent.moveUntil)
			{
				nav_->Stop(agent.actorId);
				agent.patrolPhase = kPatrolResting;
				agent.restUntil = worldTime_ + NextRestSeconds();
			}
			break;
		}
		case kPatrolResting:
		{
			if (worldTime_ >= agent.restUntil)
			{
				nav_->Resume(agent.actorId);
				IssuePatrolTarget(agent);
			}
			break;
		}
		case kPatrolIdle:
		default:
			IssuePatrolTarget(agent);
			break;
		}
	}
}

// 패스 4-b. 추격. 목표가 거의 그대로면 이동 전략이 기존 경로를 재사용한다
// (WaypointNavMovement::CanReusePath) — 그래서 매 틱 지시해도 경로를 다시 내지 않는다.
void monsterai::MonsterAISystem::RunChase(AIAgentComponent* agents)
{
	for (const uint32_t slot : chasing_)
	{
		AIAgentComponent& agent = agents[slot];
		agent.nextState = static_cast<uint8_t>(syncnet::AIState_Detect);

		nav_->Resume(agent.actorId);
		nav_->SetMoveTarget(agent.actorId, nav_->GetPos(agent.targetActorId), false);
	}
}

// 패스 4-c. 공격. 시전은 플레이어와 같은 스킬 파이프라인(SkillSet::TryCast)을 탄다 —
// 쿨다운 등으로 거부되면 이번 틱은 공격하지 않고 다음 틱에 다시 시도한다.
void monsterai::MonsterAISystem::RunAttack(AIAgentComponent* agents)
{
	for (const uint32_t slot : attacking_)
	{
		AIAgentComponent& agent = agents[slot];
		agent.nextState = static_cast<uint8_t>(syncnet::AIState_Attack);
		agent.owner->Attack();
	}
}

// 패스 4-d. 사망. 최초 1회 사망 이벤트를 발행하고, 소멸 지연이 지나면 Destroyed 로 넘긴다
// (Map::SyncActorState 가 Destroyed 를 보고 액터를 회수한다).
void monsterai::MonsterAISystem::RunDead(AIAgentComponent* agents)
{
	for (const uint32_t slot : dying_)
	{
		AIAgentComponent& agent = agents[slot];

		if ((agent.flags & kDeadHandled) == 0)
		{
			agent.flags |= kDeadHandled;
			agent.destroyAt = worldTime_ + kDestroyDelaySec;
			agent.nextState = static_cast<uint8_t>(syncnet::AIState_Dead);
			agent.owner->NotifyKilledBy(); // 킬한 플레이어에게 사망 이벤트 발행(최초 1회)
			LOG.info("Monster dead");
			continue;
		}

		if (worldTime_ >= agent.destroyAt)
		{
			if (agent.nextState != static_cast<uint8_t>(syncnet::AIState_Destroyed))
				LOG.info("Monster destoryed");
			agent.nextState = static_cast<uint8_t>(syncnet::AIState_Destroyed);
		}
	}
}

// 패스 5. 상태 기록. 바뀔 때만 쓴다 —
// Actor::SetState 는 엔티티 해시로 ECS 컴포넌트를 찾아 쓰므로 매 틱 부르면 그게 비용이다.
void monsterai::MonsterAISystem::FlushStates(AIAgentComponent* agents)
{
	for (const uint32_t slot : due_)
	{
		AIAgentComponent& agent = agents[slot];
		const syncnet::AIState next = static_cast<syncnet::AIState>(agent.nextState);
		if (agent.owner->GetState() != next)
			agent.owner->SetState(next);
	}
}

// 패스 6. 다음 사고 시점. 교전 중이거나 사망 처리 중이면 매 틱, 그 밖에는 느리게.
void monsterai::MonsterAISystem::Reschedule(AIAgentComponent* agents, AIScheduleComponent* schedules)
{
	for (const uint32_t slot : due_)
	{
		const AIAgentComponent& agent = agents[slot];
		const bool busy = agent.targetActorId >= 0
			|| static_cast<Action>(agent.action) == Action::Dead;

		schedules[slot].nextThinkTick = tick_ + (busy ? kEngagedThinkInterval : kIdleThinkInterval);
	}
}

namespace
{
	//--- MonsterBTRunner 전략 구현 ---
	//
	// 이 백엔드에는 "몬스터의 트리" 라는 것이 없다. 결정표는 공유 데이터고 개체가 갖는 것은
	// 컴포넌트뿐이라, 생성은 등록이고 틱은 무동작이다(실행은 시스템이 일괄로 한다).

	void* CreateBrain(Monster* monster)
	{
		monsterai::MonsterAISystem* system = monster->GetMap()->GetAISystem();
		if (system == nullptr)
		{
			LOG.error("MonsterEcsBT: 맵에 AI 시스템이 없다 — 몬스터 {} 는 사고하지 않는다",
				monster->GetActorId());
			return nullptr;
		}

		system->Register(monster);
		return system; // 트리 인스턴스가 없으므로 '등록됨' 표식으로만 쓴다.
	}

	void TickBrain(void* /*tree*/, Monster* /*monster*/)
	{
		// 실행은 Map::UpdateActors 가 부르는 MonsterAISystem::Update 가 일괄로 한다.
	}

	void DestroyBrain(void* /*tree*/)
	{
		// 컴포넌트는 Actor::Clear 의 DestroyEntity 가 다른 컴포넌트와 함께 정리한다.
	}
}

const MonsterBTRunner::Ops& MonsterEcsBT::Ops()
{
	static const MonsterBTRunner::Ops ops{ &CreateBrain, &TickBrain, &DestroyBrain };
	return ops;
}
