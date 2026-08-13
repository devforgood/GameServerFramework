#pragma once

class Map;
class Vector3;

// 처치 크레딧 분배.
//
// 파티 규칙(누가 인정받는가)과 전투 코드(누가 죽였는가)를 갈라 놓는 자리다. Monster 는
// "이 액터가 여기서 죽었다"까지만 알리고, 그것을 몇 사람이 나눠 갖는지는 여기서 정한다.
namespace party_credit
{
	// 처치 이벤트를 크레딧 대상 전원의 PlayerEventBroker 로 발행한다.
	//
	// 대상 = 킬러 + 같은 맵에서 반경(PartyPolicy::GetKillCreditRadius) 안에 살아 있는 파티원.
	// 파티가 없거나 조건에 맞는 파티원이 없으면 킬러 한 명에게만 간다(기존 동작과 같다).
	//
	// victim_pos 는 서버 좌표계의 처치 지점이다. 반경은 xz 평면에서만 잰다 — 높이 차로
	// 크레딧이 갈리면 다리 위아래 같은 지형에서 결과가 들쭉날쭉해진다.
	void PublishActorDead(Map* map, int killer_actor_id, const Vector3& victim_pos,
		int victim_actor_id, int victim_data_id);
}
