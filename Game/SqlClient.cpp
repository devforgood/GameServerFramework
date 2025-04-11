#include "SqlClient.h"  
#include <iostream>  
#include <stdexcept> 
#include <string>
#include <mariadb/conncpp.hpp>

void SqlClient::test() {  
    try {
        // MariaDB ���� ����
        sql::SQLString url("jdbc:mariadb://localhost:3306/testdb");
        sql::Properties properties({
            {"user", "root"},
            {"password", "1234"}
            });

        // ����̹� �� ���� ����
        sql::Driver* driver = sql::mariadb::get_driver_instance();
        std::unique_ptr<sql::Connection> conn(driver->connect(url, properties));

        // ���� Ȯ��
        if (conn->isValid()) {
            std::cout << "MariaDB�� ���������� ����Ǿ����ϴ�!" << std::endl;
        }

        // ���� ����
        std::unique_ptr<sql::Statement> stmt(conn->createStatement());
        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery("SELECT id, name FROM users"));

        // ��� ���
        while (res->next()) {
            std::cout << "ID: " << res->getInt("id")
                << ", Name: " << res->getString("name") << std::endl;
        }
    }
    catch (const sql::SQLException& e) {
        std::cerr << "SQL ����: " << e.what() << std::endl;
    }
    catch (const std::exception& e) {
        std::cerr << "����: " << e.what() << std::endl;
    }

   
}

SqlClient::SqlClient()
{
    try {
        // MariaDB ���� ����
        sql::SQLString url("jdbc:mariadb://localhost:3306/testdb");
        sql::Properties properties({
            {"user", "root"},
            {"password", "1234"}
            });

        // ����̹� �� ���� ����
        sql::Driver* driver = sql::mariadb::get_driver_instance();
		conn_ = std::unique_ptr<sql::Connection>(driver->connect(url, properties));

        // ���� Ȯ��
        if (conn_->isValid()) {
            std::cout << "MariaDB�� ���������� ����Ǿ����ϴ�!" << std::endl;
        }
    }
    catch (const sql::SQLException& e) {
        std::cerr << "SQL ����: " << e.what() << std::endl;
    }
    catch (const std::exception& e) {
        std::cerr << "����: " << e.what() << std::endl;
    }
}

SqlClient::~SqlClient()
{
}

void SqlClient::select(const std::string& query, const std::vector<std::string>& params, IResultParser& parser)
{
	try {
        // PreparedStatement ����
        std::unique_ptr<sql::PreparedStatement> pstmt(conn_->prepareStatement(query));

        // �Ű����� ���ε�
        for (size_t i = 0; i < params.size(); ++i) {
            pstmt->setString(i + 1, params[i]); // �Ű������� 1���� ����
        }

		std::unique_ptr<sql::ResultSet> res(pstmt->executeQuery());
		// ��� �Ľ�
		parser.parse(res.get());
	}
	catch (const sql::SQLException& e) {
		throw std::runtime_error(std::string("SQL ����: ") + e.what());
	}
	catch (const std::exception& e) {
		throw std::runtime_error(std::string("����: ") + e.what());
	}
}


