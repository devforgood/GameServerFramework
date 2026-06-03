#pragma once
#include <concurrentqueue/concurrentqueue.h> // 외부 라이브러리인 moodycamel 헤더 파일 (include 설정 경로에 맞게 조정 필요)


template <typename T>
class MoodycamelConcurrentQueue {
public:
    MoodycamelConcurrentQueue() = default;

    void push(const T& item) {
        queue_.enqueue(item);
    }

    void push(T&& item) {
        queue_.enqueue(std::move(item));
    }

    bool pop(T& outItem) {
        return queue_.try_dequeue(outItem);
    }

    bool empty() {
        return queue_.size_approx() == 0;
    }

    //size_t size() {
    //    return queue_.size_approx();
    //}

    void clear() {
        T dummy;
        while (queue_.try_dequeue(dummy)) {}
    }

private:
    moodycamel::ConcurrentQueue<T> queue_;
};