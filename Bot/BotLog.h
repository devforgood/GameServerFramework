#pragma once

#include <cstdio>
#include <string>
#include <string_view>

namespace bot
{
	enum class LogLevel : int
	{
		Trace = 0,
		Debug,
		Info,
		Warn,
		Error,
		Off,
	};

	// 여러 워커 스레드가 함께 쓰는 콘솔 로거.
	//
	// 봇 로직에서 부르므로 출력 자체가 부하가 되지 않아야 한다. 그래서
	//   1) 레벨 검사는 원자적 읽기 한 번(락 없음)이고,
	//   2) 실제 출력만 뮤텍스로 직렬화한다.
	// 봇 전원이 매 틱 로그를 남기면 측정 대상이 서버가 아니라 이 락이 되므로,
	// 상세 로그는 BotConfig.log.verbose_bots 로 앞쪽 봇 몇 명에게만 허용한다.
	namespace log
	{
		void SetLevel(LogLevel level);
		LogLevel GetLevel();
		bool ParseLevel(const std::string& text, LogLevel& out);

		bool ShouldLog(LogLevel level);

		// 이미 완성된 한 줄을 출력한다(개행은 여기서 붙인다).
		void Write(LogLevel level, std::string_view line);

		// printf 형식으로 한 줄을 만든다. 레벨에서 걸리면 포맷 비용도 들지 않는다.
		// std::string 인자는 반드시 c_str() 로 넘길 것.
		template <class... Args>
		void Printf(LogLevel level, const char* format, Args... args)
		{
			if (!ShouldLog(level))
				return;

			char buffer[512];
			const int written = std::snprintf(buffer, sizeof(buffer), format, args...);
			if (written <= 0)
				return;

			const size_t length = (static_cast<size_t>(written) < sizeof(buffer))
				? static_cast<size_t>(written) : sizeof(buffer) - 1;
			Write(level, std::string_view(buffer, length));
		}
	}
}
