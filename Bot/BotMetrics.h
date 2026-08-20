#pragma once

#include <array>
#include <cstdint>
#include <string>

namespace bot
{
	// 지연 분포. 1ms 버킷 1024개 + 초과 버킷 하나.
	//
	// 부하 테스트에서 평균만 보면 서버가 멈칫한 구간이 통째로 지워진다(p99 가 진짜 신호다).
	// 그렇다고 모든 표본을 모으면 봇 수 × 시간만큼 메모리를 먹으므로 고정 버킷을 쓴다.
	// 버킷은 소유 스레드에서만 갱신하고, 리포트 시점에 그 스레드가 직접 합산본을 만든다.
	struct LatencyHistogram
	{
		static constexpr int kBucketCount = 1024;   // 0 ~ 1023ms

		std::array<uint32_t, kBucketCount> buckets{};
		uint32_t overflow = 0;                       // 1023ms 초과
		uint64_t count = 0;
		uint64_t sum_ms = 0;
		uint32_t max_ms = 0;

		void Add(uint32_t latency_ms);
		void Merge(const LatencyHistogram& other);
		void Clear();

		double Average() const { return count > 0 ? static_cast<double>(sum_ms) / count : 0.0; }

		// p 는 0.0~1.0. 표본이 없으면 0, 초과 버킷에 걸리면 kBucketCount 를 돌려준다.
		uint32_t Percentile(double p) const;
	};

	// 봇 한 명의 계측치. 소유 워커 스레드에서만 읽고 쓴다(원자 연산이 필요 없다).
	struct BotStats
	{
		uint64_t packets_sent = 0;
		uint64_t packets_recv = 0;
		uint64_t bytes_sent = 0;
		uint64_t bytes_recv = 0;

		// 레이트리밋(토큰 버킷)에 걸려 보내지 못한 패킷. 서버 한도에 닿기 전에
		// 봇이 스스로 눌렀다는 뜻이라, 이 값이 크면 시나리오가 과격한 것이다.
		uint64_t packets_throttled = 0;

		uint64_t move_sent = 0;
		uint64_t skill_sent = 0;
		uint64_t skill_rejected = 0;
		// 시야 안에서 몬스터가 죽는 것을 본 횟수. 봇마다 따로 세므로 한 몬스터의 죽음을
		// 그것을 보고 있던 봇 수만큼 센다(처치 수가 아니라 사망 관측 수다).
		uint64_t kills = 0;
		uint64_t deaths = 0;         // 내 캐릭터가 죽은 횟수

		uint32_t connect_attempts = 0;
		uint32_t connect_failures = 0;
		uint32_t disconnects = 0;
		uint32_t login_success = 0;
		uint32_t login_failures = 0;
		uint32_t spawn_success = 0;
		uint32_t spawn_failures = 0;

		LatencyHistogram ping;    // Ping 왕복
		LatencyHistogram login;   // Login 요청 → 응답
		LatencyHistogram spawn;   // AddAgent 요청 → 응답

		void Merge(const BotStats& other);
	};

	// 실행 중인 봇들의 상태 분포. 리포트 한 줄을 만드는 데 필요한 만큼만 센다.
	struct BotPhaseCounts
	{
		int idle = 0;
		int connecting = 0;
		int logging_in = 0;
		int spawning = 0;
		int playing = 0;
		int dead = 0;
		int disconnected = 0;

		int Total() const { return idle + connecting + logging_in + spawning + playing + dead + disconnected; }

		void Merge(const BotPhaseCounts& other);
	};

	struct MetricsSnapshot
	{
		BotStats stats;
		BotPhaseCounts phases;

		// 시야에 들어와 있는 액터 수의 합(봇 전체). 브로드캐스트 부하의 크기를 가늠한다.
		uint64_t visible_actors = 0;

		void Merge(const MetricsSnapshot& other);
	};

	// 직전 리포트와의 차분으로 초당 값을 계산해 한 줄로 만든다.
	std::string FormatReportLine(double elapsed_seconds,
		const MetricsSnapshot& current,
		const MetricsSnapshot& previous,
		double interval_seconds);

	std::string FormatCsvHeader();
	std::string FormatCsvLine(double elapsed_seconds,
		const MetricsSnapshot& current,
		const MetricsSnapshot& previous,
		double interval_seconds);

	std::string FormatSummary(const MetricsSnapshot& total, double elapsed_seconds);
}
