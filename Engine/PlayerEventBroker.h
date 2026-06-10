#pragma once
#include "Component.h"
#include "EventBroker.h"
#include "EventMessage.h"

class PlayerEventBroker : public ComponentBase<PlayerEventBroker>
{
private:
    engine::event_broker::EventBroker<EventMessage> eventBroker_;
public:

};

