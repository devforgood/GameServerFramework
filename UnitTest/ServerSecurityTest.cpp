#include "pch.h"
#include <gtest/gtest.h>

#include <chrono>
#include <filesystem>
#include <fstream>
#include <string>

#include "Authenticators.h"
#include "MessagePolicy.h"
#include "ServerConfig.h"
#include "TokenBucket.h"
#include "syncnet_generated.h"

//---------------------------------------------------------------------------------------
// P0 보안 방어선 검증.
//
// 접속만으로 Player 가 만들어지는 구조라(GameSession::Start), 아래 정책들이 무너지면
// 로그인하지 않은 연결이 그대로 게임 로직에 도달한다. 각 방어선을 개별로 고정한다.
//---------------------------------------------------------------------------------------

// ── 메시지 인가 정책 ──────────────────────────────────────────────────────────────────

// 인증 전에 열려 있어도 되는 것은 Login/Ping 둘뿐이다.
TEST(MessagePolicyTest, OnlyLoginAndPingAllowedBeforeAuth)
{
	EXPECT_TRUE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_Login));
	EXPECT_TRUE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_Ping));

	// 상태를 바꾸거나 월드를 들여다보는 메시지는 전부 막혀야 한다.
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_AddAgent));
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_RemoveAgent));
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_SetMoveTarget));
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_UseSkill));
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_EnterGate));
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_Interact));
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_QuestAccept));
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_PartyInvite));
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_DialogSelect));
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_SetRaycast));
	EXPECT_FALSE(message_policy::IsAllowedBeforeAuth(syncnet::GameMessages::GameMessages_TreeDebugRequest));
}

// 디버그 전용 메시지는 운영 기본값에서 닫혀 있어야 한다.
TEST(MessagePolicyTest, DebugOnlyMessagesAreIdentified)
{
	EXPECT_TRUE(message_policy::IsDebugOnly(syncnet::GameMessages::GameMessages_SetRaycast));
	EXPECT_TRUE(message_policy::IsDebugOnly(syncnet::GameMessages::GameMessages_TreeDebugRequest));

	EXPECT_FALSE(message_policy::IsDebugOnly(syncnet::GameMessages::GameMessages_Login));
	EXPECT_FALSE(message_policy::IsDebugOnly(syncnet::GameMessages::GameMessages_SetMoveTarget));
	EXPECT_FALSE(message_policy::IsDebugOnly(syncnet::GameMessages::GameMessages_UseSkill));
}

// ── 레이트리밋(토큰 버킷) ─────────────────────────────────────────────────────────────

TEST(TokenBucketTest, AllowsBurstUpToCapacityThenRejects)
{
	const auto t0 = TokenBucket::Clock::time_point{};
	TokenBucket bucket;
	bucket.Configure(/*rate=*/60.0, /*capacity=*/10.0, t0);

	// 같은 시각에 capacity 만큼은 통과한다(버스트 허용).
	for (int i = 0; i < 10; ++i)
		EXPECT_TRUE(bucket.Consume(t0)) << "burst " << i << " 번째가 막혔다";

	// 그 다음은 막힌다 — 시간이 흐르지 않았으므로 보충이 없다.
	EXPECT_FALSE(bucket.Consume(t0));
}

TEST(TokenBucketTest, RefillsOverTimeAtConfiguredRate)
{
	const auto t0 = TokenBucket::Clock::time_point{};
	TokenBucket bucket;
	bucket.Configure(/*rate=*/10.0, /*capacity=*/10.0, t0);

	for (int i = 0; i < 10; ++i)
		ASSERT_TRUE(bucket.Consume(t0));
	ASSERT_FALSE(bucket.Consume(t0));

	// 0.5초 → 10/s 기준 5개 보충.
	const auto t1 = t0 + std::chrono::milliseconds(500);
	for (int i = 0; i < 5; ++i)
		EXPECT_TRUE(bucket.Consume(t1)) << "보충된 토큰 " << i << " 번째가 막혔다";
	EXPECT_FALSE(bucket.Consume(t1));
}

