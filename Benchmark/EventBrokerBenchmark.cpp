#include <benchmark/benchmark.h>
#include <atomic>
#include <thread>
#include <memory>

#include "EventBroker.h"
#include "BoostConcurrentQueue.h"
#include "MoodycamelConcurrentQueue.h"

using namespace engine::event_broker;

// 벤치마크용 더미 메시지 구조체
struct DummyMessage {
    int id;
    float value;
};

// 벤치마크용 더미 핸들러 (fast delegate에 사용)
struct DummyHandler {
    void OnMessage(const DummyMessage& msg) {
        benchmark::DoNotOptimize(msg.id);
    }
};

// 2. 다중 스레드 환경에서 Enqueue 성능 벤치마크 (Shared 상태 유지)
template <template<typename> class QueuePolicy>
void BM_SharedEnqueue(benchmark::State& state) {
    static std::atomic<EventBroker<DummyMessage, QueuePolicy>*> broker_ptr{nullptr};
    static std::atomic<int> active_threads{0};

    // 스레드 0이 브로커 인스턴스 초기화
    if (state.thread_index == 0) {
        broker_ptr.store(new EventBroker<DummyMessage, QueuePolicy>(), std::memory_order_release);
    }
    
    // 모든 스레드가 초기화를 기다림 (간단한 스핀락 활용)
    EventBroker<DummyMessage, QueuePolicy>* broker = nullptr;
    while ((broker = broker_ptr.load(std::memory_order_acquire)) == nullptr) {
        std::this_thread::yield();
    }

    active_threads.fetch_add(1, std::memory_order_acq_rel);

    // 실제 성능 측정 구간
    for (auto _ : state) {
        broker->enqueue({1, 3.14f});
    }

    // 마지막 스레드가 종료될 때 메모리 정리
    int remaining = active_threads.fetch_sub(1, std::memory_order_acq_rel);
    if (remaining == 1) { 
        delete broker;
        broker_ptr.store(nullptr, std::memory_order_release);
    }
}

// 3. 단일 스레드 환경에서 Enqueue 및 ProcessEvents 전체 사이클 벤치마크
template <template<typename> class QueuePolicy>
void BM_EnqueueAndProcess(benchmark::State& state) {
    EventBroker<DummyMessage, QueuePolicy> broker;
    DummyHandler handler;
    
    broker.template subscribe<DummyHandler, &DummyHandler::OnMessage>(&handler);

    // range에 지정된 개수만큼 큐에 Push 한 뒤 한 번에 Process (Pop)
    for (auto _ : state) {
        for (int i = 0; i < state.range(0); ++i) {
            broker.enqueue({i, 1.0f});
        }
        broker.processEvents();
    }
}

// 4. 다중 생산자 - 단일 소비자 (MPSC) 실사용 패턴 벤치마크
template <template<typename> class QueuePolicy>
void BM_MPSC(benchmark::State& state) {
    static std::atomic<EventBroker<DummyMessage, QueuePolicy>*> broker_ptr{nullptr};
    static std::atomic<int> active_threads{0};
    static DummyHandler mpsc_handler;

    if (state.thread_index == 0) {
        auto* broker = new EventBroker<DummyMessage, QueuePolicy>();
        broker->template subscribe<DummyHandler, &DummyHandler::OnMessage>(&mpsc_handler);
        broker_ptr.store(broker, std::memory_order_release);
    }
    
    EventBroker<DummyMessage, QueuePolicy>* broker = nullptr;
    while ((broker = broker_ptr.load(std::memory_order_acquire)) == nullptr) {
        std::this_thread::yield();
    }

    active_threads.fetch_add(1, std::memory_order_acq_rel);

    if (state.thread_index == 0) {
        // 스레드 0: 소비자 역할 (이벤트 발행 처리)
        for (auto _ : state) {
            broker->processEvents();
        }
    } else {
        // 나머지 스레드: 생산자 역할 (이벤트 삽입)
        for (auto _ : state) {
            broker->enqueue({1, 3.14f});
        }
    }

    int remaining = active_threads.fetch_sub(1, std::memory_order_acq_rel);
    if (remaining == 1) {
        delete broker;
        broker_ptr.store(nullptr, std::memory_order_release);
    }
}

// ===================== 매크로를 통한 벤치마크 등록 =====================

// Boost lockfree::queue (ConcurrentQueue) 벤치마크
BENCHMARK_TEMPLATE(BM_SharedEnqueue, BoostConcurrentQueue)->Threads(1)->Threads(2)->Threads(4)->Threads(8);
BENCHMARK_TEMPLATE(BM_EnqueueAndProcess, BoostConcurrentQueue)->RangeMultiplier(4)->Range(8, 1024);
BENCHMARK_TEMPLATE(BM_MPSC, BoostConcurrentQueue)->Threads(2)->Threads(4)->Threads(8);

// moodycamel::ConcurrentQueue 벤치마크
BENCHMARK_TEMPLATE(BM_SharedEnqueue, MoodycamelConcurrentQueue)->Threads(1)->Threads(2)->Threads(4)->Threads(8);
BENCHMARK_TEMPLATE(BM_EnqueueAndProcess, MoodycamelConcurrentQueue)->RangeMultiplier(4)->Range(8, 1024);
BENCHMARK_TEMPLATE(BM_MPSC, MoodycamelConcurrentQueue)->Threads(2)->Threads(4)->Threads(8);

BENCHMARK_MAIN();
