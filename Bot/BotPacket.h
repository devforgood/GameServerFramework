#pragma once

#include <cstdint>
#include <functional>
#include <string>
#include <vector>

#include "syncnet_generated.h"

namespace bot::packet
{
	// 서버 프로토콜: [2바이트 little-endian 본문 길이][flatbuffers GameMessage].
	// Engine/Message/GameMessage.h 의 header_length 와 같은 값이며, 서버는 클라 → 서버
	// 방향에서 본문 512바이트를 넘으면 연결을 끊는다(message_policy::IsValidBodyLength).
	constexpr size_t kHeaderLength = 2;
	constexpr size_t kMaxRequestBodyLength = 512;

	// 서버 → 클라 방향에는 상한이 없다(시야 진입 스냅샷은 512를 쉽게 넘는다).
	// 길이 필드가 uint16 이므로 이 값이 물리적 상한이다.
	constexpr size_t kMaxResponseBodyLength = 65535;

	using Frame = std::vector<char>;

	// 완성된 flatbuffer 앞에 길이 헤더를 붙인다.
	Frame EncodeFrame(const flatbuffers::FlatBufferBuilder& builder);

	// 수신 버퍼에서 완성된 메시지를 순서대로 꺼내 콜백에 넘긴다.
	// 반환값은 소비한 바이트 수 — 호출자는 그만큼 버퍼 앞을 버리면 된다.
	// 헤더가 0 을 주장하면(정상 서버는 보내지 않는다) 프로토콜 위반이므로
	// consumed 를 그대로 두고 false 를 돌려준다.
	bool ExtractFrames(const char* data, size_t size,
		const std::function<void(const char*, size_t)>& on_message,
		size_t& consumed);

	// 검증까지 마친 GameMessage 를 돌려준다. 실패하면 nullptr.
	const syncnet::GameMessage* ParseMessage(const char* body, size_t size);

	Frame Login(int message_id, const std::string& user_id,
		const std::string& auth_token, const std::string& reconnect_token);

	// 스폰 위치는 서버가 로그인 응답에서 정해 주므로 여기 pos 는 참고값이다
	// (서버는 요청의 pos 를 믿지 않고 자기가 정한 좌표를 쓴다).
	Frame AddAgent(int message_id, const syncnet::Vec3& pos);

	Frame SetMoveTarget(int actor_id, const syncnet::Vec3& pos);

	// 데미지 스킬은 pos 를 조준점으로 쓴다(캐스터 → pos 방향 부채꼴 판정).
	// 그래서 자기 위치가 아니라 대상의 위치를 실어야 맞는다.
	Frame UseSkill(int actor_id, int skill_id, int target_id,
		const syncnet::Vec3& target_pos, int64_t timestamp_ms);

	Frame Ping(int message_id, int seq);

	Frame EnterGate(int message_id, int gate_id);

	// NPC/오브젝트 상호작용. 서버가 같은 맵인지와 거리를 검증하고, 통과하면 퀘스트의
	// talk/interact 목표가 오르며 대화가 걸린 NPC 면 DialogNode 가 이어서 온다.
	Frame Interact(int message_id, int target_id);

	// 대화 선택지. node_id 는 지금 보고 있는 노드다 — 서버가 아는 것과 다르면 거절한다.
	// choice_index 는 서버가 보낸(조건에 걸러진) 목록에서의 번호이고, 음수면 창을 닫는다.
	Frame DialogSelect(int message_id, int node_id, int choice_index);

}
