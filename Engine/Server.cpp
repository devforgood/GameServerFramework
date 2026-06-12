#include "Server.h"
#include "World.h"
#include "flatbuffers/flatbuffers.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"
#include "LogHelper.h"
#include "Player.h"
#include "SqlClient.h"
#include "SqlClientManager.h"
#include "PlayerController.h"
#include "Common.h"

GameChannel::GameChannel()
{
	world_ = new World();
	world_->Init();
}

void GameChannel::Join(GameParticipantPtr participant)
{
	participants_.insert(participant);
	world_->join(participant->GetPlayer());
}

void GameChannel::Leave(GameParticipantPtr participant)
{
	world_->leave(participant->GetPlayer());
	participants_.erase(participant);
}

void GameChannel::UpdatePlayers()
{
	for (auto& participant : participants_)
	{
		auto player = participant->GetPlayer();
		if (player)
		{
			player->Update(1.0f);
		}
	}
}


//----------------------------------------------------------------------

GameSession::GameSession(tcp::socket socket, GameChannel& room, boost::asio::thread_pool& db_thread_pool, GameServer * server)
	: socket_(std::move(socket)),
	room_(room),
	strand_(db_thread_pool.get_executor()),
	server_(server)
{
	player_ = nullptr;
	playerController_ = new PlayerController();
	ringBuf_ = new RingBuffer(8192); // 8192 bytes
}

GameSession::~GameSession()
{
	if (playerController_ != nullptr)
	{
		delete playerController_;
		playerController_ = nullptr;
	}
	if (ringBuf_ != nullptr)
	{
		delete ringBuf_;
		ringBuf_ = nullptr;
	}
}

void GameSession::Start()
{
	SetPlayer(std::make_shared<Player>());

	room_.Join(shared_from_this());
	DoRead();

}

void GameSession::SetPlayer(std::shared_ptr<Player> player)
{
	player_ = player;
	player_->SetSession(shared_from_this());
	player_->SetServer(server_);
	playerController_->player_ = player_;
	playerController_->world_ = room_.GetWorld();
}

void GameSession::Send(std::shared_ptr<send_message>& msg)
{
	bool write_in_progress = !writeMsgs_.empty();
	writeMsgs_.push_back(msg);
	if (!write_in_progress)
	{
		DoWrite();
	}
}

void GameSession::Close()
{
	boost::system::error_code ignored_ec;
	socket_.close(ignored_ec);
	room_.Leave(shared_from_this());
}

void GameSession::DoRead() {
	size_t space = 0;
	char* write_ptr = ringBuf_->write_ptr(space);
	if (!write_ptr || space == 0) return;

	socket_.async_read_some(boost::asio::buffer(write_ptr, space),
		[this](boost::system::error_code ec, std::size_t bytes_transferred) {
			if (!ec) {
				ringBuf_->commit_write(bytes_transferred);
				ProcessPackets();
				DoRead();
			}
		});
}

void GameSession::ProcessPackets() {
	while (true) {
		if (ringBuf_->size() < GameMessage::header_length)
			return;

		const char* hdr_ptr = nullptr;
		uint16_t body_len = 0;

		if (ringBuf_->peek_ptr(hdr_ptr, GameMessage::header_length)) {
			std::memcpy(&body_len, hdr_ptr, GameMessage::header_length);
		}
		else {
			char tmp[GameMessage::header_length];
			if (!ringBuf_->peek(tmp, GameMessage::header_length)) return;
			std::memcpy(&body_len, tmp, GameMessage::header_length);
		}

		if (ringBuf_->size() < GameMessage::header_length + body_len)
			return;

		ringBuf_->read(nullptr, GameMessage::header_length);  // consume header

		auto body_view = ringBuf_->try_peek_span_cpp20(body_len);
		if (body_view) {
			HandlePacket(*body_view);
			ringBuf_->read(nullptr, body_len);  // consume body
		}
		else {
			// fallback
			std::vector<char> body(body_len);
			ringBuf_->read(body.data(), body_len);
			HandlePacket(std::span<const char>(body));
		}
	}
}

void GameSession::HandlePacket(std::span<const char> data) {
	const uint8_t* udata = reinterpret_cast<const uint8_t*>(data.data());
	playerController_->handle(syncnet::GetGameMessage(udata));
}


void GameSession::DoReadHeader()
{
	auto self(shared_from_this());
	boost::asio::async_read(socket_,
		boost::asio::buffer(readMsg_.data(), GameMessage::header_length),
		[this, self](boost::system::error_code ec, std::size_t /*length*/)
		{
			//std::cout << "recv header" << std::endl;

			if (!ec && readMsg_.decode_header())
			{
				DoReadBody();
			}
			else
			{
				room_.Leave(shared_from_this());
			}
		});
}

