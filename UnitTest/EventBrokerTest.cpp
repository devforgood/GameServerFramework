#include "pch.h"
#include <gtest/gtest.h>
#include "../Engine/EventBroker.h"
#include <string>

struct TestMessage {
    int id;
    char data[32];
};

using TestEventBroker = Engine::EventBroker::EventBroker<TestMessage>;

class EventBrokerTest : public ::testing::Test {
protected:
    void SetUp() override {
        broker = std::make_unique<TestEventBroker>();
    }

    void TearDown() override {
        broker.reset();
    }

    std::unique_ptr<TestEventBroker> broker;
};

TEST_F(EventBrokerTest, SubscribeAndPublishImmediate) {
    int receivedId = 0;
    std::string receivedData;

    auto token = broker->subscribe([&](const TestMessage& msg) {
        receivedId = msg.id;
        receivedData = msg.data;
    });

    broker->publishImmediate({ 42, "Hello" });

    EXPECT_EQ(receivedId, 42);
    EXPECT_EQ(receivedData, "Hello");
}

TEST_F(EventBrokerTest, SubscribeAndEnqueue) {
    int receivedCount = 0;

    auto token = broker->subscribe([&](const TestMessage& msg) {
        receivedCount++;
        EXPECT_EQ(msg.id, 100);
    });

    broker->enqueue({ 100, "Queued" });

    // Should not be processed yet
    EXPECT_EQ(receivedCount, 0);

    broker->processEvents();

    // Now it should be processed
    EXPECT_EQ(receivedCount, 1);
}

TEST_F(EventBrokerTest, Unsubscribe) {
    int receivedCount = 0;

    auto token = broker->subscribe([&](const TestMessage& msg) {
        receivedCount++;
    });

    broker->publishImmediate({ 1, "Msg1" });
    EXPECT_EQ(receivedCount, 1);

    broker->unsubscribe(token);

    broker->publishImmediate({ 2, "Msg2" });
    EXPECT_EQ(receivedCount, 1); // Should not increase
}

TEST_F(EventBrokerTest, Clear) {
    int receivedCount = 0;

    auto token = broker->subscribe([&](const TestMessage& msg) {
        receivedCount++;
    });

    broker->enqueue({ 1, "Msg1" });
    broker->enqueue({ 2, "Msg2" });
    
    // Clear the queue and bus
    broker->clear();

    broker->processEvents();
    
    // No events should be processed because queue was cleared
    EXPECT_EQ(receivedCount, 0);

    // Also the bus was cleared, so publishImmediate shouldn't work for the previous subscriber
    broker->publishImmediate({ 3, "Msg3" });
    EXPECT_EQ(receivedCount, 0);
}

TEST_F(EventBrokerTest, MultipleSubscribers) {
    int count1 = 0;
    int count2 = 0;

    broker->subscribe([&](const TestMessage& msg) { count1++; });
    broker->subscribe([&](const TestMessage& msg) { count2++; });

    broker->publishImmediate({ 1, "Broadcast" });

    EXPECT_EQ(count1, 1);
    EXPECT_EQ(count2, 1);
}
