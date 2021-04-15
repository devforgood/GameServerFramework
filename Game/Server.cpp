#include "Server.h"
#include "World.h"
#include "flatbuffers/flatbuffers.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"

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
		participant->send(msg);
}

//----------------------------------------------------------------------

inline void game_session::start()
{
	dispatcher_ = new MessageDispatcher();
	dispatcher_->world_ = room_.world();

	room_.join(shared_from_this());
	do_read_header();
}

inline void game_session::send(const game_message& msg)
{
	bool write_in_progress = !write_msgs_.empty();
	write_msgs_.push_back(msg);
	if (!write_in_progress)
	{
		do_write();
	}
}

void game_session::send(void* msg, int size)
{
	memcpy(send_msg_.body(), msg, size);
	send_msg_.body_length(size);
	send_msg_.encode_header();
	send(send_msg_);
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

				auto msg = syncnet::GetGameMessage(read_msg_.body());
				std::cout << "recv message type : " << msg->msg_type() << std::endl;

				switch (msg->msg_type())
				{
				case syncnet::GameMessages::GameMessages_AddAgent:			dispatcher_->dispatch(msg->msg_as_AddAgent()); break;
				case syncnet::GameMessages::GameMessages_RemoveAgent:		dispatcher_->dispatch(msg->msg_as_RemoveAgent()); break;
				case syncnet::GameMessages::GameMessages_SetMoveTarget:		dispatcher_->dispatch(msg->msg_as_SetMoveTarget()); break;
				case syncnet::GameMessages::GameMessages_Ping:				dispatcher_->dispatch(msg->msg_as_Ping()); break;
				}

				room_.world()->SendWorldState(this);


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

game_server::game_server(boost::asio::io_context& io_context, const tcp::endpoint& endpoint)
	: acceptor_(io_context, endpoint)
	, timer_(io_context, boost::posix_time::milliseconds(16)) // 60 «¡∑π¿”
{
	do_accept();

	timeAcc = 0.0f;
	lastTime_ = getPerfTime();
	timer_.async_wait(boost::bind(&game_server::tick, this, boost::asio::placeholders::error));
}

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
	float dt = getPerfTimeUsec(curTime - lastTime_) / 1000000.0f;

	std::cout << "tick " << dt << std::endl;

	// Update sample simulation.
	const float SIM_RATE = 20;
	const float DELTA_TIME = 1.0f / SIM_RATE;
	timeAcc = rcClamp(timeAcc + dt, -1.0f, 1.0f);
	int simIter = 0;
	while (timeAcc > DELTA_TIME)
	{
		timeAcc -= DELTA_TIME;
		if (simIter < 5)
		{
			room_.world()->update(DELTA_TIME);

			auto agent = room_.world()->map()->crowd()->getAgent(0);
			if (agent != nullptr)
			{
				std::cout << "update agent " << agent->active << " pos (" << agent->npos[0] * -1 << "," << agent->npos[1] << "," << agent->npos[2] << ")" << std::endl;

			}
		}
		simIter++;
	}

	lastTime_ = curTime;
	// Reschedule the timer for 1 second in the future:
	timer_.expires_at(timer_.expires_at() + boost::posix_time::milliseconds(16));
	// Posts the timer event
	timer_.async_wait(boost::bind(&game_server::tick, this, boost::asio::placeholders::error));
}