TEST(TokenBucketTest, DoesNotAccumulateBeyondCapacity)
{
	const auto t0 = TokenBucket::Clock::time_point{};
	TokenBucket bucket;
	bucket.Configure(/*rate=*/10.0, /*capacity=*/5.0, t0);

	// 한참 놀려도 capacity 이상은 쌓이지 않는다(장시간 침묵 후 대량 발사 방지).
	const auto later = t0 + std::chrono::seconds(60);
	for (int i = 0; i < 5; ++i)
		EXPECT_TRUE(bucket.Consume(later));
	EXPECT_FALSE(bucket.Consume(later));
}

TEST(TokenBucketTest, ZeroRateDisablesLimiting)
{
	const auto t0 = TokenBucket::Clock::time_point{};
	TokenBucket bucket;
	bucket.Configure(/*rate=*/0.0, /*capacity=*/0.0, t0);

	for (int i = 0; i < 1000; ++i)
		EXPECT_TRUE(bucket.Consume(t0));
}

// ── 설정 기본값 ───────────────────────────────────────────────────────────────────────

// 설정 파일이 없어도 뜨지만, 그때의 기본값은 "안전한 쪽"이어야 한다.
TEST(ServerConfigTest, DefaultsAreSecure)
{
	const NetworkConfig network;
	const AuthConfig auth;

	EXPECT_FALSE(network.allow_debug_commands) << "디버그 핸들러가 기본으로 열려 있으면 안 된다";
	EXPECT_GT(network.max_connections, 0) << "동시 접속 상한이 기본으로 꺼져 있으면 안 된다";
	EXPECT_GT(network.max_send_queue, 0) << "송신 큐 상한이 기본으로 꺼져 있으면 안 된다";
	EXPECT_GT(network.max_packets_per_second, 0) << "레이트리밋이 기본으로 꺼져 있으면 안 된다";
	EXPECT_EQ(auth.mode, "db_token") << "기본 인증 모드가 검증을 건너뛰면 안 된다";
}

// 설정 파일이 없으면 기본값을 유지한 채 기동에 성공해야 한다(설정 없이도 뜨게 한다).
TEST(ServerConfigTest, MissingFileKeepsDefaultsAndSucceeds)
{
	ServerConfig& config = ServerConfig::Instance();
	const NetworkConfig previousNetwork = config.Network();
	const AuthConfig previousAuth = config.Auth();

	NetworkConfig defaults;
	AuthConfig defaultAuth;
	config.SetForTest(defaults, defaultAuth);

	EXPECT_TRUE(config.Load("this-file-does-not-exist-12345.json"));
	EXPECT_EQ(config.Network().max_connections, defaults.max_connections);
	EXPECT_FALSE(config.Network().allow_debug_commands);
	EXPECT_EQ(config.Auth().mode, "db_token");

	config.SetForTest(previousNetwork, previousAuth);
}

TEST(ServerConfigTest, LoadsPartialFileAndKeepsUnsetDefaults)
{
	namespace fs = std::filesystem;
	const fs::path path = fs::temp_directory_path() / "gsf_server_config_test.json";

	{
		std::ofstream out(path);
		ASSERT_TRUE(out.is_open());
		// network 일부만 지정한다. 지정하지 않은 값과 auth 는 기본값을 유지해야 한다.
		out << R"({ "network": { "max_connections": 7, "allow_debug_commands": true } })";
	}

	ServerConfig& config = ServerConfig::Instance();
	const NetworkConfig previousNetwork = config.Network();
	const AuthConfig previousAuth = config.Auth();

	ASSERT_TRUE(config.Load(path.string()));

	EXPECT_EQ(config.Network().max_connections, 7);
	EXPECT_TRUE(config.Network().allow_debug_commands);
	EXPECT_EQ(config.Network().max_send_queue, NetworkConfig{}.max_send_queue) << "미지정 값은 기본값이어야 한다";
	EXPECT_EQ(config.Auth().mode, "db_token") << "auth 섹션이 없으면 기본값을 유지해야 한다";

	config.SetForTest(previousNetwork, previousAuth); // 다른 테스트에 영향 주지 않게 되돌린다
	fs::remove(path);
}

