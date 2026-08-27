#include "BotLog.h"

#include <atomic>
#include <chrono>
#include <cstdio>
#include <ctime>
#include <mutex>

namespace bot::log
{
	namespace
	{
		std::atomic<int> g_level{ static_cast<int>(LogLevel::Info) };
		std::mutex g_writeMutex;

		const char* LevelTag(LogLevel level)
		{
			switch (level)
			{
			case LogLevel::Trace: return "TRC";
			case LogLevel::Debug: return "DBG";
			case LogLevel::Info:  return "INF";
			case LogLevel::Warn:  return "WRN";
			case LogLevel::Error: return "ERR";
			default:              return "OFF";
			}
		}
	}

	void SetLevel(LogLevel level)
	{
		g_level.store(static_cast<int>(level), std::memory_order_relaxed);
	}

	LogLevel GetLevel()
	{
		return static_cast<LogLevel>(g_level.load(std::memory_order_relaxed));
	}

	bool ParseLevel(const std::string& text, LogLevel& out)
	{
		if (text == "trace") { out = LogLevel::Trace; return true; }
		if (text == "debug") { out = LogLevel::Debug; return true; }
		if (text == "info")  { out = LogLevel::Info;  return true; }
		if (text == "warn")  { out = LogLevel::Warn;  return true; }
		if (text == "error") { out = LogLevel::Error; return true; }
		if (text == "off")   { out = LogLevel::Off;   return true; }
		return false;
	}

	bool ShouldLog(LogLevel level)
	{
		return static_cast<int>(level) >= g_level.load(std::memory_order_relaxed);
	}

	void Write(LogLevel level, std::string_view line)
	{
		if (!ShouldLog(level))
			return;

		const auto now = std::chrono::system_clock::now();
		const auto time = std::chrono::system_clock::to_time_t(now);
		const auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(
			now.time_since_epoch()).count() % 1000;

		std::tm tm{};
		localtime_s(&tm, &time);

		char stamp[32];
		std::snprintf(stamp, sizeof(stamp), "%02d:%02d:%02d.%03d",
			tm.tm_hour, tm.tm_min, tm.tm_sec, static_cast<int>(ms));

		std::lock_guard<std::mutex> guard(g_writeMutex);
		std::fprintf(stdout, "[%s][%s] %.*s\n", stamp, LevelTag(level),
			static_cast<int>(line.size()), line.data());

		// 파일로 리다이렉트하면 stdout 이 통째로 버퍼링된다(콘솔일 때와 다르다).
		// 부하 테스트는 몇십 분씩 돌리며 진행을 지켜보는 것이라, 흘려보내지 않으면
		// 로그가 몇 킬로바이트씩 뭉쳐 나오고 중간에 끊으면 끝부분이 통째로 사라진다.
		std::fflush(stdout);
	}
}
