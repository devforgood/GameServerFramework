// TestServer.cpp : 이 파일에는 'main' 함수가 포함됩니다. 거기서 프로그램 실행이 시작되고 종료됩니다.
//


#include "World.h"
#include "Server.h"
#include "LogHelper.h"
//#include "BehaviorTreeCPP.h"
#include "SqlClient.h"
#include "ServerConfig.h"
#include "Common.h"

#include <thread>
#include <chrono>
#include <cstdlib>
#include <cstring>
#include <cerrno>
#include <string>
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

		// 빌드 구성을 먼저 남긴다. Debug 로 부하를 재면 송신 경로(vector/deque/shared_ptr)가
		// 검사된 이터레이터를 지나 sim 이 70배까지 느려진다 - 실제로 한 번 그 수치를
		// 두고 원인을 엉뚱한 곳에서 찾았다(PERFORMANCE.md 32절). [tick] 값을 문서의 표와
		// 비교하기 전에 첫 번째로 확인해야 하는 줄이다.
#ifdef _DEBUG
		LOG.warn("빌드: Debug — 성능 측정용이 아니다. 부하 시험은 Release 로 할 것.");
#else
		LOG.info("빌드: Release");
#endif

		if (argc < 2)
		{
			std::cerr << "Usage: game_server <port> [<port> ...] [--config <path>]\n";
			return 1;
		}

		// 설정 파일 경로. --config 로 지정하지 않으면 실행 디렉터리의 server_config.json.
		// 파일이 없으면 안전한 기본값으로 뜬다(인증 활성 / 디버그 핸들러 비활성).
		std::string configPath = "server_config.json";
		for (int i = 1; i + 1 < argc; ++i)
		{
			if (std::strcmp(argv[i], "--config") == 0)
			{
				configPath = argv[i + 1];
				break;
			}
		}

		if (!ServerConfig::Instance().Load(configPath))
		{
			std::cerr << "Failed to load config: " << configPath << "\n";
			return 1;
		}

		// 설정을 읽은 뒤에 로그 레벨을 맞춘다(설정 로드 자체의 로그는 남겨야 하므로 순서가 중요하다).
		SetLogLevel(ServerConfig::Instance().Log().level);

		std::list<tcp::endpoint> endpoints;
		for (int i = 1; i < argc; ++i)
		{
			// --config <path> 는 포트 목록이 아니다.
			if (std::strcmp(argv[i], "--config") == 0)
			{
				++i;
				continue;
			}

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

		// 월드(포트)들을 돌릴 스레드 개수는 설정 파일의 world.thread_count 에서 온다.
		// 1이면 단일 스레드(기본), 2 이상이면 월드들을 스레드에 분산해 구동한다.
		// 실제 스레드 수는 포트 개수 이하로 보정된다.
		const int worldThreadCount = ServerConfig::Instance().World().thread_count;

		ServerManager serverManager;
		serverManager.SetThreadCount(worldThreadCount);
		serverManager.Initialize(endpoints);

		serverManager.Run();
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception: " << e.what() << "\n";
	}

	return 0;
}


