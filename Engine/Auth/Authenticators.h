#pragma once

#include "IAuthenticator.h"

//---------------------------------------------------------------------------------------
// IAuthenticator 구현체들.
//---------------------------------------------------------------------------------------

// session_token 테이블을 조회해 검증한다(운영 기본).
// 토큰은 유일 키이므로 토큰 하나로 행을 찾고, 그 행의 user_id 가 클라가 주장한 userId 와
// 같은지 확인한다. 토큰만 맞으면 통과시키면 남의 토큰으로 아무 userId 나 될 수 있다.
class DbTokenAuthenticator : public IAuthenticator
{
public:
	explicit DbTokenAuthenticator(int tokenTtlSeconds) : tokenTtlSeconds_(tokenTtlSeconds) {}

	AuthResult Verify(sql::Connection* conn,
		const std::string& userId,
		const std::string& token) override;

private:
	int tokenTtlSeconds_;
};

// 검증 없이 통과시킨다. 로컬 개발/자동화 테스트 전용이며,
// ServerConfig 가 이 모드를 고를 때 경고 로그를 남긴다.
class AllowAllAuthenticator : public IAuthenticator
{
public:
	AuthResult Verify(sql::Connection* conn,
		const std::string& userId,
		const std::string& token) override;
};
