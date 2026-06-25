#include "MonsterBT.h"
#include "behaviortree_cpp/bt_factory.h"
#include "Monster.h"
#include "World.h"
#include "LogHelper.h"
#include <fstream> // std::ifstream
#include <sstream> // std::stringstream
#include <memory> // std::unique_ptr
#include <iostream> // std::cerr
#include <chrono>  // steady_clock
#include <random>  // mt19937, uniform_int_distribution
#include "Map.h"
#include "INavMovement.h"
#include "BTDebugManager.h"
#include "BTDebugNodeIds.h"


class ConditionDetectEnemy : public BT::ConditionNode
{
private:
	Monster* monster_;

public:
	ConditionDetectEnemy(const std::string& name, const BT::NodeConfig& config, Monster* monster) :
		BT::ConditionNode(name, config), monster_(monster)
	{
	}

	BT::NodeStatus tick() override
	{
		monster_->targetAgentId_ = monster_->GetMap()->DetectEnemy(monster_);
		if (monster_->targetAgentId_ >= 0)
		{
			monster_->SetState(syncnet::AIState_Detect);
			BT_DEBUG_RECORD(monster_, BTDebugNodeId::ConditionDetectEnemy, "ConditionDetectEnemy", BT::NodeStatus::SUCCESS, "enemy detected");
			//std::cout << "See enemy!" << std::endl;
			return  BT::NodeStatus::SUCCESS;
		}

		monster_->SetState(syncnet::AIState_Patrol);
		BT_DEBUG_RECORD(monster_, BTDebugNodeId::ConditionDetectEnemy, "ConditionDetectEnemy", BT::NodeStatus::FAILURE, "enemy not found");
		//std::cout << "Not see enemy" << std::endl;

		return BT::NodeStatus::FAILURE;
	}
};


// 배회(patrol) 노드.
// spawn 주변 임의 지점으로 이동하다가 목적지에 도달하면 잠시 휴식한 뒤 다음 지점을 고른다.
// 적 탐지 반응성을 유지하기 위해 매 틱 SUCCESS 를 반환하고(트리는 매 틱 루트부터 재평가),
// 이동/휴식 진행 상태는 노드 내부에서 관리한다.
//
// 튜닝 값은 아래 상수로 둔다. (BT XML 포트로 노출하면 PortsList 의 문자열이
//  BehaviorTree 라이브러리 매니페스트로 전달되며 힙 손상이 발생하므로 사용하지 않는다.)
class ActionPatrol : public BT::SyncActionNode
{
private:
	Monster* monster_;

	enum class Phase { Idle, Moving, Resting };
	Phase phase_ = Phase::Idle;
	float dest_[3] = { 0, 0, 0 };                    // 현재 목적지.
	std::chrono::steady_clock::time_point restUntil_; // 휴식 종료 시각.
	std::chrono::steady_clock::time_point moveUntil_; // 이동 타임아웃(도달 못해도 강제 종료).

	static constexpr float kPatrolRadius = 40.0f;       // 배회 반경(기본값 ~18 보다 크게).
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
	ActionPatrol(const std::string& name, const BT::NodeConfig& config, Monster* monster) :
		BT::SyncActionNode(name, config), monster_(monster)
	{
	}

	BT::NodeStatus tick() override
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
				BT_DEBUG_RECORD(monster_, BTDebugNodeId::ActionPatrol, "ActionPatrol", BT::NodeStatus::SUCCESS, "arrived, resting");
			}
			else
			{
				BT_DEBUG_RECORD(monster_, BTDebugNodeId::ActionPatrol, "ActionPatrol", BT::NodeStatus::SUCCESS, "moving to patrol point");
			}
			break;
		}
		case Phase::Resting:
		{
			if (now >= restUntil_)
			{
				nav->Resume(id);
				IssueTarget(nav, id);
				BT_DEBUG_RECORD(monster_, BTDebugNodeId::ActionPatrol, "ActionPatrol", BT::NodeStatus::SUCCESS, "rest done, new patrol point");
			}
			else
			{
				BT_DEBUG_RECORD(monster_, BTDebugNodeId::ActionPatrol, "ActionPatrol", BT::NodeStatus::SUCCESS, "resting");
			}
			break;
		}
		case Phase::Idle:
		default:
			IssueTarget(nav, id);
			BT_DEBUG_RECORD(monster_, BTDebugNodeId::ActionPatrol, "ActionPatrol", BT::NodeStatus::SUCCESS, "patrol command issued");
			break;
		}

		return BT::NodeStatus::SUCCESS;
	}
};

