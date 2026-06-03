#pragma once

#include <utility>
#include "BoostConcurrentQueue.h"
#include "NonThreadSafeQueue.h"

namespace Engine {
namespace EventBroker {

// EventQueue 껍데기 (인터페이스 래퍼)
template <typename MessageType, template<typename> class QueuePolicy = BoostConcurrentQueue>
class EventQueue {
public:
    void push(const MessageType& message) { queue_.push(message); }
    void push(MessageType&& message) { queue_.push(std::move(message)); }
    bool pop(MessageType& outMessage) { return queue_.pop(outMessage); }
    bool empty() { return queue_.empty(); }
    size_t size() { return queue_.size(); }
    void clear() { queue_.clear(); }
private:
    QueuePolicy<MessageType> queue_;
};

} // namespace EventBroker
} // namespace Engine