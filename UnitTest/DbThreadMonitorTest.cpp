#include <gtest/gtest.h>
#include "DbThreadMonitor.h"

#include "spdlog/spdlog.h"
#include "spdlog/sinks/null_sink.h"

#include <atomic>
#include <chrono>
#include <thread>
#include <vector>

using namespace std::chrono_literals;

// DbThreadMonitor 는 LOG("net") spdlog 로거를 사용한다. 테스트 하네스에는 해당
// 로거가 등록되어 있지 않으므로(InitLog 미호출), 출력이 없는 null 로거를 한 번 등록한다.
namespace
{
	void EnsureNetLogger()
	{
		if (!spdlog::get("net"))
		{
			auto logger = std::make_shared<spdlog::logger>(
				"net", std::make_shared<spdlog::sinks::null_sink_mt>());
			logger->set_level(spdlog::level::debug);
			spdlog::register_logger(logger);
		}
	}
}

class DbThreadMonitorTest : public ::testing::Test
{
protected:
	static void SetUpTestSuite()
	{
		EnsureNetLogger();
	}

	void SetUp() override
	{
		// 싱글턴 상태를 테스트마다 초기화하여 결정적으로 만든다.
		DbThreadMonitor::Instance().Reset();
		mon_ = &DbThreadMonitor::Instance();
	}

	DbThreadMonitor* mon_ = nullptr;
};

// BeginEnqueue 는 작업을 "큐 대기" 상태로 등록한다(아직 실행 시작 전).
TEST_F(DbThreadMonitorTest, BeginEnqueueTracksQueuedTask)
{
	const uint64_t token = mon_->BeginEnqueue("PlayerLoad", 1001);
	EXPECT_NE(token, 0u);

	EXPECT_EQ(mon_->InflightCount(), 1u);
	EXPECT_EQ(mon_->QueuedCount(), 1u);
	EXPECT_EQ(mon_->RunningCount(), 0u);
	EXPECT_EQ(mon_->CompletedCount(), 0u);
}

// OnStart 호출 시 "실행 중" 으로 전환된다.
TEST_F(DbThreadMonitorTest, OnStartMarksRunning)
{
	const uint64_t token = mon_->BeginEnqueue("PlayerSave", 2002);
	mon_->OnStart(token);

	EXPECT_EQ(mon_->InflightCount(), 1u);
	EXPECT_EQ(mon_->RunningCount(), 1u);
	EXPECT_EQ(mon_->QueuedCount(), 0u);
}

// OnFinish 는 작업을 제거하고 완료 수 / 스레드 통계를 갱신한다.
TEST_F(DbThreadMonitorTest, OnFinishRemovesAndAccumulates)
{
	const uint64_t token = mon_->BeginEnqueue("PlayerLoad", 3003);
	mon_->OnStart(token);
	mon_->OnFinish(token);

	EXPECT_EQ(mon_->InflightCount(), 0u);
	EXPECT_EQ(mon_->CompletedCount(), 1u);

	auto stats = mon_->SnapshotThreadStats();
	ASSERT_EQ(stats.size(), 1u);
	EXPECT_EQ(stats.begin()->second.taskCount, 1u);
}

// DbTaskScope(RAII) 는 생성 시 시작, 소멸 시 종료를 자동 기록한다.
TEST_F(DbThreadMonitorTest, DbTaskScopeRecordsStartAndFinish)
{
	const uint64_t token = mon_->BeginEnqueue("PlayerSave", 4004);
	{
		DbTaskScope scope(token);
		EXPECT_EQ(mon_->RunningCount(), 1u);
	}
	EXPECT_EQ(mon_->InflightCount(), 0u);
	EXPECT_EQ(mon_->CompletedCount(), 1u);
}

// 실행 시간이 슬로우 임계값을 넘으면 SlowCount 가 증가한다.
TEST_F(DbThreadMonitorTest, SlowTaskIncrementsSlowCount)
{
	mon_->SetSlowThreshold(1ms);

	const uint64_t token = mon_->BeginEnqueue("SlowQuery", 5005);
	mon_->OnStart(token);
	std::this_thread::sleep_for(10ms);
	mon_->OnFinish(token);

	EXPECT_EQ(mon_->CompletedCount(), 1u);
	EXPECT_EQ(mon_->SlowCount(), 1u);
}

// 빠른 작업은 슬로우로 집계되지 않는다.
TEST_F(DbThreadMonitorTest, FastTaskIsNotSlow)
{
	mon_->SetSlowThreshold(10s); // 사실상 도달 불가

	const uint64_t token = mon_->BeginEnqueue("FastQuery", 6006);
	mon_->OnStart(token);
	mon_->OnFinish(token);

	EXPECT_EQ(mon_->CompletedCount(), 1u);
	EXPECT_EQ(mon_->SlowCount(), 0u);
}

