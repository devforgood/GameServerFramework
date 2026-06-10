#pragma once
#include "Component.h"
#include "EventBroker.h"
#include "EventMessage.h"

class PlayerEventBroker : public ComponentBase<PlayerEventBroker>
{
private:
    engine::event_broker::EventBroker<EventMessage> eventBroker_;
public:
    void publish(const EventMessage& message) {
        eventBroker_.publishImmediate(message);
	}

    template<typename TObject, auto Method>
    void subscribe(TObject* object) {
        eventBroker_.template subscribe<TObject, Method>(object);
	}

    template<typename TObject, typename TEvent, auto Method>
    void subscribe(TObject* object) {
        eventBroker_.template subscribe<TObject, TEvent, Method>(object);
	}
};

