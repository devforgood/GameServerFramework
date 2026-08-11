#pragma once

namespace gamedata
{
	struct Npc; // Forward declaration of gamedata::Npc
}

// NPC 정의 래퍼.
//
// NPC 는 아직 월드에 액터로 스폰되지 않는다 — 위치와 상호작용 반경만 가진 정적 데이터이고,
// 클라이언트가 씬에 배치한 것을 서버가 데이터로 검증한다(Interact 요청의 거리 판정).
// 나중에 이동/전투하는 NPC 가 필요해지면 이 클래스가 액터를 갖게 된다.
class Npc
{
public:
	const gamedata::Npc* gamedata = nullptr; // Pointer to gamedata for Npc information
};
