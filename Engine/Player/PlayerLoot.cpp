#include "PlayerLoot.h"

#include "Actor.h"
#include "Character.h"
#include "GameData/ResourceLoader.h"
#include "GameObject.h"
#include "LogHelper.h"
#include "Map.h"
#include "PartyPolicy.h"
#include "Player.h"
#include "PlayerEventBroker.h"
#include "PlayerItem.h"
#include "PlayerWallet.h"
#include "RandomUtil.h"
#include "World.h"

void PlayerLoot::Start()
{
	auto* broker = game_object->GetComponent<PlayerEventBroker>();
	if (broker == nullptr)
		return;

	broker->subscribe<PlayerLoot, EventActorDead, &PlayerLoot::OnEventActorDead>(this);
}

bool PlayerLoot::IsKiller(int killer_actor_id) const
{
	auto* player = dynamic_cast<Player*>(game_object);
	if (player == nullptr)
		return false;

	auto character = player->GetCharacter();
	return character != nullptr && character->GetActorId() == killer_actor_id;
}

void PlayerLoot::OnEventActorDead(const EventActorDead& message)
{
	// 데이터 id 를 모르는 대상(플레이어 등)은 드랍 테이블이 없다.
	if (message.victim_data_id == 0)
		return;

	const gamedata::MonsterData* data =
		ResourceLoader::Instance().GetMonsterData(message.victim_data_id);
	if (data == nullptr)
		return;

	auto* player = dynamic_cast<Player*>(game_object);
	if (player == nullptr)
		return;

	// 난수는 월드의 RandomUtil 을 쓴다(단일 스레드라 안전하고, 시드 제어가 한 곳에 모인다).
	// 월드가 초기화되지 않은 환경(단위 테스트 등)에서는 지역 인스턴스로 폴백한다
	// — combat::RollDamage 와 같은 방식이다.
	RandomUtil* random = nullptr;
	if (auto character = player->GetCharacter())
	{
		if (character->GetMap() != nullptr && character->GetMap()->world() != nullptr)
			random = character->GetMap()->world()->random_util();
	}

	static RandomUtil fallback;
	if (random == nullptr)
		random = &fallback;

	// --- 골드: 크레딧 인원수로 분배 ---
	if (data->gold_max > 0)
	{
		const int low = data->gold_min > 0 ? data->gold_min : 0;
		const int high = data->gold_max > low ? data->gold_max : low + 1;
		const int rolled = static_cast<int>(random->GetRandomDouble(low, high));
		const int share = PartyPolicy::Instance().ShareExp(rolled, message.credit_share);

		if (share > 0)
		{
			if (auto* wallet = game_object->GetComponent<PlayerWallet>())
				wallet->AddGold(share);
		}
	}

	// --- 아이템: 마지막 일격을 넣은 사람만 ---
	// 수신자마다 굴리면 같은 드랍이 파티 인원수만큼 복사된다.
	if (!IsKiller(message.killer_actor_id))
		return;

	auto* inventory = game_object->GetComponent<PlayerItem>();
	if (inventory == nullptr)
		return;

	for (const auto& drop : data->drops)
	{
		if (drop.item_id <= 0 || drop.count <= 0)
			continue;

		// chance 는 0.0~1.0. 1.0 이상이면 항상 나온다.
		if (drop.chance < 1.0 && random->GetRandomDouble(0.0, 1.0) >= drop.chance)
			continue;

		inventory->AddItem(drop.item_id, drop.count);
		LOG.debug("드랍: 몬스터 {} -> 아이템 {} x{}", message.victim_data_id, drop.item_id, drop.count);
	}
}
