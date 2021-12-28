#pragma once
#include "location_type.h"
#include "BaseGameEntity.h"

class State;
class Miner :
    public BaseGameEntity
{
private:

    State* current_state_;
    location_type location_;
    int gold_carried_;
    int money_in_bank_;
    int thirst_;
    int fatigue_;

public:
    Miner(int id);
    void Update();
    void ChangeState(State* newState);
};

