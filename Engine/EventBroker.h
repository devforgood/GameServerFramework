#pragma once

#include "EventQueue.h"
#include "EventBus.h"
#include "NonThreadSafeBus.h"

namespace Engine {
namespace EventBroker {

template <
    typename MessageType, 
    template<typename> class QueuePolicy = NonThreadSafeQueue,
    template<typename> class BusPolicy = NonThreadSafeBus
>
class EventBroker {
public:
    template<typename TObject, auto Method>
    void subscribe(TObject* object) {
        bus_.template subscribe<TObject, Method>(object);
    }

    void publishImmediate(const MessageType& message) {
        bus_.publish(message);
    }

    void enqueue(const MessageType& message) {
        queue_.push(message);
    }

    void enqueue(MessageType&& message) {
        queue_.push(std::move(message));
    }

    void processEvents() {
        MessageType message;
        while (queue_.pop(message)) {
            bus_.publish(message);
        }
    }

    void clear() {
        queue_.clear();
        bus_.clear();
    }

private:
    QueuePolicy<MessageType> queue_;
    BusPolicy<MessageType> bus_;
};

} // namespace EventBroker
} // namespace Engine
