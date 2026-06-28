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
#include <cstdlib>
#include <cerrno>
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
			char* end = nullptr;
			errno = 0;
			long port = std::strtol(argv[i], &end, 10);

			// 숫자로 변환되지 않았거나(빈 문자열/문자 포함), 포트 범위(1~65535)를 벗어나면 거부한다.
			if (end == argv[i] || *end != '\0' || errno == ERANGE || port < 1 || port > 65535)
			{
				std::cerr << "Invalid port: " << argv[i] << " (must be 1-65535)\n";
				return 1;
			}

			tcp::endpoint endpoint(tcp::v4(), static_cast<unsigned short>(port));
			endpoints.push_back(endpoint);
		}

		// IO 스레드(=io_context) 개수 설정값.
		// 1이면 단일 스레드(기존 동작), 2 이상이면 서버(포트)들을 스레드에
		// 분산해 멀티스레드로 구동한다. 실제 스레드 수는 포트 개수 이하로 보정된다.
		const int IO_THREAD_COUNT = 1;

		ServerManager serverManager;
		serverManager.SetThreadCount(IO_THREAD_COUNT);
		serverManager.Initialize(endpoints);

		serverManager.Run();
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception: " << e.what() << "\n";
	}

	return 0;
}


