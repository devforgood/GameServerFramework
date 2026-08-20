#include <csignal>
#include <iostream>
#include <string>

// asio 가 먼저 와야 한다. windows.h(=WinSock.h) 가 앞서면 asio 가 winsock2 를 못 끌어와
// "WinSock.h has already been included" 로 컴파일이 끊긴다.
#include "BotConfig.h"
#include "BotLog.h"
#include "BotRunner.h"

#include <windows.h>

namespace
{
	// 시그널 핸들러에서 만질 수 있는 것은 원자 플래그뿐이다.
	// 실제 정리는 러너의 루프가 다음 반복에서 수행한다.
	bot::BotRunner* g_runner = nullptr;

	void HandleSignal(int)
	{
		if (g_runner != nullptr)
			g_runner->RequestStop();
	}
}

int main(int argc, char* argv[])
{
	SetConsoleOutputCP(CP_UTF8);
	SetConsoleCP(CP_UTF8);

	bot::BotConfig config;
	if (!config.Load(bot::BotConfig::FindConfigPath(argc, argv)))
		return 1;

	if (!config.ApplyCommandLine(argc, argv))
		return 1;

	std::string error;
	if (!config.Validate(error))
	{
		std::cerr << "[bot] invalid config: " << error << "\n";
		return 1;
	}

	bot::LogLevel level = bot::LogLevel::Info;
	if (!bot::log::ParseLevel(config.log.level, level))
	{
		std::cerr << "[bot] unknown log level: " << config.log.level << "\n";
		return 1;
	}
	bot::log::SetLevel(level);

	bot::BotRunner runner(config);
	g_runner = &runner;
	std::signal(SIGINT, HandleSignal);
	std::signal(SIGTERM, HandleSignal);

	const int result = runner.Run();

	g_runner = nullptr;
	return result;
}
