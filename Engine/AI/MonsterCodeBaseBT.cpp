#include "MonsterCodeBaseBT.h"
#include "../BehaviorTree/BehaviorTree.h"
#include "Monster.h"
#include "World.h"
#include "Map.h"
#include "INavMovement.h"
#include "LogHelper.h"
#include <chrono>  // steady_clock
#include <random>  // mt19937, uniform_int_distribution

// 인하우스 BT(../BehaviorTree) 로 구현한 몬스터 트리.
// GameData/Monster.xml(behaviortree_cpp) 과 노드 로직/트리 구조가 1:1 로 동일하다.
// 두 프레임워크를 같은 게임 로직으로 벤치마크 비교하기 위한 것이므로,
// MonsterBT.cpp 의 노드를 수정하면 여기도 같이 맞춰야 한다.
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

class Condition_CheckHealth : public BT::Condition
{
private:
	Monster* monster_;

public:
	static BT::Behavior* Create(Monster* monster) { return new Condition_CheckHealth(monster); }
	virtual std::string Name() override { return "ConditionCheckHealth"; }

protected:
	Condition_CheckHealth(Monster* monster) : Condition(false), monster_(monster) {}
	virtual ~Condition_CheckHealth() {}

	virtual BT::EStatus Update() override
	{
		return monster_->GetHealth() > 0 ? BT::EStatus::Success : BT::EStatus::Failure;
	}
};

class Condition_DetectEnemy : public BT::Condition
{
private:
	Monster* monster_;
	unsigned int detectTick_ = 0;                  // 스태거링용 틱 카운터.
	static constexpr unsigned int kDetectInterval = 10; // 배회 중에는 N틱마다 1회만 전수 스캔.

public:
	static BT::Behavior* Create(Monster* monster) { return new Condition_DetectEnemy(monster); }
	virtual std::string Name() override { return "ConditionDetectEnemy"; }

protected:
	Condition_DetectEnemy(Monster* monster) : Condition(false), monster_(monster) {}
	virtual ~Condition_DetectEnemy() {}

	virtual BT::EStatus Update() override
	{
		// (MonsterBT.cpp 의 ConditionDetectEnemy 와 동일 로직)
		// 교전 중(타겟 보유)에는 전체 재탐색 대신 그 대상이 아직 유효한지만 확인한다.
		// 전체 스캔은 시야 반경의 모든 셀을 훑고 후보마다 시야 판정을 하지만, 검증은 대상 하나만 본다.
		if (monster_->targetAgentId_ >= 0)
		{
			if (monster_->GetMap()->IsTargetVisible(monster_, monster_->targetAgentId_))
			{
				monster_->SetState(syncnet::AIState_Detect);
				return BT::EStatus::Success;
			}
			monster_->targetAgentId_ = -1; // 놓쳤다 — 아래에서 새 대상을 찾는다.
		}

		// 스태거링: 적 탐지 그리드 스캔은 비싸다. 배회 중에는 N틱마다 1회만 실제 스캔하고,
		// actorId 로 위상을 분산해 같은 틱에 모든 몬스터가 몰리지 않게 한다.
		const unsigned int phase = static_cast<unsigned int>(monster_->GetActorId());
		if (((phase + detectTick_++) % kDetectInterval) != 0)
		{
			monster_->SetState(syncnet::AIState_Patrol);
			return BT::EStatus::Failure;
		}

		monster_->targetAgentId_ = monster_->GetMap()->DetectEnemy(monster_);
		if (monster_->targetAgentId_ >= 0)
		{
			monster_->SetState(syncnet::AIState_Detect);
			return BT::EStatus::Success;
		}

		monster_->SetState(syncnet::AIState_Patrol);
		return BT::EStatus::Failure;
	}
};

