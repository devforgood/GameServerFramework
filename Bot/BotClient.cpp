#include "BotClient.h"

#include <chrono>

#include "../BehaviorTree/BehaviorTree.h"
#include "BotBehaviorTree.h"
#include "BotLog.h"

namespace bot
{
	namespace
	{
		int64_t UnixNowMs()
		{
			return std::chrono::duration_cast<std::chrono::milliseconds>(
				std::chrono::system_clock::now().time_since_epoch()).count();
		}

		// 보낸 시각부터 지금까지를 ms 로(반올림) 돌려준다.
		uint32_t ElapsedMs(const std::chrono::steady_clock::time_point& sent_at)
		{
			const auto elapsed = std::chrono::steady_clock::now() - sent_at;
			const auto micros = std::chrono::duration_cast<std::chrono::microseconds>(elapsed).count();
			if (micros <= 0)
				return 0;
			return static_cast<uint32_t>((micros + 500) / 1000);
		}
	}

	void SendBucket::Configure(double rate_per_second, double burst)
	{
		rate_ = rate_per_second;
		capacity_ = burst;
		tokens_ = burst;
		started_ = false;
	}

	bool SendBucket::TryConsume(double now)
	{
		if (!started_)
		{
			started_ = true;
			last_refill_ = now;
		}
		else if (now > last_refill_)
		{
			tokens_ += (now - last_refill_) * rate_;
			if (tokens_ > capacity_)
				tokens_ = capacity_;
			last_refill_ = now;
		}

		if (tokens_ < 1.0)
			return false;

		tokens_ -= 1.0;
		return true;
	}

	BotClient::BotClient(boost::asio::io_context& io_context, const BotConfig& config,
		int index, std::string user_id, bool verbose)
		: config_(config)
		, index_(index)
		, user_id_(std::move(user_id))
		, verbose_(verbose)
		, session_(io_context, config.server.host, config.server.port, &stats_)
	{
		blackboard_.actions = this;
		blackboard_.ai.search_radius = config.ai.search_radius;
		blackboard_.ai.attack_range = config.ai.attack_range;
		blackboard_.ai.attack_skill_id = config.ai.attack_skill_id;
		blackboard_.ai.attack_interval_ms = config.ai.attack_interval_ms;
		blackboard_.ai.move_repath_ms = config.ai.move_repath_ms;
		blackboard_.ai.wander_radius = config.ai.wander_radius;
		blackboard_.ai.wander_interval_ms = config.ai.wander_interval_ms;
		blackboard_.ai.arrive_epsilon = config.ai.arrive_epsilon;

		// 봇마다 다른 시드. 같은 시드를 쓰면 전원이 같은 좌표로 몰려가 한 셀에만
		// 부하가 실린다(관심영역 분산을 전혀 못 재게 된다).
		blackboard_.rng.seed(static_cast<uint32_t>(0x9E3779B9u * (index + 1) + 0x1234567u));

		bucket_.Configure(config.limits.max_packets_per_second, config.limits.packet_burst);

		tree_ = CreateBotTree(&blackboard_);

		session_.SetMessageHandler([this](const syncnet::GameMessage* message) { OnMessage(message); });
		session_.SetCloseHandler([this](const std::string& reason) { OnClosed(reason); });
	}

	BotClient::~BotClient()
	{
		DestroyBotTree(tree_);
		tree_ = nullptr;
	}

	void BotClient::EnterPhase(BotPhase phase)
	{
		phase_ = phase;
	}

	void BotClient::Start()
	{
		if (shutting_down_ || phase_ == BotPhase::Connecting)
			return;

		EnterPhase(BotPhase::Connecting);
		session_.Connect([this](bool success, const std::string& error) { OnConnected(success, error); });
	}

