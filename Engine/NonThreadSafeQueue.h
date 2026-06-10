#pragma once

#include <queue>
#include <utility>

namespace engine {
namespace event_broker {

// 2. Non Thread-safe 큐 정책
template <typename T>
class NonThreadSafeQueue {
public:
    void push(const T& item) { queue_.push(item); }
    void push(T&& item) { queue_.push(std::move(item)); }
    
    bool pop(T& outItem) {
        if (queue_.empty()) return false;
        outItem = std::move(queue_.front());
        queue_.pop();
        return true;
    }
    
    bool empty() { return queue_.empty(); }
    size_t size() { return queue_.size(); }
    
    void clear() {
        std::queue<T> emptyQueue;
        std::swap(queue_, emptyQueue);
    }

private:
    std::queue<T> queue_;
};

} // namespace EventBroker
} // namespace Engine