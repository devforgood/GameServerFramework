#pragma once

#include <array>
#include <atomic>
#include <cstddef>
#include <memory>

#include "PlayerSaveData.h"

//
// PlayerSaveBufferPool
// ------------------------------------------------------------------
// 플레이어 한 명이 저장할 때마다 돌려쓰는 PlayerSaveData 버퍼 묶음.
//
// 저장은 게임 스레드가 채우고 DB 스레드가 읽는다. 그래서 버퍼 하나로는
// 부족하고(DB 가 읽는 중인 걸 덮어쓴다), 매번 새로 할당하면 접속자 수만큼
// 할당/해제가 반복된다. 슬롯 3개면
//   1) DB 가 지금 쓰고 있는 것
//   2) 다음 주기에 나갈 것
//   3) 즉시 저장(사망/로그아웃)용
// 이 동시에 존재하는 최악의 경우를 덮는다.
//
// 슬롯이 언제 비는지는 shared_ptr 의 참조 수로 안다. Acquire() 가 돌려준
// 포인터를 DB 작업이 들고 있는 동안에는 use_count() 가 1 을 넘으므로 그
// 슬롯은 다시 나가지 않는다. 덤으로, 플레이어가 먼저 소멸해도 DB 에 물려
// 있는 슬롯은 살아남는다.
//
class PlayerSaveBufferPool
{
public:
    static constexpr std::size_t kSlotCount = 3;

    PlayerSaveBufferPool()
    {
        for (auto& slot : slots_)
            slot = std::make_shared<PlayerSaveData>();
    }

    // 게임 스레드에서만 호출한다.
    // 비어 있는 슬롯을 초기화해서 돌려준다. 셋 다 DB 에 물려 있으면 nullptr.
    //
    // nullptr 을 받으면 이번 저장은 통째로 건너뛰어야 한다. 컴포넌트의
    // Save() 는 변경분을 뽑아가면서 dirty 를 지우므로, 담을 곳이 없는데
    // 부르면 그 변경분이 그대로 증발한다.
    std::shared_ptr<PlayerSaveData> Acquire()
    {
        for (std::size_t n = 0; n < kSlotCount; ++n)
        {
            std::shared_ptr<PlayerSaveData>& slot = slots_[cursor_];
            cursor_ = (cursor_ + 1) % kSlotCount;

            // 1 == 이 풀만 들고 있다 = DB 작업이 끝났다.
            // 경합이 나도 안전한 쪽으로만 틀린다(막 놓인 슬롯을 2 로 읽으면
            // 그냥 건너뛸 뿐, 쓰는 중인 슬롯을 1 로 읽는 일은 없다).
            if (slot.use_count() != 1)
                continue;

            // use_count 를 1 로 떨어뜨린 그 감소 연산과 짝을 맞춘다.
            // DB 스레드가 이 버퍼를 다 읽은 뒤에야 우리가 덮어쓰게 된다.
            std::atomic_thread_fence(std::memory_order_acquire);

            slot->Reset();
            return slot;
        }

        return nullptr;
    }

    // 지금 DB 에 물려 있는 슬롯 수(진단/테스트용).
    [[nodiscard]] std::size_t InFlightCount() const
    {
        std::size_t count = 0;
        for (const auto& slot : slots_)
        {
            if (slot.use_count() != 1)
                ++count;
        }
        return count;
    }

private:
    std::array<std::shared_ptr<PlayerSaveData>, kSlotCount> slots_;
    std::size_t cursor_ = 0; // 다음에 살펴볼 슬롯. 게임 스레드 전용.
};
