#pragma once

#include <chrono>  // steady_clock
#include <cstdint>
#include <random>  // mt19937, uniform_int_distribution

#include "BTDebugNodeIds.h"
#include "Monster.h"
#include "Map.h"
#include "INavMovement.h"
#include "LogHelper.h"

//---------------------------------------------------------------------------------------
// 몬스터 AI 노드 로직의 단일 원본.
//
// BT 백엔드는 두 가지다(Monster::BTBackend).
//   - BTCpp    : behaviortree_cpp + GameData/Monster.xml (BT 디버그 뷰어 지원)
//   - CodeBase : ../BehaviorTree(인하우스) + 코드 빌더 (틱 비용이 낮다 — 기본값)
// 예전에는 같은 게임 로직을 두 프레임워크의 노드 클래스로 각각 구현해 두 파일이 통째로
// 중복됐다. 지금은 로직을 아래의 백엔드 중립 구조체 하나로만 쓰고, 각 백엔드의 노드 클래스는
// 템플릿 어댑터(MonsterBT.cpp / MonsterCodeBaseBT.cpp)가 컴파일 타임에 생성한다.
//
// 로직 구조체의 요구 사항:
//   - static constexpr NodeKind    kKind     : 어댑터가 붙일 기반 클래스(조건/액션) 선택
//   - static constexpr const char* kName     : BT XML 의 노드 ID 이자 디버그 표시 이름
//   - static constexpr uint16_t    kDebugId  : BTDebugNodeId
//   - TickResult Tick(Monster*)              : 실제 로직. 노드별 상태는 구조체 멤버로 둔다.
//
// Tick 은 헤더에 inline 으로 둔다. 어댑터의 가상 함수 안으로 그대로 인라인되어야
// 리팩터링 전과 틱 비용이 같기 때문이다(호출 한 단계도 추가되지 않는다).
//
// 트리 구조(토폴로지)는 여전히 백엔드별로 따로 기술한다 — BTCpp 는 GameData/Monster.xml,
// CodeBase 는 MonsterCodeBaseBT.cpp 의 빌더. 둘이 어긋나지 않는지는
// UnitTest/MonsterBTTest.cpp 가 두 백엔드를 같은 시나리오로 돌려 고정한다.
//---------------------------------------------------------------------------------------

namespace monsterbt
{
	enum class NodeKind : uint8_t
	{
		Condition,
		Action,
	};

	// 백엔드 중립 상태값. 어댑터가 각 프레임워크의 열거형으로 변환한다.
	enum class Status : uint8_t
	{
		Success,
		Failure,
		Running,
	};

	// reason 은 BT 디버그 뷰어에 실리는 사유 문자열이다(항상 리터럴).
	// 디버그를 쓰지 않는 백엔드/빌드에서는 어댑터가 버리고, 최적화 단계에서 함께 사라진다.
	struct TickResult
	{
		Status status;
		const char* reason;
	};

	inline constexpr TickResult Success(const char* reason) { return { Status::Success, reason }; }
	inline constexpr TickResult Failure(const char* reason) { return { Status::Failure, reason }; }
	inline constexpr TickResult Running(const char* reason) { return { Status::Running, reason }; }

	//-----------------------------------------------------------------------------------
	// 조건 노드
	//-----------------------------------------------------------------------------------

	// 체력이 남아 있으면 성공. 실패하면 트리가 사망 분기로 넘어간다.
	struct CheckHealth
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "ConditionCheckHealth";
		static constexpr uint16_t kDebugId = BTDebugNodeId::ConditionCheckHealth;

