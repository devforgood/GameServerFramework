#include "PlayerItem.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "PlayerEventBroker.h"
#include "GameObject.h"
#include "GameData/ResourceLoader.h"

void PlayerItem::Load(std::any data)
{
	const auto& load_data = std::any_cast<const PlayerLoadData&>(data);

	characterId_ = static_cast<int>(load_data.player.id);

	items_.Clear();
	for (const auto& vo : load_data.items)
	{
		if (vo.count <= 0)
			continue; // 수량 0 인 행은 이미 소진된 것이다
		items_.AddPersisted(vo.item_id, vo);
	}
}

void PlayerItem::Save(std::any data)
{
	auto* save_data = std::any_cast<PlayerSaveData*>(data);

	if (items_.HasPendingChanges())
		save_data->items = items_.Flush();
}

int PlayerItem::AddItem(int item_id, int count)
{
	if (item_id <= 0 || count <= 0)
		return 0;

	// 데이터에 없는 아이템은 넣지 않는다. 잘못된 id 가 인벤토리에 남으면
	// 클라이언트가 이름조차 못 붙이는 유령 아이템이 된다.
	if (ResourceLoader::Instance().GetItem(item_id) == nullptr)
		return 0;

	if (PlayerItemVO* vo = items_.Modify(item_id))
	{
		vo->count += count;
	}
	else
	{
		PlayerItemVO fresh{};
		fresh.character_id = characterId_;
		fresh.item_id = item_id;
		fresh.count = count;
		if (!items_.Add(item_id, fresh))
			return 0;
	}

	markDirty();
	publish(EventItemAcquired{ characterId_, item_id, count });
	return count;
}

int PlayerItem::RemoveItem(int item_id, int count)
{
	if (count <= 0)
		return 0;

	PlayerItemVO* vo = items_.Modify(item_id);
	if (vo == nullptr || vo->count < count)
		return 0;

	vo->count -= count;
	if (vo->count == 0)
		items_.Remove(item_id);

	markDirty();
	return count;
}

bool PlayerItem::UseItem(int item_id, int count)
{
	if (RemoveItem(item_id, count) == 0)
		return false;

	publish(EventItemUsed{ characterId_, item_id, count });
	return true;
}

int PlayerItem::RemoveQuestItems(int quest_id)
{
	if (quest_id <= 0)
		return 0;

	std::vector<int> item_ids;
	items_.CollectKeys(item_ids);

	int removed = 0;
	for (int item_id : item_ids)
	{
		const gamedata::Item* data = ResourceLoader::Instance().GetItem(item_id);
		if (data == nullptr || data->type != "quest" || data->quest_id != quest_id)
			continue;

		const PlayerItemVO* vo = items_.Find(item_id);
		if (vo == nullptr)
			continue;

		if (RemoveItem(item_id, vo->count) > 0)
			++removed;
	}
	return removed;
}

int PlayerItem::GetCount(int item_id) const
{
	const PlayerItemVO* vo = items_.Find(item_id);
	return vo != nullptr ? vo->count : 0;
}

bool PlayerItem::Has(int item_id, int count) const
{
	return GetCount(item_id) >= count;
}

void PlayerItem::publish(const EventMessage& message)
{
	if (game_object == nullptr)
		return;
	if (auto* broker = game_object->GetComponent<PlayerEventBroker>())
		broker->publish(message);
}
