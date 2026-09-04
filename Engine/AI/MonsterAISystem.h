#pragma once

#include <cstdint>
#include <vector>

#include "MonsterBTRunner.h"

namespace engine
{
	class EntityManager;
}
class INavMovement;
class Map;
class Monster;

//---------------------------------------------------------------------------------------
// 몬스터 AI 를 ECS 로 돌리는 백엔드.
//
//   Behavior Tree = 결정,  컴포넌트 = 상태,  시스템 = 실행
//
// 다른 두 백엔드는 몬스터 한 마리마다 노드 객체 13개를 힙에 만들고, 매 틱 그 트리를 가상
// 호출로 타고 내려가며 노드가 곧바로 세계를 바꾼다. 마리 수만큼 트리가 복제되고, 실행
// 경로는 개체마다 달라 분기 예측이 서지 않으며, 노드 하나하나가 Monster 객체를 다시 만진다.
//
// 여기서는 트리를 "타고 내려가지" 않는다. 이 트리(아래 그림)는 Fallback 하나에 조건이
// 세 개뿐이라, 조건 비트 조합에서 행동으로 가는 표 하나로 컴파일된다.
//
//   Fallback                                조건 비트           행동
//   ├─ Sequence                            ─────────────────────────────
//   │  ├─ CheckHealth                      !Alive            → Dead
//   │  └─ Fallback                          Alive             → Patrol
//   │     ├─ Sequence                       Alive+Target      → Chase
//   │     │  ├─ DetectEnemy                 Alive+Target+범위 → Attack
//   │     │  └─ Fallback
//   │     │     ├─ Sequence [AttackRange, Attack]
//   │     │     └─ Chase
//   │     └─ Patrol
//   └─ Sequence [Dead, Delay(2s) → Destroyed]
//
// 그래서 한 틱은 트리 순회가 아니라 **동종 작업의 배치 패스** 몇 개로 끝난다.
//
//   1) 스케줄 스캔  : 4바이트 배열만 훑어 이번 틱에 사고할 개체를 고른다
//   2) 조건 평가    : 생존 → (생존한 것만) 탐지 → (교전 중인 것만) 사거리
//                     비싼 조건일수록 더 좁은 무리에만 돈다
//   3) 결정         : 조건 비트로 표를 찾아 행동별 버킷에 담는다(분기 없음)
//   4) 실행         : 버킷마다 한 가지 일만 하는 루프 — 배회 / 추격 / 공격 / 사망
//
// 버킷이 곧 의도(intent) 목록이고, 4)의 각 루프가 그 의도를 실행하는 시스템이다.
// 결정과 실행이 이렇게 갈라져 있어서, 트리는 세계를 직접 만지지 않는다.
//
// 성능의 근거가 되는 선택 세 가지:
//
//   - 컴포넌트를 둘로 쪼갰다. 매 틱 전수 순회하는 것은 4바이트 AIScheduleComponent 뿐이고
//     (40,000마리라도 160KB), 사고할 차례가 된 개체만 64바이트 AIAgentComponent 를 만진다.
//     배회 중인 몬스터는 kIdleThinkInterval 틱에 한 번만 사고하므로 대부분의 틱에서
//     Monster 객체는 캐시에 올라오지도 않는다.
//   - 시각을 벽시계에서 시뮬레이션 시간으로 바꿨다. steady_clock::now() 는 마리마다 부르면
//     그대로 틱 비용이 되고(윈도우에서 QPC), 무엇보다 시간이 dt 와 무관해져 테스트가 흔들린다.
//     휴식/이동 타임아웃/사망 후 소멸 지연은 전부 float 초 비교다.
//   - 상태는 바뀔 때만 쓴다. Actor::SetState 는 ECS 컴포넌트를 엔티티 해시로 찾아 쓰므로
//     매 틱 부르면 비싸다. 예전 트리는 배회 중에도 매 틱 SetState(Patrol) 을 불렀다.
//
// 사고 주기를 늦춰도 반응이 늦지 않는 이유:
//   - 교전 중(타깃 보유)이거나 사망 처리 중이면 매 틱 사고한다(kEngagedThinkInterval).
//   - 배회 중 주기는 예전 DetectEnemy 노드가 스스로 걸던 스태거 주기(10틱)와 같다.
//     즉 가장 비싼 적 탐지 스캔의 간격은 예전과 같고, 대신 나머지 전부를 함께 건너뛴다.
//   - 피격/체력 변화는 Monster 가 Wake 로 즉시 깨운다(다음 틱에 사고).
//
// 이 백엔드는 BT 디버그 뷰어(BTDebugManager)를 지원하지 않는다 — 뷰어는 behaviortree_cpp
// 트리에만 붙는다. 필요하면 몬스터 스폰 전에 Monster::btBackend_ 를 BTCpp 로 돌린다.
// 세 백엔드가 같은 결정을 내리는지는 UnitTest/MonsterBTTest.cpp 가 고정한다.
//---------------------------------------------------------------------------------------

namespace monsterai
{
	// 조건 비트. 트리의 Condition 노드 하나가 비트 하나다.
	enum ConditionBit : uint8_t
	{
		kAlive = 1 << 0,
		kHasTarget = 1 << 1,
		kInAttackRange = 1 << 2,
		kConditionMask = 0x07,
	};

	enum class Action : uint8_t
	{
		Patrol,
		Chase,
		Attack,
		Dead,
	};

