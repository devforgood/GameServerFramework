#include "SqlClientManager.h"
#include "SqlClient.h" // SqlClient 헤더 파일 포함
#include "SqlScript.h"
#include <fstream> // std::ifstream
#include <sstream> // std::stringstream
#include <memory> // std::unique_ptr
#include <iostream> // std::cerr
#include <string>
#include <vector>
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
    std::ifstream file("SQL/generated/create_tables.sql");
    if (!file)
    {
        LOG.error("create_tables.sql file not found.");
        return false;
    }

    std::stringstream buffer;
    buffer << file.rdbuf();
    const std::vector<std::string> statements = sql_script::Split(buffer.str());

    sql::Connection* conn = nullptr;
    try
    {
        conn = sqlClientPtr->getConnection();
    }
    catch (std::exception& e)
    {
        LOG.error("create_tables: connection failed: " + std::string(e.what()));
        return false;
    }

    if (conn == nullptr)
    {
        LOG.error("create_tables: no database connection.");
        return false;
    }

    // 문장마다 따로 잡는다. 스키마를 맞추는 ALTER 하나가 실패해도(예: 이미 다른 타입으로
    // 존재하는 컬럼) 나머지 테이블 생성까지 통째로 날리면 안 된다.
    int failed = 0;
    for (const std::string& statement : statements)
    {
        try
        {
            std::unique_ptr<sql::Statement> stmt(conn->createStatement());
            stmt->execute(statement);
        }
        catch (sql::SQLException& e)
        {
            ++failed;
            LOG.error("create_tables: [{}] 실패: {}", statement, e.what());
        }
        catch (std::exception& e)
        {
            ++failed;
            LOG.error("create_tables: [{}] 실패: {}", statement, e.what());
        }
    }

    if (failed > 0)
    {
        LOG.error("create_tables: {}개 문장이 실패했습니다(전체 {}).",
            failed, static_cast<int>(statements.size()));
        return false;
    }

    LOG.info("create_tables: {}개 문장 적용 완료.", static_cast<int>(statements.size()));
    return true;
}