// 실행이 stuck 임계값을 넘으면 CollectStuckTasks 가 해당 작업을 보고한다.
TEST_F(DbThreadMonitorTest, CollectStuckTasksDetectsLongRunning)
{
	mon_->SetStuckThreshold(1ms);

	const uint64_t token = mon_->BeginEnqueue("DeadlockSuspect", 7007);
	mon_->OnStart(token);
	std::this_thread::sleep_for(10ms);

	auto stuck = mon_->CollectStuckTasks();
	ASSERT_EQ(stuck.size(), 1u);
	EXPECT_EQ(stuck[0].id, token);
	EXPECT_EQ(stuck[0].name, "DeadlockSuspect");
	EXPECT_EQ(stuck[0].playerId, 7007);
	EXPECT_GT(stuck[0].runningMs, 0.0);

	mon_->OnFinish(token);
	EXPECT_TRUE(mon_->CollectStuckTasks().empty());
}

// 큐에서 대기만 하는(시작 전) 작업은 stuck 으로 보고되지 않는다.
TEST_F(DbThreadMonitorTest, QueuedTaskIsNotReportedStuck)
{
	mon_->SetStuckThreshold(1ms);

	mon_->BeginEnqueue("QueuedOnly", 8008); // OnStart 호출하지 않음
	std::this_thread::sleep_for(10ms);

	EXPECT_TRUE(mon_->CollectStuckTasks().empty());
}

// 알 수 없는 토큰에 대한 OnStart/OnFinish 는 무시되며 상태를 바꾸지 않는다.
TEST_F(DbThreadMonitorTest, UnknownTokenIsIgnored)
{
	mon_->OnStart(999999);
	mon_->OnFinish(999999);

	EXPECT_EQ(mon_->InflightCount(), 0u);
	EXPECT_EQ(mon_->CompletedCount(), 0u);
}

// 같은 스레드에서 여러 작업을 처리하면 스레드 통계가 누적된다.
TEST_F(DbThreadMonitorTest, MultipleTasksAccumulateOnSameThread)
{
	const int N = 5;
	for (int i = 0; i < N; ++i)
	{
		const uint64_t token = mon_->BeginEnqueue("PlayerLoad", 100 + i);
		DbTaskScope scope(token);
	}

	EXPECT_EQ(mon_->CompletedCount(), static_cast<uint64_t>(N));
	EXPECT_EQ(mon_->InflightCount(), 0u);

	auto stats = mon_->SnapshotThreadStats();
	ASSERT_EQ(stats.size(), 1u); // 모두 같은(테스트) 스레드
	EXPECT_EQ(stats.begin()->second.taskCount, static_cast<uint64_t>(N));
}

// Reset 은 모든 누적 상태와 카운터를 초기화한다.
TEST_F(DbThreadMonitorTest, ResetClearsState)
{
	const uint64_t token = mon_->BeginEnqueue("PlayerLoad", 1);
	mon_->OnStart(token);
	mon_->OnFinish(token);
	ASSERT_EQ(mon_->CompletedCount(), 1u);

	mon_->Reset();

	EXPECT_EQ(mon_->InflightCount(), 0u);
	EXPECT_EQ(mon_->CompletedCount(), 0u);
	EXPECT_EQ(mon_->SlowCount(), 0u);
	EXPECT_TRUE(mon_->SnapshotThreadStats().empty());
}

// 여러 스레드에서 동시에 enqueue/실행해도 카운트가 정확하고 누수가 없어야 한다(스레드 안전성).
TEST_F(DbThreadMonitorTest, ConcurrentTasksAreThreadSafe)
{
	const int kThreads = 8;
	const int kPerThread = 200;

	std::vector<std::thread> workers;
	workers.reserve(kThreads);
	for (int t = 0; t < kThreads; ++t)
	{
		workers.emplace_back([this, t]() {
			for (int i = 0; i < kPerThread; ++i)
			{
				const uint64_t token = mon_->BeginEnqueue("ConcurrentLoad", t * 1000 + i);
				DbTaskScope scope(token);
			}
		});
	}
	for (auto& w : workers)
		w.join();

	EXPECT_EQ(mon_->CompletedCount(), static_cast<uint64_t>(kThreads * kPerThread));
	EXPECT_EQ(mon_->InflightCount(), 0u);

	// 스레드별 처리 건수의 합은 전체 완료 수와 같아야 한다.
	uint64_t totalFromStats = 0;
	for (const auto& kv : mon_->SnapshotThreadStats())
		totalFromStats += kv.second.taskCount;
	EXPECT_EQ(totalFromStats, static_cast<uint64_t>(kThreads * kPerThread));
}
