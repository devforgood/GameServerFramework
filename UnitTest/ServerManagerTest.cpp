#include <gtest/gtest.h>
#include "Server.h"

#include <boost/asio.hpp>

#include <atomic>
#include <chrono>
#include <thread>
#include <vector>
#include <numeric>

using namespace std::chrono_literals;

// ServerManager 의 멀티스레드 샤딩 설계를 검증한다.
//
// 설계 요약:
//  - 스레드(=io_context)마다 자기에게 배정된 GameServer 만 처리한다.
//  - 워커 수는 [1, 서버 개수] 로 보정된다(서버 없는 스레드는 무의미).
//  - 서버는 워커들에 라운드로빈으로 배정되어, 한 서버는 정확히 한 스레드에 묶인다.
//
// 실제 Run()/Initialize() 는 무한 루프 + 실제 포트 바인딩 + DB 초기화를 하므로
// 단위 테스트에 부적합하다. 그래서 분배 로직을 순수 함수(ResolveWorkerCount /
// BuildShardingPlan)로 분리했고, 여기서 그 로직과 per-thread io_context 모델을 검증한다.

//======================================================================
// 1) 워커 수 보정 로직
//======================================================================

// 요청 스레드 수가 서버 수보다 적으면 그대로 사용된다.
TEST(ServerShardingTest, WorkerCountUsedWhenBelowServerCount)
{
	EXPECT_EQ(ServerManager::ResolveWorkerCount(/*servers=*/4, /*requested=*/2), 2);
	EXPECT_EQ(ServerManager::ResolveWorkerCount(8, 3), 3);
}

// 요청 스레드 수가 서버 수를 넘으면 서버 수로 상한 처리된다.
TEST(ServerShardingTest, WorkerCountClampedToServerCount)
{
	EXPECT_EQ(ServerManager::ResolveWorkerCount(/*servers=*/2, /*requested=*/8), 2);
	EXPECT_EQ(ServerManager::ResolveWorkerCount(1, 16), 1);
}

// 0 이하의 스레드 수는 최소 1로 보정된다.
TEST(ServerShardingTest, WorkerCountAtLeastOne)
{
	EXPECT_EQ(ServerManager::ResolveWorkerCount(/*servers=*/4, /*requested=*/0), 1);
	EXPECT_EQ(ServerManager::ResolveWorkerCount(4, -5), 1);
}

// 서버 수와 스레드 수가 같으면 1서버:1스레드.
TEST(ServerShardingTest, WorkerCountEqualsServerCount)
{
	EXPECT_EQ(ServerManager::ResolveWorkerCount(4, 4), 4);
}

//======================================================================
// 2) 라운드로빈 배정 계획
//======================================================================

// 단일 워커면 모든 서버가 워커 0에 배정된다(기존 단일 스레드 동작).
TEST(ServerShardingTest, SingleWorkerGetsAllServers)
{
	auto plan = ServerManager::BuildShardingPlan(/*servers=*/3, /*workers=*/1);
	ASSERT_EQ(plan.size(), 3u);
	for (int w : plan)
		EXPECT_EQ(w, 0);
}

// 서버 수 == 워커 수면 각 서버가 서로 다른 워커에 1:1 배정된다.
TEST(ServerShardingTest, OneServerPerWorker)
{
	auto plan = ServerManager::BuildShardingPlan(/*servers=*/4, /*workers=*/4);
	ASSERT_EQ(plan.size(), 4u);
	EXPECT_EQ(plan[0], 0);
	EXPECT_EQ(plan[1], 1);
	EXPECT_EQ(plan[2], 2);
	EXPECT_EQ(plan[3], 3);
}

// 서버 수 > 워커 수면 라운드로빈으로 골고루 분배된다.
TEST(ServerShardingTest, RoundRobinDistribution)
{
	auto plan = ServerManager::BuildShardingPlan(/*servers=*/5, /*workers=*/2);
	ASSERT_EQ(plan.size(), 5u);
	// 0,1,0,1,0
	EXPECT_EQ(plan[0], 0);
	EXPECT_EQ(plan[1], 1);
	EXPECT_EQ(plan[2], 0);
	EXPECT_EQ(plan[3], 1);
	EXPECT_EQ(plan[4], 0);
}

