#pragma once

#include <atomic>
#include <chrono>
#include <memory>
#include <string>
#include <thread>
#include <vector>

#include <boost/asio.hpp>

#include "BotClient.h"
#include "BotConfig.h"
#include "BotMetrics.h"

namespace bot
{
	//-----------------------------------------------------------------------------------
	// 봇 묶음 하나를 담당하는 워커. io_context 하나 + 스레드 하나 + 봇 N명을 소유한다.
	//
	// 이 구조가 이 프로젝트의 핵심 제약이다. 봇의 소켓 콜백도, AI 틱도, 계측치 집계도
	// 전부 이 스레드에서만 실행되므로
	//   - 봇끼리 공유하는 상태가 없어 락 경합이 없고,
	//   - 한 봇이 끊기거나 느려져도 다른 봇의 진행에 영향을 주지 않으며,
	//   - 워커를 늘리면 코어 수만큼 선형으로 부하를 올릴 수 있다.
	//
	// 바깥(리포터 스레드)에서 이 워커의 상태를 읽어야 할 때도 직접 만지지 않고
	// io_context 에 작업을 post 해서 워커 스레드가 스스로 스냅샷을 만들게 한다.
	//-----------------------------------------------------------------------------------
	class BotWorker
	{
	public:
		BotWorker(const BotConfig& config, int worker_index);
		~BotWorker();

		BotWorker(const BotWorker&) = delete;
		BotWorker& operator=(const BotWorker&) = delete;

		// 스레드를 시작하기 전에만 부른다.
		void AddBot(int global_index, std::string user_id, bool verbose);

		int BotCount() const { return static_cast<int>(bots_.size()); }

		void Start(std::chrono::steady_clock::time_point run_start);

		// 봇들에게 정상 종료를 지시하고 io_context 를 멈춘다(다른 스레드에서 호출 가능).
		void RequestShutdown();

		void Join();

		// 워커 스레드에게 스냅샷을 만들게 하고 받아 온다.
		// 워커가 이미 멈췄으면 마지막으로 만든 스냅샷을 돌려준다.
		MetricsSnapshot Collect();

	private:
		void ScheduleTick();
		void OnTick(const boost::system::error_code& error);
		double ElapsedSeconds() const;
		MetricsSnapshot BuildSnapshot() const;

		const BotConfig& config_;
		const int worker_index_;

		boost::asio::io_context io_context_;
		boost::asio::executor_work_guard<boost::asio::io_context::executor_type> work_guard_;
		boost::asio::steady_timer tick_timer_;

		std::vector<std::unique_ptr<BotClient>> bots_;
		std::thread thread_;

		std::chrono::steady_clock::time_point run_start_;

		// 램프업 진행 위치. 경과 시간 × 초당 접속 수만큼만 봇을 깨운다.
		size_t started_bots_ = 0;
		double connect_rate_ = 1.0;

		// 워커가 멈춘 뒤에도 요약을 만들 수 있도록 마지막 스냅샷을 남겨 둔다.
		MetricsSnapshot last_snapshot_;
		std::atomic<bool> running_{ false };
		bool shutdown_requested_ = false;
	};
}
