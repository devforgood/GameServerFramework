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

public:
	static void test();

	SqlClient();
	~SqlClient();

	void select(const std::string& query, const std::vector<std::string>& params, IResultParser& parser);
};

