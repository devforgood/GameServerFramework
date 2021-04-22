// TestServer.cpp : 이 파일에는 'main' 함수가 포함됩니다. 거기서 프로그램 실행이 시작되고 종료됩니다.
//


#include "World.h"
#include "Server.h"
#include <boost/filesystem.hpp>

#include "spdlog/spdlog.h"
#include "spdlog/sinks/daily_file_sink.h"


//----------------------------------------------------------------------
int main(int argc, char* argv[])
{
	try
	{
		if(boost::filesystem::exists("logs")==false)
			boost::filesystem::create_directory("logs");

		auto daily_sink = std::make_shared<spdlog::sinks::daily_file_sink_mt>("logs/logfile.log", 23, 59);
		// create synchronous  loggers
		auto net_logger = std::make_shared<spdlog::logger>("net", daily_sink);

		spdlog::register_logger(net_logger);

		spdlog::get("net")->info("Game Server start!!");


		if (argc < 2)
		{
			std::cerr << "Usage: game_server <port> [<port> ...]\n";
			return 1;
		}

		boost::asio::io_context io_context;

		std::list<game_server> servers;
		for (int i = 1; i < argc; ++i)
		{
			tcp::endpoint endpoint(tcp::v4(), std::atoi(argv[i]));
			servers.emplace_back(io_context, endpoint);
		}

		io_context.run();
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception: " << e.what() << "\n";
	}

	return 0;
}

