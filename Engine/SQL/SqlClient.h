#pragma once
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

	// 보관된 접속 정보로 conn_ 를 새로 맺는다.
	void connect();

public:
	// 접속 정보는 ServerConfig(db.url / db.user / db.password)에서 온다.
	SqlClient();
	~SqlClient();

	void select(const std::string& query, const std::vector<std::string>& params, IResultParser& parser);

	// 정상 경로용: ping 없이 현재 커넥션 포인터를 그대로 반환한다.
	sql::Connection * getConnection() {
		return conn_.get();
	}

	// 오류 경로에서만 호출한다. 서버로 ping 을 보내 살아있는지 확인한다(왕복 1회).
	bool isConnectionValid();

	// 커넥션을 다시 맺고 살아있는 포인터를 반환한다(오류 경로 전용).
	sql::Connection * reconnect();
};

