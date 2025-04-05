#include "Monster.h"
#include "../BehaviorTree/BehaviorTree.h"
#include <random>
#include <functional>
#include "World.h"
#include "DetourCommon.h"
#include "MathHelper.h"
#include "behaviortree_cpp/bt_factory.h"

extern std::_Binder<std::_Unforced, std::uniform_int_distribution<>&, std::default_random_engine&> dice;


class Condition_DetectEnemy : public BT::Condition
{
private:
	Monster* monster_;

public:
	static Behavior* Create(bool InIsNegation, Monster * monster) { return new Condition_DetectEnemy(InIsNegation, monster); }
	virtual std::string Name() override { return "Condition_DetectEnemy"; }

protected:
	Condition_DetectEnemy(bool InIsNegation, Monster* monster)
		: Condition(InIsNegation), monster_(monster)
	{

	}

	virtual ~Condition_DetectEnemy() {}
	virtual BT::EStatus Update() override
	{
		monster_->target_agent_id_ = monster_->world()->DetectEnemy(monster_->agent_id());
		if (monster_->target_agent_id_ >=0)
		{
			monster_->SetState(syncnet::AIState_Detect);
			std::cout << "See enemy!" << std::endl;
			return !IsNegation ? BT::EStatus::Success : BT::EStatus::Failure;
		}
		else
		{
			monster_->SetState(syncnet::AIState_Patrol);
			std::cout << "Not see enemy" << std::endl;
			return !IsNegation ? BT::EStatus::Failure : BT::EStatus::Success;
		}
	}
};

class Action_Chase :public BT::Action
{
private:
	Monster* monster_;

public:
	static Behavior* Create(Monster* monster) { return new Action_Chase(monster); }
	virtual std::string Name() override { return "Action_Follow"; }

protected:
	Action_Chase(Monster* monster) : monster_(monster){}
	virtual ~Action_Chase() {}
	virtual BT::EStatus Update() override
	{
		monster_->world()->map()->setMoveTarget(monster_->world()->map()->getPos(monster_->target_agent_id_), false, monster_->agent_id());

		return BT::EStatus::Success;
	}
};

class Action_Patrol :public BT::Action
{
private:
	Monster* monster_;

public:
	static Behavior* Create(Monster* monster) { return new Action_Patrol(monster); }
	virtual std::string Name() override { return "Action_Patrol"; }

protected:
	Action_Patrol(Monster* monster) : monster_(monster) {}
	virtual ~Action_Patrol() {}
	virtual BT::EStatus Update() override
	{
		monster_->world()->map()->patrol(monster_->agent_id(), monster_->spawn_pos_, monster_->spawn_ref_);
		return BT::EStatus::Success;
	}
};

// todo : "Attack" , "Flee"
class Condition_AttackRange : public BT::Condition
{
private:
	Monster* monster_;

public:
	static Behavior* Create(bool InIsNegation, Monster* monster) { return new Condition_AttackRange(InIsNegation, monster); }
	virtual std::string Name() override { return "Condition_AttackRange"; }

protected:
	Condition_AttackRange(bool InIsNegation, Monster* monster)
		: Condition(InIsNegation), monster_(monster)
	{

	}

	virtual ~Condition_AttackRange() {}
	virtual BT::EStatus Update() override
	{
		if (monster_->AttackRange() >= 0)
		{
			monster_->SetState(syncnet::AIState_Attack);
			std::cout << "Attack enemy!" << std::endl;
			return !IsNegation ? BT::EStatus::Success : BT::EStatus::Failure;
		}
		else
		{
			monster_->SetState(syncnet::AIState_Detect);
			std::cout << "Chase enemy" << std::endl;
			return !IsNegation ? BT::EStatus::Failure : BT::EStatus::Success;
		}
	}
};



int lua_Attack(lua_State* L) {
	Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
	std::cout << "Executing Attack! " << monster->name_ << std::endl;
	lua_pushstring(L, "SUCCESS"); // 결과 반환
	return 1;
}

int lua_Defend(lua_State* L) {
	Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
	std::cout << "Executing Defend! " << monster->name_ << std::endl;
	lua_pushstring(L, "FAILURE");
	return 1;
}

