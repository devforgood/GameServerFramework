// TestServer.cpp : 이 파일에는 'main' 함수가 포함됩니다. 거기서 프로그램 실행이 시작되고 종료됩니다.
//


#include "World.h"
#include "Server.h"
#include "LogHelper.h"
//#include "BehaviorTreeCPP.h"
#include "SqlClient.h"
#include "Common.h"

void test()
{
	//BehaviorTreeCPP::test();
	//SqlClient::test();

	auto itr = ResourceLoader::Instance().GetItems().find(1);
	if (itr != ResourceLoader::Instance().GetItems().end())
	{
		LOG.info("Item ID: {}, Name: {}", itr->first, itr->second->name_id().c_str());
	}
	else
	{
		LOG.error("Item not found");
	}
}

//----------------------------------------------------------------------
int main(int argc, char* argv[])
{
	try
	{
		InitLog();
		LOG.info("Game Server start!!");

		if (argc < 2)
		{
			std::cerr << "Usage: game_server <port> [<port> ...]\n";
			return 1;
		}

		auto io_context = std::make_shared<boost::asio::io_context>();

		std::list<game_server> servers;
		for (int i = 1; i < argc; ++i)
		{
			tcp::endpoint endpoint(tcp::v4(), std::atoi(argv[i]));
			servers.emplace_back(io_context, endpoint);
		}

		ResourceLoader::Instance().LoadResources();
		test();


		io_context->run();
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception: " << e.what() << "\n";
	}

	return 0;
}