class ActionChase : public BT::SyncActionNode
{
private:
	Monster* monster_;

public:
	ActionChase(const std::string& name, const BT::NodeConfig& config, Monster* monster) :
		BT::SyncActionNode(name, config), monster_(monster)
	{
	}

	BT::NodeStatus tick() override
	{
		monster_->SetState(syncnet::AIState_Detect);
		monster_->Resume();
		INavMovement* nav = monster_->GetMap()->GetNavMap();
		nav->SetMoveTarget(monster_->GetActorId(), nav->GetPos(monster_->targetAgentId_), false);
		BT_DEBUG_RECORD(monster_, BTDebugNodeId::ActionChase, "ActionChase", BT::NodeStatus::SUCCESS, "chase target position updated");
		return BT::NodeStatus::SUCCESS;
	}
};

class ConditionAttackRange : public BT::ConditionNode
{
private:
	Monster* monster_;

public:
	ConditionAttackRange(const std::string& name, const BT::NodeConfig& config, Monster* monster) :
		BT::ConditionNode(name, config), monster_(monster)
	{
	}

	BT::NodeStatus tick() override
	{
		if (monster_->AttackRange() >= 0)
		{
			BT_DEBUG_RECORD(monster_, BTDebugNodeId::ConditionAttackRange, "ConditionAttackRange", BT::NodeStatus::SUCCESS, "target in attack range");
			//std::cout << "Attack Range!" << std::endl;
			return BT::NodeStatus::SUCCESS;
		}

		//std::cout << "Not Attack Range" << std::endl;
		BT_DEBUG_RECORD(monster_, BTDebugNodeId::ConditionAttackRange, "ConditionAttackRange", BT::NodeStatus::FAILURE, "target out of range");

		return BT::NodeStatus::FAILURE;
	}
};

class ActionAttack : public BT::SyncActionNode
{
private:
	Monster* monster_;

public:
	ActionAttack(const std::string& name, const BT::NodeConfig& config, Monster* monster) :
		BT::SyncActionNode(name, config), monster_(monster)
	{
	}

	BT::NodeStatus tick() override
	{
		monster_->SetState(syncnet::AIState_Attack);
		monster_->Attack();
		BT_DEBUG_RECORD(monster_, BTDebugNodeId::ActionAttack, "ActionAttack", BT::NodeStatus::SUCCESS, "attack command issued");
		//std::cout << "Attack enemy!" << std::endl;
		return BT::NodeStatus::SUCCESS;
	}
};

class ConditionCheckHealth : public BT::ConditionNode
{
private:
	Monster* monster_;

public:
	ConditionCheckHealth(const std::string& name, const BT::NodeConfig& config, Monster* monster) : 
		BT::ConditionNode(name, config), monster_(monster)
	{
	}

	BT::NodeStatus tick() override
	{

		// 체력이 0보다 크면 성공, 아니면 실패
		if (monster_->GetHealth() > 0)
		{
			BT_DEBUG_RECORD(monster_, BTDebugNodeId::ConditionCheckHealth, "ConditionCheckHealth", BT::NodeStatus::SUCCESS, "health > 0");
			return BT::NodeStatus::SUCCESS;
		}

		BT_DEBUG_RECORD(monster_, BTDebugNodeId::ConditionCheckHealth, "ConditionCheckHealth", BT::NodeStatus::FAILURE, "health <= 0");
		return BT::NodeStatus::FAILURE;
	}
};