	// 트리를 컴파일한 결정표. 조건 조합 8가지에 대한 행동이 전부 여기 있다.
	// (Fallback 의 우선순위가 그대로 들어 있다 — 생존 조건이 먼저, 그다음 탐지, 그다음 사거리)
	inline constexpr Action kDecisionTable[8] = {
		/* ---            */ Action::Dead,
		/* Alive          */ Action::Patrol,
		/* Target         */ Action::Dead,   // 죽었으면 타깃 유무는 보지 않는다
		/* Alive+Target   */ Action::Chase,
		/* Range          */ Action::Dead,
		/* Alive+Range    */ Action::Patrol, // 타깃 없이 사거리 비트만 서는 일은 없다
		/* Target+Range   */ Action::Dead,
		/* Alive+T+Range  */ Action::Attack,
	};

	// 매 틱 전수 순회하는 유일한 배열. 작을수록 좋다.
	struct AIScheduleComponent
	{
		uint32_t nextThinkTick = 0;
	};

	// 개체별 실행 상태. 사고할 차례가 된 개체만 만진다.
	// 캐시 라인 하나에 들어가도록 유지할 것(아래 static_assert).
	struct AIAgentComponent
	{
		Monster* owner = nullptr;
		int32_t actorId = -1;
		int32_t targetActorId = -1;
		float spawnPos[3]{};  // 배회의 중심점(스폰 시 네비메시에 스냅된 위치)
		float dest[3]{};      // 현재 배회 목적지
		float restUntil = 0.0f;  // 시뮬레이션 시간(초)
		float moveUntil = 0.0f;  // 배회 이동 타임아웃
		float destroyAt = 0.0f;  // 사망 후 소멸 시각
		uint8_t patrolPhase = 0; // PatrolPhase
		uint8_t conditions = 0;  // ConditionBit 조합
		uint8_t action = 0;      // Action
		uint8_t nextState = 0;   // 이번 틱에 있어야 할 syncnet::AIState
		uint8_t flags = 0;       // AgentFlag
		uint8_t reserved[3]{};
	};

	static_assert(sizeof(AIAgentComponent) <= 64, "에이전트 레코드가 캐시 라인을 넘었다");

	// Map::InitEcs 가 부른다. EntityManager 는 등록된 타입만 다룰 수 있다.
	void RegisterComponents(engine::EntityManager& entityManager);

	class MonsterAISystem
	{
	public:
		// 교전 중/사망 처리 중에는 매 틱, 배회 중에는 이 주기로만 사고한다.
		static constexpr uint32_t kEngagedThinkInterval = 1;
		static constexpr uint32_t kIdleThinkInterval = 10;

		// 사망 후 소멸까지의 지연(초). 예전 트리의 Delay(2000ms) 자리다.
		static constexpr float kDestroyDelaySec = 2.0f;

		MonsterAISystem(Map* map, engine::EntityManager& entityManager)
			: map_(map), entityManager_(&entityManager) {}

		// 스폰 시 슬롯(컴포넌트)을 만든다. 해제는 Actor::Clear 의 DestroyEntity 가 함께 처리한다.
		void Register(Monster* monster);

		// 다음 틱에 반드시 사고하게 만든다(피격/사망 등 즉시 반응이 필요한 사건).
		void Wake(Monster* monster);

		// Map::UpdateActors 가 매 틱 한 번 부른다.
		void Update(float deltaTime);

		size_t AgentCount() const;
		uint32_t CurrentTick() const { return tick_; }
		float WorldTime() const { return worldTime_; }

		// 지금까지 결정을 내린 횟수(개체×틱). 스케줄링이 실제로 걸리는지 보는 창이다 —
		// 배회만 하는 몬스터라면 틱 수의 1/kIdleThinkInterval 근처여야 한다.
		uint64_t ThinkCount() const { return thinkCount_; }

	private:
		enum PatrolPhase : uint8_t { kPatrolIdle, kPatrolMoving, kPatrolResting };
		enum AgentFlag : uint8_t { kDeadHandled = 1 << 0 };

		// 한 틱의 패스들. 각각 하나의 동종 작업만 한다.
		void CollectDue(AIScheduleComponent* schedules, size_t count);
		void EvaluateAlive(AIAgentComponent* agents);
		void EvaluateDetect(AIAgentComponent* agents);
		void EvaluateAttackRange(AIAgentComponent* agents);
		void Decide(AIAgentComponent* agents);
		void RunPatrol(AIAgentComponent* agents);
		void RunChase(AIAgentComponent* agents);
		void RunAttack(AIAgentComponent* agents);
		void RunDead(AIAgentComponent* agents);
		void FlushStates(AIAgentComponent* agents);
		void Reschedule(AIAgentComponent* agents, AIScheduleComponent* schedules);

		void IssuePatrolTarget(AIAgentComponent& agent);
		float NextRestSeconds();

		Map* map_;
		engine::EntityManager* entityManager_;
		INavMovement* nav_ = nullptr;

		uint32_t tick_ = 0;
		float worldTime_ = 0.0f; // 시뮬레이션 시간(초). 벽시계를 쓰지 않는다.
		uint64_t thinkCount_ = 0;
		uint32_t rngState_ = 0x9E3779B9u; // 휴식 시간용 xorshift

		// 패스 사이에 개체를 넘기는 버킷. 슬롯 인덱스만 담는다.
		// (한 틱 안에서는 컴포넌트 배열이 재배치되지 않으므로 인덱스가 유효하다 —
		//  몬스터 제거는 Map 이 나중 단계에서 모아 처리한다.)
		std::vector<uint32_t> due_;
		std::vector<uint32_t> alive_;
		std::vector<uint32_t> engaged_;
		std::vector<uint32_t> patrolling_;
		std::vector<uint32_t> chasing_;
		std::vector<uint32_t> attacking_;
		std::vector<uint32_t> dying_;
	};
}

// MonsterBTRunner 가 쓰는 ECS 백엔드 전략 테이블.
class MonsterEcsBT
{
public:
	static const MonsterBTRunner::Ops& Ops();
};
