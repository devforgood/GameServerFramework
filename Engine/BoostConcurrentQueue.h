#pragma once

#include <utility>
#include <boost/lockfree/queue.hpp>

namespace engine {
namespace event_broker {

// 1. Thread-safe 큐 정책 (동시성 큐 - Lock-Free 알고리즘)
// std::mutex와 lock_guard를 제거하고 부스트의 lockfree::queue를 활용합니다.
template <typename T>
class BoostConcurrentQueue {
public:
    // 초기 용량 할당 (필요 시 자동으로 증가)
    BoostConcurrentQueue() : queue_(1024) {}

    void push(const T& item) {
        queue_.push(item);
    }

    void push(T&& item) {
        queue_.push(std::move(item));
    }

    bool pop(T& outItem) {
        return queue_.pop(outItem);
    }

    bool empty() {
        return queue_.empty();
    }

    void clear() {
        queue_.consume_all([](const T&) {});
    }

private:
    boost::lockfree::queue<T> queue_;
};

} // namespace EventBroker
} // namespace Engine