#include "Server.h"
#include "World.h"
#include "flatbuffers/flatbuffers.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"
#include "LogHelper.h"
#include "Player.h"
#include "SqlClient.h"
#include "SqlClientManager.h"

game_room::game_room()
{
	world_ = new World();
	world_->Init();
}

void game_room::join(game_participant_ptr participant)
{
	participants_.insert(participant);
	//for (auto msg : recent_msgs_)
	//	participant->deliver(msg);
}

void game_room::leave(game_participant_ptr participant)
{
	participants_.erase(participant);
}

void game_room::deliver(std::shared_ptr<send_message> msg)
{
	recent_msgs_.push_back(msg);
	while (recent_msgs_.size() > max_recent_msgs)
		recent_msgs_.pop_front();

	for (auto participant : participants_)
		participant->send(msg);
}

//----------------------------------------------------------------------

game_session::game_session(tcp::socket socket, game_room& room, boost::asio::thread_pool& db_thread_pool, game_server * server)
	: socket_(std::move(socket)),
	room_(room),
	strand_(db_thread_pool.get_executor()),
	server_(server)
{
	dispatcher_ = nullptr;
	player_ = nullptr;
}

game_session::~game_session()
{
	if (dispatcher_ != nullptr)
	{
		delete dispatcher_;
		dispatcher_ = nullptr;
	}
}

void game_session::start()
{
	player_ = std::make_shared<Player>();
	player_->set_session(shared_from_this());
	player_->set_server(server_);
	dispatcher_ = new MessageDispatcher();
	dispatcher_->world_ = room_.world();
	dispatcher_->player_ = player_;


	room_.join(shared_from_this());
	do_read_header();

}

void game_session::send(std::shared_ptr<send_message> msg)
{
	bool write_in_progress = !write_msgs_.empty();
	write_msgs_.push_back(msg);
	if (!write_in_progress)
	{
		do_write();
	}
}


void game_session::do_read_header()
{
	auto self(shared_from_this());
	boost::asio::async_read(socket_,
		boost::asio::buffer(read_msg_.data(), game_message::header_length),
		[this, self](boost::system::error_code ec, std::size_t /*length*/)
		{
			//std::cout << "recv header" << std::endl;

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

void game_session::do_read_body()
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
				//std::cout << "recv message type : " << msg->msg_type() << std::endl;

				switch (msg->msg_type())
				{
				case syncnet::GameMessages::GameMessages_AddAgent:			dispatcher_->dispatch(msg->msg_as_AddAgent()); break;
				case syncnet::GameMessages::GameMessages_RemoveAgent:		dispatcher_->dispatch(msg->msg_as_RemoveAgent()); break;
				case syncnet::GameMessages::GameMessages_SetMoveTarget:		dispatcher_->dispatch(msg->msg_as_SetMoveTarget()); break;
				case syncnet::GameMessages::GameMessages_Ping:				dispatcher_->dispatch(msg->msg_as_Ping()); break;
				case syncnet::GameMessages::GameMessages_SetRaycast:		dispatcher_->dispatch(msg->msg_as_SetRaycast()); break;
				case syncnet::GameMessages::GameMessages_Login:				dispatcher_->dispatch(msg->msg_as_Login()); break;
				}

				do_read_header();
			}
			else
			{
				room_.leave(shared_from_this());
			}
		});
}


void game_session::do_write()
{
	auto self(shared_from_this());
	boost::asio::async_write(socket_, write_msgs_.front()->to_buffers(),
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


game_server::game_server(std::shared_ptr<boost::asio::io_context> io_context, const tcp::endpoint& endpoint)
	: acceptor_(*io_context, endpoint)
	, timer_(*io_context, boost::posix_time::milliseconds(16)) // 60 프레임
	, io_context_(io_context)
	, db_thread_pool_(DB_THREAD_POOL_SIZE)
{
	initialize_db_thread_pool();
	do_accept();

	timeAcc = 0.0f;
	lastTime_ = getPerfTime();
	timer_.async_wait(boost::bind(&game_server::tick, this, boost::asio::placeholders::error));
}


std::atomic<int> initialized_threads(0); // 초기화된 스레드 수 추적

void game_server::initialize_db_thread_pool()
{
	std::cout << "Initializing DB thread pool..." 
		<< " on Thread " << std::this_thread::get_id() << std::endl;


	// 각 스레드에서 초기화 작업 수행
	for (int i = 0; i < DB_THREAD_POOL_SIZE; ++i) {
		boost::asio::post(db_thread_pool_, [i]() {
			static thread_local bool initialized = false;
			if (!initialized) {
				LOG.info("DB thread pool initialized on thread: {}, thread ID {}", i, std::hash<std::thread::id>{}(std::this_thread::get_id()));

				// 스레드별 초기화 작업
				SqlClientManager::getInstance().init();
				initialized = true;

				// 초기화 완료를 semaphore로 알림
				initialized_threads.fetch_add(1, std::memory_order_relaxed);
				while (initialized_threads.load(std::memory_order_relaxed) < DB_THREAD_POOL_SIZE)
				{
					LOG.info("Waiting for other threads to initialize... {}", i);
					std::this_thread::yield(); // 다른 스레드가 초기화 완료를 기다리도록 함
				}
				LOG.info("threads initialized. Proceeding with DB operations. {}", i);
			}
			});
	}
}

void game_server::do_accept()
{
	LOG.info("Game Server Ready");

	acceptor_.async_accept(
		[this](boost::system::error_code ec, tcp::socket socket)
		{
			if (!ec)
			{
				std::cout << "connected" << std::endl;

				std::make_shared<game_session>(std::move(socket), room_, db_thread_pool_, this)->start();
			}

			do_accept();
		});
}

void game_server::tick(const boost::system::error_code& e) 
{
	TimeVal curTime = getPerfTime();
	float dt = getPerfTimeUsec(curTime - lastTime_) / 1000000.0f;

	//std::cout << "tick " << dt << std::endl;

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
				//std::cout << "update agent " << agent->active << " pos (" << agent->npos[0] * -1 << "," << agent->npos[1] << "," << agent->npos[2] << ")" << std::endl;

			}
		}
		simIter++;
	}

	lastTime_ = curTime;
	int elapsed_time = getPerfTimeUsec(getPerfTime() - curTime) / 1000;

	// Reschedule the timer for 1 second in the future:
	timer_.expires_at(timer_.expires_at() + boost::posix_time::milliseconds(TICK_RATES - elapsed_time));
	// Posts the timer event
	timer_.async_wait(boost::bind(&game_server::tick, this, boost::asio::placeholders::error));
}

