#pragma once

#include <atomic>
#include <cstddef>
#include <memory>
#include <vector>

#include "SendMessage.h"
#include "LogHelper.h"

//
// SendMessagePool
// ------------------------------------------------------------------
// 패킷 하나를 내보낼 때마다 새로 만들던 send_message(=FlatBufferBuilder)를
// 스레드마다 돌려쓴다. send_message 는 생성 시 1KB 를 미리 잡으므로, 관심영역
// 모드에서는 "접속자 수 x 틱" 만큼의 할당/해제가 그대로 프레임 비용이 된다.
//
// 왜 스레드마다인가
//   io_context 하나에 스레드 하나만 돌리고, 그 스레드가 자기 서버의 게임 로직과
//   IO 핸들러를 직렬로 실행한다(Server.h 의 IoWorker). 메시지를 만드는 쪽도,
//   마지막 참조가 사라지는 쪽(GameSession::OnWrite)도 같은 스레드다. 그래서
//   풀 자체에는 락이 필요 없다.
//
// 언제 비는지는 shared_ptr 참조 수로 안다
//   브로드캐스트는 같은 메시지를 여러 세션의 전송 큐에 함께 넣는다. "이제 다
//   나갔다"를 알 방법이 참조 수 말고는 없다(PlayerSaveBufferPool 과 같은 이유).
//   덤으로 맵이나 플레이어가 먼저 사라져도 전송 중인 메시지는 살아남는다.
//
// 왜 재사용이 되는가
//   FlatBufferBuilder::Clear() 는 내부 버퍼를 그대로 두고 쓰기 위치만 되돌린다.
//   한 번 커진 버퍼는 다음 메시지에서 그대로 쓰인다.
//
// 저장 버퍼와 다른 점: 여기서는 실패하지 않는다. 빌릴 것이 없으면 늘린다.
// nullptr 을 돌려주면 패킷이 통째로 사라지는데, 그건 할당 한 번보다 훨씬 나쁘다.
//
class SendMessagePool
{
public:
    // 처음에 만들어 두는 슬롯 수. 모자라면 늘어난다.
    static constexpr std::size_t kInitialSlots = 64;

    // 커서 근처를 몇 칸까지 훑어볼지. 대개 첫 칸에서 끝난다(방금 놓인 것이 그 자리다).
    static constexpr std::size_t kNearScan = 32;

    // 여기를 넘으면 경고를 한 번 낸다. 풀이 이만큼 커졌다는 것은 전송 큐가
    // 밀리고 있다는 뜻이지, 풀이 잘못됐다는 뜻이 아니다.
    static constexpr std::size_t kSoftCap = 4096;

    // 이 스레드의 풀. 워커 스레드마다 따로 존재한다.
    static SendMessagePool& ForThisThread()
    {
        thread_local SendMessagePool pool;
        return pool;
    }

    // 비워진 상태의 메시지를 하나 빌린다. 곧바로 조립을 시작하면 된다.
    static std::shared_ptr<send_message> Acquire()
    {
        return ForThisThread().Take();
    }

    // 지금 전송에 물려 있는 슬롯 수(진단/테스트용).
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

    [[nodiscard]] std::size_t Capacity() const { return slots_.size(); }

    // 보통은 Acquire() 로 이 스레드의 풀을 쓴다. 직접 만드는 것은 테스트용이다.
    SendMessagePool()
    {
        slots_.reserve(kInitialSlots);
        for (std::size_t i = 0; i < kInitialSlots; ++i)
            slots_.push_back(std::make_shared<send_message>());
    }

    // 이 풀에서 하나 빌린다. Acquire() 가 이것을 부른다.
    std::shared_ptr<send_message> Take()
    {
        // 1) 커서 근처. 전송은 대체로 넣은 순서대로 끝나므로 커서가 한 바퀴 돌아올
        //    무렵이면 그 자리는 이미 비어 있다. 거의 항상 여기서 끝난다.
        // (near/far 는 windows.h 매크로라 변수 이름으로 쓸 수 없다)
        const std::size_t nearCount = slots_.size() < kNearScan ? slots_.size() : kNearScan;
        for (std::size_t n = 0; n < nearCount; ++n)
        {
            if (auto found = TryTakeAt(cursor_))
                return found;
            Advance();
        }

        // 2) 근처가 다 물려 있으면 전체를 한 번 훑는다. 여기까지 오는 것은 드물다
        //    (느린 클라이언트 때문에 전송 큐가 밀린 상황).
        for (std::size_t i = 0; i < slots_.size(); ++i)
        {
            if (auto found = TryTakeAt(i))
            {
                cursor_ = i;
                Advance();
                return found;
            }
        }

        // 3) 정말 다 나가 있다. 늘린다.
        return Grow();
    }

private:
    std::shared_ptr<send_message> TryTakeAt(std::size_t index)
    {
        std::shared_ptr<send_message>& slot = slots_[index];

        // 1 == 이 풀만 들고 있다 = 전송이 끝났다.
        if (slot.use_count() != 1)
            return nullptr;

        // use_count 를 1 로 떨어뜨린 그 감소 연산과 짝을 맞춘다.
        std::atomic_thread_fence(std::memory_order_acquire);

        slot->Clear(); // 버퍼는 그대로, 쓰기 위치만 처음으로
        return slot;
    }

    void Advance()
    {
        cursor_ = (cursor_ + 1 == slots_.size()) ? 0 : cursor_ + 1;
    }

    std::shared_ptr<send_message> Grow()
    {
        auto created = std::make_shared<send_message>();
        slots_.push_back(created);

        if (slots_.size() == kSoftCap)
        {
            LOG.warn("SendMessagePool 이 {} 개까지 늘었습니다 - 전송 큐가 밀리고 있는지 확인하세요",
                     slots_.size());
        }

        cursor_ = 0;
        return created;
    }

    std::vector<std::shared_ptr<send_message>> slots_;
    std::size_t cursor_ = 0; // 다음에 살펴볼 슬롯. 이 스레드 전용.
};
