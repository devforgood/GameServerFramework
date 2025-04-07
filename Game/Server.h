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

#ifndef _WIN32_WINNT         
#define _WIN32_WINNT 0x0A00   // Windows 10
#endif  

#include <boost/asio.hpp>
#include <boost/bind.hpp>
#include "Message.h"
#include "PerfTimer.h"
#include "MessageDispatcher.h"
#include "SendMessage.h"

using boost::asio::ip::tcp;



//----------------------------------------------------------------------

typedef std::deque<std::shared_ptr<send_message>> game_message_queue;

//----------------------------------------------------------------------

class game_participant
{
public:
	virtual ~game_participant() {}
	virtual void send(std::shared_ptr<send_message> msg) = 0;
};

typedef std::shared_ptr<game_participant> game_participant_ptr;

//----------------------------------------------------------------------

class World;
class game_room
{
public:
	void join(game_participant_ptr participant);

	void leave(game_participant_ptr participant);

	void deliver(std::shared_ptr<send_message> msg);

	game_room();

	World* world() { return world_; }
private:
	std::set<game_participant_ptr> participants_;
	enum { max_recent_msgs = 100 };
	game_message_queue recent_msgs_;
	World * world_;
};

//----------------------------------------------------------------------
class Player;
class MessageDispatcher;
class game_session
	: public game_participant,
	public std::enable_shared_from_this<game_session>
{
public:
	game_session(tcp::socket socket, game_room& room);
	~game_session();

	void start();

	void send(std::shared_ptr<send_message>  msg);


private:
	void do_read_header();

	void do_read_body();

	void do_write();

	tcp::socket socket_;
	game_room& room_;
	game_message read_msg_;
	game_message_queue write_msgs_;
	MessageDispatcher* dispatcher_;
	Player* player_;
};

//----------------------------------------------------------------------

class game_server
{
	const int TICK_RATES = 100; // ms

public:
	game_server(boost::asio::io_context& io_context,
		const tcp::endpoint& endpoint);

private:
	void do_accept();
	void tick(const boost::system::error_code& e);

	tcp::acceptor acceptor_;
	game_room room_;
	boost::asio::deadline_timer timer_;
	TimeVal lastTime_;
	float timeAcc;
};