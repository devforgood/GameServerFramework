#include "BotMetrics.h"

#include <cstdio>

namespace bot
{
	void LatencyHistogram::Add(uint32_t latency_ms)
	{
		++count;
		sum_ms += latency_ms;
		if (latency_ms > max_ms)
			max_ms = latency_ms;

		if (latency_ms < static_cast<uint32_t>(kBucketCount))
			++buckets[latency_ms];
		else
			++overflow;
	}

	void LatencyHistogram::Merge(const LatencyHistogram& other)
	{
		for (int i = 0; i < kBucketCount; ++i)
			buckets[i] += other.buckets[i];

		overflow += other.overflow;
		count += other.count;
		sum_ms += other.sum_ms;
		if (other.max_ms > max_ms)
			max_ms = other.max_ms;
	}

	void LatencyHistogram::Clear()
	{
		buckets.fill(0);
		overflow = 0;
		count = 0;
		sum_ms = 0;
		max_ms = 0;
	}

	uint32_t LatencyHistogram::Percentile(double p) const
	{
		if (count == 0)
			return 0;

		if (p < 0.0) p = 0.0;
		if (p > 1.0) p = 1.0;

		// 표본 n개에서 p 분위는 ceil(n*p) 번째 표본이다(1-기반).
		uint64_t rank = static_cast<uint64_t>(static_cast<double>(count) * p);
		if (static_cast<double>(rank) < static_cast<double>(count) * p)
			++rank;
		if (rank == 0)
			rank = 1;

		uint64_t seen = 0;
		for (int i = 0; i < kBucketCount; ++i)
		{
			seen += buckets[i];
			if (seen >= rank)
				return static_cast<uint32_t>(i);
		}
		return static_cast<uint32_t>(kBucketCount);
	}

	void BotStats::Merge(const BotStats& other)
	{
		packets_sent += other.packets_sent;
		packets_recv += other.packets_recv;
		bytes_sent += other.bytes_sent;
		bytes_recv += other.bytes_recv;
		packets_throttled += other.packets_throttled;
		move_sent += other.move_sent;
		skill_sent += other.skill_sent;
		skill_rejected += other.skill_rejected;
		kills += other.kills;
		deaths += other.deaths;
		connect_attempts += other.connect_attempts;
		connect_failures += other.connect_failures;
		disconnects += other.disconnects;
		login_success += other.login_success;
		login_failures += other.login_failures;
		spawn_success += other.spawn_success;
		spawn_failures += other.spawn_failures;
		ping.Merge(other.ping);
		login.Merge(other.login);
		spawn.Merge(other.spawn);
	}

	void BotPhaseCounts::Merge(const BotPhaseCounts& other)
	{
		idle += other.idle;
		connecting += other.connecting;
		logging_in += other.logging_in;
		spawning += other.spawning;
		playing += other.playing;
		dead += other.dead;
		disconnected += other.disconnected;
	}

	void MetricsSnapshot::Merge(const MetricsSnapshot& other)
	{
		stats.Merge(other.stats);
		phases.Merge(other.phases);
		visible_actors += other.visible_actors;
	}

	namespace
	{
		double PerSecond(uint64_t current, uint64_t previous, double interval_seconds)
		{
			if (interval_seconds <= 0.0)
				return 0.0;
			const uint64_t delta = current >= previous ? current - previous : 0;
			return static_cast<double>(delta) / interval_seconds;
		}
	}

	std::string FormatReportLine(double elapsed_seconds,
		const MetricsSnapshot& current,
		const MetricsSnapshot& previous,
		double interval_seconds)
	{
		const BotStats& c = current.stats;
		const BotStats& p = previous.stats;

		const int playing = current.phases.playing + current.phases.dead;
		const double avgVisible = playing > 0
			? static_cast<double>(current.visible_actors) / playing : 0.0;

		char line[512];
		std::snprintf(line, sizeof(line),
			"t=%6.1fs play=%4d conn=%4d dead=%3d down=%4d | tx %7.1f/s rx %8.1f/s "
			"| in %8.1fKB/s out %6.1fKB/s | ping p50 %u p95 %u p99 %u max %u ms "
			"| mdeath %llu skill %llu rej %llu | view %.1f",
			elapsed_seconds,
			current.phases.playing,
			current.phases.connecting + current.phases.logging_in + current.phases.spawning,
			current.phases.dead,
			current.phases.disconnected + current.phases.idle,
			PerSecond(c.packets_sent, p.packets_sent, interval_seconds),
			PerSecond(c.packets_recv, p.packets_recv, interval_seconds),
			PerSecond(c.bytes_recv, p.bytes_recv, interval_seconds) / 1024.0,
			PerSecond(c.bytes_sent, p.bytes_sent, interval_seconds) / 1024.0,
			c.ping.Percentile(0.50), c.ping.Percentile(0.95), c.ping.Percentile(0.99), c.ping.max_ms,
			static_cast<unsigned long long>(c.kills),
			static_cast<unsigned long long>(c.skill_sent),
			static_cast<unsigned long long>(c.skill_rejected),
			avgVisible);
		return line;
	}

