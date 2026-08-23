#pragma once
#include "Component.h"
#include "DbChangeTracker.h"
#include "./SQL/generated/vo.h"

// 플레이어의 재화(골드).
//
// player 행이 아니라 별도 테이블을 쓴다 — PlayerLevel 이 저장할 때 VOPlayer 를 통째로
// 다시 만들기 때문에, 같은 행에 골드를 얹으면 서로 덮어쓴다.
class PlayerWallet : public ComponentBase<PlayerWallet>
{
public:
	virtual void Load(std::any data) override;
	virtual void Save(std::any data) override;

	// 지급. 실제로 늘어난 양을 돌려준다.
	long long AddGold(long long amount);

	// 차감. 잔액이 모자라면 아무것도 하지 않고 false.
	bool SpendGold(long long amount);

	long long GetGold() const { return gold_; }

private:
	VOPlayerWallet buildVO() const;

	DbRowTracker<VOPlayerWallet> row_;
	long long gold_ = 0;
	int characterId_ = 0;
};
