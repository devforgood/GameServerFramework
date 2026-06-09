#include "SqlClientManager.h"
#include "SqlClient.h" // SqlClient 헤더 파일 포함
#include <fstream> // std::ifstream
#include <sstream> // std::stringstream
#include <memory> // std::unique_ptr
#include <iostream> // std::cerr
#include <mariadb/conncpp.hpp>
#include "LogHelper.h"


void SqlClientManager::init()
{
	// SqlClient 객체를 생성하여 thread_specific_ptr에 저장합니다.
	sqlClientPtr.reset(new SqlClient());
    if(!create_tables())     {
        // 테이블 생성 실패 시 처리
		LOG.error("Failed to create tables.");
	}
}

bool SqlClientManager::create_tables()
{
    try {
        // create_tables.sql 파일을 읽어 SQL 문자열로 만듭니다.
        std::ifstream file("SQL/generated/create_tables.sql");
        if (file)
        {
            std::stringstream buffer;
            buffer << file.rdbuf();
            std::string sql = buffer.str();

            // SqlClient의 Connection을 통해 SQL 실행
            sql::Connection* conn = sqlClientPtr->getConnection();
            if (conn)
            {
                std::unique_ptr<sql::Statement> stmt(conn->createStatement());
                stmt->execute(sql);
            }
        }
        else
        {
            // 파일이 없을 경우 로깅 등 처리
            // 예: std::cerr << "create_tables.sql 파일을 찾을 수 없습니다." << std::endl;
            LOG.error("create_tables.sql file not found.");
            return false;
        }
    }
    catch (sql::SQLException& e)
    {
        // SQL 예외 처리
		LOG.error("SQLException: " + std::string(e.what()));
        return false;
    }
    catch (std::exception& e)
    {
		// 일반 예외 처리
		LOG.error("Exception: " + std::string(e.what()));
        return false;
    }

    return true;
}