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
	sinks.push_back(std::make_shared<spdlog::sinks::stdout_sink_st>()); // console
	sinks.push_back(std::make_shared<spdlog::sinks::daily_file_sink_mt>("logs/logfile.log", 23, 59)); //file
	// create synchronous  loggers
	auto net_logger = std::make_shared<spdlog::logger>("net", sinks.begin(), sinks.end());
	spdlog::register_logger(net_logger);

}

