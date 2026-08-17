#pragma once

#include "Component.h"
#include "EventMessage.h"

//---------------------------------------------------------------------------------------
// 몬스터 처치 보상(골드 / 아이템 드랍).
//
// 분배 규칙:
//   골드   — 경험치와 같이 크레딧 인원수로 나눈다. 나누지 않으면 파티를 맺는 것만으로
//            수입이 인원 배로 뛴다.
//   아이템 — 마지막 일격을 넣은 사람만 굴린다. 수신자마다 굴리면 파티 인원수만큼
//            드랍이 복사된다(같은 이벤트가 전원에게 가기 때문).
//
// 바닥에 떨어뜨리지 않고 인벤토리로 바로 넣는다. 바닥 루팅은 루팅 오브젝트와 습득
// 프로토콜이 필요해서 별도 작업이다.
//---------------------------------------------------------------------------------------
class PlayerLoot : public ComponentBase<PlayerLoot>
{
public:
	virtual void Start() override;

	void OnEventActorDead(const EventActorDead& message);

private:
	// 이 플레이어의 캐릭터가 마지막 일격을 넣었는가.
	bool IsKiller(int killer_actor_id) const;
};
