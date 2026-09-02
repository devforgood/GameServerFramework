#include "Server.h"

#include <cassert>
#include <functional>
#include "World.h"
#include "flatbuffers/flatbuffers.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"
#include "Recast.h"
#include "LogHelper.h"
#include "Player.h"
#include "SqlClient.h"
#include "SqlClientManager.h"
#include "PlayerController.h"
#include "Common.h"
#include "DbMonitorTicker.h"
#include "ServerConfig.h"
#include "IAuthenticator.h"
#include "MessagePolicy.h"

GameChannel::GameChannel()
{
	world_ = new World();
	world_->Init();
	// 맵 데이터(Map.json monster_spawn)에 배치된 몬스터를 스폰한다.
	// Init 과 분리해 벤치마크/테스트에서는 자동 스폰되지 않는다.
	world_->SpawnMapMonsters();
}

void GameChannel::Join(GameParticipantPtr participant)
{
	participants_.insert(participant);
	world_->join(participant->GetPlayer());
}

void GameChannel::Leave(GameParticipantPtr participant)
{
	// 세션이 끊겨도 캐릭터를 즉시 제거하지 않는다. 로그인한 플레이어면 유예 시간 동안
	// 캐릭터를 유지해 같은 userId 로 재접속 시 넘겨받을 수 있게 한다(핸드오버).
	world_->BeginDisconnect(participant->GetPlayer());
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

	const NetworkConfig& netCfg = ServerConfig::Instance().Network();
	packetBucket_.Configure(netCfg.max_packets_per_second, netCfg.packet_burst,
		std::chrono::steady_clock::now());

	if (server_ != nullptr)
		server_->OnSessionOpened();
}

