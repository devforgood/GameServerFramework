#include <gtest/gtest.h>

#include <algorithm>
#include <memory>
#include <vector>

#include "PlayerSaveBufferPool.h"
#include "PlayerSaveData.h"
#include "DbChangeTracker.h"
#include "DbRecord.h"
#include "SQL/generated/vo.h"

// PlayerSaveBufferPool 단위 테스트.
//
// 저장 버퍼는 게임 스레드가 채우고 DB 스레드가 읽는다. 이 풀이 지켜야 하는
// 계약은 세 가지다.
//   1) DB 가 아직 들고 있는 슬롯은 절대 다시 내주지 않는다(변경분 유실 방지).
//   2) 다시 내줄 때는 이전 내용이 "없음" 으로 보여야 한다(유령 저장 방지).
//   3) 그러면서도 vector 버퍼는 버리지 않는다(재할당 회피 — 이 타입의 목적).

namespace
{
    // 컴포넌트가 하는 일과 같은 방식으로 아이템 변경분을 채운다.
    void FillItems(PlayerSaveData& data, int count)
    {
        auto& records = data.items.emplace();
        records.clear();
        for (int i = 0; i < count; ++i)
        {
            PlayerItemVO vo{};
            vo.item_id = i + 1;
            vo.count = 1;
            records.push_back({ vo, DbAction::Insert });
        }
    }
}

// 반납된 슬롯은 다시 나온다 — 즉 매번 새로 할당하지 않는다.
TEST(PlayerSaveBufferPoolTest, RecyclesTheSameSlots)
{
    PlayerSaveBufferPool pool;

    std::vector<const PlayerSaveData*> seen;
    for (int i = 0; i < 20; ++i)
    {
        auto slot = pool.Acquire();      // 빌리고
        ASSERT_NE(slot, nullptr);
        seen.push_back(slot.get());
    }                                     // 바로 반납(스코프 종료)

    // 서로 다른 주소는 슬롯 수를 넘지 않는다.
    std::vector<const PlayerSaveData*> distinct;
    for (const auto* p : seen)
    {
        if (std::find(distinct.begin(), distinct.end(), p) == distinct.end())
            distinct.push_back(p);
    }
    EXPECT_LE(distinct.size(), PlayerSaveBufferPool::kSlotCount);
    EXPECT_GT(distinct.size(), 1u); // 풀이 돌긴 한다
}

// 다시 내줄 때 이전 저장의 내용이 남아 보이면 안 된다.
TEST(PlayerSaveBufferPoolTest, AcquireClearsPreviousContent)
{
    PlayerSaveBufferPool pool;

    {
        auto slot = pool.Acquire();
        ASSERT_NE(slot, nullptr);
        FillItems(*slot, 3);
        slot->wallet.emplace().vo.gold = 500;
        EXPECT_TRUE(slot->items.has_value());
        EXPECT_TRUE(slot->wallet.has_value());
    }

    // 풀을 한 바퀴 돌려 같은 슬롯을 다시 받는다.
    for (std::size_t i = 0; i < PlayerSaveBufferPool::kSlotCount; ++i)
    {
        auto slot = pool.Acquire();
        ASSERT_NE(slot, nullptr);
        EXPECT_FALSE(slot->items.has_value());
        EXPECT_FALSE(slot->wallet.has_value());
        EXPECT_FALSE(slot->player.has_value());
        EXPECT_FALSE(slot->skills.has_value());
        EXPECT_FALSE(slot->location.has_value());
        EXPECT_FALSE(slot->quest_actives.has_value());
        EXPECT_FALSE(slot->quest_state.has_value());
    }
}

// 비우되 버퍼는 남긴다 — 재사용의 핵심.
TEST(PlayerSaveBufferPoolTest, KeepsVectorCapacityAcrossReuse)
{
    PlayerSaveBufferPool pool;

    const void* first_buffer = nullptr;
    std::size_t first_capacity = 0;
    {
        auto slot = pool.Acquire();
        ASSERT_NE(slot, nullptr);
        FillItems(*slot, 64);
        first_buffer = slot->items->data();
        first_capacity = slot->items->capacity();
        EXPECT_GE(first_capacity, 64u);
    }

    // 같은 슬롯이 돌아올 때까지 풀을 돈다.
    for (std::size_t i = 0; i < PlayerSaveBufferPool::kSlotCount; ++i)
    {
        auto slot = pool.Acquire();
        ASSERT_NE(slot, nullptr);
        if (slot->items->capacity() == first_capacity &&
            slot->items->data() == first_buffer)
        {
            // 값은 "없음" 인데 버퍼는 그대로다.
            EXPECT_FALSE(slot->items.has_value());
            SUCCEED();
            return;
        }
    }
    FAIL() << "재사용된 슬롯이 이전 vector 버퍼를 유지하지 않았다";
}

// DB 가 들고 있는 슬롯은 내주지 않는다. 전부 물려 있으면 nullptr.
TEST(PlayerSaveBufferPoolTest, RefusesSlotsStillInFlight)
{
    PlayerSaveBufferPool pool;

    std::vector<std::shared_ptr<PlayerSaveData>> in_flight;
    for (std::size_t i = 0; i < PlayerSaveBufferPool::kSlotCount; ++i)
    {
        auto slot = pool.Acquire();
        ASSERT_NE(slot, nullptr);
        in_flight.push_back(slot);
    }
    EXPECT_EQ(pool.InFlightCount(), PlayerSaveBufferPool::kSlotCount);

    // 셋 다 사용 중 -> 이번 저장은 건너뛰어야 한다.
    EXPECT_EQ(pool.Acquire(), nullptr);

    // 하나 반납하면 다시 나온다.
    in_flight.pop_back();
    EXPECT_EQ(pool.InFlightCount(), PlayerSaveBufferPool::kSlotCount - 1);
    EXPECT_NE(pool.Acquire(), nullptr);
}

// 저장이 DB 스레드에 떠 있는 동안 플레이어(=풀)가 소멸해도 버퍼는 살아있다.
TEST(PlayerSaveBufferPoolTest, InFlightSlotOutlivesPool)
{
    std::shared_ptr<PlayerSaveData> in_flight;
    {
        auto pool = std::make_unique<PlayerSaveBufferPool>();
        in_flight = pool->Acquire();
        ASSERT_NE(in_flight, nullptr);
        FillItems(*in_flight, 2);
    } // 풀 소멸

    ASSERT_TRUE(in_flight->items.has_value());
    EXPECT_EQ(in_flight->items->size(), 2u);
}

// 트래커의 Flush(out) 은 out 을 비우고 채우되 버퍼는 그대로 쓴다.
TEST(DbCollectionTrackerTest, FlushIntoExistingVectorReusesBuffer)
{
    DbCollectionTracker<int, PlayerItemVO> tracker;

    std::vector<DbRecord<PlayerItemVO>> out;
    out.reserve(32);
    const void* buffer = out.data();

    PlayerItemVO vo{};
    vo.item_id = 7;
    vo.count = 3;
    tracker.Add(7, vo);

    tracker.Flush(out);
    ASSERT_EQ(out.size(), 1u);
    EXPECT_EQ(out[0].vo.item_id, 7);
    EXPECT_EQ(out[0].action, DbAction::Insert);
    EXPECT_EQ(out.data(), buffer); // 재할당 없음

    // 두 번째 Flush 는 이전 내용을 남기지 않는다.
    tracker.Flush(out);
    EXPECT_TRUE(out.empty());
    EXPECT_EQ(out.data(), buffer);
}
