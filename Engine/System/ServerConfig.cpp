#include "ServerConfig.h"

#include <cstdlib>
#include <filesystem>
#include <fstream>

#include <nlohmann/json.hpp>

#include "LogHelper.h"

namespace
{
	// 환경변수를 읽는다(없으면 빈 문자열). getenv 는 MSVC 에서 경고가 나므로 _dupenv_s 를 쓴다.
	std::string ReadEnv(const char* name)
	{
#if defined(_MSC_VER)
		char* value = nullptr;
		size_t len = 0;
		if (_dupenv_s(&value, &len, name) != 0 || value == nullptr)
			return std::string();
		std::string result(value);
		std::free(value);
		return result;
#else
		const char* value = std::getenv(name);
		return value != nullptr ? std::string(value) : std::string();
#endif
	}

	// j 에 key 가 있고 타입이 맞을 때만 out 에 넣는다(부분 설정 파일을 허용한다).
	template <typename T>
	void ReadField(const nlohmann::json& j, const char* key, T& out)
	{
		auto it = j.find(key);
		if (it == j.end() || it->is_null())
			return;
		out = it->get<T>();
	}
}

ServerConfig& ServerConfig::Instance()
{
	static ServerConfig instance;
	return instance;
}

bool ServerConfig::Load(const std::string& path)
{
	// 비밀번호 환경변수는 설정 파일 존재 여부와 무관하게 항상 적용한다.
	const std::string envPassword = ReadEnv("GAMESERVER_DB_PASSWORD");

	if (!std::filesystem::exists(path))
	{
		LOG.warn("ServerConfig: '{}' 없음. 기본 설정으로 기동한다.", path);
		if (!envPassword.empty())
			db_.password = envPassword;
		return true;
	}

	try
	{
		std::ifstream file(path);
		if (!file)
		{
			LOG.error("ServerConfig: '{}' 열기 실패", path);
			return false;
		}

		nlohmann::json root = nlohmann::json::parse(file, nullptr, true, /*ignore_comments=*/true);

		if (auto it = root.find("db"); it != root.end())
		{
			ReadField(*it, "url", db_.url);
			ReadField(*it, "user", db_.user);
			ReadField(*it, "password", db_.password);
		}

		if (auto it = root.find("network"); it != root.end())
		{
			ReadField(*it, "max_connections", network_.max_connections);
			ReadField(*it, "max_send_queue", network_.max_send_queue);
			ReadField(*it, "max_packets_per_second", network_.max_packets_per_second);
			ReadField(*it, "packet_burst", network_.packet_burst);
			ReadField(*it, "allow_debug_commands", network_.allow_debug_commands);
		}

		if (auto it = root.find("auth"); it != root.end())
		{
			ReadField(*it, "mode", auth_.mode);
			ReadField(*it, "token_ttl_seconds", auth_.token_ttl_seconds);
		}
	}
	catch (const std::exception& e)
	{
		// 설정이 있는데 읽지 못했다면, 의도와 다른 값으로 뜨는 것보다 기동 실패가 낫다.
		LOG.error("ServerConfig: '{}' 파싱 실패: {}", path, e.what());
		return false;
	}

	if (!envPassword.empty())
		db_.password = envPassword;

	if (auth_.mode == "allow_all")
		LOG.warn("ServerConfig: auth.mode=allow_all — 인증이 비활성화되어 있다. 운영에서 쓰지 말 것.");
	if (network_.allow_debug_commands)
		LOG.warn("ServerConfig: network.allow_debug_commands=true — 디버그 핸들러가 열려 있다.");

	LOG.info("ServerConfig: '{}' 로드 완료 (auth={}, max_connections={})",
		path, auth_.mode, network_.max_connections);
	return true;
}
