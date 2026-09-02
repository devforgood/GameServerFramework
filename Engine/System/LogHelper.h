#pragma once
#include <boost/filesystem.hpp>

#include "spdlog/spdlog.h"
#include "spdlog/sinks/daily_file_sink.h"
#include "spdlog/sinks/stdout_sinks.h"

#define LOG (*(spdlog::get("net")))

inline void InitLog() 
{
	if (boost::filesystem::exists("logs") == false)
		boost::filesystem::create_directory("logs");
	std::vector<spdlog::sink_ptr> sinks;
	// _st 가 아니라 _mt 여야 한다. 월드를 여러 스레드에서 돌리면(world.thread_count > 1)
	// 워커마다 이 로거로 들어오는데, _st 싱크는 잠그지 않아 출력이 섞이고 내부 상태가 깨진다.
	sinks.push_back(std::make_shared<spdlog::sinks::stdout_sink_mt>()); // console
	sinks.push_back(std::make_shared<spdlog::sinks::daily_file_sink_mt>("logs/logfile.log", 23, 59)); //file
	// create synchronous  loggers
	auto net_logger = std::make_shared<spdlog::logger>("net", sinks.begin(), sinks.end());
	net_logger->set_level(spdlog::level::debug);
	spdlog::register_logger(net_logger);

}

// 기동 후 설정(log.level)으로 레벨을 낮춘다.
//
// 이게 필요한 이유는 성능이다. 로그는 두 싱크(콘솔 + 파일)에 동기로 쓰이고, 콘솔 싱크는
// 워커 스레드 전부가 뮤텍스 하나를 두고 줄을 선다. 이동/스킬처럼 패킷마다 한 줄을 남기는
// 로그가 켜져 있으면, 부하가 걸렸을 때 월드 틱이 아니라 로그가 프레임을 붙잡는다.
// (실측: 봇 3200명에서 초당 2800줄이 나왔고 프레임이 수백 ms 씩 멈췄다.)
inline void SetLogLevel(const std::string& level)
{
	auto logger = spdlog::get("net");
	if (!logger)
		return;

	const spdlog::level::level_enum parsed = spdlog::level::from_str(level);
	// from_str 은 모르는 이름에 off 를 돌려준다. 오타로 로그가 통째로 사라지는 것보다
	// 기존 레벨을 유지하는 편이 낫다.
	if (parsed == spdlog::level::off && level != "off")
	{
		logger->warn("알 수 없는 log.level '{}' — 기존 레벨을 유지한다", level);
		return;
	}
	logger->set_level(parsed);
}

