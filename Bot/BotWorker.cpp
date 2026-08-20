#include "BotWorker.h"

#include <future>

#include "BotLog.h"

namespace bot
{
	BotWorker::BotWorker(const BotConfig& config, int worker_index)
		: config_(config)
		, worker_index_(worker_index)
		, io_context_(1)   // 힌트: 이 io_context 는 스레드 하나만 돌린다(내부 락을 줄인다)
		, work_guard_(boost::asio::make_work_guard(io_context_))
		, tick_timer_(io_context_)
	{
	}

	BotWorker::~BotWorker()
	{
		Join();
	}

	void BotWorker::AddBot(int global_index, std::string user_id, bool verbose)
	{
		bots_.push_back(std::make_unique<BotClient>(io_context_, config_, global_index,
			std::move(user_id), verbose));
	}

	void BotWorker::Start(std::chrono::steady_clock::time_point run_start)
	{
		run_start_ = run_start;

		// 전체 초당 접속 수를 워커 수로 나눠 가진다. 최소 1/s 는 보장해야
		// 워커가 많을 때 램프업이 영원히 끝나지 않는 일이 없다.
		const double share = static_cast<double>(config_.run.connects_per_second)
			/ static_cast<double>(config_.run.worker_threads);
		connect_rate_ = share > 0.1 ? share : 0.1;

		running_.store(true, std::memory_order_release);

		thread_ = std::thread([this]()
			{
				ScheduleTick();
				io_context_.run();
				running_.store(false, std::memory_order_release);
			});
	}

	double BotWorker::ElapsedSeconds() const
	{
		return std::chrono::duration<double>(std::chrono::steady_clock::now() - run_start_).count();
	}

	void BotWorker::ScheduleTick()
	{
		tick_timer_.expires_after(std::chrono::milliseconds(config_.run.tick_ms));
		tick_timer_.async_wait([this](const boost::system::error_code& error) { OnTick(error); });
	}

	void BotWorker::OnTick(const boost::system::error_code& error)
	{
		if (error || shutdown_requested_)
			return;

		const double now = ElapsedSeconds();

		// 램프업: 경과 시간에 비례한 수만큼만 접속을 시작한다. 한꺼번에 붙이면
		// 수락 큐와 로그인 DB 왕복이 먼저 막혀서, 재려던 정상 부하가 아니라
		// 접속 폭주 구간만 측정하게 된다.
		const size_t allowed = static_cast<size_t>(now * connect_rate_) + 1;
		while (started_bots_ < bots_.size() && started_bots_ < allowed)
		{
			bots_[started_bots_]->Start();
			++started_bots_;
		}

		for (auto& bot : bots_)
			bot->Tick(now);

		ScheduleTick();
	}

	MetricsSnapshot BotWorker::BuildSnapshot() const
	{
		MetricsSnapshot snapshot;

		for (const auto& bot : bots_)
		{
			snapshot.stats.Merge(bot->Stats());
			snapshot.visible_actors += bot->VisibleActors();

			switch (bot->Phase())
			{
			case BotPhase::Idle:         ++snapshot.phases.idle; break;
			case BotPhase::Connecting:   ++snapshot.phases.connecting; break;
			case BotPhase::LoggingIn:    ++snapshot.phases.logging_in; break;
			case BotPhase::Spawning:     ++snapshot.phases.spawning; break;
			case BotPhase::Playing:      ++snapshot.phases.playing; break;
			case BotPhase::Dead:         ++snapshot.phases.dead; break;
			case BotPhase::Disconnected: ++snapshot.phases.disconnected; break;
			}
		}

		return snapshot;
	}

	MetricsSnapshot BotWorker::Collect()
	{
		// 워커가 멈춘 뒤(Join 이후)에는 마지막 스냅샷이 확정값이다.
		if (!running_.load(std::memory_order_acquire))
			return last_snapshot_;

		// promise 를 shared_ptr 로 잡는 이유: 아래 대기가 타임아웃돼도 post 한 작업은
		// 나중에 실행될 수 있다. 지역 변수를 참조로 넘기면 그때 이미 사라진 객체를 건드린다.
		auto promise = std::make_shared<std::promise<MetricsSnapshot>>();
		std::future<MetricsSnapshot> future = promise->get_future();

		// 계측치는 워커 스레드가 소유한다. 여기서 직접 읽으면 봇이 갱신하는 중일 수
		// 있으므로, 스냅샷 작성 자체를 워커 스레드에 맡긴다(락이 필요 없는 이유).
		boost::asio::post(io_context_, [this, promise]()
			{
				last_snapshot_ = BuildSnapshot();
				promise->set_value(last_snapshot_);
			});

		if (future.wait_for(std::chrono::seconds(2)) != std::future_status::ready)
		{
			log::Printf(LogLevel::Warn, "worker %d: 스냅샷 응답 없음(워커가 멈췄나?)", worker_index_);
			return last_snapshot_;
		}

		return future.get();
	}

	void BotWorker::RequestShutdown()
	{
		if (!running_.load(std::memory_order_acquire))
			return;

		boost::asio::post(io_context_, [this]()
			{
				shutdown_requested_ = true;

				tick_timer_.cancel();

				for (auto& bot : bots_)
					bot->Shutdown();

				// 마지막 스냅샷을 남기고 루프를 끝낸다.
				last_snapshot_ = BuildSnapshot();
				work_guard_.reset();
				io_context_.stop();
			});
	}

	void BotWorker::Join()
	{
		if (thread_.joinable())
			thread_.join();
	}
}