		TickResult Tick(Monster* monster)
		{
			if (monster->GetHealth() > 0)
				return Success("health > 0");

			return Failure("health <= 0");
		}
	};

	struct DetectEnemy
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "ConditionDetectEnemy";
		static constexpr uint16_t kDebugId = BTDebugNodeId::ConditionDetectEnemy;

		unsigned int detectTick_ = 0;                       // 스태거링용 틱 카운터.
		static constexpr unsigned int kDetectInterval = 10; // 배회 중에는 N틱마다 1회만 전수 스캔.

		TickResult Tick(Monster* monster)
		{
			// 교전 중(타겟 보유)에는 전체 재탐색 대신 그 대상이 아직 유효한지만 확인한다.
			// 전체 스캔은 시야 반경의 모든 셀을 훑고 후보마다 시야 판정을 하지만, 검증은 대상 하나만 본다.
			if (monster->targetActorId_ >= 0)
			{
				if (monster->GetMap()->IsTargetVisible(monster, monster->targetActorId_))
				{
					monster->SetState(syncnet::AIState_Detect);
					return Success("target still visible");
				}
				monster->targetActorId_ = -1; // 놓쳤다 — 아래에서 새 대상을 찾는다.
			}

			// 스태거링: 적 탐지 그리드 스캔은 비싸다(월드 업데이트 병목). 배회 중에는 N틱마다 1회만
			// 실제 스캔하고, actorId 로 위상을 분산해 같은 틱에 모든 몬스터가 몰리지 않게 한다.
			const unsigned int phase = static_cast<unsigned int>(monster->GetActorId());
			if (((phase + detectTick_++) % kDetectInterval) != 0)
			{
				// 이번 틱은 스캔 생략 → 배회 유지.
				monster->SetState(syncnet::AIState_Patrol);
				return Failure("detect skipped (staggered)");
			}

			monster->targetActorId_ = monster->GetMap()->DetectEnemy(monster);
			if (monster->targetActorId_ >= 0)
			{
				monster->SetState(syncnet::AIState_Detect);
				return Success("enemy detected");
			}

			monster->SetState(syncnet::AIState_Patrol);
			return Failure("enemy not found");
		}
	};

	struct AttackRange
	{
		static constexpr NodeKind kKind = NodeKind::Condition;
		static constexpr const char* kName = "ConditionAttackRange";
		static constexpr uint16_t kDebugId = BTDebugNodeId::ConditionAttackRange;

		TickResult Tick(Monster* monster)
		{
			if (monster->AttackRange() >= 0)
				return Success("target in attack range");

			return Failure("target out of range");
		}
	};

	//-----------------------------------------------------------------------------------
	// 액션 노드
	//-----------------------------------------------------------------------------------

	// 배회(patrol) 노드.
	// spawn 주변 임의 지점으로 이동하다가 목적지에 도달하면 잠시 휴식한 뒤 다음 지점을 고른다.
	// 적 탐지 반응성을 유지하기 위해 매 틱 Success 를 반환하고(트리는 매 틱 루트부터 재평가),
	// 이동/휴식 진행 상태는 노드 내부에서 관리한다.
	//
	// 튜닝 값은 아래 상수로 둔다. (BT XML 포트로 노출하면 PortsList 의 문자열이
	//  BehaviorTree 라이브러리 매니페스트로 전달되며 힙 손상이 발생하므로 사용하지 않는다.)
	struct Patrol
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "ActionPatrol";
		static constexpr uint16_t kDebugId = BTDebugNodeId::ActionPatrol;

		static constexpr float kPatrolRadius = 40.0f;       // 배회 반경(기본값 ~18 보다 크게).
		static constexpr int   kRestMinMs = 100;            // 목적지 도달 후 최소 휴식(ms).
		static constexpr int   kRestMaxMs = 1000;           // 목적지 도달 후 최대 휴식(ms).
		static constexpr float kArriveDistSq = 1.5f * 1.5f; // 도달 판정 수평 거리^2.
		static constexpr int   kMoveTimeoutMs = 8000;       // 이동 최대 허용 시간.

		enum class Phase { Idle, Moving, Resting };
		Phase phase_ = Phase::Idle;
		float dest_[3] = { 0, 0, 0 };                     // 현재 목적지.
		std::chrono::steady_clock::time_point restUntil_; // 휴식 종료 시각.
		std::chrono::steady_clock::time_point moveUntil_; // 이동 타임아웃(도달 못해도 강제 종료).

		// 목적지까지의 수평(x,z) 거리^2.
		static float HorizontalDistSq(const float* a, const float* b)
		{
			const float dx = a[0] - b[0];
			const float dz = a[2] - b[2];
			return dx * dx + dz * dz;
		}

		// 새 배회 목적지를 발급한다.
		void IssueTarget(INavMovement* nav, int id, const float* spawnPos)
		{
			if (nav->Patrol(id, spawnPos, kPatrolRadius, dest_))
			{
				phase_ = Phase::Moving;
				moveUntil_ = std::chrono::steady_clock::now() + std::chrono::milliseconds(kMoveTimeoutMs);
			}
		}

		TickResult Tick(Monster* monster)
		{
			INavMovement* nav = monster->GetMap()->GetNavMap();
			const int id = monster->GetActorId();
			const auto now = std::chrono::steady_clock::now();

			switch (phase_)
			{
			case Phase::Moving:
			{
				const float* pos = nav->GetPos(id);
				if (HorizontalDistSq(pos, dest_) <= kArriveDistSq || now >= moveUntil_)
				{
					// 도달(또는 타임아웃) → 랜덤 휴식 시작.
					static thread_local std::mt19937 rng{ std::random_device{}() };
					const int restMs = std::uniform_int_distribution<int>(kRestMinMs, kRestMaxMs)(rng);
					nav->Stop(id);
					phase_ = Phase::Resting;
					restUntil_ = now + std::chrono::milliseconds(restMs);
					return Success("arrived, resting");
				}
				return Success("moving to patrol point");
			}
			case Phase::Resting:
			{
				if (now >= restUntil_)
				{
					nav->Resume(id);
					IssueTarget(nav, id, monster->GetSpawnPos());
					return Success("rest done, new patrol point");
				}
				return Success("resting");
			}
			case Phase::Idle:
			default:
				IssueTarget(nav, id, monster->GetSpawnPos());
				return Success("patrol command issued");
			}
		}
	};

	struct Chase
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "ActionChase";
		static constexpr uint16_t kDebugId = BTDebugNodeId::ActionChase;

		TickResult Tick(Monster* monster)
		{
			monster->SetState(syncnet::AIState_Detect);
			monster->Resume();
			INavMovement* nav = monster->GetMap()->GetNavMap();
			nav->SetMoveTarget(monster->GetActorId(), nav->GetPos(monster->targetActorId_), false);
			return Success("chase target position updated");
		}
	};

	struct Attack
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "ActionAttack";
		static constexpr uint16_t kDebugId = BTDebugNodeId::ActionAttack;

		TickResult Tick(Monster* monster)
		{
			monster->SetState(syncnet::AIState_Attack);
			monster->Attack();
			return Success("attack command issued");
		}
	};

	struct Dead
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "ActionDead";
		static constexpr uint16_t kDebugId = BTDebugNodeId::ActionDead;

		TickResult Tick(Monster* monster)
		{
			monster->SetState(syncnet::AIState::AIState_Dead);
			monster->NotifyKilledBy(); // 킬한 플레이어에게 사망 이벤트 발행(최초 1회)
			LOG.info("Monster dead");
			return Success("dead state applied");
		}
	};

	struct Destroyed
	{
		static constexpr NodeKind kKind = NodeKind::Action;
		static constexpr const char* kName = "ActionDestroyed";
		static constexpr uint16_t kDebugId = BTDebugNodeId::ActionDestroyed;

		TickResult Tick(Monster* monster)
		{
			monster->SetState(syncnet::AIState::AIState_Destroyed);
			LOG.info("Monster destoryed");
			return Success("destroyed state applied");
		}
	};

	// 트리에 등장하는 노드 전부. 백엔드 어댑터가 이 목록으로 노드 타입을 한꺼번에 등록한다.
	// 노드를 추가하면 여기에도 넣고, 두 백엔드의 트리 구조에 배치한다.
	template <class... Logics>
	struct NodeList {};

	using AllNodes = NodeList<
		CheckHealth,
		DetectEnemy,
		AttackRange,
		Patrol,
		Chase,
		Attack,
		Dead,
		Destroyed>;
}
