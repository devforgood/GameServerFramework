#include "MonsterCodeBaseBT.h"
#include "../BehaviorTree/BehaviorTree.h"
#include "Monster.h"
#include "World.h"
#include "Map.h"

class Condition_DetectEnemy : public BT::Condition
{
private:
	Monster* monster_;

public:
	static Behavior* Create(bool InIsNegation, Monster* monster) { return new Condition_DetectEnemy(InIsNegation, monster); }
	virtual std::string Name() override { return "Condition_DetectEnemy"; }

protected:
	Condition_DetectEnemy(bool InIsNegation, Monster* monster)
		: Condition(InIsNegation), monster_(monster)
	{

	}

	virtual ~Condition_DetectEnemy() {}
	virtual BT::EStatus Update() override
	{
		monster_->targetAgentId_ = monster_->map()->DetectEnemy(monster_);
		if (monster_->targetAgentId_ >= 0)
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
	Action_Chase(Monster* monster) : monster_(monster) {}
	virtual ~Action_Chase() {}
	virtual BT::EStatus Update() override
	{
		monster_->map()->GetNavMap()->setMoveTarget(monster_->map()->GetNavMap()->getPos(monster_->targetAgentId_), false, monster_->agent_id());

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
		monster_->map()->GetNavMap()->patrol(monster_->agent_id(), monster_->spawnPos_, monster_->spawnRef_);
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


BT::BehaviorTree* MonsterCodeBaseBT::createTree(Monster* monster)
{
	BT::BehaviorTreeBuilder* Builder = new BT::BehaviorTreeBuilder();
	BT::BehaviorTree* tree = Builder
		->ActiveSelector()
		->Sequence()
		->Condition(Condition_DetectEnemy::Create(false, monster))
		->Back()
		->ActiveSelector()
		->Sequence()
		->Condition(BT::Condition_IsHealthLow::Create(true))
		->Back()
		->Action(Action_Chase::Create(monster))
		->Back()
		//	->Back()
		//->Parallel(BT::EPolicy::RequireAll, BT::EPolicy::RequireOne)
		->Condition(Condition_AttackRange::Create(true, monster))
		->Back()
		->Action(BT::Action_Attack::Create())
		->Back()
		->Back()
		->Back()
		->Back()
		->Action(Action_Patrol::Create(monster))
		->End();

	delete Builder;

	return tree;
}