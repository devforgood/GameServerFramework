#pragma once

#include <atomic>

namespace Engine {
namespace EventBroker {

// 쓰레드 세이프 정책: std::mutex를 대체하는 경량화된 Lock-Free 기반(SpinLock) 동작
struct ThreadSafe {
    void lock() {
        // std::mutex 오버헤드를 줄이기 위해 atomic_flag 기반으로 스핀락 구현
        while (flag_.test_and_set(std::memory_order_acquire)) {
        }
    }
    void unlock() {
        flag_.clear(std::memory_order_release);
    }
    bool try_lock() { 
        return !flag_.test_and_set(std::memory_order_acquire); 
    }
private:
    std::atomic_flag flag_ = ATOMIC_FLAG_INIT;
};

} // namespace EventBroker
} // namespace Engine