// 배정은 균형적이어야 한다: 각 워커가 맡는 서버 수의 최대-최소 차가 1 이하.
TEST(ServerShardingTest, DistributionIsBalanced)
{
	const int servers = 7;
	const int workers = 3;
	auto plan = ServerManager::BuildShardingPlan(servers, workers);
	ASSERT_EQ(plan.size(), static_cast<size_t>(servers));

	std::vector<int> load(workers, 0);
	for (int w : plan)
	{
		ASSERT_GE(w, 0);
		ASSERT_LT(w, workers);
		load[w]++;
	}

	int mn = *std::min_element(load.begin(), load.end());
	int mx = *std::max_element(load.begin(), load.end());
	EXPECT_LE(mx - mn, 1);

	// 합은 전체 서버 수와 같아야 한다(누락/중복 없음).
	EXPECT_EQ(std::accumulate(load.begin(), load.end(), 0), servers);
}

// 경계: 서버 0개면 빈 계획.
TEST(ServerShardingTest, NoServersYieldsEmptyPlan)
{
	EXPECT_TRUE(ServerManager::BuildShardingPlan(0, 1).empty());
}

// ResolveWorkerCount + BuildShardingPlan 조합: 모든 서버는 유효 워커에 배정되고
// 모든 워커는 최소 1개 서버를 갖는다(빈 스레드가 생기지 않는다).
TEST(ServerShardingTest, NoIdleWorkersAfterClamp)
{
	const int servers = 3;
	const int requested = 10; // 서버보다 많이 요청
	int workers = ServerManager::ResolveWorkerCount(servers, requested);
	EXPECT_EQ(workers, servers);

	auto plan = ServerManager::BuildShardingPlan(servers, workers);
	std::vector<int> load(workers, 0);
	for (int w : plan) load[w]++;
	for (int l : load)
		EXPECT_GE(l, 1); // 모든 워커가 일을 맡는다
}

//======================================================================
// 3) per-thread io_context 모델: 동시성과 격리 검증
//======================================================================
//
// RunWorker 의 핵심 모델을 그대로 모사한다. io_context 마다 정확히 하나의
// 스레드를 돌려서: (a) 여러 스레드가 실제로 동시에 진행되고, (b) 각 스레드는
// 자기 io_context 의 작업만 처리하여 상태가 서로 격리되는지 확인한다.

TEST(ServerConcurrencyTest, PerThreadIoContextsRunConcurrently)
{
	const int kWorkers = 4;

	std::vector<std::shared_ptr<boost::asio::io_context>> contexts;
	for (int i = 0; i < kWorkers; ++i)
		contexts.push_back(std::make_shared<boost::asio::io_context>());

	// 모든 워커가 동시에 살아있는지 확인하기 위한 배리어 역할의 카운터.
	std::atomic<int> arrived{ 0 };
	std::atomic<bool> allArrived{ false };
	std::atomic<int> sawConcurrency{ 0 };

	// 워커별로 독립된 상태(격리 검증용).
	std::vector<int> perWorkerData(kWorkers, 0);

	for (int i = 0; i < kWorkers; ++i)
	{
		auto ctx = contexts[i];
		boost::asio::post(*ctx, [i, &perWorkerData, &arrived, &allArrived, &sawConcurrency, kWorkers]() {
			// 자기 워커의 데이터만 건드린다 → 락 없이 안전해야 한다.
			perWorkerData[i] = (i + 1) * 100;

			// 모든 워커가 이 지점에 도달할 때까지 대기. 단일 스레드라면
			// 영원히 못 모이므로, 모이면 곧 진짜 동시 실행됐다는 의미.
			arrived.fetch_add(1, std::memory_order_acq_rel);
			auto deadline = std::chrono::steady_clock::now() + 2s;
			while (arrived.load(std::memory_order_acquire) < kWorkers)
			{
				if (std::chrono::steady_clock::now() > deadline)
					return; // 동시성 실패 시 데드락 방지
				std::this_thread::yield();
			}
			allArrived.store(true, std::memory_order_release);
			sawConcurrency.fetch_add(1, std::memory_order_acq_rel);
			});
	}

	// 각 io_context 를 자기 전용 스레드에서 구동(= RunWorker 모델).
	std::vector<std::thread> threads;
	for (int i = 0; i < kWorkers; ++i)
	{
		auto ctx = contexts[i];
		threads.emplace_back([ctx]() { ctx->run(); });
	}
	for (auto& t : threads)
		t.join();

	// (a) 동시성: 모든 워커가 배리어를 통과했다 → 진짜 병렬 실행됨.
	EXPECT_TRUE(allArrived.load());
	EXPECT_EQ(sawConcurrency.load(), kWorkers);

	// (b) 격리: 각 워커는 자기 슬롯에만 기록했다.
	for (int i = 0; i < kWorkers; ++i)
		EXPECT_EQ(perWorkerData[i], (i + 1) * 100);
}