GameSession::~GameSession()
{
	if (server_ != nullptr)
		server_->OnSessionClosed();

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

bool GameSession::ConsumePacketToken()
{
	return packetBucket_.Consume(std::chrono::steady_clock::now());
}

void GameSession::Disconnect(const char* reason)
{
	if (closed_)
		return;
	closed_ = true;

	LOG.warn("session disconnected: {}", reason);

	boost::system::error_code ignored;
	socket_.shutdown(tcp::socket::shutdown_both, ignored);
	socket_.close(ignored);
	room_.Leave(shared_from_this());
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
	if (closed_)
		return;

	// 백프레셔: 수신을 멈춘 클라이언트가 있으면 async_write 가 완료되지 않아 큐가 계속 쌓인다.
	// 상한이 없으면 그 세션 하나가 서버 메모리를 끝까지 끌어올린다(브로드캐스트가 매 틱 밀어넣는다).
	// 따라가지 못하는 연결은 살려둘 가치가 없으므로 끊는다.
	const int limit = ServerConfig::Instance().Network().max_send_queue;
	if (limit > 0 && static_cast<int>(writeMsgs_.size()) >= limit)
	{
		Disconnect("send queue overflow (client not reading)");
		return;
	}

	bool write_in_progress = !writeMsgs_.empty();
	writeMsgs_.push_back(msg);
	if (!write_in_progress)
	{
		DoWrite();
	}
}

void GameSession::Close()
{
	if (closed_)
		return;
	closed_ = true;

	boost::system::error_code ignored_ec;
	socket_.close(ignored_ec);
	room_.Leave(shared_from_this());
}

void GameSession::DoRead() {
	if (closed_)
		return;

	// 한 프레임의 최대 크기 = 길이 헤더 + 본문 상한(message_policy::IsValidBodyLength).
	constexpr size_t kMaxFrameLength = GameMessage::header_length + GameMessage::max_body_length;

	// 최대 크기 패킷 하나가 통째로 들어갈 연속 공간을 데이터 뒤에 확보한 뒤에 읽는다.
	// 이 read 는 그 영역 안에서만 끝나므로 어떤 패킷도 버퍼 끝에서 쪼개지지 않고,
	// 덕분에 ProcessPackets 는 헤더든 본문이든 항상 연속 스팬으로 집어낼 수 있다.
	// 옮기는 시점도 여기가 가장 싸다 - ProcessPackets 가 완성된 패킷을 모두 소비한
	// 직후라 남은 건 미완성 패킷 하나뿐이고, 대개는 0바이트다.
	// 진행 중인 async_read 도, 살아있는 body 스팬도 없으므로 내부 포인터를 옮겨도 안전하다.
	if (!ringBuf_->reserve_linear_write(kMaxFrameLength)) {
		// 도달 불가 - 남는 건 미완성 패킷 하나(< kMaxFrameLength)뿐이라 자리는 항상 난다.
		// 그래도 걸린다면 버퍼 상태가 깨진 것이므로, 조용히 멈추지 말고 끊는다.
		Disconnect("receive buffer exhausted");
		return;
	}

	size_t space = 0;
	char* write_ptr = ringBuf_->write_ptr(space);
	if (!write_ptr || space == 0) return;

	// 핸들러 앞에 shared_from_this() 를 묶어 세션 수명을 잡는다(기존 self 캡처와 동일).
	socket_.async_read_some(boost::asio::buffer(write_ptr, space),
		std::bind_front(&GameSession::OnRead, shared_from_this()));
}

void GameSession::OnRead(const boost::system::error_code& ec, std::size_t bytesTransferred)
{
	if (ec) {
		// 클라 종료/연결 끊김. 세션과 플레이어(현재 맵의 액터/브로드캐스트 목록)를
		// 즉시 정리한다. 이 분기가 없으면 read 는 멈추지만 정리가 안 돼, 다음 브로드
		// 캐스트 write 실패 시점까지 세션이 남아 있게 된다.
		HandleIoFailure();
		return;
	}

	ringBuf_->commit_write(bytesTransferred);
	ProcessPackets();
	DoRead();
}

void GameSession::HandleIoFailure()
{
	if (closed_)
		return;

	closed_ = true;
	room_.Leave(shared_from_this());
}

void GameSession::ProcessPackets() {
	while (!closed_) {
		if (ringBuf_->size() < GameMessage::header_length)
			return;

		// 헤더가 링 경계에 걸치는 경우까지 RingBuffer 안에서 처리한다.
		static_assert(GameMessage::header_length == sizeof(uint16_t));
		const auto body_len_opt = ringBuf_->peek_value<uint16_t>();
		if (!body_len_opt) return;
		const uint16_t body_len = *body_len_opt;

		// 길이를 먼저 검사한다(자세한 이유는 message_policy::IsValidBodyLength).
		// 정상 클라이언트는 절대 보내지 않는 값이므로 프로토콜 위반으로 끊는다.
		if (!message_policy::IsValidBodyLength(body_len)) {
			Disconnect("invalid packet length");
			return;
		}

		if (ringBuf_->size() < GameMessage::header_length + body_len)
			return;

		// 레이트리밋은 파싱 전에 건다(파싱 비용 자체가 공격 표면이다).
		if (!ConsumePacketToken()) {
			Disconnect("packet rate limit exceeded");
			return;
		}

		ringBuf_->read(nullptr, GameMessage::header_length);  // consume header

		// DoRead 가 매 읽기 전에 선형 공간을 확보하므로 본문은 언제나 연속이다.
		const auto body_view = ringBuf_->try_peek_span_cpp20(body_len);
		if (!body_view) {
			// 불변식이 깨진 경우. 잘못된 메모리를 파싱하느니 끊는다.
			assert(false && "body wrapped despite reserve_linear_write");
			Disconnect("internal buffer state");
			return;
		}

		HandlePacket(*body_view);
		ringBuf_->read(nullptr, body_len);  // consume body
	}
}

void GameSession::HandlePacket(std::span<const char> data) {
	const uint8_t* udata = reinterpret_cast<const uint8_t*>(data.data());

	// FlatBuffers 는 신뢰할 수 없는 입력을 검증 없이 읽으면 버퍼 밖을 참조한다.
	// (오프셋/길이가 전부 버퍼 안의 값이므로 조작된 패킷 하나로 임의 주소를 읽게 만들 수 있다.)
	// Verifier 는 모든 오프셋·문자열·벡터가 버퍼 경계 안인지 확인한다.
	flatbuffers::Verifier verifier(udata, data.size());
	if (!syncnet::VerifyGameMessageBuffer(verifier)) {
		Disconnect("malformed flatbuffer");
		return;
	}

	const syncnet::GameMessage* msg = syncnet::GetGameMessage(udata);
	if (msg == nullptr) {
		Disconnect("empty game message");
		return;
	}

	playerController_->handle(msg);
}

void GameSession::DoWrite()
{
	if (closed_)
		return;

	boost::asio::async_write(socket_, writeMsgs_.front()->to_buffers(),
		std::bind_front(&GameSession::OnWrite, shared_from_this()));
}

void GameSession::OnWrite(const boost::system::error_code& ec, std::size_t /*length*/)
{
	if (ec)
	{
		HandleIoFailure();
		return;
	}

	writeMsgs_.pop_front();
	if (!writeMsgs_.empty())
	{
		DoWrite();
	}
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

// DB 스레드 풀의 각 스레드에서 한 번씩 도는 초기화 작업.
// 스레드마다 SqlClient 를 초기화하고, 모든 스레드가 끝날 때까지 서로 기다린다.
static void InitializeDbThread(int index, int poolSize)
{
	static thread_local bool initialized = false;
	if (initialized)
		return;

	LOG.info("DB thread pool initialized on thread: {}, thread ID {}", index, std::hash<std::thread::id>{}(std::this_thread::get_id()));

	// 스레드별 초기화 작업
	SqlClientManager::getInstance().init();
	initialized = true;

	// 초기화 완료를 semaphore로 알림
	initialized_threads.fetch_add(1, std::memory_order_relaxed);
	while (initialized_threads.load(std::memory_order_relaxed) < poolSize)
	{
		LOG.info("Waiting for other threads to initialize... {}", index);
		std::this_thread::yield(); // 다른 스레드가 초기화 완료를 기다리도록 함
	}
	LOG.info("threads initialized. Proceeding with DB operations. {}", index);
}

void GameServer::InitializeDbThreadPool()
{
	std::cout << "Initializing DB thread pool..." 
		<< " on Thread " << std::this_thread::get_id() << std::endl;


	// 각 스레드에서 초기화 작업 수행
	for (int i = 0; i < DB_THREAD_POOL_SIZE; ++i) {
		boost::asio::post(dbThreadPool_, std::bind_front(InitializeDbThread, i, DB_THREAD_POOL_SIZE));
	}
}

void GameServer::DoAccept()
{
	if (!acceptingConnections_)
		return;

	LOG.info("Game Server Ready");

	// 핸들러는 멤버 함수로 뺀다. bind_front 는 인자를 완전 전달하므로
	// 이동만 되는 tcp::socket 도 그대로 넘어간다.
	acceptor_.async_accept(std::bind_front(&GameServer::OnAccept, this));
}

void GameServer::OnAccept(const boost::system::error_code& ec, tcp::socket socket)
{
	if (!ec)
	{
		// 동시 접속 상한. 세션을 만들기 전에 끊는다 — 세션을 만들고 나서 끊으면
		// 상한을 넘긴 만큼의 Player/World join/leave 비용을 그대로 지불한다.
		const int limit = ServerConfig::Instance().Network().max_connections;
		if (limit > 0 && sessionCount_ >= limit)
		{
			LOG.warn("연결 거부: 동시 접속 상한 {} 도달", limit);
			boost::system::error_code ignored;
			socket.shutdown(tcp::socket::shutdown_both, ignored);
			socket.close(ignored);
		}
		else
		{
			std::make_shared<GameSession>(std::move(socket), channel_, dbThreadPool_, this)->Start();
		}
	}

	// 다음 연결을 계속 기다린다.
	DoAccept();
}

void GameServer::BeginShutdown()
{
	acceptingConnections_ = false;

	boost::system::error_code ignored;
	acceptor_.close(ignored);

	// 접속 중인 플레이어를 전부 저장한다. 예전에는 60초 주기 저장뿐이라
	// 재시작할 때마다 최대 60초치 진행이 사라졌다.
	World* world = channel_.GetWorld();
	if (world != nullptr)
	{
		const int saved = world->SaveAllPlayers();
		LOG.info("shutdown: {} 명의 플레이어 저장 요청", saved);
	}
}

int GameServer::UpdateGameLogic(float delta)
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

	return simIter;
}

int ServerManager::ResolveWorkerCount(int serverCount, int requestedThreadCount)
{
	// 각 io_context(=스레드)는 자신에게 할당된 서버만 처리하므로,
	// 서버가 없는 스레드는 의미가 없다. 따라서 워커 수는 서버(포트)
	// 개수를 넘지 않으며 최소 1개는 유지한다.
	int workerCount = requestedThreadCount;
	if (workerCount < 1) workerCount = 1;
	if (serverCount > 0 && workerCount > serverCount) workerCount = serverCount;
	return workerCount;
}

std::vector<int> ServerManager::BuildShardingPlan(int serverCount, int workerCount)
{
	std::vector<int> plan;
	if (serverCount <= 0 || workerCount <= 0) return plan;

	// 서버(포트)를 워커들에 라운드로빈으로 분배한다. 한 서버는 정확히
	// 하나의 io_context/스레드에 묶이므로 그 서버의 World/세션 상태는
	// 항상 같은 스레드에서만 접근되어 락 없이 안전하다.
	plan.reserve(serverCount);
	for (int i = 0; i < serverCount; ++i)
	{
		plan.push_back(i % workerCount);
	}
	return plan;
}

bool ServerManager::Initialize(std::list<tcp::endpoint>& endpoints)
{
	try
	{
		// 인증기는 설정(auth.mode)을 읽어 고른다. ServerConfig::Load 는 main 에서 이미 끝났다.
		AuthService::Instance().InitFromConfig();

		ResourceLoader::Instance().LoadResources();

		// 설정된 스레드 수를 서버 개수에 맞춰 보정하고, 서버→워커 배정 계획을 세운다.
		int serverCount = static_cast<int>(endpoints.size());
		int workerCount = ResolveWorkerCount(serverCount, threadCount_);
		std::vector<int> plan = BuildShardingPlan(serverCount, workerCount);

		workers_.resize(workerCount);
		for (auto& worker : workers_)
		{
			worker.ioContext = std::make_shared<boost::asio::io_context>();
		}

		// 배정 계획에 따라 서버를 해당 워커의 io_context에 묶는다.
		int idx = 0;
		for (std::list<tcp::endpoint>::iterator it = endpoints.begin();
			it != endpoints.end(); ++it, ++idx)
		{
			auto& worker = workers_[plan[idx]];
			auto server = std::make_shared<GameServer>(worker.ioContext, *it);
			worker.servers.push_back(server);
		}

		LOG.info("ServerManager initialized: {} server(s) over {} IO thread(s)",
			serverCount, workerCount);

		return true;
	}
	catch (std::exception& e)
	{
		std::cerr << "Failed to initialize server: " << e.what() << std::endl;
		return false;
	}
}

// 시그널 전용 io_context 를 돌리는 스레드 진입점.
// io_context::run 은 오버로드가 있어 함수 포인터로 바로 넘기기 어렵기 때문에 감싼다.
static void RunSignalContext(boost::asio::io_context* context)
{
	context->run();
}

void ServerManager::OnSignal(const boost::system::error_code& ec, int signalNumber)
{
	if (ec)
		return;

	LOG.info("signal {} 수신 — 정상 종료를 시작한다", signalNumber);
	RequestShutdown();
}

void ServerManager::Run()
{
	if (workers_.empty())
	{
		return;
	}

	// SIGINT(Ctrl+C) / SIGTERM 을 받으면 루프를 멈추게 한다.
	// 핸들러는 별도 io_context 에서 돌린다 — 워커의 io_context 는 poll() 로만 돌아가서
	// 게임 로직이 길어지면 시그널 처리가 늦어진다.
	boost::asio::io_context signalContext;
	boost::asio::signal_set signals(signalContext, SIGINT, SIGTERM);
	signals.async_wait(std::bind_front(&ServerManager::OnSignal, this));
	std::thread signalThread(RunSignalContext, &signalContext);

	// DB 스레드 풀 감시/리포트는 전역 공유 자원을 건드리므로 primary 워커에서만 돈다.
	// 주기 계산은 DbMonitorTicker 안에 있고, 여기서는 프레임마다 Tick 만 호출한다.
	workers_[0].dbMonitor = std::make_shared<DbMonitorTicker>(std::chrono::steady_clock::now());

	// 워커가 1개면 추가 스레드 없이 현재 스레드에서 단일 실행한다(기존 동작).
	// 워커가 N개면 추가로 N-1개의 스레드를 띄우고, 첫 워커는 현재 스레드에서 돈다.
	std::vector<std::thread> threads;
	threads.reserve(workers_.size() - 1);
	for (size_t i = 1; i < workers_.size(); ++i)
	{
		// workers_ 는 Run 동안 크기가 변하지 않으므로 원소 참조를 넘겨도 안전하다.
		threads.emplace_back(&ServerManager::RunWorker, this, std::ref(workers_[i]));
	}

	// 첫 워커는 추가 스레드 없이 현재 스레드에서 돈다. 이 호출이 돌아오면 종료 절차에 들어간다.
	RunWorker(workers_[0]);

	for (auto& t : threads)
	{
		if (t.joinable())
		{
			t.join();
		}
	}

	signalContext.stop();
	if (signalThread.joinable())
		signalThread.join();

	LOG.info("모든 워커 종료 완료");
}

void ServerManager::ShutdownWorker(IoWorker& worker)
{
	// 1) 새 연결을 막고, 접속 중인 플레이어의 저장을 요청한다(DB 스레드로 나간다).
	worker.BeginShutdown();

	// 2) 저장 작업이 DB 스레드에 도달하고 완료 콜백이 io_context 로 돌아올 시간을 준다.
	//    저장은 fire-and-forget 이라 완료를 기다릴 핸들이 없으므로, 남은 핸들러를 흘려보내며
	//    짧게 폴링한다. 무한정 기다리지 않도록 상한(3초)을 둔다.
	const auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(3);
	while (std::chrono::steady_clock::now() < deadline)
	{
		if (worker.PollIo() == 0)
			std::this_thread::sleep_for(std::chrono::milliseconds(20));
	}
}

namespace
{
	double MillisBetween(std::chrono::steady_clock::time_point from,
		std::chrono::steady_clock::time_point to)
	{
		return std::chrono::duration<double, std::milli>(to - from).count();
	}

	//-------------------------------------------------------------------------------------
	// 워커 한 스레드의 프레임 시간을 모아 주기적으로 한 줄로 남긴다.
	//
	// 부하 테스트에서 "동시 접속을 몇 명까지 받을 수 있는가" 는 결국 "월드 틱이 어디서부터
	// 밀리는가" 이고, 그것은 서버 안에서만 보인다. 클라이언트가 재는 왕복 지연에는 네트워크와
	// 클라 틱이 섞여 있어 원인을 가리지 못한다.
	//
	// 프레임 루프는 16ms 주기이고 월드 시뮬레이션은 그 안에서 10Hz(100ms)로 돈다.
	// 그래서 예산이 둘이라 따로 센다.
	//   frame : 프레임 하나(IO 폴링 + 게임 로직)가 16ms 를 넘겼는가
	//   sim   : 월드를 실제로 돌린 프레임의 로직 시간이 100ms 예산을 넘겼는가
	//   late  : 한 프레임에서 10Hz 틱을 두 번 이상 돌려야 했던 횟수(이미 밀린 상태다)
	//   drop  : 따라잡기 상한(5틱)을 넘겨 시뮬레이션을 버린 횟수
	//
	// 통계는 워커 스레드의 지역 변수다 — 측정이 스레드 사이에 새 공유 상태를 만들지 않는다.
	//-------------------------------------------------------------------------------------
	class WorkerTickStats
	{
	public:
		explicit WorkerTickStats(std::chrono::steady_clock::time_point now)
			: windowStart_(now)
		{
		}

		void Add(double frameMs, double logicMs, int simTicks)
		{
			++frames_;
			frameMsSum_ += frameMs;
			if (frameMs > frameMsMax_) frameMsMax_ = frameMs;
			if (frameMs > kFrameBudgetMs) ++frameOver_;

			if (simTicks <= 0)
				return;

			++simFrames_;
			simTicks_ += simTicks;
			simMsSum_ += logicMs;
			if (logicMs > simMsMax_) simMsMax_ = logicMs;
			if (logicMs > kSimBudgetMs) ++simOver_;
			if (simTicks > 1) ++late_;
			if (simTicks > kMaxCatchUpTicks) ++drop_;
		}

		// 주기가 지났으면 한 줄 남기고 창을 비운다. 세션이 하나도 없고 예산을 넘긴 적도
		// 없으면 남기지 않는다(아무도 안 붙은 서버가 로그를 채우지 않도록).
		void MaybeReport(int workerIndex, size_t serverCount, int sessionCount,
			std::chrono::steady_clock::time_point now)
		{
			if (now - windowStart_ < kReportInterval)
				return;

			const double windowSec = std::chrono::duration<double>(now - windowStart_).count();
			if (frames_ > 0 && (sessionCount > 0 || frameOver_ > 0 || simOver_ > 0))
			{
				LOG.info("[tick] worker={} servers={} sess={} | frame avg {:.2f} max {:.2f} ms "
					"over16 {}/{} ({:.0f} fps) | sim avg {:.2f} max {:.2f} ms over100 {}/{} "
					"late {} drop {}",
					workerIndex, serverCount, sessionCount,
					frameMsSum_ / frames_, frameMsMax_, frameOver_, frames_,
					frames_ / windowSec,
					simFrames_ > 0 ? simMsSum_ / simFrames_ : 0.0, simMsMax_,
					simOver_, simFrames_, late_, drop_);
			}

			windowStart_ = now;
			frames_ = 0; frameMsSum_ = 0.0; frameMsMax_ = 0.0; frameOver_ = 0;
			simFrames_ = 0; simTicks_ = 0; simMsSum_ = 0.0; simMsMax_ = 0.0; simOver_ = 0;
			late_ = 0; drop_ = 0;
		}

	private:
		static constexpr double kFrameBudgetMs = 16.0;   // RunWorker 의 목표 프레임 간격
		static constexpr double kSimBudgetMs = 100.0;    // 10Hz 시뮬레이션 한 틱의 예산
		static constexpr int kMaxCatchUpTicks = 5;       // UpdateGameLogic 의 따라잡기 상한
		static constexpr std::chrono::seconds kReportInterval{ 5 };

		std::chrono::steady_clock::time_point windowStart_;

		int frames_ = 0;
		double frameMsSum_ = 0.0;
		double frameMsMax_ = 0.0;
		int frameOver_ = 0;

		int simFrames_ = 0;
		int simTicks_ = 0;
		double simMsSum_ = 0.0;
		double simMsMax_ = 0.0;
		int simOver_ = 0;
		int late_ = 0;
		int drop_ = 0;
	};
}

void ServerManager::RunWorker(IoWorker& worker)
{
	bool running = true;
	const std::chrono::milliseconds targetInterval(16);
	(void)running; // 종료 조건은 shutdownRequested_ 가 정한다
	TimeVal lastTime = getPerfTime();

	// workers_ 는 Run 동안 크기가 변하지 않으므로 원소 주소로 워커 번호를 구할 수 있다.
	const int workerIndex = static_cast<int>(&worker - workers_.data());
	WorkerTickStats stats(std::chrono::steady_clock::now());

	while (!shutdownRequested_.load(std::memory_order_relaxed))
	{
		auto frameStart = std::chrono::steady_clock::now();

		worker.PollIo();

		TimeVal curTime = getPerfTime();
		float delta = getPerfTimeUsec(curTime - lastTime) / 1000000.0f;
		lastTime = curTime;

		auto logicStart = std::chrono::steady_clock::now();
		const int simTicks = worker.UpdateGameLogic(delta);
		auto logicEnd = std::chrono::steady_clock::now();

		worker.TickDbMonitor(frameStart);

		auto frameEnd = std::chrono::steady_clock::now();

		stats.Add(MillisBetween(frameStart, frameEnd), MillisBetween(logicStart, logicEnd), simTicks);
		stats.MaybeReport(workerIndex, worker.servers.size(), worker.SessionCount(), frameEnd);

		auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(frameEnd - frameStart);
		auto sleepTime = targetInterval - elapsed;

		if (sleepTime > std::chrono::milliseconds(0))
		{
			std::this_thread::sleep_for(sleepTime);
		}
	}

	ShutdownWorker(worker);
}