TEST(ServerConfigTest, RejectsMalformedFile)
{
	namespace fs = std::filesystem;
	const fs::path path = fs::temp_directory_path() / "gsf_server_config_broken.json";

	{
		std::ofstream out(path);
		ASSERT_TRUE(out.is_open());
		out << "{ this is not json";
	}

	ServerConfig& config = ServerConfig::Instance();
	const NetworkConfig previousNetwork = config.Network();
	const AuthConfig previousAuth = config.Auth();

	// 설정이 있는데 못 읽으면, 의도와 다른 값으로 뜨는 것보다 기동 실패가 낫다.
	EXPECT_FALSE(config.Load(path.string()));

	config.SetForTest(previousNetwork, previousAuth);
	fs::remove(path);
}

// ── 인증기 ────────────────────────────────────────────────────────────────────────────

// DB 커넥션 없이/빈 입력으로 부르면 통과시키지 않는다.
// (DB 장애나 빈 요청이 인증 우회가 되면 안 된다)
TEST(AuthenticatorTest, DbTokenRejectsWithoutConnectionOrInput)
{
	DbTokenAuthenticator authenticator(3600);

	EXPECT_FALSE(authenticator.Verify(nullptr, "user", "token").ok) << "커넥션이 없으면 거부해야 한다";
	EXPECT_FALSE(authenticator.Verify(nullptr, "", "").ok);
}

TEST(AuthenticatorTest, AllowAllPassesNonEmptyUserButStillRejectsEmpty)
{
	AllowAllAuthenticator authenticator;

	const AuthResult ok = authenticator.Verify(nullptr, "user", "");
	EXPECT_TRUE(ok.ok) << "allow_all 은 토큰 없이도 통과시킨다(로컬 개발 전용)";

	// userId 조차 없으면 통과시킬 대상이 없다.
	EXPECT_FALSE(authenticator.Verify(nullptr, "", "token").ok);
}

// AuthService 는 설정 모드에 맞는 구현체를 고른다.
TEST(AuthenticatorTest, ServiceSelectsImplementationFromConfig)
{
	ServerConfig& config = ServerConfig::Instance();
	const NetworkConfig previousNetwork = config.Network();
	const AuthConfig previousAuth = config.Auth();

	AuthConfig allowAll;
	allowAll.mode = "allow_all";
	config.SetForTest(previousNetwork, allowAll);
	AuthService::Instance().InitFromConfig();
	ASSERT_NE(AuthService::Instance().Get(), nullptr);
	EXPECT_TRUE(AuthService::Instance().Get()->Verify(nullptr, "user", "").ok);

	AuthConfig dbToken;
	dbToken.mode = "db_token";
	config.SetForTest(previousNetwork, dbToken);
	AuthService::Instance().InitFromConfig();
	ASSERT_NE(AuthService::Instance().Get(), nullptr);
	// db_token 은 커넥션 없이 통과시키지 않는다.
	EXPECT_FALSE(AuthService::Instance().Get()->Verify(nullptr, "user", "token").ok);

	// 알 수 없는 모드는 열어주는 쪽이 아니라 db_token 으로 닫는다.
	AuthConfig unknown;
	unknown.mode = "something-else";
	config.SetForTest(previousNetwork, unknown);
	AuthService::Instance().InitFromConfig();
	ASSERT_NE(AuthService::Instance().Get(), nullptr);
	EXPECT_FALSE(AuthService::Instance().Get()->Verify(nullptr, "user", "token").ok);

	config.SetForTest(previousNetwork, previousAuth);
	AuthService::Instance().InitFromConfig();
}

// ── 패킷 길이 검증 ────────────────────────────────────────────────────────────────────

// ProcessPackets 는 헤더 길이를 먼저 검사한다. 상한을 넘는 길이는 링버퍼 용량보다 클 수
// 있어서, "더 받으면 되겠지" 하고 기다리면 그 세션이 영원히 멈춘다(조용한 정지).
TEST(PacketLengthTest, RejectsLengthsOutsideProtocolBounds)
{
	constexpr uint16_t kLimit = static_cast<uint16_t>(GameMessage::max_body_length);

	EXPECT_FALSE(message_policy::IsValidBodyLength(0)) << "빈 본문은 파싱할 것이 없다";
	EXPECT_FALSE(message_policy::IsValidBodyLength(kLimit + 1)) << "상한 초과는 세션을 멈추게 한다";
	EXPECT_FALSE(message_policy::IsValidBodyLength(65535)) << "uint16 최대값도 거부되어야 한다";

	EXPECT_TRUE(message_policy::IsValidBodyLength(1));
	EXPECT_TRUE(message_policy::IsValidBodyLength(kLimit)) << "경계값은 허용되어야 한다";
}