	std::string FormatCsvHeader()
	{
		return "elapsed_s,playing,connecting,dead,down,tx_pps,rx_pps,rx_kbps,tx_kbps,"
			"ping_p50,ping_p95,ping_p99,ping_max,kills,skills,skill_rejected,avg_visible";
	}

	std::string FormatCsvLine(double elapsed_seconds,
		const MetricsSnapshot& current,
		const MetricsSnapshot& previous,
		double interval_seconds)
	{
		const BotStats& c = current.stats;
		const BotStats& p = previous.stats;

		const int playing = current.phases.playing + current.phases.dead;
		const double avgVisible = playing > 0
			? static_cast<double>(current.visible_actors) / playing : 0.0;

		char line[512];
		std::snprintf(line, sizeof(line),
			"%.1f,%d,%d,%d,%d,%.1f,%.1f,%.1f,%.1f,%u,%u,%u,%u,%llu,%llu,%llu,%.2f",
			elapsed_seconds,
			current.phases.playing,
			current.phases.connecting + current.phases.logging_in + current.phases.spawning,
			current.phases.dead,
			current.phases.disconnected + current.phases.idle,
			PerSecond(c.packets_sent, p.packets_sent, interval_seconds),
			PerSecond(c.packets_recv, p.packets_recv, interval_seconds),
			PerSecond(c.bytes_recv, p.bytes_recv, interval_seconds) / 1024.0,
			PerSecond(c.bytes_sent, p.bytes_sent, interval_seconds) / 1024.0,
			c.ping.Percentile(0.50), c.ping.Percentile(0.95), c.ping.Percentile(0.99), c.ping.max_ms,
			static_cast<unsigned long long>(c.kills),
			static_cast<unsigned long long>(c.skill_sent),
			static_cast<unsigned long long>(c.skill_rejected),
			avgVisible);
		return line;
	}

	std::string FormatSummary(const MetricsSnapshot& total, double elapsed_seconds)
	{
		const BotStats& s = total.stats;

		char text[2048];
		std::snprintf(text, sizeof(text),
			"\n================ 부하 테스트 요약 ================\n"
			"실행 시간          : %.1f s\n"
			"접속 시도/실패     : %u / %u\n"
			"로그인 성공/실패   : %u / %u   (p50 %u ms, p95 %u ms, max %u ms)\n"
			"스폰 성공/실패     : %u / %u   (p50 %u ms, p95 %u ms, max %u ms)\n"
			"연결 끊김          : %u\n"
			"송신 패킷/바이트   : %llu / %.1f MB  (%.1f pps)\n"
			"수신 패킷/바이트   : %llu / %.1f MB  (%.1f pps)\n"
			"레이트리밋 보류    : %llu\n"
			"이동 명령          : %llu\n"
			"스킬 사용/거부     : %llu / %llu\n"
			"몬스터 사망 관측    : %llu (봇별 관측이라 한 몬스터를 여러 봇이 센다)\n"
			"봇 사망            : %llu\n"
			"Ping RTT           : avg %.1f ms, p50 %u, p95 %u, p99 %u, max %u (표본 %llu)\n"
			"==================================================\n",
			elapsed_seconds,
			s.connect_attempts, s.connect_failures,
			s.login_success, s.login_failures,
			s.login.Percentile(0.50), s.login.Percentile(0.95), s.login.max_ms,
			s.spawn_success, s.spawn_failures,
			s.spawn.Percentile(0.50), s.spawn.Percentile(0.95), s.spawn.max_ms,
			s.disconnects,
			static_cast<unsigned long long>(s.packets_sent),
			static_cast<double>(s.bytes_sent) / (1024.0 * 1024.0),
			elapsed_seconds > 0 ? static_cast<double>(s.packets_sent) / elapsed_seconds : 0.0,
			static_cast<unsigned long long>(s.packets_recv),
			static_cast<double>(s.bytes_recv) / (1024.0 * 1024.0),
			elapsed_seconds > 0 ? static_cast<double>(s.packets_recv) / elapsed_seconds : 0.0,
			static_cast<unsigned long long>(s.packets_throttled),
			static_cast<unsigned long long>(s.move_sent),
			static_cast<unsigned long long>(s.skill_sent),
			static_cast<unsigned long long>(s.skill_rejected),
			static_cast<unsigned long long>(s.kills),
			static_cast<unsigned long long>(s.deaths),
			s.ping.Average(), s.ping.Percentile(0.50), s.ping.Percentile(0.95),
			s.ping.Percentile(0.99), s.ping.max_ms,
			static_cast<unsigned long long>(s.ping.count));
		return text;
	}
}
