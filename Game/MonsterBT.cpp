#include "MonsterBT.h"
#include "behaviortree_cpp/bt_factory.h"
#include "Monster.h"
#include "World.h"




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
		monster_->target_agent_id_ = monster_->world()->DetectEnemy(monster_);
		if (monster_->target_agent_id_ >= 0)
		{
			monster_->SetState(syncnet::AIState_Detect);
			//std::cout << "See enemy!" << std::endl;
			return  BT::NodeStatus::SUCCESS;
		}

		monster_->SetState(syncnet::AIState_Patrol);
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
		monster_->world()->map()->patrol(monster_->agent_id(), monster_->spawn_pos_, monster_->spawn_ref_);
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
		monster_->world()->map()->setMoveTarget(monster_->world()->map()->getPos(monster_->target_agent_id_), false, monster_->agent_id());
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
			//std::cout << "Attack Range!" << std::endl;
			return BT::NodeStatus::SUCCESS;
		}

		//std::cout << "Not Attack Range" << std::endl;

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
		//std::cout << "Attack enemy!" << std::endl;
		return BT::NodeStatus::SUCCESS;
	}
};

// clang-format off
static const char* xml_text = R"(

 <root BTCPP_format="4" >

     <BehaviorTree ID="MainTree">
        <Fallback>
			<Sequence>
				<ConditionDetectEnemy/>
				<Fallback>
					<Sequence>
						<ConditionAttackRange/>
						<ActionAttack/>
					</Sequence>
					<ActionChase/>
				</Fallback>
			</Sequence>
			<ActionPatrol/>
        </Fallback>
     </BehaviorTree>

 </root>
 )";
// clang-format on



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

	return new BT::Tree(factory.createTreeFromText(xml_text));

}
