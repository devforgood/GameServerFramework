#pragma once

namespace Engine {
namespace EventBroker {

// 넌 쓰레드 세이프 정책: 아무런 락 동작을 수행하지 않음 (오버헤드 없음)
struct NonThreadSafe {
    void lock() {}
    void unlock() {}
    bool try_lock() { return true; }
};

} // namespace EventBroker
} // namespace Engine