	void BotClient::OnConnected(bool success, const std::string& error)
	{
		if (!success)
		{
			if (verbose_)
				log::Printf(LogLevel::Warn, "[%s] connect 실패: %s", user_id_.c_str(), error.c_str());

			EnterPhase(BotPhase::Disconnected);
			reconnect_at_ = now_ + config_.run.reconnect_delay_ms / 1000.0;
			return;
		}

		if (verbose_)
			log::Printf(LogLevel::Debug, "[%s] connected", user_id_.c_str());

		SendLogin();
	}

	void BotClient::OnClosed(const std::string& reason)
	{
		if (shutting_down_)
			return;

		if (verbose_)
			log::Printf(LogLevel::Warn, "[%s] 연결 종료: %s", user_id_.c_str(), reason.c_str());

		ResetForReconnect();
		EnterPhase(BotPhase::Disconnected);
		reconnect_at_ = now_ + config_.run.reconnect_delay_ms / 1000.0;
	}

	void BotClient::ResetForReconnect()
	{
		blackboard_.view.Clear();
		blackboard_.has_character = false;
		blackboard_.self_actor_id = 0;
		blackboard_.target_actor_id = 0;
		blackboard_.self_dead = false;
		blackboard_.next_attack_at = 0.0;
		blackboard_.next_move_at = 0.0;
		blackboard_.next_wander_at = 0.0;
	}

	void BotClient::SendLogin()
	{
		EnterPhase(BotPhase::LoggingIn);
		login_message_id_ = NextMessageId();
		login_sent_at_ = std::chrono::steady_clock::now();

		SendCritical(packet::Login(login_message_id_, user_id_,
			config_.bots.auth_token, reconnect_token_));
	}

	void BotClient::SendSpawn()
	{
		EnterPhase(BotPhase::Spawning);
		spawn_message_id_ = NextMessageId();
		spawn_sent_at_ = std::chrono::steady_clock::now();

		// 서버는 요청의 좌표를 믿지 않고 로그인 응답에서 정한 위치에 스폰한다.
		SendCritical(packet::AddAgent(spawn_message_id_, blackboard_.spawn_pos.ToNet()));
	}

	void BotClient::SendPing()
	{
		ping_message_id_ = NextMessageId();
		ping_sent_at_ = std::chrono::steady_clock::now();
		SendThrottled(packet::Ping(ping_message_id_, ping_message_id_));
	}

	void BotClient::SendThrottled(packet::Frame frame)
	{
		if (!session_.IsConnected())
			return;

		if (!bucket_.TryConsume(now_))
		{
			++stats_.packets_throttled;
			return;
		}

		session_.Send(std::move(frame));
	}

	void BotClient::SendCritical(packet::Frame frame)
	{
		bucket_.TryConsume(now_);
		session_.Send(std::move(frame));
	}

	void BotClient::MoveTo(const Vec3& pos)
	{
		if (!blackboard_.has_character)
			return;

		++stats_.move_sent;
		SendThrottled(packet::SetMoveTarget(blackboard_.self_actor_id, pos.ToNet()));
	}

	void BotClient::Attack(int target_actor_id, const Vec3& target_pos)
	{
		if (!blackboard_.has_character)
			return;

		++stats_.skill_sent;

		// 데미지 스킬은 pos 를 조준점으로 삼아 캐스터 → pos 방향 부채꼴로 판정한다.
		// 그래서 대상의 위치를 그대로 실어 보낸다(클라이언트와 같은 규칙).
		SendThrottled(packet::UseSkill(blackboard_.self_actor_id,
			blackboard_.ai.attack_skill_id, target_actor_id, target_pos.ToNet(), UnixNowMs()));
	}

	void BotClient::Tick(double now)
	{
		now_ = now;
		blackboard_.now = now;

		if (shutting_down_)
			return;

		if (phase_ == BotPhase::Disconnected && config_.run.reconnect && now_ >= reconnect_at_)
		{
			Start();
			return;
		}

		if (phase_ != BotPhase::Playing && phase_ != BotPhase::Dead)
			return;

		if (config_.ai.ping_interval_ms > 0 && now_ >= next_ping_at_)
		{
			next_ping_at_ = now_ + config_.ai.ping_interval_ms / 1000.0;
			SendPing();
		}

		if (tree_ != nullptr)
		{
			++blackboard_.bt_ticks;
			tree_->Tick();
		}
	}

