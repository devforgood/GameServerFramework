#pragma once

#include <chrono>
#include <memory>
#include <string>

#include <boost/asio.hpp>

#include "BotBlackboard.h"
#include "BotConfig.h"
#include "BotMetrics.h"
#include "BotSession.h"

namespace BT
{
	class BehaviorTree;
}

namespace bot
{
	enum class BotPhase
	{
		Idle,           // 아직 접속을 시작하지 않음(램프업 대기)
		Connecting,
		LoggingIn,
		Spawning,       // AddAgent 응답 대기
		Playing,
		Dead,           // 캐릭터 사망(서버가 부활시킬 때까지 대기)
		Disconnected,   // 끊김. 재접속 대기 중
	};

	// 초당 허용 패킷 수를 제한하는 토큰 버킷.
	// 서버도 같은 방식으로 세션을 제한하므로(Engine/TokenBucket.h), 봇이 서버 한도보다
	// 낮게 스스로를 눌러야 부하 테스트가 '레이트리밋으로 강제 종료' 로 끝나지 않는다.
	class SendBucket
	{
	public:
		void Configure(double rate_per_second, double burst);
		bool TryConsume(double now);

	private:
		double rate_ = 20.0;
		double capacity_ = 40.0;
		double tokens_ = 40.0;
		double last_refill_ = 0.0;
		bool started_ = false;
	};

	//-----------------------------------------------------------------------------------
	// 봇 한 명. 접속 → 로그인 → 스폰 → BT 로 사냥 시나리오를 수행한다.
	//
	// 다른 봇과 공유하는 상태가 하나도 없다. 소켓, 시야, 블랙보드, BT 인스턴스, 난수원,
	// 계측치를 모두 자기가 소유하고, 자기를 담당하는 워커 스레드에서만 만진다.
	// 그래서 봇 수를 늘려도 봇끼리 서로 막지 않고, 한 봇이 끊겨도 옆 봇에 영향이 없다.
	//-----------------------------------------------------------------------------------
	class BotClient : public BotActions
	{
	public:
		BotClient(boost::asio::io_context& io_context, const BotConfig& config,
			int index, std::string user_id, bool verbose);
		~BotClient() override;

		BotClient(const BotClient&) = delete;
		BotClient& operator=(const BotClient&) = delete;

		// 접속을 시작한다(램프업 스케줄러가 부른다).
		void Start();

		// 워커의 고정 간격 틱. now 는 실행 시작 이후 경과 초(단조 시계).
		void Tick(double now);

		// 정상 종료: 서버에 정리할 기회를 주고 소켓을 닫는다.
		void Shutdown();

		BotPhase Phase() const { return phase_; }
		const BotStats& Stats() const { return stats_; }
		size_t VisibleActors() const { return blackboard_.view.Size(); }
		const std::string& UserId() const { return user_id_; }

		// BotActions
		void MoveTo(const Vec3& pos) override;
		void Attack(int target_actor_id, const Vec3& target_pos) override;

	private:
		void OnConnected(bool success, const std::string& error);
		void OnClosed(const std::string& reason);
		void OnMessage(const syncnet::GameMessage* message);

		void HandleLoginResponse(const syncnet::GameMessage* message);
		void HandleAddAgentResponse(const syncnet::GameMessage* message);
		void HandleUpdateActorNotify(const syncnet::GameMessage* message);
		void HandleUseSkill(const syncnet::GameMessage* message);
		void HandlePing(const syncnet::GameMessage* message);
		void HandleEnterGate(const syncnet::GameMessage* message);

		void SendLogin();
		void SendSpawn();
		void SendPing();

		// 레이트리밋을 거쳐 보낸다. 토큰이 없으면 버리고 packets_throttled 를 센다.
		void SendThrottled(packet::Frame frame);

		// 로그인/스폰처럼 빠뜨리면 봇이 진행하지 못하는 패킷. 토큰이 있으면 쓰고,
		// 없어도 보낸다(접속 직후라 실제로는 거의 항상 토큰이 남아 있다).
		void SendCritical(packet::Frame frame);

		int NextMessageId() { return ++message_id_; }

		void ResetForReconnect();
		void EnterPhase(BotPhase phase);

		double NowSeconds() const { return now_; }

		const BotConfig& config_;
		const int index_;
		const std::string user_id_;
		const bool verbose_;

		BotStats stats_;
		BotSession session_;

		BotBlackboard blackboard_;
		BT::BehaviorTree* tree_ = nullptr;

		BotPhase phase_ = BotPhase::Idle;
		double now_ = 0.0;
		double reconnect_at_ = 0.0;
		double next_ping_at_ = 0.0;

		int message_id_ = 0;
		int login_message_id_ = 0;
		int spawn_message_id_ = 0;
		int ping_message_id_ = 0;

		// 왕복 지연은 틱 시각(now_)이 아니라 실제 시계로 잰다. now_ 는 100ms 틱에서만
		// 갱신되므로, 그것으로 재면 한 틱 안에 돌아온 응답이 전부 0ms 로 기록된다.
		std::chrono::steady_clock::time_point login_sent_at_;
		std::chrono::steady_clock::time_point spawn_sent_at_;
		std::chrono::steady_clock::time_point ping_sent_at_;

		SendBucket bucket_;

		// 서버가 준 재접속 토큰(플레이어 uuid). 재접속 시 되돌려 보내면 유예 중이던
		// 기존 캐릭터를 그대로 넘겨받는다(핸드오버 경로도 부하 테스트 대상이다).
		std::string reconnect_token_;

		int map_id_ = 0;
		bool shutting_down_ = false;
	};
}
