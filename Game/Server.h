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
#include "Message.h"

using boost::asio::ip::tcp;



//----------------------------------------------------------------------

typedef std::deque<game_message> game_message_queue;

//----------------------------------------------------------------------

class game_participant
{
public:
	virtual ~game_participant() {}
	virtual void deliver(const game_message& msg) = 0;
};

typedef std::shared_ptr<game_participant> game_participant_ptr;

//----------------------------------------------------------------------

class game_room
{
public:
	void join(game_participant_ptr participant)
	{
		participants_.insert(participant);
		//for (auto msg : recent_msgs_)
		//	participant->deliver(msg);
	}

	void leave(game_participant_ptr participant)
	{
		participants_.erase(participant);
	}

	void deliver(const game_message& msg)
	{
		recent_msgs_.push_back(msg);
		while (recent_msgs_.size() > max_recent_msgs)
			recent_msgs_.pop_front();

		for (auto participant : participants_)
			participant->deliver(msg);
	}

private:
	std::set<game_participant_ptr> participants_;
	enum { max_recent_msgs = 100 };
	game_message_queue recent_msgs_;
};

//----------------------------------------------------------------------

class game_session
	: public game_participant,
	public std::enable_shared_from_this<game_session>
{
public:
	game_session(tcp::socket socket, game_room& room)
		: socket_(std::move(socket)),
		room_(room)
	{
	}

	void start()
	{
		room_.join(shared_from_this());
		do_read_header();
	}

	void deliver(const game_message& msg)
	{
		bool write_in_progress = !write_msgs_.empty();
		write_msgs_.push_back(msg);
		if (!write_in_progress)
		{
			do_write();
		}
	}

private:
	void do_read_header()
	{
		auto self(shared_from_this());
		boost::asio::async_read(socket_,
			boost::asio::buffer(read_msg_.data(), game_message::header_length),
			[this, self](boost::system::error_code ec, std::size_t /*length*/)
			{
				std::cout << "recv header" << std::endl;

				if (!ec && read_msg_.decode_header())
				{
					do_read_body();
				}
				else
				{
					room_.leave(shared_from_this());
				}
			});
	}

	void do_read_body()
	{
		auto self(shared_from_this());
		boost::asio::async_read(socket_,
			boost::asio::buffer(read_msg_.body(), read_msg_.body_length()),
			[this, self](boost::system::error_code ec, std::size_t /*length*/)
			{
				if (!ec)
				{
					//room_.deliver(read_msg_);
					read_msg_.body()[read_msg_.body_length()] = 0;
					std::cout << read_msg_.body() << std::endl;


					do_read_header();
				}
				else
				{
					room_.leave(shared_from_this());
				}
			});
	}

	void do_write()
	{
		auto self(shared_from_this());
		boost::asio::async_write(socket_,
			boost::asio::buffer(write_msgs_.front().data(),
				write_msgs_.front().length()),
			[this, self](boost::system::error_code ec, std::size_t /*length*/)
			{
				if (!ec)
				{
					write_msgs_.pop_front();
					if (!write_msgs_.empty())
					{
						do_write();
					}
				}
				else
				{
					room_.leave(shared_from_this());
				}
			});
	}

	tcp::socket socket_;
	game_room& room_;
	game_message read_msg_;
	game_message_queue write_msgs_;
};

//----------------------------------------------------------------------

class game_server
{
public:
	game_server(boost::asio::io_context& io_context,
		const tcp::endpoint& endpoint)
		: acceptor_(io_context, endpoint)
	{
		do_accept();
	}

private:
	void do_accept()
	{
		acceptor_.async_accept(
			[this](boost::system::error_code ec, tcp::socket socket)
			{
				if (!ec)
				{
					std::cout << "connected" << std::endl;

					std::make_shared<game_session>(std::move(socket), room_)->start();
				}

				do_accept();
			});
	}

	tcp::acceptor acceptor_;
	game_room room_;
};