	void BotClient::Shutdown()
	{
		shutting_down_ = true;
		session_.Close("shutdown");
		EnterPhase(BotPhase::Idle);
	}

	void BotClient::OnMessage(const syncnet::GameMessage* message)
	{
		switch (message->msg_type())
		{
		case syncnet::GameMessages::GameMessages_Login:
			HandleLoginResponse(message);
			break;
		case syncnet::GameMessages::GameMessages_AddAgent:
			HandleAddAgentResponse(message);
			break;
		case syncnet::GameMessages::GameMessages_UpdateActorNotify:
			HandleUpdateActorNotify(message);
			break;
		case syncnet::GameMessages::GameMessages_UseSkill:
			HandleUseSkill(message);
			break;
		case syncnet::GameMessages::GameMessages_Ping:
			HandlePing(message);
			break;
		case syncnet::GameMessages::GameMessages_EnterGate:
			HandleEnterGate(message);
			break;
		default:
			break;
		}
	}

	void BotClient::HandleLoginResponse(const syncnet::GameMessage* message)
	{
		if (message->id() != login_message_id_)
			return;

		stats_.login.Add(ElapsedMs(login_sent_at_));

		const syncnet::Login* login = message->msg_as_Login();
		if (message->result() != syncnet::StatusCode::StatusCode_Success || login == nullptr)
		{
			++stats_.login_failures;
			log::Printf(LogLevel::Warn, "[%s] 로그인 거부", user_id_.c_str());

			// 서버는 인증 실패 연결을 곧바로 끊는다. 재시도는 재접속 경로로 돌린다.
			EnterPhase(BotPhase::Disconnected);
			reconnect_at_ = now_ + config_.run.reconnect_delay_ms / 1000.0;
			return;
		}

		++stats_.login_success;
		map_id_ = login->mapId();

		if (const syncnet::Vec3* pos = login->pos())
		{
			blackboard_.spawn_pos = Vec3(*pos);
			blackboard_.self_pos = blackboard_.spawn_pos;
			blackboard_.wander_target = blackboard_.spawn_pos;
		}

		if (login->uuid() != nullptr)
			reconnect_token_ = login->uuid()->str();

		// actorId 가 0 이 아니면 재접속 핸드오버다. 서버가 유지하던 캐릭터를 그대로
		// 넘겨받으므로 AddAgent 를 보내지 않는다(클라이언트와 같은 규칙).
		if (login->actorId() != 0)
		{
			blackboard_.self_actor_id = login->actorId();
			blackboard_.has_character = true;
			EnterPhase(BotPhase::Playing);

			if (verbose_)
				log::Printf(LogLevel::Info, "[%s] 재접속 핸드오버 actorId=%d map=%d",
					user_id_.c_str(), login->actorId(), map_id_);
			return;
		}

		if (verbose_)
			log::Printf(LogLevel::Info, "[%s] 로그인 성공 map=%d spawn=(%.1f,%.1f,%.1f)",
				user_id_.c_str(), map_id_,
				blackboard_.spawn_pos.x, blackboard_.spawn_pos.y, blackboard_.spawn_pos.z);

		SendSpawn();
	}

