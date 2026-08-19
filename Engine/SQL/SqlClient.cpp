#include "SqlClient.h"  
#include <chrono>
#include <iostream>  
#include <stdexcept> 
#include <string>
#include <mariadb/conncpp.hpp>
#include "LogHelper.h"
#include "ServerConfig.h"

SqlClient::SqlClient()
    : url_(ServerConfig::Instance().Db().url)
    , user_(ServerConfig::Instance().Db().user)
    , password_(ServerConfig::Instance().Db().password)
{
    // 연결에 실패해도 생성자는 예외를 던지지 않는다(DB 스레드 풀 기동을 막지 않기 위함).
    // 대신 실패 사유를 로그에 크게 남긴다 — 이걸 std::cerr 로만 버리면 나중에
    // 모든 쿼리가 "no db connection" 으로 실패하는데 그 이유를 알 수 없게 된다.
    TryConnect("startup");
}

SqlClient::~SqlClient()
{
}

void SqlClient::connect()
{
    sql::SQLString url(url_);
    sql::Properties properties({
        {"user", user_},
        {"password", password_}
        });

    sql::Driver* driver = sql::mariadb::get_driver_instance();
    conn_ = std::unique_ptr<sql::Connection>(driver->connect(url, properties));
}

bool SqlClient::TryConnect(const char* phase)
{
    try
    {
        connect();
        if (conn_ != nullptr)
        {
            nextRetryAt_ = {};
            LOG.info("SqlClient: DB 연결 성공 ({}), url='{}', user='{}'", phase, url_, user_);
            return true;
        }
        LOG.error("SqlClient: DB 연결 실패 ({}) — 드라이버가 널을 돌려줌. url='{}', user='{}'",
            phase, url_, user_);
    }
    catch (sql::SQLException& e)  // getErrorCode() 가 const 가 아니라 const 참조로 못 잡는다
    {
        LOG.error("SqlClient: DB 연결 실패 ({}) url='{}', user='{}': [{}] {}",
            phase, url_, user_, e.getErrorCode(), e.what());
    }
    catch (const std::exception& e)
    {
        LOG.error("SqlClient: DB 연결 실패 ({}) url='{}', user='{}': {}",
            phase, url_, user_, e.what());
    }

    conn_.reset();
    return false;
}

sql::Connection* SqlClient::getConnection()
{
    // 기동 시 연결에 실패했다면 conn_ 는 비어 있다. 그대로 두면 DB 가 나중에 올라와도
    // 서버를 재시작하기 전까지 모든 쿼리가 계속 실패한다 — 여기서 다시 시도해 복구한다.
    //
    // 다만 매 호출마다 시도하면 DB 가 죽어 있는 동안 모든 DB 작업이 접속 타임아웃만큼
    // 스레드를 붙잡는다. 실패한 뒤에는 쿨다운이 지날 때까지 바로 nullptr 를 돌려준다.
    // (SqlClient 는 스레드마다 하나라 여기에는 동기화가 필요 없다.)
    if (conn_ != nullptr)
        return conn_.get();

    const auto now = std::chrono::steady_clock::now();
    if (nextRetryAt_ != std::chrono::steady_clock::time_point{} && now < nextRetryAt_)
        return nullptr;

    if (!TryConnect("retry"))
        nextRetryAt_ = now + kReconnectCooldown;

    return conn_.get();
}

bool SqlClient::isConnectionValid()
{
    // isValid() 는 서버로 ping 을 보내는 동기 호출이므로 오류 경로에서만 쓴다.
    try {
        return conn_ && conn_->isValid();
    }
    catch (const std::exception&) {
        return false;
    }
}

sql::Connection* SqlClient::reconnect()
{
    // 죽은 커넥션을 버리고 새로 맺는다. 실패하면 예외가 그대로 전파된다.
    conn_.reset();
    connect();
    return conn_.get();
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


