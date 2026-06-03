#pragma once

#include "EventQueue.h"
#include "EventBus.h"

namespace Engine {
namespace EventBroker {

template <
    typename MessageType, 
    template<typename> class QueuePolicy = BoostConcurrentQueue,
    typename BusLockPolicy = ThreadSafe
>
class EventBroker {
public:
    using Callback = typename EventBus<MessageType, BusLockPolicy>::Callback;
    using Token = typename EventBus<MessageType, BusLockPolicy>::Token;

    Token subscribe(Callback callback) {
        return bus_.subscribe(std::move(callback));
    }

    void unsubscribe(Token token) {
        bus_.unsubscribe(token);
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
    EventQueue<MessageType, QueuePolicy> queue_;
    EventBus<MessageType, BusLockPolicy> bus_;
};

} // namespace EventBroker
} // namespace Engine