	void BotClient::HandleAddAgentResponse(const syncnet::GameMessage* message)
	{
		if (message->id() != spawn_message_id_)
			return;

		stats_.spawn.Add(ElapsedMs(spawn_sent_at_));

		// actorId 0 을 실패로 보면 안 된다. 내비메시 crowd 의 첫 에이전트가 0 을 받으므로
		// 맵마다 한 명은 정상적으로 0 을 배정받는다(150봇 부하 테스트에서 실제로 걸렸다).
		const syncnet::AddAgent* add_agent = message->msg_as_AddAgent();
		if (message->result() != syncnet::StatusCode::StatusCode_Success || add_agent == nullptr)
		{
			++stats_.spawn_failures;
			log::Printf(LogLevel::Warn, "[%s] 스폰 실패", user_id_.c_str());
			return;
		}

		++stats_.spawn_success;
		blackboard_.self_actor_id = add_agent->actorId();
		blackboard_.has_character = true;
		if (const syncnet::Vec3* pos = add_agent->pos())
			blackboard_.self_pos = Vec3(*pos);

		EnterPhase(BotPhase::Playing);

		if (verbose_)
			log::Printf(LogLevel::Info, "[%s] 스폰 완료 actorId=%d", user_id_.c_str(),
				blackboard_.self_actor_id);
	}

	void BotClient::HandleUpdateActorNotify(const syncnet::GameMessage* message)
	{
		const syncnet::UpdateActorNotify* notify = message->msg_as_UpdateActorNotify();
		if (notify == nullptr)
			return;

		const WorldView::ApplyResult result = blackboard_.view.Apply(notify, now_,
			blackboard_.has_character ? blackboard_.self_actor_id : WorldView::kNoSelf);

		stats_.kills += result.monster_deaths;

		if (!blackboard_.has_character)
			return;

		const ActorSnapshot* self = blackboard_.view.Find(blackboard_.self_actor_id);
		if (self == nullptr)
			return;

		blackboard_.self_pos = self->pos;

		const bool dead = self->IsDead();
		if (dead && !blackboard_.self_dead)
			++stats_.deaths;

		blackboard_.self_dead = dead;

		if (phase_ == BotPhase::Playing || phase_ == BotPhase::Dead)
			EnterPhase(dead ? BotPhase::Dead : BotPhase::Playing);
	}

	void BotClient::HandleUseSkill(const syncnet::GameMessage* message)
	{
		// 서버는 거부를 캐스터에게만 되돌려 준다(id 0, result != Success).
		// 다른 플레이어의 시전 브로드캐스트는 Success 로 오므로 세지 않는다.
		if (message->result() == syncnet::StatusCode::StatusCode_Success)
			return;

		const syncnet::UseSkill* skill = message->msg_as_UseSkill();
		if (skill == nullptr || !blackboard_.has_character
			|| skill->id() != blackboard_.self_actor_id)
			return;

		++stats_.skill_rejected;

		// 서버가 거부했다는 것은 쿨다운 예측이 서버보다 앞섰다는 뜻이다.
		// 다음 시도를 한 박자 늦춰 거부만 반복하는 상태에 빠지지 않게 한다.
		blackboard_.next_attack_at = now_ + blackboard_.ai.attack_interval_ms / 1000.0;
	}

	void BotClient::HandlePing(const syncnet::GameMessage* message)
	{
		if (message->id() != ping_message_id_)
			return;

		stats_.ping.Add(ElapsedMs(ping_sent_at_));
	}

	void BotClient::HandleEnterGate(const syncnet::GameMessage* message)
	{
		// 서버가 먼저 보내는 강제 이동(레이드 종료 등). 캐릭터가 새로 만들어지므로
		// actorId 를 갈아끼우고 시야를 비운다.
		if (message->id() != 0 || message->result() != syncnet::StatusCode::StatusCode_Success)
			return;

		const syncnet::EnterGate* gate = message->msg_as_EnterGate();
		if (gate == nullptr)
			return;

		blackboard_.view.Clear();
		blackboard_.target_actor_id = 0;
		blackboard_.self_actor_id = gate->actorId();
		blackboard_.has_character = true;
		map_id_ = gate->mapId();

		if (const syncnet::Vec3* pos = gate->pos())
		{
			blackboard_.self_pos = Vec3(*pos);
			blackboard_.spawn_pos = blackboard_.self_pos;
			blackboard_.wander_target = blackboard_.self_pos;
		}
	}
}
