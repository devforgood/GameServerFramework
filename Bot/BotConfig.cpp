#include "BotConfig.h"

#include <nlohmann/json.hpp>

#include <cstdlib>
#include <cstring>
#include <fstream>
#include <iostream>
#include <string>

namespace bot
{
	namespace
	{
		using json = nlohmann::json;

		// 있으면 읽고 없으면 그대로 둔다. null 이 들어 있어도 무시한다
		// (게임 데이터와 같은 규칙 — null 하나로 전체 파싱을 실패시키지 않는다).
		template <class T>
		void ReadField(const json& obj, const char* key, T& out)
		{
			if (!obj.is_object())
				return;
			auto it = obj.find(key);
			if (it == obj.end() || it->is_null())
				return;
			out = it->get<T>();
		}

		const json& Section(const json& root, const char* key, const json& fallback)
		{
			auto it = root.find(key);
			return (it != root.end() && it->is_object()) ? *it : fallback;
		}

		bool ParseInt(const char* text, int& out)
		{
			char* end = nullptr;
			errno = 0;
			long value = std::strtol(text, &end, 10);
			if (end == text || *end != '\0' || errno == ERANGE)
				return false;
			out = static_cast<int>(value);
			return true;
		}
	}

	std::string BotConfig::FindConfigPath(int argc, char* argv[])
	{
		for (int i = 1; i + 1 < argc; ++i)
		{
			if (std::strcmp(argv[i], "--config") == 0)
				return argv[i + 1];
		}
		return "bot_config.json";
	}

	bool BotConfig::Load(const std::string& path)
	{
		std::ifstream file(path);
		if (!file.is_open())
		{
			std::cout << "[bot] config not found: " << path << " (기본값으로 실행한다)\n";
			return true;
		}

		json root;
		try
		{
			file >> root;
		}
		catch (const std::exception& e)
		{
			std::cerr << "[bot] config parse error: " << e.what() << "\n";
			return false;
		}

		const json empty = json::object();

		const json& serverJson = Section(root, "server", empty);
		ReadField(serverJson, "host", server.host);
		{
			int port = server.port;
			ReadField(serverJson, "port", port);
			server.port = static_cast<uint16_t>(port);
		}

		const json& botsJson = Section(root, "bots", empty);
		ReadField(botsJson, "count", bots.count);
		ReadField(botsJson, "user_prefix", bots.user_prefix);
		ReadField(botsJson, "user_index_start", bots.user_index_start);
		ReadField(botsJson, "auth_token", bots.auth_token);

		const json& runJson = Section(root, "run", empty);
		ReadField(runJson, "worker_threads", run.worker_threads);
		ReadField(runJson, "connects_per_second", run.connects_per_second);
		ReadField(runJson, "duration_seconds", run.duration_seconds);
		ReadField(runJson, "tick_ms", run.tick_ms);
		ReadField(runJson, "report_interval_seconds", run.report_interval_seconds);
		ReadField(runJson, "reconnect", run.reconnect);
		ReadField(runJson, "reconnect_delay_ms", run.reconnect_delay_ms);

		const json& aiJson = Section(root, "ai", empty);
		ReadField(aiJson, "search_radius", ai.search_radius);
		ReadField(aiJson, "attack_range", ai.attack_range);
		ReadField(aiJson, "attack_skill_id", ai.attack_skill_id);
		ReadField(aiJson, "attack_interval_ms", ai.attack_interval_ms);
		ReadField(aiJson, "move_repath_ms", ai.move_repath_ms);
		ReadField(aiJson, "wander_radius", ai.wander_radius);
		ReadField(aiJson, "wander_interval_ms", ai.wander_interval_ms);
		ReadField(aiJson, "arrive_epsilon", ai.arrive_epsilon);
		ReadField(aiJson, "ping_interval_ms", ai.ping_interval_ms);

		const json& questJson = Section(root, "quest", empty);
		ReadField(questJson, "enabled", quest.enabled);
		ReadField(questJson, "gamedata_dir", quest.gamedata_dir);
		ReadField(questJson, "branch_offset", quest.branch_offset);
		ReadField(questJson, "fresh_accounts", quest.fresh_accounts);

		const json& limitsJson = Section(root, "limits", empty);
		ReadField(limitsJson, "max_packets_per_second", limits.max_packets_per_second);
		ReadField(limitsJson, "packet_burst", limits.packet_burst);

		const json& logJson = Section(root, "log", empty);
		ReadField(logJson, "level", log.level);
		ReadField(logJson, "verbose_bots", log.verbose_bots);
		ReadField(logJson, "csv_path", log.csv_path);

		return true;
	}

