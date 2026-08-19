#pragma once
#include <chrono>
#include <memory> // std::unique_ptr를 사용하기 위해 필요
#include <string>
#include <vector>

namespace sql
{
	class Connection;
    class ResultSet;
}

class IResultParser {
public:
    virtual ~IResultParser() = default;

    // ResultSet에서 데이터를 파싱하는 순수 가상 함수
    virtual void parse(sql::ResultSet* resultSet) = 0;
};

class SqlClient
{
private:
	std::unique_ptr<sql::Connection> conn_; // MariaDB 연결 객체

	// 재연결 시 커넥션을 다시 만들기 위한 접속 정보.
	// (헤더에 conncpp 타입을 끌어들이지 않도록 std::string 으로 보관한다)
	std::string url_;
	std::string user_;
	std::string password_;

	// 연결 실패가 이어질 때 재시도 간격. DB 가 죽어 있는 동안 모든 DB 작업이
	// 접속 타임아웃만큼 스레드를 붙잡지 않게 한다.
	static constexpr std::chrono::seconds kReconnectCooldown{5};

	// 다음 재시도가 허용되는 시각(기본값이면 아직 실패한 적 없음).
	std::chrono::steady_clock::time_point nextRetryAt_{};

	// 보관된 접속 정보로 conn_ 를 새로 맺는다.
	void connect();

	// connect() 를 감싸 실패 사유를 로그로 남긴다(예외를 밖으로 내지 않는다).
	// phase 는 로그에 남길 호출 맥락("startup" / "retry").
	bool TryConnect(const char* phase);

public:
	// 접속 정보는 ServerConfig(db.url / db.user / db.password)에서 온다.
	SqlClient();
	~SqlClient();

	void select(const std::string& query, const std::vector<std::string>& params, IResultParser& parser);

	// 정상 경로용: ping 없이 현재 커넥션 포인터를 그대로 반환한다.
	// 아직 연결되지 않았다면(기동 시 실패) 쿨다운을 두고 재접속을 시도한다.
	// 그래도 연결하지 못하면 nullptr — 호출자는 이 경우를 다뤄야 한다.
	sql::Connection * getConnection();

	// 오류 경로에서만 호출한다. 서버로 ping 을 보내 살아있는지 확인한다(왕복 1회).
	bool isConnectionValid();

	// 커넥션을 다시 맺고 살아있는 포인터를 반환한다(오류 경로 전용).
	sql::Connection * reconnect();
};

