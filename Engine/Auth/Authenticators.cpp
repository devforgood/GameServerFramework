#include "Authenticators.h"

#include <chrono>

#include "./SQL/generated/dao.h"
#include "LogHelper.h"
#include "ServerConfig.h"

AuthResult DbTokenAuthenticator::Verify(sql::Connection* conn,
	const std::string& userId,
	const std::string& token)
{
	if (conn == nullptr)
		return AuthResult::Fail("no db connection");
	if (userId.empty() || token.empty())
		return AuthResult::Fail("empty userId or token");

	SessionTokenVO vo;
	try
	{
		SessionTokenDAO dao(conn);
		if (!dao.Select(token, vo))
			return AuthResult::Fail("token not found");
	}
	catch (const std::exception& e)
	{
		// 조회 자체가 실패하면 통과시키지 않는다(DB 장애를 인증 우회로 만들지 않기 위함).
		return AuthResult::Fail(std::string("token lookup failed: ") + e.what());
	}

	// 토큰의 소유자와 클라가 주장한 계정이 같아야 한다.
	if (vo.user_id != userId)
		return AuthResult::Fail("token/userId mismatch");

	if (tokenTtlSeconds_ > 0)
	{
		const auto age = std::chrono::system_clock::now() - vo.issued_at;
		if (age > std::chrono::seconds(tokenTtlSeconds_))
			return AuthResult::Fail("token expired");
	}

	return AuthResult::Ok(vo.player_id);
}

AuthResult AllowAllAuthenticator::Verify(sql::Connection*,
	const std::string& userId,
	const std::string&)
{
	if (userId.empty())
		return AuthResult::Fail("empty userId");
	return AuthResult::Ok(0);
}

AuthService& AuthService::Instance()
{
	static AuthService instance;
	return instance;
}

void AuthService::InitFromConfig()
{
	const AuthConfig& cfg = ServerConfig::Instance().Auth();

	if (cfg.mode == "allow_all")
	{
		authenticator_ = std::make_unique<AllowAllAuthenticator>();
		LOG.warn("AuthService: allow_all 모드 — 로그인 검증 없음");
		return;
	}

	if (cfg.mode != "db_token")
		LOG.warn("AuthService: 알 수 없는 auth.mode '{}' — db_token 으로 처리한다", cfg.mode);

	authenticator_ = std::make_unique<DbTokenAuthenticator>(cfg.token_ttl_seconds);
	LOG.info("AuthService: db_token 모드 (ttl={}s)", cfg.token_ttl_seconds);
}

void AuthService::SetAuthenticator(std::unique_ptr<IAuthenticator> authenticator)
{
	authenticator_ = std::move(authenticator);
}
