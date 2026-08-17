#pragma once

#include <memory>
#include <string>

//---------------------------------------------------------------------------------------
// 로그인 인증 seam.
//
// 게임 서버는 "이 userId 가 이 토큰을 가지고 있는가"만 묻는다. 토큰을 누가 어떻게
// 발급했는지는 구현체가 안다 — 지금은 DB 의 session_token 테이블을 조회하고(DbTokenAuthenticator),
// Lobby 가 gRPC 로 검증을 제공하게 되면 그 구현체로 갈아끼우면 게임 서버 코드는 그대로다.
//
// Verify 는 DB 스레드에서 블로킹으로 실행된다(게임 스레드에서 부르지 말 것).
// 호출은 PlayerController::handle(Login) 이 PlayerDbDispatcher 를 통해 비동기로 한다.
//---------------------------------------------------------------------------------------

namespace sql
{
	class Connection;
}

struct AuthResult
{
	bool ok = false;

	// 인증에 성공했을 때 계정에 연결된 캐릭터 id(0 이면 아직 없음 — 신규 생성 대상).
	long long playerId = 0;

	// 실패 사유. 클라에는 그대로 내려보내지 않고(계정 존재 여부가 새어나간다) 로그에만 쓴다.
	std::string reason;

	static AuthResult Fail(std::string why) { return AuthResult{ false, 0, std::move(why) }; }
	static AuthResult Ok(long long id) { return AuthResult{ true, id, {} }; }
};

class IAuthenticator
{
public:
	virtual ~IAuthenticator() = default;

	// DB 스레드에서 호출된다. conn 은 그 스레드의 커넥션이다.
	virtual AuthResult Verify(sql::Connection* conn,
		const std::string& userId,
		const std::string& token) = 0;
};

// 프로세스 전역 인증기. ServerConfig 의 auth.mode 에 따라 기동 시 한 번 세팅된다.
class AuthService
{
public:
	static AuthService& Instance();

	// 기동 시 1회. ServerConfig 를 읽어 구현체를 고른다.
	void InitFromConfig();

	// 테스트에서 구현체를 직접 갈아끼운다.
	void SetAuthenticator(std::unique_ptr<IAuthenticator> authenticator);

	IAuthenticator* Get() { return authenticator_.get(); }

private:
	AuthService() = default;

	std::unique_ptr<IAuthenticator> authenticator_;
};
