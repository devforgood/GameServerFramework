#include <gtest/gtest.h>

#include <memory>
#include <set>
#include <string>
#include <vector>

#include "SendMessagePool.h"

// SendMessagePool 단위 테스트.
//
// 이 풀이 지켜야 하는 계약.
//   1) 아직 전송에 물려 있는 메시지는 절대 다시 내주지 않는다(보내는 도중에 덮어쓰기 금지).
//   2) 다시 내줄 때는 비워진 상태여야 한다(이전 패킷 잔재가 섞이면 안 된다).
//   3) 그러면서도 한 번 커진 내부 버퍼는 버리지 않는다(재할당 회피 - 이 풀의 목적).
//   4) 빌릴 것이 없으면 늘린다. 절대 실패하지 않는다(패킷 유실이 할당보다 나쁘다).
//
// 테스트는 자기 풀 인스턴스를 직접 만든다. ForThisThread() 는 스레드마다 하나뿐이라
// 테스트끼리 상태가 섞인다.

// 반납된 슬롯은 다시 나온다 - 즉 매번 새로 할당하지 않는다.
TEST(SendMessagePoolTest, RecyclesTheSameSlot)
{
    SendMessagePool pool;

    const send_message* first = nullptr;
    {
        auto msg = pool.Take();
        first = msg.get();
        ASSERT_NE(nullptr, first);
    }

    auto again = pool.Take();
    EXPECT_EQ(first, again.get()) << "반납한 슬롯이 그대로 다시 나와야 한다";
    EXPECT_EQ(SendMessagePool::kInitialSlots, pool.Capacity()) << "재사용했으면 늘어날 이유가 없다";
}

// 전송 큐에 물려 있는 동안에는 같은 메시지가 두 번 나오면 안 된다.
TEST(SendMessagePoolTest, NeverHandsOutAMessageStillInFlight)
{
    SendMessagePool pool;

    std::vector<std::shared_ptr<send_message>> held;
    std::set<const send_message*> seen;

    for (std::size_t i = 0; i < SendMessagePool::kInitialSlots; ++i)
    {
        auto msg = pool.Take();
        EXPECT_TRUE(seen.insert(msg.get()).second) << i << "번째에서 물려 있는 메시지가 또 나왔다";
        held.push_back(std::move(msg));
    }

    EXPECT_EQ(SendMessagePool::kInitialSlots, pool.InFlightCount());
}

// 다 물려 있으면 늘린다. nullptr 을 돌려주면 패킷이 통째로 사라진다.
TEST(SendMessagePoolTest, GrowsInsteadOfFailing)
{
    SendMessagePool pool;

    std::vector<std::shared_ptr<send_message>> held;
    for (std::size_t i = 0; i < SendMessagePool::kInitialSlots; ++i)
        held.push_back(pool.Take());

    auto extra = pool.Take();
    ASSERT_NE(nullptr, extra) << "빌릴 것이 없어도 실패하면 안 된다";
    EXPECT_EQ(SendMessagePool::kInitialSlots + 1, pool.Capacity());

    // 늘어난 슬롯도 다른 것과 겹치지 않아야 한다.
    for (const auto& msg : held)
        EXPECT_NE(msg.get(), extra.get());
}

// 다시 나온 메시지는 비어 있어야 한다.
TEST(SendMessagePoolTest, HandsOutClearedMessages)
{
    SendMessagePool pool;

    const send_message* first = nullptr;
    {
        auto msg = pool.Take();
        first = msg.get();
        auto text = msg->CreateString(std::string(64, 'x'));
        msg->Finish(text);
        EXPECT_GT(msg->GetSize(), 0u);
    }

    auto again = pool.Take();
    ASSERT_EQ(first, again.get());
    EXPECT_EQ(0u, again->GetSize()) << "이전 패킷 내용이 남아 있으면 안 된다";
}

// 이 풀의 존재 이유: 한 번 커진 버퍼를 다음 메시지가 그대로 쓴다.
TEST(SendMessagePoolTest, KeepsGrownBufferAcrossReuse)
{
    SendMessagePool pool;

    // 기본 1024 바이트를 넘겨 버퍼를 키운다.
    const std::string big(8192, 'y');
    const uint8_t* grownEnd = nullptr;
    const send_message* slot = nullptr;
    {
        auto msg = pool.Take();
        slot = msg.get();
        auto text = msg->CreateString(big);
        msg->Finish(text);
        ASSERT_GT(msg->GetSize(), 8192u);
        // Clear() 는 쓰기 위치만 되돌리고 버퍼는 남긴다. 그 버퍼의 끝 주소를 기억해 둔다.
    }

    auto again = pool.Take();
    ASSERT_EQ(slot, again.get());
    grownEnd = again->GetCurrentBufferPointer(); // 빈 상태에서는 버퍼 끝을 가리킨다

    // 같은 크기를 다시 담아도 버퍼를 새로 잡지 않아야 한다.
    auto text = again->CreateString(big);
    again->Finish(text);
    EXPECT_EQ(grownEnd, again->GetCurrentBufferPointer() + again->GetSize())
        << "재사용 때 버퍼를 다시 할당했다 - Clear() 가 용량을 버린 것";
}

// 진단용 카운터가 실제 보유 수와 맞아야 한다.
TEST(SendMessagePoolTest, InFlightCountTracksHeldMessages)
{
    SendMessagePool pool;
    EXPECT_EQ(0u, pool.InFlightCount());

    auto a = pool.Take();
    auto b = pool.Take();
    EXPECT_EQ(2u, pool.InFlightCount());

    // 브로드캐스트처럼 같은 메시지를 여러 곳이 들고 있어도 슬롯 하나다.
    auto sharedCopy = b;
    EXPECT_EQ(2u, pool.InFlightCount());

    a.reset();
    EXPECT_EQ(1u, pool.InFlightCount());

    b.reset();
    EXPECT_EQ(1u, pool.InFlightCount()) << "복사본이 남아 있으면 아직 물려 있는 것이다";

    sharedCopy.reset();
    EXPECT_EQ(0u, pool.InFlightCount());
}