int lua_Patrol(lua_State* L) {
	Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
	std::cout << "Executing Patrol! " << monster->name_ << std::endl;
	lua_pushstring(L, "SUCCESS");
	return 1;
}

int lua_LookAround(lua_State* L) {
	Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
	std::cout << "Executing LookAround! " << monster->name_ << std::endl;
	lua_pushstring(L, "SUCCESS");
	return 1;
}



// clang-format off
static const char* xml_text = R"(

 <root BTCPP_format="4" >

     <BehaviorTree ID="MainTree">
        <Sequence name="root">
            <AlwaysSuccess/>
            <SaySomething   message="this works too" />
            <ThinkWhatToSay text="{the_answer}"/>
            <SaySomething   message="{the_answer}" />
        </Sequence>
     </BehaviorTree>

 </root>
 )";
// clang-format on


class SaySomething : public BT::SyncActionNode
{
public:
	SaySomething(const std::string& name, const BT::NodeConfig& config) :
		BT::SyncActionNode(name, config)
	{
	}

	BT::NodeStatus tick() override
	{
		std::string msg;
		getInput("message", msg);
		std::cout << msg << std::endl;
		return BT::NodeStatus::SUCCESS;
	}

	static BT::PortsList providedPorts()
	{
		return { BT::InputPort<std::string>("message") };
	}
};

class ThinkWhatToSay : public BT::SyncActionNode
{
public:
	ThinkWhatToSay(const std::string& name, const BT::NodeConfig& config) :
		BT::SyncActionNode(name, config)
	{
	}

	BT::NodeStatus tick() override
	{
		setOutput("text", "The answer is 42");
		return BT::NodeStatus::SUCCESS;
	}

	static BT::PortsList providedPorts()
	{
		return { BT::OutputPort<std::string>("text") };
	}
};


Monster::Monster(int agent_id, World* world)
	: GameObject(agent_id, world), bt_(nullptr)
{
	BT::BehaviorTreeBuilder* Builder = new BT::BehaviorTreeBuilder();
	bt_ = Builder
		->ActiveSelector()
			->Sequence()
				->Condition(Condition_DetectEnemy::Create(false, this))
					->Back()
				->ActiveSelector()
					->Sequence()
						->Condition(BT::Condition_IsHealthLow::Create(true))
							->Back()
						->Action(Action_Chase::Create(this))
							->Back()
					//	->Back()
					//->Parallel(BT::EPolicy::RequireAll, BT::EPolicy::RequireOne)
						->Condition(Condition_AttackRange::Create(true, this))
							->Back()
						->Action(BT::Action_Attack::Create())
							->Back()
						->Back()
					->Back()
				->Back()
			->Action(Action_Patrol::Create(this))
		->End();
	delete Builder;


	if(world_ != nullptr) {
		auto agent = world_->map()->getAgent(agent_id);
		if (agent != nullptr)
		{
			dtVcopy(spawn_pos_, agent->npos);
			spawn_ref_ = agent->corridor.getPath()[0];
		}
	}

	name_ = "Monster:" + std::to_string(agent_id);


	BT::BehaviorTreeFactory factory;

	factory.registerNodeType<SaySomething>("SaySomething");
	factory.registerNodeType<ThinkWhatToSay>("ThinkWhatToSay");

    tree_ = new BT::Tree(factory.createTreeFromText(xml_text));
}

Monster::~Monster()
{
	if (bt_ != nullptr)
		delete bt_;
}

void Monster::Update()
{
	bt_->Tick();
	//runBehaviorTree(this);
	tree_->tickOnce();
}

int Monster::AttackRange()
{
	const dtCrowdAgent* this_agent = world_->map()->crowd()->getAgent(agent_id());
	const dtCrowdAgent* agent = world_->map()->crowd()->getAgent(target_agent_id_);

	if (ManhattanDistance(this_agent->npos, agent->npos) > 3)
		return -1;

	float hitPoint[3];
	if (world_->map()->raycast(agent_id(), agent->npos, hitPoint) == false)
	{
		return target_agent_id_;
	}
	return -1;
}

void Monster::registerLuaFunctionAll()
{
	registerLuaFunction("Attack", lua_Attack);
	registerLuaFunction("Defend", lua_Defend);
	registerLuaFunction("Patrol", lua_Patrol);
	registerLuaFunction("LookAround", lua_LookAround);
}

