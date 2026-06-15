#include "pch.h"
#include <gtest/gtest.h>
#include "EventBroker.h"
#include <string>

struct TestMessage {
    int id;
    char data[32];
};

using TestEventBroker = engine::event_broker::EventBroker<TestMessage>;

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

class Listener {
public:
    int receivedId = 0;
    std::string receivedData;
    int receivedCount = 0;

    void OnMessage(const TestMessage& msg) {
        receivedId = msg.id;
        receivedData = msg.data;
        receivedCount++;
    }
};

TEST_F(EventBrokerTest, SubscribeAndPublishImmediate) {
    Listener listener;

    broker->subscribe<Listener, &Listener::OnMessage>(&listener);

    broker->publishImmediate({ 42, "Hello" });

    EXPECT_EQ(listener.receivedId, 42);
    EXPECT_EQ(listener.receivedData, "Hello");
}

TEST_F(EventBrokerTest, SubscribeAndEnqueue) {
    Listener listener;

    broker->subscribe<Listener, &Listener::OnMessage>(&listener);

    broker->enqueue({ 100, "Queued" });

    // Should not be processed yet
    EXPECT_EQ(listener.receivedCount, 0);

    broker->processEvents();

    // Now it should be processed
    EXPECT_EQ(listener.receivedCount, 1);
    EXPECT_EQ(listener.receivedId, 100);
    EXPECT_EQ(listener.receivedData, "Queued");
}

TEST_F(EventBrokerTest, Clear) {
    Listener listener;

    broker->subscribe<Listener, &Listener::OnMessage>(&listener);

    broker->enqueue({ 1, "Msg1" });
    broker->enqueue({ 2, "Msg2" });
    
    // Clear the queue and bus
    broker->clear();

    broker->processEvents();
    
    // No events should be processed because queue was cleared
    EXPECT_EQ(listener.receivedCount, 0);

    // Also the bus was cleared, so publishImmediate shouldn't work for the previous subscriber
    broker->publishImmediate({ 3, "Msg3" });
    EXPECT_EQ(listener.receivedCount, 0);
}

TEST_F(EventBrokerTest, MultipleSubscribers) {
    Listener listener1;
    Listener listener2;

    broker->subscribe<Listener, &Listener::OnMessage>(&listener1);
    broker->subscribe<Listener, &Listener::OnMessage>(&listener2);

    broker->publishImmediate({ 1, "Broadcast" });

    EXPECT_EQ(listener1.receivedCount, 1);
    EXPECT_EQ(listener2.receivedCount, 1);
}
