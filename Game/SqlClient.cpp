#include "SqlClient.h"  
#include <iostream>  
#include <stdexcept> 
#include <string>
#include <mariadb/conncpp.hpp>

void SqlClient::test() {  
    try {
        // MariaDB 연결 설정
        sql::SQLString url("jdbc:mariadb://localhost:3306/testdb");
        sql::Properties properties({
            {"user", "root"},
            {"password", "1234"}
            });

        // 드라이버 및 연결 생성
        sql::Driver* driver = sql::mariadb::get_driver_instance();
        std::unique_ptr<sql::Connection> conn(driver->connect(url, properties));

        // 연결 확인
        if (conn->isValid()) {
            std::cout << "MariaDB에 성공적으로 연결되었습니다!" << std::endl;
        }

        // 쿼리 실행
        std::unique_ptr<sql::Statement> stmt(conn->createStatement());
        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery("SELECT id, name FROM users"));

        // 결과 출력
        while (res->next()) {
            std::cout << "ID: " << res->getInt("id")
                << ", Name: " << res->getString("name") << std::endl;
        }
    }
    catch (const sql::SQLException& e) {
        std::cerr << "SQL 오류: " << e.what() << std::endl;
    }
    catch (const std::exception& e) {
        std::cerr << "오류: " << e.what() << std::endl;
    }

   
}

SqlClient::SqlClient()
{
    try {
        // MariaDB 연결 설정
        sql::SQLString url("jdbc:mariadb://localhost:3306/testdb");
        sql::Properties properties({
            {"user", "root"},
            {"password", "1234"}
            });

        // 드라이버 및 연결 생성
        sql::Driver* driver = sql::mariadb::get_driver_instance();
		conn_ = std::unique_ptr<sql::Connection>(driver->connect(url, properties));

        // 연결 확인
        if (conn_->isValid()) {
            std::cout << "MariaDB에 성공적으로 연결되었습니다!" << std::endl;
        }
    }
    catch (const sql::SQLException& e) {
        std::cerr << "SQL 오류: " << e.what() << std::endl;
    }
    catch (const std::exception& e) {
        std::cerr << "오류: " << e.what() << std::endl;
    }
}

SqlClient::~SqlClient()
{
}

void SqlClient::select(const std::string& query, const std::vector<std::string>& params, IResultParser& parser)
{
	try {
        // PreparedStatement 생성
        std::unique_ptr<sql::PreparedStatement> pstmt(conn_->prepareStatement(query));

        // 매개변수 바인딩
        for (size_t i = 0; i < params.size(); ++i) {
            pstmt->setString(i + 1, params[i]); // 매개변수는 1부터 시작
        }

		std::unique_ptr<sql::ResultSet> res(pstmt->executeQuery());
		// 결과 파싱
		parser.parse(res.get());
	}
	catch (const sql::SQLException& e) {
		throw std::runtime_error(std::string("SQL 오류: ") + e.what());
	}
	catch (const std::exception& e) {
		throw std::runtime_error(std::string("오류: ") + e.what());
	}
}


