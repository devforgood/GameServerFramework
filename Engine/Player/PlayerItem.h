#pragma once
#include "Component.h"
#include "DbChangeTracker.h"
#include "EventMessage.h"
#include "./SQL/generated/vo.h"
#include <vector>

// 플레이어 인벤토리. 아이템 종류마다 한 줄(수량 누적)이다.
//
// 획득/사용을 이벤트로 알리므로, 수집(collect)·사용(use_item) 퀘스트가 인벤토리를
// 직접 들여다보지 않고도 진행된다.
class PlayerItem : public ComponentBase<PlayerItem>
{
public:
	virtual void Load(std::any data) override;
	virtual void Save(std::any data) override;

	// 아이템을 넣는다. 실제로 늘어난 수량을 돌려주고 EventItemAcquired 를 발행한다.
	int AddItem(int item_id, int count);

	// 아이템을 뺀다. 보유량이 모자라면 아무것도 하지 않고 0 을 돌려준다
	// (부분 차감은 "5개 필요한데 3개만 사라짐" 같은 상태를 만든다).
	int RemoveItem(int item_id, int count);

	// 사용. 차감에 성공하면 EventItemUsed 를 발행한다.
	bool UseItem(int item_id, int count = 1);

	// 특정 퀘스트 전용 아이템(item.json 의 type "quest" + quest_id)을 전부 회수한다.
	// 퀘스트를 끝내거나 포기할 때 부른다. 회수한 종류 수를 돌려준다.
	int RemoveQuestItems(int quest_id);

	int GetCount(int item_id) const;
	bool Has(int item_id, int count = 1) const;
	size_t DistinctCount() const { return items_.Size(); }

	void CollectItemIds(std::vector<int>& out) const { items_.CollectKeys(out); }

private:
	void publish(const EventMessage& message);

	DbCollectionTracker<int, PlayerItemVO> items_;
	int characterId_ = 0;
};
