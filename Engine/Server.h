#pragma once

#include <cstdlib>
#include <deque>
#include <iostream>
#include <list>
#include <memory>
#include <set>
#include <utility>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <span>
#include <vector>
#include <thread>

#ifndef _WIN32_WINNT         
#define _WIN32_WINNT 0x0A00   // Windows 10
#endif  

#include <boost/asio.hpp>
#include <boost/bind.hpp>
#include "GameMessage.h"
#include "PerfTimer.h"
#include "PlayerController.h"
#include "SendMessage.h"

using boost::asio::ip::tcp;


class Player;
class PlayerController;
class GameServer;
class RingBuffer;

//----------------------------------------------------------------------

typedef std::deque<std::shared_ptr<send_message>> GameMessageQueue;

//----------------------------------------------------------------------

class GameParticipant
{
public:
	virtual ~GameParticipant() {}
	virtual void Send(std::shared_ptr<send_message>& msg) = 0;
	virtual std::shared_ptr<Player> GetPlayer() = 0;
};

typedef std::shared_ptr<GameParticipant> GameParticipantPtr;

//----------------------------------------------------------------------

class World;
class GameChannel
{
public:
	void Join(GameParticipantPtr participant);

	void Leave(GameParticipantPtr participant);

	void UpdatePlayers();

	GameChannel();

	World* GetWorld() { return world_; }
private:
	World * world_;
	std::set<GameParticipantPtr> participants_;
};

//----------------------------------------------------------------------

class GameSession
	: public GameParticipant,
	public std::enable_shared_from_this<GameSession>
{
public:
	GameSession(tcp::socket socket, GameChannel& room, boost::asio::thread_pool & db_thread_pool, GameServer * server);
	~GameSession();

	void Start();

	void Send(std::shared_ptr<send_message>& msg);
	void Close();

	virtual std::shared_ptr<Player> GetPlayer() override
	{
		return player_;
	}
	void SetPlayer(std::shared_ptr<Player> player);
private:
	void DoReadHeader();
	void DoReadBody();
	void DoWrite();
	void DoRead();
	void ProcessPackets();
	void HandlePacket(std::span<const char> data);


	tcp::socket socket_;
	GameChannel& room_;
	RingBuffer* ringBuf_;
	GameMessage readMsg_;
	GameMessageQueue writeMsgs_;
	PlayerController* playerController_;
	std::shared_ptr<Player> player_;
	boost::asio::strand<boost::asio::thread_pool::executor_type> strand_;
	GameServer* server_;

	friend Player;

};

//----------------------------------------------------------------------

class GameServer
{
	const static int TICK_RATES = 100; // ms
	const static int DB_THREAD_POOL_SIZE = 4;

public:
	GameServer(std::shared_ptr<boost::asio::io_context> io_context,
		const tcp::endpoint& endpoint);

	std::shared_ptr<boost::asio::io_context> get_io_context()
	{
		return ioContext_;
	}

	void UpdateGameLogic(float delta);

	// 이 서버가 돌리는 월드. 파티처럼 맵을 넘어 다른 플레이어를 찾아야 하는 곳에서 쓴다.
	World* GetWorld() { return channel_.GetWorld(); }

private:
	void DoAccept();

	tcp::acceptor acceptor_;
	GameChannel channel_;
	float timeAcc;
	float playerUpdateAcc_;
	std::shared_ptr<boost::asio::io_context> ioContext_;
	boost::asio::thread_pool dbThreadPool_;


private:
	void InitializeDbThreadPool();
};

class ServerManager
{
	// 기본 IO 스레드 수. 1이면 기존과 동일하게 단일 스레드로 동작한다.
	static const int DEFAULT_IO_THREAD_COUNT = 1;

public:

	bool Initialize(std::list<tcp::endpoint>& endpoints);
	void Run();

	// 멀티스레드 구동 시 사용할 IO 스레드(=io_context) 개수를 설정한다.
	// 실제 스레드 수는 서버(포트) 개수를 넘지 않도록 Initialize에서 보정된다.
	void SetThreadCount(int count) { threadCount_ = count; }

	// --- 멀티스레드 샤딩 로직 (부수효과 없는 순수 함수, 단위 테스트 대상) ---

	// 서버(포트) 개수와 요청된 스레드 수로부터 실제 워커(io_context/스레드) 수를
	// 계산한다. 최소 1개를 보장하고, 서버 수보다 많은 워커는 의미가 없으므로
	// 서버 개수로 상한을 둔다. (서버가 0개면 1을 돌려준다.)
	static int ResolveWorkerCount(int serverCount, int requestedThreadCount);

	// serverCount개의 서버를 workerCount개 워커에 라운드로빈으로 배정한 결과.
	// 반환 벡터의 i번째 원소는 i번째 서버가 배정된 워커 인덱스이다.
	static std::vector<int> BuildShardingPlan(int serverCount, int workerCount);

private:
	// 하나의 io_context와 그 io_context에 할당된 서버 묶음.
	// io_context마다 정확히 하나의 스레드만 돌리므로, 해당 io_context에
	// 속한 서버/세션의 핸들러와 게임 로직은 서로 동시에 실행되지 않는다.
	// (스레드 간 게임 상태 공유가 없어 락 없이 안전하다.)
	struct IoWorker
	{
		std::shared_ptr<boost::asio::io_context> ioContext;
		std::vector<std::shared_ptr<GameServer>> servers;
	};

	// 단일 워커(스레드)의 메인 루프. primary 워커만 DB 스레드 풀 감시를 수행한다.
	void RunWorker(IoWorker& worker, bool primary);

	std::vector<IoWorker> workers_;
	int threadCount_ = DEFAULT_IO_THREAD_COUNT;
};