#include "PartyCredit.h"
#include "Actor.h"
#include "Character.h"
#include "EventMessage.h"
#include "Map.h"
#include "Party.h"
#include "PartyManager.h"
#include "PartyPolicy.h"
#include "Player.h"
#include "PlayerEventBrokerProxy.h"
#include "Vector3.h"
#include <vector>

namespace
{
	// 액터가 크레딧을 받을 수 있는 상태인지. 죽은 캐릭터는 사냥에 참여하지 못한 것으로 본다.
	bool CanReceiveCredit(Actor* actor)
	{
		return actor != nullptr && actor->GetHealth() > 0;
	}
}

void party_credit::PublishActorDead(Map* map, int killer_actor_id, const Vector3& victim_pos,
	int victim_actor_id, int victim_data_id, int victim_reward_exp)
{
	if (map == nullptr)
		return;

	auto killer = map->FindActor(killer_actor_id);
	if (killer == nullptr || !killer->IsCharacter())
		return; // 몬스터끼리의 처치 등 — 크레딧을 받을 주체가 없다

	// 크레딧을 받을 브로커들. 실제로 발행할 수 있는 대상만 모아야 분배 인원수가 맞는다
	// (빙의 전 캐릭터를 세면, 아무도 받지 않은 몫만큼 남들의 경험치가 깎인다).
	std::vector<PlayerEventBrokerProxy*> receivers;
	if (auto* killer_proxy = killer->GetComponent<PlayerEventBrokerProxy>())
		receivers.push_back(killer_proxy); // 마지막 일격을 넣은 쪽은 거리 조건 없이 받는다
	else
		return;

	const long killer_player_id = static_cast<Character*>(killer.get())->GetPlayerId();
	const Party* party = PartyManager::Instance().FindByPlayer(killer_player_id);
	if (party != nullptr)
	{
		const float radius = PartyPolicy::Instance().GetKillCreditRadius();
		const float radius_sq = radius * radius;

		for (long member_id : party->GetMembers())
		{
			if (member_id == killer_player_id)
				continue;

			// 다른 맵(또는 다른 인스턴스)에 있는 파티원은 여기서 찾히지 않는다.
			// 같은 맵 조건이 별도 검사 없이 이 조회로 걸러진다.
			auto member = map->FindPlayer(member_id);
			if (member == nullptr)
				continue;

			auto character = member->GetCharacter();
			if (!CanReceiveCredit(character.get()))
				continue;

			const Vector3& pos = character->GetPosition();
			const float dx = pos.x - victim_pos.x;
			const float dz = pos.z - victim_pos.z;
			if (dx * dx + dz * dz > radius_sq)
				continue;

			if (auto* proxy = character->GetComponent<PlayerEventBrokerProxy>())
				receivers.push_back(proxy);
		}
	}

	const int share = static_cast<int>(receivers.size());
	for (auto* receiver : receivers)
		receiver->publish(EventActorDead{ killer_actor_id, victim_actor_id, victim_data_id, share, victim_reward_exp });
}