class Condition_AttackRange : public BT::Condition
{
private:
	Monster* monster_;

public:
	static BT::Behavior* Create(Monster* monster) { return new Condition_AttackRange(monster); }
	virtual std::string Name() override { return "ConditionAttackRange"; }

protected:
	Condition_AttackRange(Monster* monster) : Condition(false), monster_(monster) {}
	virtual ~Condition_AttackRange() {}

	virtual BT::EStatus Update() override
	{
		return monster_->AttackRange() >= 0 ? BT::EStatus::Success : BT::EStatus::Failure;
	}
};

class Action_Attack : public BT::Action
{
private:
	Monster* monster_;

public:
	static BT::Behavior* Create(Monster* monster) { return new Action_Attack(monster); }
	virtual std::string Name() override { return "ActionAttack"; }

protected:
	Action_Attack(Monster* monster) : monster_(monster) {}
	virtual ~Action_Attack() {}

	virtual BT::EStatus Update() override
	{
		monster_->SetState(syncnet::AIState_Attack);
		monster_->Attack();
		return BT::EStatus::Success;
	}
};

class Action_Chase : public BT::Action
{
private:
	Monster* monster_;

public:
	static BT::Behavior* Create(Monster* monster) { return new Action_Chase(monster); }
	virtual std::string Name() override { return "ActionChase"; }

protected:
	Action_Chase(Monster* monster) : monster_(monster) {}
	virtual ~Action_Chase() {}

	virtual BT::EStatus Update() override
	{
		monster_->SetState(syncnet::AIState_Detect);
		monster_->Resume();
		INavMovement* nav = monster_->GetMap()->GetNavMap();
		nav->SetMoveTarget(monster_->GetActorId(), nav->GetPos(monster_->targetAgentId_), false);
		return BT::EStatus::Success;
	}
};

// 배회 노드. MonsterBT.cpp 의 ActionPatrol 과 동일: spawn 주변 임의 지점으로 이동하다
// 도달하면 잠시 휴식 후 다음 지점을 고른다. 매 틱 Success 를 반환하고 진행 상태는 내부 관리.
class Action_Patrol : public BT::Action
{
private:
	Monster* monster_;

	enum class Phase { Idle, Moving, Resting };
	Phase phase_ = Phase::Idle;
	float dest_[3] = { 0, 0, 0 };                    // 현재 목적지.
	std::chrono::steady_clock::time_point restUntil_; // 휴식 종료 시각.
	std::chrono::steady_clock::time_point moveUntil_; // 이동 타임아웃(도달 못해도 강제 종료).

	static constexpr float kPatrolRadius = 40.0f;       // 배회 반경.
	static constexpr int   kRestMinMs = 100;            // 목적지 도달 후 최소 휴식(ms).
	static constexpr int   kRestMaxMs = 1000;           // 목적지 도달 후 최대 휴식(ms).
	static constexpr float kArriveDistSq = 1.5f * 1.5f; // 도달 판정 수평 거리^2.
	static constexpr int   kMoveTimeoutMs = 8000;       // 이동 최대 허용 시간.

	// 목적지까지의 수평(x,z) 거리^2.
	float HorizontalDistSq(const float* a, const float* b) const
	{
		const float dx = a[0] - b[0];
		const float dz = a[2] - b[2];
		return dx * dx + dz * dz;
	}

	// 새 배회 목적지를 발급한다.
	void IssueTarget(INavMovement* nav, int id)
	{
		if (nav->Patrol(id, monster_->spawnPos_, kPatrolRadius, dest_))
		{
			phase_ = Phase::Moving;
			moveUntil_ = std::chrono::steady_clock::now() + std::chrono::milliseconds(kMoveTimeoutMs);
		}
	}

public:
	static BT::Behavior* Create(Monster* monster) { return new Action_Patrol(monster); }
	virtual std::string Name() override { return "ActionPatrol"; }

protected:
	Action_Patrol(Monster* monster) : monster_(monster) {}
	virtual ~Action_Patrol() {}

