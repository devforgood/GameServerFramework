#include "pch.h"
#include "Miner.h"
#include "State.h"
#include <cassert>

void Miner::Update()
{
	thirst_ += 1;
	if (current_state_)
		current_state_->Execute(this);
}

void Miner::ChangeState(State* newState)
{
	assert(current_state_ && newState);

	current_state_->Exit(this);

	current_state_ = newState;

	current_state_->Enter(this);
}