#pragma once

#include <cstdint>

#include "GameMessage.h"
#include "syncnet_generated.h"

//---------------------------------------------------------------------------------------
// 메시지 종류별 인가 정책.
//
// 접속만으로 Player 가 만들어지므로(GameSession::Start), 이 정책이 없으면 로그인하지 않은
// 연결이 모든 핸들러에 도달한다. 정책을 핸들러 밖의 순수 함수로 빼서 단위 테스트로 고정한다
// — 새 메시지를 추가할 때 화이트리스트에 넣는 걸 잊으면 테스트가 잡는다.
//---------------------------------------------------------------------------------------

namespace message_policy
{
	// 인증 전에 처리해도 되는 메시지.
	//   Login : 인증 그 자체
	//   Ping  : 연결 확인(상태를 바꾸지 않고 응답만 돌려준다)
	// 그 외에는 전부 로그인 이후에만 받는다.
	inline bool IsAllowedBeforeAuth(syncnet::GameMessages type)
	{
		return type == syncnet::GameMessages::GameMessages_Login
			|| type == syncnet::GameMessages::GameMessages_Ping;
	}

	// 운영에서는 닫아 두는 디버그 전용 메시지.
	// (ServerConfig.network.allow_debug_commands 가 true 일 때만 처리한다)
	inline bool IsDebugOnly(syncnet::GameMessages type)
	{
		return type == syncnet::GameMessages::GameMessages_SetRaycast
			|| type == syncnet::GameMessages::GameMessages_TreeDebugRequest;
	}

	// 헤더가 주장하는 본문 길이가 프로토콜 범위 안인지.
	//
	// 상한을 넘는 값을 그냥 두면 "바이트가 더 오면 처리하겠다"는 상태로 남는데, 그 길이는
	// 링버퍼 용량보다 클 수 있어서 영원히 채워지지 않는다 — 세션이 조용히 멈춘다.
	// 0 도 정상 클라이언트가 만들 수 없는 값이다(빈 본문은 파싱할 것이 없다).
	inline bool IsValidBodyLength(uint16_t bodyLength)
	{
		return bodyLength > 0 && bodyLength <= GameMessage::max_body_length;
	}
}