void GameSession::DoReadBody()
{
	auto self(shared_from_this());
	boost::asio::async_read(socket_,
		boost::asio::buffer(readMsg_.body(), readMsg_.body_length()),
		[this, self](boost::system::error_code ec, std::size_t /*length*/)
		{
			if (!ec)
			{
				//room_.deliver(readMsg_);

				playerController_->handle(syncnet::GetGameMessage(readMsg_.body()));
				//std::cout << "recv message type : " << msg->msg_type() << std::endl;

				DoReadHeader();
			}
			else
			{
				room_.Leave(shared_from_this());
			}
		});
}


void GameSession::DoWrite()
{
	auto self(shared_from_this());
	boost::asio::async_write(socket_, writeMsgs_.front()->to_buffers(),
		[this, self](boost::system::error_code ec, std::size_t /*length*/)
		{
			if (!ec)
			{
				writeMsgs_.pop_front();
				if (!writeMsgs_.empty())
				{
					DoWrite();
				}
			}
			else
			{
				room_.Leave(shared_from_this());
			}
		});
}

//----------------------------------------------------------------------


GameServer::GameServer(std::shared_ptr<boost::asio::io_context> io_context, const tcp::endpoint& endpoint)
	: acceptor_(*io_context, endpoint)
	, ioContext_(io_context)
	, dbThreadPool_(DB_THREAD_POOL_SIZE)
{
	InitializeDbThreadPool();
	DoAccept();

	timeAcc = 0.0f;
	playerUpdateAcc_ = 0.0f;
}


std::atomic<int> initialized_threads(0); // 초기화된 스레드 수 추적

void GameServer::InitializeDbThreadPool()
{
	std::cout << "Initializing DB thread pool..." 
		<< " on Thread " << std::this_thread::get_id() << std::endl;


	// 각 스레드에서 초기화 작업 수행
	for (int i = 0; i < DB_THREAD_POOL_SIZE; ++i) {
		boost::asio::post(dbThreadPool_, [i]() {
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

void GameServer::DoAccept()
{
	LOG.info("Game Server Ready");

	acceptor_.async_accept(
		[this](boost::system::error_code ec, tcp::socket socket)
		{
			if (!ec)
			{
				std::cout << "connected" << std::endl;

				std::make_shared<GameSession>(std::move(socket), channel_, dbThreadPool_, this)->Start();
			}

			DoAccept();
		});
}

void GameServer::UpdateGameLogic(float delta)
{
	//std::cout << "tick " << delta << std::endl;

	// Update sample simulation.
	const float SIM_RATE = 10;
	const float DELTA_TIME = 1.0f / SIM_RATE;
	timeAcc = rcClamp(timeAcc + delta, -1.0f, 1.0f);
	int simIter = 0;
	while (timeAcc > DELTA_TIME)
	{
		timeAcc -= DELTA_TIME;
		if (simIter < 5)
		{
			channel_.GetWorld()->update(DELTA_TIME);
		}
		simIter++;
	}

	// player update (every 1 second)
	playerUpdateAcc_ += delta;
	if (playerUpdateAcc_ >= 1.0f)
	{
		channel_.UpdatePlayers();
		playerUpdateAcc_ -= 1.0f;
	}
}

bool ServerManager::Initialize(std::list<tcp::endpoint>& endpoints)
{
	try
	{
		ioContext_ = std::make_shared<boost::asio::io_context>();

		for (std::list<tcp::endpoint>::iterator it = endpoints.begin();
			it != endpoints.end(); ++it)
		{
			auto server = std::make_shared<GameServer>(ioContext_, *it);
			servers_.push_back(server);
		}

		ResourceLoader::Instance().LoadResources();
		return true;
	}
	catch (std::exception& e)
	{
		std::cerr << "Failed to initialize server: " << e.what() << std::endl;
		return false;
	}
}

void ServerManager::Tick(float delta)
{
	for (auto& server : servers_)
	{
		server->UpdateGameLogic(delta);
	}
}

void ServerManager::Run()
{
	bool running = true;
	const std::chrono::milliseconds targetInterval(16);
	TimeVal lastTime = getPerfTime();

	while (running)
	{
		auto frameStart = std::chrono::steady_clock::now();

		ioContext_->poll();

		TimeVal curTime = getPerfTime();
		float delta = getPerfTimeUsec(curTime - lastTime) / 1000000.0f;
		lastTime = curTime;
		Tick(delta);

		auto frameEnd = std::chrono::steady_clock::now();
		auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(frameEnd - frameStart);
		auto sleepTime = targetInterval - elapsed;

		if (sleepTime > std::chrono::milliseconds(0))
		{
			std::this_thread::sleep_for(sleepTime);
		}
	}
}


