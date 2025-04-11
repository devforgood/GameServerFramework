#pragma once
#include <memory> // std::unique_ptr�� ����ϱ� ���� �ʿ�
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

    // ResultSet���� �����͸� �Ľ��ϴ� ���� ���� �Լ�
    virtual void parse(sql::ResultSet* resultSet) = 0;
};

class SqlClient
{
private:
	std::unique_ptr<sql::Connection> conn_; // MariaDB ���� ��ü

public:
	static void test();

	SqlClient();
	~SqlClient();

	void select(const std::string& query, const std::vector<std::string>& params, IResultParser& parser);
};

