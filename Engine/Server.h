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
	RingBuffer* ring_buf_;
	GameMessage read_msg_;
	GameMessageQueue write_msgs_;
	PlayerController* player_controller_;
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
		return io_context_;
	}

	void UpdateGameLogic(float delta);

private:
	void DoAccept();

	tcp::acceptor acceptor_;
	GameChannel channel_;
	float timeAcc;
	float playerUpdateAcc_;
	std::shared_ptr<boost::asio::io_context> io_context_;
	boost::asio::thread_pool db_thread_pool_;


private:
	void InitializeDbThreadPool();
};

class ServerManager
{
public:

	bool Initialize(std::list<tcp::endpoint>& endpoints);
	void Tick(float delta);
	void Run();

private:
	std::list<std::shared_ptr<GameServer>> servers_;
	std::shared_ptr<boost::asio::io_context> io_context_;
};