	bool BotConfig::ApplyCommandLine(int argc, char* argv[])
	{
		for (int i = 1; i < argc; ++i)
		{
			const char* arg = argv[i];
			const bool hasValue = (i + 1 < argc);

			auto takeInt = [&](int& out) {
				if (!hasValue || !ParseInt(argv[i + 1], out))
				{
					std::cerr << "[bot] invalid value for " << arg << "\n";
					return false;
				}
				++i;
				return true;
			};

			if (std::strcmp(arg, "--config") == 0)
			{
				if (!hasValue) { std::cerr << "[bot] --config requires a path\n"; return false; }
				++i; // 경로는 Load 에서 이미 사용했다.
			}
			else if (std::strcmp(arg, "--host") == 0)
			{
				if (!hasValue) { std::cerr << "[bot] --host requires a value\n"; return false; }
				server.host = argv[++i];
			}
			else if (std::strcmp(arg, "--port") == 0)
			{
				int port = 0;
				if (!takeInt(port)) return false;
				if (port < 1 || port > 65535) { std::cerr << "[bot] port out of range\n"; return false; }
				server.port = static_cast<uint16_t>(port);
			}
			else if (std::strcmp(arg, "--bots") == 0)
			{
				if (!takeInt(bots.count)) return false;
			}
			else if (std::strcmp(arg, "--threads") == 0)
			{
				if (!takeInt(run.worker_threads)) return false;
			}
			else if (std::strcmp(arg, "--duration") == 0)
			{
				if (!takeInt(run.duration_seconds)) return false;
			}
			else if (std::strcmp(arg, "--rampup") == 0)
			{
				if (!takeInt(run.connects_per_second)) return false;
			}
			else if (std::strcmp(arg, "--prefix") == 0)
			{
				if (!hasValue) { std::cerr << "[bot] --prefix requires a value\n"; return false; }
				bots.user_prefix = argv[++i];
			}
			else if (std::strcmp(arg, "--token") == 0)
			{
				if (!hasValue) { std::cerr << "[bot] --token requires a value\n"; return false; }
				bots.auth_token = argv[++i];
			}
			else if (std::strcmp(arg, "--quest") == 0)
			{
				if (!hasValue) { std::cerr << "[bot] --quest requires on|off\n"; return false; }
				const char* value = argv[++i];
				if (std::strcmp(value, "on") == 0) quest.enabled = true;
				else if (std::strcmp(value, "off") == 0) quest.enabled = false;
				else { std::cerr << "[bot] --quest expects on|off\n"; return false; }
			}
			else if (std::strcmp(arg, "--branch") == 0)
			{
				if (!takeInt(quest.branch_offset)) return false;
			}
			else if (std::strcmp(arg, "--gamedata") == 0)
			{
				if (!hasValue) { std::cerr << "[bot] --gamedata requires a path\n"; return false; }
				quest.gamedata_dir = argv[++i];
			}
			else if (std::strcmp(arg, "--reuse-accounts") == 0)
			{
				quest.fresh_accounts = false;
			}
			else if (std::strcmp(arg, "--log") == 0)
			{
				if (!hasValue) { std::cerr << "[bot] --log requires a value\n"; return false; }
				log.level = argv[++i];
			}
			else if (std::strcmp(arg, "--csv") == 0)
			{
				if (!hasValue) { std::cerr << "[bot] --csv requires a path\n"; return false; }
				log.csv_path = argv[++i];
			}
			else if (std::strcmp(arg, "--help") == 0 || std::strcmp(arg, "-h") == 0)
			{
				PrintUsage();
				return false;
			}
			else
			{
				std::cerr << "[bot] unknown argument: " << arg << "\n";
				PrintUsage();
				return false;
			}
		}
		return true;
	}

	bool BotConfig::Validate(std::string& error) const
	{
		if (bots.count < 1) { error = "bots.count must be >= 1"; return false; }
		if (run.worker_threads < 1) { error = "run.worker_threads must be >= 1"; return false; }
		if (run.tick_ms < 10) { error = "run.tick_ms must be >= 10"; return false; }
		if (run.connects_per_second < 1) { error = "run.connects_per_second must be >= 1"; return false; }
		if (run.duration_seconds < 0) { error = "run.duration_seconds must be >= 0"; return false; }
		if (limits.max_packets_per_second < 1) { error = "limits.max_packets_per_second must be >= 1"; return false; }
		if (limits.packet_burst < 1) { error = "limits.packet_burst must be >= 1"; return false; }
		if (ai.attack_range <= 0.0f) { error = "ai.attack_range must be > 0"; return false; }
		if (ai.search_radius <= 0.0f) { error = "ai.search_radius must be > 0"; return false; }
		if (quest.branch_offset < 0) { error = "quest.branch_offset must be >= 0"; return false; }
		return true;
	}

	void BotConfig::PrintUsage()
	{
		std::cout <<
			"Usage: Bot [--config <path>] [options]\n"
			"  --host <ip>        게임 서버 주소 (기본 127.0.0.1)\n"
			"  --port <n>         게임 서버 포트 (기본 65001)\n"
			"  --bots <n>         봇 수\n"
			"  --threads <n>      워커 스레드 수\n"
			"  --duration <sec>   실행 시간(0 = 무한, Ctrl+C 로 종료)\n"
			"  --rampup <n>       초당 접속 시도 수\n"
			"  --prefix <str>     계정 id 접두어\n"
			"  --token <str>      인증 토큰(auth.mode=db_token 일 때)\n"
			"  --quest on|off     메인 퀘스트 시나리오 진행(기본 on)\n"
			"  --branch <n>       가지 번호 시작값(봇마다 다른 체인/분기를 탄다)\n"
			"  --gamedata <path>  게임 데이터 폴더(기본: 리포의 통합 폴더를 자동 탐색)\n"
			"  --reuse-accounts   실행마다 새 계정을 만들지 않는다(이어서 진행)\n"
			"  --log <level>      trace|debug|info|warn|error\n"
			"  --csv <path>       주기 리포트를 CSV 로 기록\n";
	}
}
