// TestServer.cpp : 이 파일에는 'main' 함수가 포함됩니다. 거기서 프로그램 실행이 시작되고 종료됩니다.
//


#include "World.h"
#include "Server.h"
#include "LogHelper.h"
//#include "BehaviorTreeCPP.h"
#include "SqlClient.h"
#include "Common.h"

#include <thread>
#include <chrono>
#include <boost/array.hpp>

using namespace std::chrono_literals;

//----------------------------------------------------------------------
int main(int argc, char* argv[])
{
	try
	{
		SetConsoleOutputCP(CP_UTF8);
		SetConsoleCP(CP_UTF8);

		InitLog();
		LOG.info("Game Server start!!");

		if (argc < 2)
		{
			std::cerr << "Usage: game_server <port> [<port> ...]\n";
			return 1;
		}

		std::list<tcp::endpoint> endpoints;
		for (int i = 1; i < argc; ++i)
		{
			tcp::endpoint endpoint(tcp::v4(), std::atoi(argv[i]));
			endpoints.push_back(endpoint);
		}

		ServerManager serverManager;
		serverManager.Initialize(endpoints);

		serverManager.Run();
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception: " << e.what() << "\n";
	}

	return 0;
}


