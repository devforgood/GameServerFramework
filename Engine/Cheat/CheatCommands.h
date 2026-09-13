#pragma once

#include <string>
#include <string_view>
#include <vector>

class GameObject;

//---------------------------------------------------------------------------------------
// 치트 명령.
//
// 클라이언트의 채팅 창에 입력한 한 줄이 syncnet::Chat 으로 올라오면 PlayerController 가
// 이리로 넘긴다. 여기서 하는 일은 "그 줄이 무슨 명령인지 해석하고 실행한 뒤, 채팅 창에
// 되돌려 줄 한 줄을 만드는 것" 이 전부다 — 네트워크도 패킷도 모른다.
//
// 인가(운영에서 열려 있으면 안 된다)는 여기서 보지 않는다. 호출하는 쪽(PlayerController)이
// ServerConfig.network.allow_debug_commands 로 막는다. 그래야 "치트가 꺼져 있다" 는 응답과
// "그런 명령이 없다" 는 응답이 섞이지 않는다.
//
// 대상이 Player* 가 아니라 GameObject* 인 이유는 컴포넌트만 있으면 동작하기 때문이다.
// 캐릭터에 즉시 반영하는 단계에서만 Player 로 내려다본다(PlayerQuest 의 보상 지급과 같은 방식).
// 덕분에 세션도 DB 도 없는 단위 테스트에서 GameObject 하나로 검증할 수 있다.
//
// 명령표(Commands)는 이 서버가 받아 주는 치트의 유일한 원본이다. help 응답도, 클라의 입력
// 자동완성 목록(syncnet::CheatList)도 전부 이 표에서 나온다 — 목록을 클라에 따로 적어 두면
// 서버에서 지운 명령이 자동완성에는 남아 "있는 줄 알고 친 명령" 이 된다.
//---------------------------------------------------------------------------------------

namespace cheat
{
	struct Result
	{
		// 아는 명령이었는가. false 면 reply 에 "모르는 명령" 안내가 들어 있다.
		bool handled = false;

		// 채팅 창에 그대로 뿌릴 한 줄(또는 여러 줄).
		std::string reply;
	};

	// 명령표의 한 줄.
	struct CommandInfo
	{
		// 명령 이름. 치는 쪽에서는 앞에 '/' 를 붙여도 되고 안 붙여도 된다.
		const char* name = "";

		// 인자 힌트. 예: "<monsterId> [count] [radius]". 인자가 없으면 빈 문자열.
		// <> 는 필수, [] 는 생략 가능이라는 뜻이다.
		const char* args = "";

		// 한 줄 설명. help 와 자동완성 목록에 그대로 나간다.
		const char* help = "";

		// 첫 인자를 자동완성할 때 어느 데이터에서 id 를 고를지.
		// "monster" / "item" / "skill" / "map" / "quest" 중 하나이며, 없으면 빈 문자열.
		//
		// 클라도 같은 GameData 를 읽으므로(단일 소스) 종류만 알려 주면 id 에 이름을 붙여
		// 후보를 만들 수 있다. 서버가 후보를 만들어 보내지 않는 이유이기도 하다 —
		// 몬스터 종류만 수백 개라 목록을 통째로 보내는 것은 낭비다.
		const char* complete = "";
	};

	// 이 서버가 받아 주는 치트 목록(help 와 클라 자동완성의 원본).
	const std::vector<CommandInfo>& Commands();

	// line 은 채팅 창에 입력된 원문이다. 앞뒤 공백과 맨 앞의 '/' 는 여기서 벗긴다.
	// player 가 없거나 빈 줄이면 handled=false 로 돌려준다.
	Result Execute(GameObject* player, std::string_view line);
}
