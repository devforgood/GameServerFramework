#include "Server.h"
#include "World.h"

game_room::game_room()
{
	world_ = new World();
	world_->Init();
}

inline void game_room::join(game_participant_ptr participant)
{
	participants_.insert(participant);
	//for (auto msg : recent_msgs_)
	//	participant->deliver(msg);
}

inline void game_room::leave(game_participant_ptr participant)
{
	participants_.erase(participant);
}

inline void game_room::deliver(const game_message& msg)
{
	recent_msgs_.push_back(msg);
	while (recent_msgs_.size() > max_recent_msgs)
		recent_msgs_.pop_front();

	for (auto participant : participants_)
		participant->deliver(msg);
}

//----------------------------------------------------------------------

inline void game_session::start()
{
	room_.join(shared_from_this());
	do_read_header();
}

inline void game_session::deliver(const game_message& msg)
{
	bool write_in_progress = !write_msgs_.empty();
	write_msgs_.push_back(msg);
	if (!write_in_progress)
	{
		do_write();
	}
}

inline void game_session::do_read_header()
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

inline void game_session::do_read_body()
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


inline void game_session::do_write()
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

//----------------------------------------------------------------------

void game_server::do_accept()
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

void game_server::tick(const boost::system::error_code& e) 
{
	TimeVal curTime = getPerfTime();
	float dt = getPerfTimeUsec(curTime - lastTime_) / 1000.0f;

	//std::cout << "tick " << dt << std::endl;
	room_.world()->update(dt);

	lastTime_ = curTime;
	// Reschedule the timer for 1 second in the future:
	timer_.expires_at(timer_.expires_at() + boost::posix_time::milliseconds(16));
	// Posts the timer event
	timer_.async_wait(boost::bind(&game_server::tick, this, boost::asio::placeholders::error));
}
