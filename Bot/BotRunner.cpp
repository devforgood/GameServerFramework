#include "BotRunner.h"

#include <chrono>
#include <cstdio>
#include <fstream>
#include <thread>

#include "BotLog.h"

namespace bot
{
	namespace
	{
		// 이번 실행을 가리키는 짧은 꼬리표. 계정 id 는 캐릭터 이름이기도 해서(player.name 은
		// UNIQUE) 실행마다 달라야 새 캐릭터로 시작할 수 있다.
		std::string MakeRunTag()
		{
			const auto now = std::chrono::system_clock::now().time_since_epoch();
			const auto seconds = std::chrono::duration_cast<std::chrono::seconds>(now).count();

			char buffer[16];
			std::snprintf(buffer, sizeof(buffer), "%05x",
				static_cast<unsigned int>(seconds & 0xFFFFF));
			return buffer;
		}
	}

	BotRunner::BotRunner(const BotConfig& config)
		: config_(config)
		, csv_path_(config.log.csv_path)
	{
		if (config.quest.enabled && config.quest.fresh_accounts)
			run_tag_ = MakeRunTag();
	}

	std::string BotRunner::MakeUserId(int index) const
	{
		char buffer[128];
		if (run_tag_.empty())
		{
			std::snprintf(buffer, sizeof(buffer), "%s%06d",
				config_.bots.user_prefix.c_str(), config_.bots.user_index_start + index);
		}
		else
		{
			// 메인 퀘스트는 반복할 수 없다. 지난 실행의 계정으로 다시 붙으면 이미 끝낸
			// 퀘스트를 받지 못해 "처음부터" 가 성립하지 않는다.
			std::snprintf(buffer, sizeof(buffer), "%s%s_%06d",
				config_.bots.user_prefix.c_str(), run_tag_.c_str(),
				config_.bots.user_index_start + index);
		}
		return buffer;
	}

	void BotRunner::LoadScenario()
	{
		if (!config_.quest.enabled)
		{
			log::Printf(LogLevel::Info, "시나리오 진행 꺼짐 — 사냥/배회만 한다");
			return;
		}

		std::string error;
		if (!scenario_.Load(config_.quest.gamedata_dir, error))
		{
			log::Printf(LogLevel::Warn,
				"시나리오 데이터를 읽지 못했다(%s). 시나리오 없이 계속한다", error.c_str());
			return;
		}

		scenario_ready_ = true;

		const std::vector<int>& chains = scenario_.MainChains();
		std::string chain_text;
		for (int chain_id : chains)
		{
			if (!chain_text.empty())
				chain_text += ", ";
			chain_text += std::to_string(chain_id);
		}

		log::Printf(LogLevel::Info,
			"시나리오 데이터 로드 완료 — 메인 체인 %d개(%s), 봇마다 번호로 가지를 고른다",
			static_cast<int>(chains.size()), chain_text.c_str());
	}

	BotRunner::~BotRunner() = default;

	void BotRunner::BuildWorkers()
	{
		const int worker_count = config_.run.worker_threads;
		const BotScenario* scenario = scenario_ready_ ? &scenario_ : nullptr;

		workers_.reserve(worker_count);
		for (int i = 0; i < worker_count; ++i)
			workers_.push_back(std::make_unique<BotWorker>(config_, scenario, i));

		// 봇을 워커에 라운드로빈으로 배정한다. 봇끼리 공유 상태가 없으므로 어떤 배정이든
		// 정확도에는 영향이 없고, 균등 분배가 스레드 간 부하만 고르게 만든다.
		for (int index = 0; index < config_.bots.count; ++index)
		{
			const bool verbose = index < config_.log.verbose_bots;
			workers_[index % worker_count]->AddBot(index, MakeUserId(index), verbose);
		}
	}

	MetricsSnapshot BotRunner::CollectAll()
	{
		MetricsSnapshot total;
		for (auto& worker : workers_)
			total.Merge(worker->Collect());
		return total;
	}

	void BotRunner::WriteCsvLine(const std::string& line)
	{
		if (csv_path_.empty())
			return;

		std::ofstream file(csv_path_, std::ios::app);
		if (!file.is_open())
			return;

		if (!csv_header_written_)
		{
			file << FormatCsvHeader() << "\n";
			csv_header_written_ = true;
		}

		file << line << "\n";
	}

	int BotRunner::Run()
	{
		LoadScenario();
		BuildWorkers();

		log::Printf(LogLevel::Info,
			"봇 %d명 / 워커 %d스레드 / %s:%u / 램프업 %d cps / 실행 %d초",
			config_.bots.count, config_.run.worker_threads,
			config_.server.host.c_str(), static_cast<unsigned>(config_.server.port),
			config_.run.connects_per_second, config_.run.duration_seconds);

		const auto run_start = std::chrono::steady_clock::now();
		for (auto& worker : workers_)
			worker->Start(run_start);

		const auto report_interval = std::chrono::seconds(
			config_.run.report_interval_seconds > 0 ? config_.run.report_interval_seconds : 5);

		MetricsSnapshot previous;
		auto next_report = run_start + report_interval;

		for (;;)
		{
			std::this_thread::sleep_for(std::chrono::milliseconds(100));

			const auto now = std::chrono::steady_clock::now();
			const double elapsed = std::chrono::duration<double>(now - run_start).count();

			if (stop_requested_.load(std::memory_order_acquire))
			{
				log::Printf(LogLevel::Info, "중단 요청을 받았다. 봇을 정리한다.");
				break;
			}

			if (config_.run.duration_seconds > 0 && elapsed >= config_.run.duration_seconds)
				break;

			if (now < next_report)
				continue;

			const double interval = std::chrono::duration<double>(report_interval).count();
			const MetricsSnapshot current = CollectAll();

			const std::string line = FormatReportLine(elapsed, current, previous, interval);
			log::Write(LogLevel::Info, line);
			WriteCsvLine(FormatCsvLine(elapsed, current, previous, interval));

			previous = current;
			next_report += report_interval;
		}

		for (auto& worker : workers_)
			worker->RequestShutdown();

		for (auto& worker : workers_)
			worker->Join();

		const double elapsed = std::chrono::duration<double>(
			std::chrono::steady_clock::now() - run_start).count();

		const MetricsSnapshot total = CollectAll();
		std::fputs(FormatSummary(total, elapsed).c_str(), stdout);

		// 접속조차 못 했으면 테스트가 성립하지 않은 것이므로 실패로 알린다.
		return total.stats.login_success > 0 ? 0 : 1;
	}
}
