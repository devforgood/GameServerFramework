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
#include <atomic>
#include <chrono>

// 실제 정의는 Directory.Build.props 가 컴파일러 명령줄로 넘긴다(_WIN32_WINNT=0x0A00).
// 여기 있는 것은 이 헤더를 그 설정 없이 컴파일했을 때를 위한 보험이다 — 헤더에서만
// 정의하면 Server.h 가 boost 보다 먼저 include 된 TU 에서만 먹어서, 같은 라이브러리
// 안에 Windows 10 으로 컴파일된 TU 와 Boost 가 기본값으로 밀어 넣은 Windows 7 TU 가
// 섞이게 된다.
#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0A00   // Windows 10
#endif

#include <boost/asio.hpp>
#include "GameMessage.h"
#include "PerfTimer.h"
#include "DbMonitorTicker.h"
#include "PlayerController.h"
#include "SendMessage.h"
#include "TokenBucket.h"

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

	// 인증 상태는 이 TCP 연결에 속한다(Player 는 재접속 유예로 세션보다 오래 살 수 있다).
	// 로그인 검증을 통과하기 전에는 Login/Ping 외의 메시지를 처리하지 않는다.
	bool IsAuthenticated() const { return authenticated_; }
	void SetAuthenticated(bool value) { authenticated_ = value; }

	// 비동기 인증 완료 후 이어붙이기 위해 컨트롤러를 노출한다.
	// 세션 shared_ptr 을 잡고 있는 동안에만 유효하다(컨트롤러는 세션이 소유한다).
	PlayerController* GetController() { return playerController_; }

private:
	void DoWrite();
	void DoRead();

	// 비동기 read/write 완료 콜백. bind_front 로 shared_from_this() 를 앞에 묶어서 넘기므로
	// 핸들러가 살아 있는 동안 세션도 함께 유지된다(기존 [this, self] 캡처와 같은 역할).
	void OnRead(const boost::system::error_code& ec, std::size_t bytesTransferred);
	void OnWrite(const boost::system::error_code& ec, std::size_t length);

	// read/write 실패로 연결이 끊겼을 때 한 번만 정리한다.
	void HandleIoFailure();
	void ProcessPackets();
	void HandlePacket(std::span<const char> data);

	// 레이트리밋(토큰 버킷). 한 패킷을 처리해도 되는지 묻고 토큰을 하나 쓴다.
	bool ConsumePacketToken();

	// 프로토콜 위반/한도 초과로 세션을 끊는다.
	void Disconnect(const char* reason);

	tcp::socket socket_;
	GameChannel& room_;
	RingBuffer* ringBuf_;
	GameMessageQueue writeMsgs_;

	// 모아쓰기(gather write) 상태. 큐에 쌓인 메시지를 한 번의 async_write 로 내보낸다.
	// writeBuffers_ 는 그 write 가 끝날 때까지 살아 있어야 하므로 멤버로 둔다.
	std::vector<boost::asio::const_buffer> writeBuffers_;
	size_t writeBatch_ = 0;      // 지금 나가는 중인 메시지 개수(완료 시 이만큼 큐에서 뺀다)
	PlayerController* playerController_;
	std::shared_ptr<Player> player_;
	boost::asio::strand<boost::asio::thread_pool::executor_type> strand_;
	GameServer* server_;

	bool authenticated_ = false;
	bool closed_ = false;         // 중복 정리 방지(한 번만 Leave 한다)

	// 수신 패킷 레이트리밋. 설정값(max_packets_per_second / packet_burst)으로 초기화한다.
	TokenBucket packetBucket_;

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

	// 이 프레임에 필요했던 10Hz 시뮬레이션 틱 수를 돌려준다(0이면 이번 프레임에는
	// 월드를 돌리지 않았다). 2 이상이면 이미 틱이 밀려 따라잡는 중이라는 뜻이다.
	int UpdateGameLogic(float delta);

	// 동시 접속 수 상한(ServerConfig.network.max_connections) 관리.
	// 세션 생성/소멸에서 부른다. 같은 io_context(=한 스레드)에서만 호출되므로 락이 없다.
	int SessionCount() const { return sessionCount_; }
	void OnSessionOpened() { ++sessionCount_; }
	void OnSessionClosed() { if (sessionCount_ > 0) --sessionCount_; }

	// 종료 절차: 새 연결 수락을 멈추고, 접속 중인 플레이어를 전부 저장한다.
	void BeginShutdown();

