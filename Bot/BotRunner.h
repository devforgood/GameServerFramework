#pragma once

#include <atomic>
#include <memory>
#include <string>
#include <vector>

#include "BotConfig.h"
#include "BotMetrics.h"
#include "BotScenario.h"
#include "BotWorker.h"

namespace bot
{
	//-----------------------------------------------------------------------------------
	// 워커들을 만들고 봇을 나눠 준 뒤, 리포트를 찍고 종료를 조율한다.
	//
	// 실행 스레드(=main)는 봇 로직을 직접 돌리지 않는다. 주기적으로 각 워커에게
	// 스냅샷을 요청해 합치고 한 줄을 출력할 뿐이다 — 측정이 부하 발생을 방해하지 않게
	// 하려는 의도적인 분리다.
	//-----------------------------------------------------------------------------------
	class BotRunner
	{
	public:
		explicit BotRunner(const BotConfig& config);
		~BotRunner();

		BotRunner(const BotRunner&) = delete;
		BotRunner& operator=(const BotRunner&) = delete;

		// 봇을 전부 돌리고 요약을 출력한다. 반환값은 프로세스 종료 코드.
		int Run();

		// 시그널 핸들러에서 부른다(원자 플래그만 건드린다).
		void RequestStop() { stop_requested_.store(true, std::memory_order_release); }

	private:
		MetricsSnapshot CollectAll();
		void BuildWorkers();
		void WriteCsvLine(const std::string& line);

		// 시나리오 데이터를 한 번 읽는다. 실패하면 경고만 남기고 시나리오 없이 돌린다 —
		// 데이터가 없다고 부하 테스트 자체를 못 하게 만들 이유는 없다.
		void LoadScenario();
		std::string MakeUserId(int index) const;

		const BotConfig& config_;

		// 봇 전원이 const 참조로 공유한다(실행 중에는 바뀌지 않는다).
		BotScenario scenario_;
		bool scenario_ready_ = false;

		// 이번 실행을 가리키는 짧은 꼬리표. 계정 id 에 붙여 매 실행 새 캐릭터로 시작한다.
		std::string run_tag_;

		std::vector<std::unique_ptr<BotWorker>> workers_;
		std::atomic<bool> stop_requested_{ false };
		std::string csv_path_;
		bool csv_header_written_ = false;
	};
}