// 조작된 flatbuffer 는 Verifier 에서 걸러져야 한다.
// (Verifier 없이 GetGameMessage 로 읽으면 버퍼 밖을 참조한다)
TEST(PacketVerifyTest, RejectsMalformedFlatBuffer)
{
	// 임의의 쓰레기 바이트열.
	const uint8_t garbage[32] = {
		0xFF, 0xFF, 0xFF, 0x7F, 0xAA, 0xBB, 0xCC, 0xDD,
		0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
		0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	};

	flatbuffers::Verifier verifier(garbage, sizeof(garbage));
	EXPECT_FALSE(syncnet::VerifyGameMessageBuffer(verifier));

	// 잘린 버퍼도 통과하면 안 된다.
	flatbuffers::Verifier truncated(garbage, 3);
	EXPECT_FALSE(syncnet::VerifyGameMessageBuffer(truncated));
}

// Verifier 만으로는 부족하다: union 페이로드가 아예 없는 패킷도 통과한다
// (flatbuffers 의 VerifyTable(nullptr) 이 true 라서). 이 경우 msg_as_XXX() 가 nullptr 를
// 돌려주므로, 핸들러가 역참조하기 전에 msg() 널 검사로 걸러야 한다.
// → PlayerController::handle(GameMessage*) 의 "본문 없는 메시지 거부" 분기가 그 방어선이다.
TEST(PacketVerifyTest, VerifierAcceptsMessageWithMissingUnionPayload)
{
	flatbuffers::FlatBufferBuilder builder(256);

	// msg_type 만 UseSkill 로 두고 페이로드는 붙이지 않는다.
	syncnet::GameMessageBuilder gmb(builder);
	gmb.add_msg_type(syncnet::GameMessages::GameMessages_UseSkill);
	builder.Finish(gmb.Finish());

	flatbuffers::Verifier verifier(builder.GetBufferPointer(), builder.GetSize());
	EXPECT_TRUE(syncnet::VerifyGameMessageBuffer(verifier))
		<< "이 패킷이 Verifier 에서 걸린다면 아래 널 검사는 불필요해진다";

	const syncnet::GameMessage* parsed = syncnet::GetGameMessage(builder.GetBufferPointer());
	ASSERT_NE(parsed, nullptr);
	EXPECT_EQ(parsed->msg_type(), syncnet::GameMessages::GameMessages_UseSkill);

	// 핸들러가 이 값을 역참조하면 죽는다 — 그래서 dispatch 전에 msg() 를 검사한다.
	EXPECT_EQ(parsed->msg(), nullptr);
	EXPECT_EQ(parsed->msg_as_UseSkill(), nullptr);
}

// 정상적으로 만든 메시지는 Verifier 를 통과해야 한다(검증이 과하게 막지 않는지 확인).
TEST(PacketVerifyTest, AcceptsWellFormedMessage)
{
	flatbuffers::FlatBufferBuilder builder(256);
	auto ping = syncnet::CreatePing(builder, 42);
	auto msg = syncnet::CreateGameMessage(
		builder, syncnet::GameMessages::GameMessages_Ping, ping.Union(), 1,
		syncnet::StatusCode::StatusCode_Success);
	builder.Finish(msg);

	flatbuffers::Verifier verifier(builder.GetBufferPointer(), builder.GetSize());
	ASSERT_TRUE(syncnet::VerifyGameMessageBuffer(verifier));

	const syncnet::GameMessage* parsed = syncnet::GetGameMessage(builder.GetBufferPointer());
	ASSERT_NE(parsed, nullptr);
	EXPECT_EQ(parsed->msg_type(), syncnet::GameMessages::GameMessages_Ping);
	ASSERT_NE(parsed->msg_as_Ping(), nullptr);
	EXPECT_EQ(parsed->msg_as_Ping()->seq(), 42);
}