class ActionDead : public BT::SyncActionNode
{
private:
	Monster* monster_;

public:
	ActionDead(const std::string& name, const BT::NodeConfig& config, Monster* monster) :
		BT::SyncActionNode(name, config), monster_(monster)
	{
	}

	BT::NodeStatus tick() override
	{
		// 사망 상태로 변경
		monster_->SetState(syncnet::AIState::AIState_Dead);
		// 킬한 플레이어에게 사망 이벤트 발행(최초 1회)
		monster_->NotifyKilledBy();
		BT_DEBUG_RECORD(monster_, BTDebugNodeId::ActionDead, "ActionDead", BT::NodeStatus::SUCCESS, "dead state applied");
		LOG.info("Monster dead");
		return BT::NodeStatus::SUCCESS;
	}
};

class ActionDestroyed : public BT::SyncActionNode
{
private:
	Monster* monster_;

public:
	ActionDestroyed(const std::string& name, const BT::NodeConfig& config, Monster* monster) :
		BT::SyncActionNode(name, config), monster_(monster) 
	{
	}

	BT::NodeStatus tick() override
	{
		// 파괴 상태로 변경
		monster_->SetState(syncnet::AIState::AIState_Destroyed);
		BT_DEBUG_RECORD(monster_, BTDebugNodeId::ActionDestroyed, "ActionDestroyed", BT::NodeStatus::SUCCESS, "destroyed state applied");
		LOG.info("Monster destoryed");
		return BT::NodeStatus::SUCCESS;
	}
};

std::string loadFile(std::string filename) 
{
	try {
		std::ifstream file(filename);
		if (file)
		{
			std::stringstream buffer;
			buffer << file.rdbuf();
			return buffer.str();
		}
		else
		{
			LOG.error("%s file not found.", filename.c_str());
			return nullptr;
		}
	}
	catch (std::exception& e)
	{
		// 일반 예외 처리
		LOG.error("Exception: " + std::string(e.what()));
		return nullptr;
	}
	return nullptr;
}


BT::Tree* MonsterBT::createTree(Monster* monster) 
{
	BT::BehaviorTreeFactory factory;

	// DetectEnemy 노드를 등록할 때 Monster 포인터를 전달
	factory.registerBuilder<ConditionDetectEnemy>("ConditionDetectEnemy", [monster](const std::string& name, const BT::NodeConfig& config) {
		return std::make_unique<ConditionDetectEnemy>(name, config, monster);
		});

	factory.registerBuilder<ActionPatrol>("ActionPatrol", [monster](const std::string& name, const BT::NodeConfig& config) {
		return std::make_unique<ActionPatrol>(name, config, monster);
		});

	factory.registerBuilder<ActionChase>("ActionChase", [monster](const std::string& name, const BT::NodeConfig& config) {
		return std::make_unique<ActionChase>(name, config, monster);
		});

	factory.registerBuilder<ConditionAttackRange>("ConditionAttackRange", [monster](const std::string& name, const BT::NodeConfig& config) {
		return std::make_unique<ConditionAttackRange>(name, config, monster);
		});

	factory.registerBuilder<ActionAttack>("ActionAttack", [monster](const std::string& name, const BT::NodeConfig& config) {
		return std::make_unique<ActionAttack>(name, config, monster);
		});

	factory.registerBuilder<ConditionCheckHealth>("ConditionCheckHealth", [monster](const std::string& name, const BT::NodeConfig& config) {
		return std::make_unique<ConditionCheckHealth>(name, config, monster);
		});

	factory.registerBuilder<ActionDead>("ActionDead", [monster](const std::string& name, const BT::NodeConfig& config) {
		return std::make_unique<ActionDead>(name, config, monster);
		});

	factory.registerBuilder<ActionDestroyed>("ActionDestroyed", [monster](const std::string& name, const BT::NodeConfig& config) {	
		return std::make_unique<ActionDestroyed>(name, config, monster);
		});
		

	return new BT::Tree(factory.createTreeFromText(loadFile("GameData/Monster.xml")));

}