private:
	void DoAccept();

	// 수락 완료 콜백. 상한을 넘으면 세션을 만들기 전에 끊고, 아니면 세션을 시작한 뒤
	// 다음 수락을 다시 건다. 소켓은 이동으로 받는다.
	void OnAccept(const boost::system::error_code& ec, tcp::socket socket);

	tcp::acceptor acceptor_;

	// channel_ 보다 먼저 선언한다(= 나중에 소멸한다).
	// channel_ 이 소멸하면 participants_ 가 풀리면서 ~GameSession 이 돌고, 그 소멸자가
	// OnSessionClosed() 로 이 카운터를 건드린다. 뒤에 선언하면 이미 소멸한 멤버를 만진다.
	int sessionCount_ = 0;
	bool acceptingConnections_ = true;

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

	// SIGINT/SIGTERM 을 받으면 모든 워커의 루프를 멈추게 한다.
	// 시그널 핸들러 컨텍스트에서 불릴 수 있으므로 atomic 플래그만 건드린다.
	void RequestShutdown() { shutdownRequested_ = true; }
	bool IsShutdownRequested() const { return shutdownRequested_; }

private:
	// 하나의 io_context와 그 io_context에 할당된 서버 묶음.
	// io_context마다 정확히 하나의 스레드만 돌리므로, 해당 io_context에
	// 속한 서버/세션의 핸들러와 게임 로직은 서로 동시에 실행되지 않는다.
	// (스레드 간 게임 상태 공유가 없어 락 없이 안전하다.)
	struct IoWorker
	{
		std::shared_ptr<boost::asio::io_context> ioContext;
		std::vector<std::shared_ptr<GameServer>> servers;
		// DB 스레드 풀 감시 티커. 감시 대상이 전역 공유 자원이라 워커 하나만 보유하고
		// 나머지 워커는 비어 있다(nullptr). 수명은 이 shared_ptr 이 관리한다.
		std::shared_ptr<DbMonitorTicker> dbMonitor;

		// 배정된 서버들을 한 프레임 진행시킨다. 프레임 루프에서 매번 불리므로 인라인이다.
		// 돌려주는 값은 이 프레임에 돌려야 했던 10Hz 틱 수의 합이다(틱이 밀리는지 재는 데 쓴다).
		int UpdateGameLogic(float delta)
		{
			int simTicks = 0;
			for (auto& server : servers)
			{
				simTicks += server->UpdateGameLogic(delta);
			}
			return simTicks;
		}

		// 이 워커가 맡은 서버들의 현재 세션 수 합. 틱 통계에 함께 남겨서
		// "몇 명이 붙었을 때 밀리기 시작했는가" 를 한 줄에서 읽을 수 있게 한다.
		int SessionCount() const
		{
			int total = 0;
			for (const auto& server : servers)
			{
				total += server->SessionCount();
			}
			return total;
		}

		// 배정된 서버들의 새 연결 수락을 멈추고 접속 중인 플레이어 저장을 요청한다.
		void BeginShutdown()
		{
			for (auto& server : servers)
			{
				server->BeginShutdown();
			}
		}

		// 대기 중인 IO 핸들러를 처리하고 실행한 핸들러 수를 돌려준다.
		size_t PollIo()
		{
			return ioContext->poll();
		}

		// DB 스레드 풀 감시 티커를 가진 워커에서만 실제로 동작한다(나머지는 즉시 반환).
		void TickDbMonitor(std::chrono::steady_clock::time_point now)
		{
			if (dbMonitor)
			{
				dbMonitor->Tick(now);
			}
		}
	};

	// 단일 워커(스레드)의 메인 루프.
	void RunWorker(IoWorker& worker);

	// SIGINT/SIGTERM 수신 콜백. 시그널용 io_context 스레드에서 불린다.
	void OnSignal(const boost::system::error_code& ec, int signalNumber);

	// 종료 요청을 받은 워커가 자기 서버들을 정리한다(수락 중단 + 플레이어 저장).
	void ShutdownWorker(IoWorker& worker);

	std::vector<IoWorker> workers_;
	int threadCount_ = DEFAULT_IO_THREAD_COUNT;
	std::atomic<bool> shutdownRequested_{ false };
};