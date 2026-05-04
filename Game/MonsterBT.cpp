#include "MonsterBT.h"
#include "behaviortree_cpp/bt_factory.h"
#include "Monster.h"
#include "World.h"
#include "LogHelper.h"
#include <fstream> // std::ifstream
#include <sstream> // std::stringstream
#include <memory> // std::unique_ptr
#include <iostream> // std::cerr
#include "Map.h"
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
		monster_->target_agent_id_ = monster_->map()->DetectEnemy(monster_);
		if (monster_->target_agent_id_ >= 0)
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


class ActionPatrol : public BT::SyncActionNode
{
private:
	Monster* monster_;

public:
	ActionPatrol(const std::string& name, const BT::NodeConfig& config, Monster* monster) :
		BT::SyncActionNode(name, config), monster_(monster)
	{
	}

	BT::NodeStatus tick() override
	{
		monster_->map()->GetNavMap()->patrol(monster_->agent_id(), monster_->spawn_pos_, monster_->spawn_ref_);
		BT_DEBUG_RECORD(monster_, BTDebugNodeId::ActionPatrol, "ActionPatrol", BT::NodeStatus::SUCCESS, "patrol command issued");
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
		monster_->map()->GetNavMap()->setMoveTarget(monster_->map()->GetNavMap()->getPos(monster_->target_agent_id_), false, monster_->agent_id());
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
		if (monster_->health() > 0)
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
		

	return new BT::Tree(factory.createTreeFromText(loadFile("Monster.xml")));

}


