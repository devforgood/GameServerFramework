#pragma once

#include <vector>
#include <functional>
#include <mutex>
#include <memory>
#include <atomic>
#include <algorithm>
#include <cstdint>
#include "ThreadSafe.h"
#include "NonThreadSafe.h"

namespace Engine {
namespace EventBroker {

template <typename MessageType>
class EventBus {
public:
    using Stub = void(*)(void*, const MessageType&);

    struct Subscriber {
        void* object;
        Stub stub;
    };

    EventBus() {
        // 초기 빈 구독자 리스트 생성
        auto empty_list = std::make_shared<std::vector<Subscriber>>();
        subscribers_.store(empty_list, std::memory_order_relaxed);
    }

    template<typename TObject, auto Method>
    static void stub_helper(void* object, const MessageType& message) {
        (static_cast<TObject*>(object)->*Method)(message);
    }

    template<typename TObject, auto Method>
    void subscribe(TObject* object) {
        // Write 작업 직렬화 (구독/해제는 빈도가 낮으므로 SpinLock 사용)
        std::lock_guard<ThreadSafe> lock(lockPolicy_);
        
        // [Read] 현재 상태 로드
        auto current_list = subscribers_.load(std::memory_order_acquire);
        
        // [Copy] 새로운 복사본 리스트 생성
        auto new_list = std::make_shared<std::vector<Subscriber>>(*current_list);
        
        // [Update] 복사본에 새로운 구독자 추가
        new_list->push_back({object, &stub_helper<TObject, Method>});
        
        // 원자적 포인터 교체
        subscribers_.store(new_list, std::memory_order_release);
    }

    // RCU Read 영역: 락 없이 안전하게 동작 (Wait-Free)
    void publish(const MessageType& message) {
        // 원자적으로 shared_ptr를 복사하여 참조 카운트를 증가시킵니다.
        // 읽기 수행 중 다른 쓰레드가 구독(Update)을 진행하여 리스트를 교체하더라도,
        // current_list의 참조 카운트가 유지되므로 현재 접근 중인 메모리가 절대 해제되지 않습니다.
        auto current_list = subscribers_.load(std::memory_order_acquire);
        
        for (const auto& sub : *current_list) {
            sub.stub(sub.object, message);
        }
        // current_list의 스코프가 종료되면 자동으로 참조 카운트가 감소하며, 필요시 메모리가 해제됩니다.
    }

    void clear() {
        std::lock_guard<ThreadSafe> lock(lockPolicy_);
        auto empty_list = std::make_shared<std::vector<Subscriber>>();
        subscribers_.store(empty_list, std::memory_order_release);
    }

private:
    std::atomic<std::shared_ptr<std::vector<Subscriber>>> subscribers_;
    ThreadSafe lockPolicy_;
};

} // namespace EventBroker
} // namespace Engine
