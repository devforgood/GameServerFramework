#pragma once

#include <string>
#include <string_view>

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
//---------------------------------------------------------------------------------------

namespace cheat
{
	struct Result
	{
		// 아는 명령이었는가. false 면 reply 에 "모르는 명령" 안내가 들어 있다.
		bool handled = false;

		// 채팅 창에 그대로 뿌릴 한 줄.
		std::string reply;
	};

	// line 은 채팅 창에 입력된 원문이다. 앞뒤 공백과 맨 앞의 '/' 는 여기서 벗긴다.
	// player 가 없거나 빈 줄이면 handled=false 로 돌려준다.
	Result Execute(GameObject* player, std::string_view line);
}
