#pragma once

#include <string>

//---------------------------------------------------------------------------------------
// 서버 실행 설정.
//
// 기동 시 JSON 파일에서 한 번 읽고 이후에는 읽기 전용이다(게임 스레드에서 락 없이 읽는다).
// 파일이 없으면 아래 기본값으로 동작한다 — 기본값은 "로컬 개발에서 바로 뜨는 값"이 아니라
// "운영에서 안전한 값"이다. 예를 들어 인증은 기본이 실제 검증(db_token)이고, 디버그
// 핸들러는 기본이 비활성이다. 로컬에서 편하게 쓰려면 설정 파일에서 명시적으로 낮춰야 한다.
//
// 비밀번호는 파일보다 환경변수를 우선한다(레포/이미지에 커밋되지 않게).
//   GAMESERVER_DB_PASSWORD
//---------------------------------------------------------------------------------------

struct DbConfig
{
	std::string url = "jdbc:mariadb://localhost:3306/testdb";
	std::string user = "root";
	std::string password;   // 환경변수 GAMESERVER_DB_PASSWORD 가 있으면 그 값이 이긴다
};

struct NetworkConfig
{
	// 서버(포트) 하나가 동시에 유지하는 최대 세션 수. 초과 연결은 즉시 끊는다.
	int max_connections = 2000;

	// 세션당 송신 대기 큐 상한(메시지 개수). 넘으면 그 세션을 끊는다.
	// 수신을 멈춘 클라이언트가 서버 메모리를 끌어올리는 것을 막는 유일한 방어선이다.
	int max_send_queue = 256;

	// 세션당 수신 패킷 레이트리밋(토큰 버킷).
	//   max_packets_per_second : 초당 보충량
	//   packet_burst           : 버킷 최대 용량(순간 몰림 허용치)
	int max_packets_per_second = 60;
	int packet_burst = 120;

	// SetRaycast / TreeDebugRequest 같은 디버그 전용 핸들러 허용 여부.
	// 운영에서는 반드시 false 여야 한다.
	bool allow_debug_commands = false;
};

struct AuthConfig
{
	// "db_token"  : session_token 테이블을 조회해 토큰을 검증한다(운영 기본).
	// "allow_all" : 검증 없이 통과시킨다. 로컬 개발/자동화 테스트 전용.
	std::string mode = "db_token";

	// 발급 후 이 시간이 지난 토큰은 거부한다(초). 0 이면 만료를 보지 않는다.
	int token_ttl_seconds = 3600;
};

class ServerConfig
{
public:
	static ServerConfig& Instance();

	// path 의 JSON 을 읽어 설정을 채운다.
	// 파일이 없으면 기본값을 유지하고 true 를 돌려준다(설정 없이도 뜨게 한다).
	// 파일이 있는데 파싱에 실패하면 false — 이때는 잘못된 설정으로 뜨는 것보다 죽는 게 낫다.
	bool Load(const std::string& path);

	const DbConfig& Db() const { return db_; }
	const NetworkConfig& Network() const { return network_; }
	const AuthConfig& Auth() const { return auth_; }

	// 테스트에서 설정을 직접 주입한다(파일 없이 동작을 고정하기 위함).
	void SetForTest(const NetworkConfig& network, const AuthConfig& auth)
	{
		network_ = network;
		auth_ = auth;
	}

private:
	ServerConfig() = default;

	DbConfig db_;
	NetworkConfig network_;
	AuthConfig auth_;
};
