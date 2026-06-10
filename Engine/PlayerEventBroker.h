#pragma once
#include "Component.h"
#include "EventBroker.h"
#include "EventMessage.h"

class PlayerSkill : public ComponentBase<PlayerSkill>
{
private:
    EventBroker<EventMessage> eventBroker_;
public:

};

