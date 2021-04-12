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
				case syncnet::GameMessages::GameMessages_AddAgent:
				{
					std::cout << "x : " << msg->msg_as_AddAgent()->pos()->x() << std::endl;
					std::cout << "y : " << msg->msg_as_AddAgent()->pos()->y() << std::endl;
					std::cout << "z : " << msg->msg_as_AddAgent()->pos()->z() << std::endl;

					float* v = new float[3];
					v[0] = msg->msg_as_AddAgent()->pos()->x() * -1;
					v[1] = msg->msg_as_AddAgent()->pos()->y();
					v[2] = msg->msg_as_AddAgent()->pos()->z();
					room_.world()->map()->addAgent(v);
					break;
				}
				case syncnet::GameMessages::GameMessages_RemoveAgent:
					std::cout << "agent id : " << msg->msg_as_RemoveAgent()->agentId() << std::endl;
					break;
				case syncnet::GameMessages::GameMessages_SetMoveTarget:
				{
					std::cout << "agent id : " << msg->msg_as_SetMoveTarget()->agentId() << std::endl;
					std::cout << "x : " << msg->msg_as_SetMoveTarget()->pos()->x() << std::endl;
					std::cout << "y : " << msg->msg_as_SetMoveTarget()->pos()->y() << std::endl;
					std::cout << "z : " << msg->msg_as_SetMoveTarget()->pos()->z() << std::endl;

					float* v = new float[3];
					v[0] = msg->msg_as_SetMoveTarget()->pos()->x() * -1;
					v[1] = msg->msg_as_SetMoveTarget()->pos()->y();
					v[2] = msg->msg_as_SetMoveTarget()->pos()->z();
					room_.world()->map()->setMoveTarget(v, false);
					break;
				}
				case syncnet::GameMessages::GameMessages_Ping:
					std::cout << "ping seq : " << msg->msg_as_Ping()->seq() << std::endl;
					break;
				}




				flatbuffers::FlatBufferBuilder builder(1024);
				flatbuffers::Offset<syncnet::AgentInfo> agent_info;
				std::vector<flatbuffers::Offset<syncnet::AgentInfo>> agent_info_vector;
				for (int i = 0; i < room_.world()->map()->crowd()->getAgentCount(); ++i)
				{
					const dtCrowdAgent* agent = room_.world()->map()->crowd()->getAgent(i);
					if (agent->active == false)
						continue;

					syncnet::Vec3 pos(agent->npos[0] * -1, agent->npos[1], agent->npos[2]);
					std::cout << "agent " << agent->active << " pos (" << pos.x() << "," << pos.y() << "," << pos.z() << ")" << std::endl;

					agent_info = syncnet::CreateAgentInfo(builder, i, &pos);
					agent_info_vector.push_back(agent_info);
				}
				auto agents = builder.CreateVector(agent_info_vector);
				auto getAgents = syncnet::CreateGetAgents(builder, agents);

				//auto agent = room_.world()->map()->crowd()->getAgent(0);
				//if (agent != nullptr)
				//{
				//	syncnet::Vec3 pos(agent->npos[0] * -1, agent->npos[1], agent->npos[2]);
				//	std::cout << "agent "<< agent->active << " pos (" << pos.x() << "," << pos.y() << "," << pos.z() << ")" << std::endl;

				//	agent_info = syncnet::CreateAgentInfo(builder, 1, &pos);
				//}
				//else
				//{
				//	syncnet::Vec3 pos(0, 0, 0);
				//	agent_info = syncnet::CreateAgentInfo(builder, 1, &pos);
				//}

				auto send_msg = syncnet::CreateGameMessage(builder, syncnet::GameMessages::GameMessages_GetAgents, getAgents.Union());
				builder.Finish(send_msg);

				memcpy(send_msg_.body(), builder.GetBufferPointer(), builder.GetSize());
				send_msg_.body_length(builder.GetSize());
				send_msg_.encode_header();
				send(send_msg_);

				//read_msg_.body()[read_msg_.body_length()] = 0;
				//std::cout << read_msg_.body() << std::endl;




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
