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
#include <mutex>
#include <set>
#include "LogHelper.h"
#include "./generated/schema_columns.h"

namespace
{
	// ER_DUP_FIELDNAME. 이미 있는 컬럼을 추가하려 했을 때의 코드(MySQL/MariaDB 공통).
	constexpr int32_t kDuplicateColumnError = 1060;

	// 이미 있는 테이블에는 CREATE TABLE IF NOT EXISTS 가 아무것도 하지 않으므로, 나중에
	// 스키마에 추가된 컬럼은 따로 채워 넣어야 한다.
	//
	// SQL 만으로 하려면 ADD COLUMN IF NOT EXISTS 가 필요한데 그건 MariaDB 전용 문법이라
	// MySQL 서버에서는 문장이 통째로 문법 오류가 된다(= 마이그레이션이 도는 척하면서
	// 아무것도 안 한다). 어느 서버에서든 같게 동작하도록 실제 컬럼을 읽어 대조한다.
	//
	// 반환값은 추가한 컬럼 수. 실패한 것이 있으면 outFailed 를 올린다.
	int ReconcileColumns(sql::Connection* conn, int& outFailed)
	{
		std::set<std::string> existingTables;
		std::set<std::string> existingColumns; // "table.column"

		try
		{
			std::unique_ptr<sql::Statement> stmt(conn->createStatement());
			std::unique_ptr<sql::ResultSet> rs(stmt->executeQuery(
				"SELECT TABLE_NAME, COLUMN_NAME FROM information_schema.COLUMNS "
				"WHERE TABLE_SCHEMA = DATABASE()"));

			while (rs->next())
			{
				const std::string table = rs->getString(1).c_str();
				const std::string column = rs->getString(2).c_str();
				existingTables.insert(table);
				existingColumns.insert(table + "." + column);
			}
		}
		catch (std::exception& e)
		{
			// 대조할 수 없으면 아무것도 바꾸지 않는다. 근거 없이 ALTER 를 던지는 것보다
			// 그대로 두고 사람이 보게 하는 편이 낫다.
			++outFailed;
			LOG.error("create_tables: 컬럼 목록 조회 실패: {}", e.what());
			return 0;
		}

		int added = 0;
		for (const SchemaColumn& col : kSchemaColumns)
		{
			// 테이블 자체가 없으면 CREATE 가 실패한 것이고 그 로그가 이미 남아 있다.
			if (existingTables.find(col.table) == existingTables.end())
				continue;

			if (existingColumns.find(std::string(col.table) + "." + col.column) != existingColumns.end())
				continue;

			std::string statement = "ALTER TABLE `" + std::string(col.table)
				+ "` ADD COLUMN `" + col.column + "` " + col.definition;
			if (col.after != nullptr)
				statement += " AFTER `" + std::string(col.after) + "`";

			try
			{
				std::unique_ptr<sql::Statement> stmt(conn->createStatement());
				stmt->execute(statement);
				++added;
				LOG.info("create_tables: 컬럼 추가 {}.{}", col.table, col.column);
			}
			catch (sql::SQLException& e)
			{
				// 조회 시점에는 없었는데 추가 시점에는 있다 — 다른 서버 프로세스가
				// 그 사이에 넣었다는 뜻이다. 원하는 상태가 됐으므로 실패가 아니다.
				if (e.getErrorCode() == kDuplicateColumnError)
				{
					LOG.info("create_tables: 컬럼 {}.{} 은(는) 이미 추가돼 있었다.",
						col.table, col.column);
					continue;
				}

				++outFailed;
				LOG.error("create_tables: [{}] 실패: {}", statement, e.what());
			}
			catch (std::exception& e)
			{
				++outFailed;
				LOG.error("create_tables: [{}] 실패: {}", statement, e.what());
			}
		}

		return added;
	}
}


void SqlClientManager::init()
{
	// 커넥션은 스레드마다 하나씩 필요하다.
	sqlClientPtr.reset(new SqlClient());

	// 스키마 정비는 프로세스에 한 번이면 된다. DB 스레드마다 돌리면 모두가 같은 시점에
	// information_schema 를 읽어 "컬럼이 없다"고 판단하고 같은 ALTER 를 동시에 던진다.
	// 하나만 성공하고 나머지는 "Duplicate column name" 으로 실패해, 스키마는 멀쩡한데
	// 기동이 실패한 것처럼 보인다. 먼저 온 스레드가 대표로 수행하고 나머지는 그 결과를
	// 기다렸다가(call_once 가 보장한다) 그대로 쓴다.
	static std::once_flag schemaOnce;
	std::call_once(schemaOnce, [this]()
	{
		if (!create_tables())
			LOG.error("Failed to create tables.");
	});
}

bool SqlClientManager::create_tables()
{
    // 실행 디렉터리에 따라 위치가 달라진다(Game/ 에서 실행하면 SQL/generated,
    // x64/Debug 같은 출력 폴더에서 실행하면 소스 트리를 거슬러 올라가야 한다).
    // 찾지 못하면 마이그레이션이 통째로 건너뛰어지고, 나중에 없는 컬럼을 읽다 죽는다 —
    // 조용히 지나가지 않도록 후보 경로를 모두 시도하고 실패를 크게 남긴다.
    static const char* kScriptCandidates[] = {
        "SQL/generated/create_tables.sql",
        "../../Game/SQL/generated/create_tables.sql",
        "../../Engine/SQL/generated/create_tables.sql",
        "Engine/SQL/generated/create_tables.sql",
    };

    std::ifstream file;
    for (const char* candidate : kScriptCandidates)
    {
        file.open(candidate);
        if (file)
        {
            LOG.info("create_tables: '{}' 사용", candidate);
            break;
        }
        file.clear();
    }

    if (!file)
    {
        LOG.error("create_tables.sql 을 찾지 못했습니다 — 스키마 마이그레이션을 건너뜁니다. "
                  "새 컬럼/테이블이 없는 DB 라면 이후 쿼리가 실패합니다.");
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

    // 문장마다 따로 잡는다. 테이블 하나가 실패해도 나머지 생성까지 통째로 날리면 안 된다.
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

    // 테이블을 만든 뒤, 이미 있던 테이블에 빠진 컬럼을 채운다.
    const int added = ReconcileColumns(conn, failed);

    if (failed > 0)
    {
        LOG.error("create_tables: {}건이 실패했습니다(문장 {}개, 컬럼 추가 {}개).",
            failed, static_cast<int>(statements.size()), added);
        return false;
    }

    LOG.info("create_tables: {}개 문장 적용 완료, 컬럼 {}개 추가.",
        static_cast<int>(statements.size()), added);
    return true;
}