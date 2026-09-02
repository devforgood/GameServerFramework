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

		const char* GoalKindName(QuestGoalKind kind)
		{
			switch (kind)
			{
			case QuestGoalKind::Travel:   return "이동";
			case QuestGoalKind::Interact: return "대화";
			case QuestGoalKind::Hunt:     return "사냥";
			default:                      return "없음";
			}
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
		const BotScenario* scenario, int index, std::string user_id, bool verbose)
		: config_(config)
		, scenario_(scenario)
		, index_(index)
		, user_id_(std::move(user_id))
		, verbose_(verbose)
		, session_(io_context, config.server.host, config.server.PortFor(index), &stats_)
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

		// 봇 번호가 곧 가지 번호다. 번호로 가르면 실행마다 같은 봇이 같은 가지를 타므로
		// 재현할 수 있고, 봇을 늘리면 가지에 골고루 퍼진다(무작위로 고르면 한쪽으로 쏠린다).
		blackboard_.quest.Configure(scenario_, index + config.quest.branch_offset);

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
		blackboard_.quest.ResetForReconnect();
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

	void BotClient::Interact(int target_id)
	{
		if (!blackboard_.has_character)
			return;

		++stats_.interacts_sent;
		SendThrottled(packet::Interact(NextMessageId(), target_id));
	}

	void BotClient::SelectDialog(int node_id, int choice_index)
	{
		if (!blackboard_.has_character)
			return;

		++stats_.dialogs_sent;
		SendThrottled(packet::DialogSelect(NextMessageId(), node_id, choice_index));
	}

	void BotClient::EnterGate(int gate_id)
	{
		if (!blackboard_.has_character)
			return;

		gate_message_id_ = NextMessageId();
		SendThrottled(packet::EnterGate(gate_message_id_, gate_id));
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

		// 무엇을 할지 먼저 정하고(목표 하나) 트리는 그것을 실행한다.
		blackboard_.quest.Update(now_);

		// 무엇을 하려는지 남긴다(기본 로그 수준에서는 나오지 않는다). 봇이 맵을 오가거나
		// 제자리에 서 있을 때, 그것이 의도한 행동인지 갇힌 것인지는 이것으로만 갈린다.
		if (verbose_ && log::ShouldLog(LogLevel::Debug))
		{
			QuestGoal goal;
			if (blackboard_.quest.TakeGoalChange(goal))
			{
				log::Printf(LogLevel::Debug,
					"[%s] 목표: %s quest=%d map=%d npc=%d gate=%d (내 맵 %d, 레벨 %d)",
					user_id_.c_str(), GoalKindName(goal.kind), goal.quest_id, goal.map_id,
					goal.npc_id, goal.gate_id, blackboard_.quest.MapId(),
					blackboard_.quest.Level());
			}
		}

		// 계획에서 빠진 퀘스트는 남긴다. 조용히 건너뛰면 시나리오가 어디서 멈췄는지
		// 알 방법이 없다 — 패킷 수치는 그대로 나오기 때문이다.
		BotQuestBrain::SkippedQuest skipped;
		while (blackboard_.quest.TakeSkippedQuest(skipped))
		{
			log::Printf(LogLevel::Warn, "[%s] 퀘스트 %d 건너뜀: %s",
				user_id_.c_str(), skipped.quest_id, skipped.reason);
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
		case syncnet::GameMessages::GameMessages_QuestSync:
			HandleQuestSync(message);
			break;
		case syncnet::GameMessages::GameMessages_DialogNode:
			HandleDialogNode(message);
			break;
		case syncnet::GameMessages::GameMessages_Interact:
			HandleInteract(message);
			break;
		case syncnet::GameMessages::GameMessages_PlayerStatSync:
			HandlePlayerStatSync(message);
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
		blackboard_.quest.SetMapId(map_id_);

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
		// 두 갈래로 온다: 내가 밟은 게이트의 응답(id = 내 요청 번호)과, 서버가 먼저 보내는
		// 강제 이동(id 0, 레이드 종료 등). 어느 쪽이든 캐릭터가 새로 만들어지므로
		// actorId 를 갈아끼우고 시야를 비운다.
		const bool mine = gate_message_id_ != 0 && message->id() == gate_message_id_;
		if (!mine && message->id() != 0)
			return;

		if (message->result() != syncnet::StatusCode::StatusCode_Success)
		{
			// 거절(거리/쿨타임/레벨). 다음 시도는 게이트 송신 간격이 알아서 늦춰 준다.
			if (verbose_)
				log::Printf(LogLevel::Debug, "[%s] 게이트 이동 거절", user_id_.c_str());
			return;
		}

		const syncnet::EnterGate* gate = message->msg_as_EnterGate();
		if (gate == nullptr)
			return;

		blackboard_.view.Clear();
		blackboard_.target_actor_id = 0;
		blackboard_.self_actor_id = gate->actorId();
		blackboard_.has_character = true;
		map_id_ = gate->mapId();

		++stats_.map_changes;
		blackboard_.quest.SetMapId(map_id_);

		// 맵이 바뀌면 열려 있던 대화는 서버에서도 의미가 없다.
		blackboard_.quest.OnDialogClosed();

		if (const syncnet::Vec3* pos = gate->pos())
		{
			blackboard_.self_pos = Vec3(*pos);
			blackboard_.spawn_pos = blackboard_.self_pos;
			blackboard_.wander_target = blackboard_.self_pos;
		}
	}
	void BotClient::HandleQuestSync(const syncnet::GameMessage* message)
	{
		const syncnet::QuestSync* sync = message->msg_as_QuestSync();
		if (sync == nullptr)
			return;

		if (const auto* quests = sync->quests())
		{
			for (const syncnet::QuestInfo* info : *quests)
			{
				if (info == nullptr)
					continue;

				int progress[3] = { 0, 0, 0 };
				int progress_count = 0;
				if (const auto* values = info->progress())
				{
					progress_count = static_cast<int>(values->size());
					for (int i = 0; i < progress_count && i < 3; ++i)
						progress[i] = values->Get(i);
				}

				const bool accepted = blackboard_.quest.ApplyQuestInfo(info->questId(),
					info->state(), info->stage(), progress, progress_count);

				if (accepted)
				{
					++stats_.quests_accepted;
					if (verbose_)
						log::Printf(LogLevel::Info, "[%s] 퀘스트 %d 수락", user_id_.c_str(),
							info->questId());
				}
			}
		}

		if (const auto* removed = sync->removed())
		{
			for (int quest_id : *removed)
				blackboard_.quest.ApplyQuestRemoved(quest_id);
		}

		if (const auto* completed = sync->completed())
		{
			for (int quest_id : *completed)
			{
				blackboard_.quest.ApplyQuestCompleted(quest_id);
				++stats_.quests_completed;

				if (verbose_)
					log::Printf(LogLevel::Info, "[%s] 퀘스트 %d 완료", user_id_.c_str(), quest_id);
			}
		}
	}

	void BotClient::HandleDialogNode(const syncnet::GameMessage* message)
	{
		const syncnet::DialogNode* node = message->msg_as_DialogNode();
		if (node == nullptr)
			return;

		// nodeId 0 은 "대화가 끝났다"는 뜻이다.
		if (node->nodeId() == 0)
		{
			blackboard_.quest.OnDialogClosed();
			return;
		}

		// 서버가 방금 누른 선택지를 거절하면(레벨 미달 등) 같은 노드를 그대로 다시 보낸다.
		// 다시 눌러도 답은 같으니 창을 닫고 물러선다 — 그러지 않으면 한 대화 안에서
		// 같은 선택지만 두드리다 상호작용 횟수만 늘어난다.
		if (message->result() != syncnet::StatusCode::StatusCode_Success)
		{
			blackboard_.quest.OnDialogActionFailed();
			return;
		}

		// 서버는 조건(show_if)에 걸러진 목록만 보낸다. 그 순서 그대로 들고 있다가
		// 데이터의 어느 선택지인지 text_id 로 되짚는다.
		std::vector<std::string> choice_text_ids;
		if (const auto* choices = node->choices())
		{
			choice_text_ids.reserve(choices->size());
			for (const syncnet::DialogChoiceInfo* choice : *choices)
			{
				choice_text_ids.push_back(choice != nullptr && choice->textId() != nullptr
					? choice->textId()->str() : std::string());
			}
		}

		blackboard_.quest.OnDialogOpened(node->npcId(), node->nodeId(), std::move(choice_text_ids));
	}

	void BotClient::HandlePlayerStatSync(const syncnet::GameMessage* message)
	{
		const syncnet::PlayerStatSync* stat = message->msg_as_PlayerStatSync();
		if (stat == nullptr)
			return;

		// 레벨을 알아야 게이트의 required_level 과 퀘스트의 min_level 을 미리 볼 수 있다.
		// 그전에는 안 될 일을 두드려 보고 거절로만 알았다.
		blackboard_.quest.SetLevel(stat->level());

		if (verbose_)
			log::Printf(LogLevel::Info, "[%s] 레벨 %d", user_id_.c_str(), stat->level());
	}

	void BotClient::HandleInteract(const syncnet::GameMessage* message)
	{
		if (message->result() == syncnet::StatusCode::StatusCode_Success)
			return;

		// 거절이면 대화도 열리지 않는다(거리/맵이 어긋났다). 목표를 다시 세워
		// 같은 자리에서 두드리기만 하지 않게 한다.
		++stats_.interacts_rejected;
		blackboard_.quest.MarkGoalStale();
	}
}
