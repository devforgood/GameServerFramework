#pragma once

#include <utility>
#include <boost/lockfree/queue.hpp>

namespace Engine {
namespace EventBroker {

// 1. Thread-safe 큐 정책 (동시성 큐 - Lock-Free 알고리즘)
// std::mutex와 lock_guard를 제거하고 부스트의 lockfree::queue를 활용합니다.
template <typename T>
class ConcurrentQueue {
public:
    // 초기 용량 할당 (필요 시 자동으로 증가)
    ConcurrentQueue() : queue_(1024) {}

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

    size_t size() {
        // boost::lockfree::queue는 본질적으로 정확한 size()를 제공하지 않습니다.
        // Lock-Free 환경에서 요소 개수는 유동적이므로 0을 반환하도록 합니다.
        return 0;
    }

    void clear() {
        queue_.consume_all([](const T&) {});
    }

private:
    boost::lockfree::queue<T> queue_;
};

} // namespace EventBroker
} // namespace Engine