	virtual BT::EStatus Update() override
	{
		INavMovement* nav = monster_->GetMap()->GetNavMap();
		const int id = monster_->GetActorId();
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
			}
			break;
		}
		case Phase::Resting:
		{
			if (now >= restUntil_)
			{
				nav->Resume(id);
				IssueTarget(nav, id);
			}
			break;
		}
		case Phase::Idle:
		default:
			IssueTarget(nav, id);
			break;
		}

		return BT::EStatus::Success;
	}
};

class Action_Dead : public BT::Action
{
private:
	Monster* monster_;

public:
	static BT::Behavior* Create(Monster* monster) { return new Action_Dead(monster); }
	virtual std::string Name() override { return "ActionDead"; }

protected:
	Action_Dead(Monster* monster) : monster_(monster) {}
	virtual ~Action_Dead() {}

	virtual BT::EStatus Update() override
	{
		monster_->SetState(syncnet::AIState::AIState_Dead);
		monster_->NotifyKilledBy(); // 킬한 플레이어에게 사망 이벤트 발행(최초 1회)
		LOG.info("Monster dead");
		return BT::EStatus::Success;
	}
};

class Action_Destroyed : public BT::Action
{
private:
	Monster* monster_;

public:
	static BT::Behavior* Create(Monster* monster) { return new Action_Destroyed(monster); }
	virtual std::string Name() override { return "ActionDestroyed"; }

protected:
	Action_Destroyed(Monster* monster) : monster_(monster) {}
	virtual ~Action_Destroyed() {}

	virtual BT::EStatus Update() override
	{
		monster_->SetState(syncnet::AIState::AIState_Destroyed);
		LOG.info("Monster destoryed");
		return BT::EStatus::Success;
	}
};

// behaviortree_cpp 의 <Delay delay_msec="..."> 에 해당하는 데코레이터.
// 최초 틱에 타이머를 시작해 경과 전까지 Running, 경과 후 자식을 틱하고 그 결과를 반환한다.
class Decorator_Delay : public BT::Decorator
{
private:
	std::chrono::milliseconds delay_;
	std::chrono::steady_clock::time_point until_;

public:
	static BT::Behavior* Create(int delayMs) { return new Decorator_Delay(delayMs); }
	virtual std::string Name() override { return "Delay"; }
	virtual void Release() override
	{
		if (Child != nullptr)
			Child->Release();
		delete this;
	}

protected:
	Decorator_Delay(int delayMs) : delay_(delayMs) {}
	virtual ~Decorator_Delay() {}

	virtual void OnInitialize() override
	{
		until_ = std::chrono::steady_clock::now() + delay_;
	}

	virtual BT::EStatus Update() override
	{
		if (std::chrono::steady_clock::now() < until_)
			return BT::EStatus::Running;
		return Child->Tick();
	}
};

BT::BehaviorTree* MonsterCodeBaseBT::createTree(Monster* monster)
{
	BT::BehaviorTreeBuilder builder;
	return builder
		.Selector()                                                          // Fallback (root)
			->Sequence()                                                     // 생존 분기
				->Condition(Condition_CheckHealth::Create(monster))->Back()
				->Selector()                                                 // Fallback
					->Sequence()
						->Condition(Condition_DetectEnemy::Create(monster))->Back()
						->Selector()                                         // Fallback
							->Sequence()
								->Condition(Condition_AttackRange::Create(monster))->Back()
								->Action(Action_Attack::Create(monster))->Back()
							->Back()
							->Action(Action_Chase::Create(monster))->Back()
						->Back()
					->Back()
					->Action(Action_Patrol::Create(monster))->Back()
				->Back()
			->Back()
			->Sequence()                                                     // 사망 분기
				->Action(Action_Dead::Create(monster))->Back()
				->Action(Decorator_Delay::Create(2000))
					->Action(Action_Destroyed::Create(monster))->Back()
				->Back()
			->Back()
		->End();
}