// 한 io_context 의 핸들러들은 단일 스레드에서 순차 실행되므로, 공유 카운터를
// 락 없이 증가시켜도 경합이 없다(= GameServer 상태가 자기 스레드에 묶이는 근거).
TEST(ServerConcurrencyTest, HandlersOnOneContextAreSerialized)
{
	boost::asio::io_context ctx;

	long counter = 0; // 일부러 비원자적: 단일 스레드 직렬 실행이면 안전
	const int kPosts = 10000;

	for (int i = 0; i < kPosts; ++i)
		boost::asio::post(ctx, [&counter]() { ++counter; });

	// 한 스레드로만 구동.
	ctx.run();

	EXPECT_EQ(counter, kPosts);
}

// 서로 다른 io_context 에 배정된 작업은 서로의 진행을 막지 않는다(격리).
// 한 컨텍스트가 바쁜 동안에도 다른 컨텍스트는 자기 작업을 완료할 수 있다.
TEST(ServerConcurrencyTest, ContextsDoNotBlockEachOther)
{
	auto busyCtx = std::make_shared<boost::asio::io_context>();
	auto freeCtx = std::make_shared<boost::asio::io_context>();

	std::atomic<bool> releaseBusy{ false };
	std::atomic<bool> freeDone{ false };

	// busy 컨텍스트: 신호가 올 때까지 핸들러 안에서 스핀(자기 스레드를 점유).
	boost::asio::post(*busyCtx, [&releaseBusy]() {
		auto deadline = std::chrono::steady_clock::now() + 2s;
		while (!releaseBusy.load(std::memory_order_acquire))
		{
			if (std::chrono::steady_clock::now() > deadline) break;
			std::this_thread::yield();
		}
		});

	// free 컨텍스트: 즉시 완료되어야 한다(busy 에 막히지 않음).
	boost::asio::post(*freeCtx, [&freeDone]() {
		freeDone.store(true, std::memory_order_release);
		});

	std::thread busyThread([busyCtx]() { busyCtx->run(); });
	std::thread freeThread([freeCtx]() { freeCtx->run(); });

	// free 작업은 busy 가 아직 점유 중이어도 완료되어야 한다.
	auto deadline = std::chrono::steady_clock::now() + 2s;
	while (!freeDone.load(std::memory_order_acquire) &&
		std::chrono::steady_clock::now() < deadline)
	{
		std::this_thread::yield();
	}
	EXPECT_TRUE(freeDone.load()) << "다른 컨텍스트가 busy 컨텍스트에 의해 막혔다";

	// 정리: busy 를 풀어주고 조인.
	releaseBusy.store(true, std::memory_order_release);
	busyThread.join();
	freeThread.join();
}
