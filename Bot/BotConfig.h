#pragma once

#include <string>
#include <cstdint>
#include <vector>

namespace bot
{
	// 봇 러너 실행 설정. bot_config.json 에서 읽고 커맨드라인으로 덮어쓴다.
	// 값은 시작 시점에 확정되며 이후 변경되지 않으므로, 모든 워커 스레드가 const 참조로 공유한다.
	struct BotConfig
	{
		struct Server
		{
			std::string host = "127.0.0.1";
			uint16_t port = 65001;

			// 서버가 포트를 여러 개 열고 있을 때(포트 하나 = 월드 하나 = 서버 스레드 하나)
			// 봇을 그 포트들에 나눠 붙인다. 비어 있으면 port 하나만 쓴다.
			// 포트 하나에만 몰면 서버가 스레드를 몇 개 띄우든 월드 하나만 돌아서,
			// 늘어난 스레드가 부하를 나눠 갖는지 확인할 수 없다.
			std::vector<uint16_t> ports;

			// 봇 번호로 포트를 고른다. 무작위로 고르면 포트별 인원이 실행마다 달라져
			// 스레드 사이 부하 차이가 측정 잡음이 된다.
			uint16_t PortFor(int bot_index) const
			{
				if (ports.empty())
					return port;
				const size_t slot = static_cast<size_t>(bot_index) % ports.size();
				return ports[slot];
			}
		} server;

		struct Bots
		{
			int count = 50;

			// 계정 id 는 prefix + 6자리 인덱스로 만든다(bot_000001). 봇마다 달라야
			// 서버가 같은 계정의 이전 세션을 쫓아내지(EvictExistingLogin) 않는다.
			std::string user_prefix = "bot_";
			int user_index_start = 1;

			// 서버 auth.mode 가 db_token 이면 발급받은 토큰이 필요하다.
			// allow_all(로컬 개발)이면 비워 둔다.
			std::string auth_token;
		} bots;

		struct Run
		{
			int worker_threads = 4;

			// 초당 새로 연결할 봇 수. 한꺼번에 붙이면 수락 큐와 DB 로그인이 막혀
			// 측정하려던 정상 부하가 아니라 접속 폭주만 재는 결과가 된다.
			int connects_per_second = 20;

			// 0 이면 Ctrl+C 까지 무한 실행.
			int duration_seconds = 60;

			// 봇 AI 틱 간격. 클라이언트 프레임에 해당한다.
			int tick_ms = 100;

			int report_interval_seconds = 5;

			// 연결이 끊긴 봇을 다시 붙일지. 부하 유지가 목적이면 켠다.
			bool reconnect = true;
			int reconnect_delay_ms = 2000;
		} run;

		struct Ai
		{
			// 사냥 대상 탐색 반경(클라이언트 좌표 단위).
			float search_radius = 30.0f;

			// 이 거리 안에 들어오면 공격한다. 스킬 사거리(skill.json range)보다
			// 살짝 짧게 잡아야 이동 오차로 빗나가지 않는다.
			float attack_range = 1.6f;
			int attack_skill_id = 1;
			int attack_interval_ms = 600;

			// 추격 중 SetMoveTarget 재전송 간격. 목표가 움직이므로 주기적으로 갱신한다.
			int move_repath_ms = 500;

			// 사냥 대상이 없을 때 배회하는 반경과 목표 갱신 주기.
			float wander_radius = 15.0f;
			int wander_interval_ms = 4000;

			// 목표 지점 도착 판정 거리.
			float arrive_epsilon = 1.0f;

			// 서버 왕복 측정용 하트비트 간격(0 이면 보내지 않는다).
			int ping_interval_ms = 1000;
		} ai;

		struct Quest
		{
			// 메인 퀘스트 시나리오를 진행할지. 끄면 예전처럼 사냥/배회만 한다.
			bool enabled = true;

			// 게임 데이터 폴더. 비우면 서버와 같은 통합 폴더를 스스로 찾는다
			// (GameDataPath::Resolve — Client/Assets/Resources/GameData/).
			std::string gamedata_dir;

			// 가지 번호의 시작값. 봇 번호에 이 값을 더해 어느 체인/어느 분기를 탈지
			// 정한다. 실행을 나눠 돌리면서 다른 가지를 보고 싶을 때 옮긴다.
			int branch_offset = 0;

			// 실행마다 계정 id 에 접미사를 붙여 새 캐릭터로 시작한다.
			// 메인 퀘스트는 반복할 수 없어서, 같은 계정으로 다시 돌리면 이미 끝낸
			// 퀘스트를 받지 못한다 — "처음부터" 진행하려면 이것이 켜져 있어야 한다.
			bool fresh_accounts = true;
		} quest;

		struct Limits
		{
			// 봇 한 명이 초당 보낼 수 있는 패킷 수. 서버 설정(network.max_packets_per_second)
			// 보다 낮게 잡아야 부하 테스트가 레이트리밋 강제 종료로 끝나지 않는다.
			int max_packets_per_second = 20;
			int packet_burst = 40;
		} limits;

		struct Log
		{
			// trace | debug | info | warn | error
			std::string level = "info";

			// 상세 로그를 남길 봇 수(앞에서부터). 전부 남기면 로그가 부하의 원인이 된다.
			int verbose_bots = 2;

			// 주기 리포트를 CSV 로도 남긴다(빈 문자열이면 남기지 않는다).
			std::string csv_path;
		} log;

		// 설정 파일을 읽는다. 파일이 없으면 기본값을 그대로 쓰고 true 를 돌려준다
		// (형식 오류만 실패로 본다).
		bool Load(const std::string& path);

		// --host --port --bots --threads --duration --config 등을 반영한다.
		// 파일보다 우선한다. 잘못된 인자는 false 와 함께 사용법을 출력한다.
		bool ApplyCommandLine(int argc, char* argv[]);

		// 값의 앞뒤가 맞는지 검사한다(스레드 수 1 이상, 봇 수 1 이상 등).
		bool Validate(std::string& error) const;

		// argc/argv 에서 --config 경로만 먼저 뽑는다(파일을 읽기 전에 필요하다).
		static std::string FindConfigPath(int argc, char* argv[]);

		static void PrintUsage();
	};
}
