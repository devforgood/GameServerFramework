#include "PlayerWallet.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"

void PlayerWallet::Load(std::any data)
{
	const auto& load_data = std::any_cast<const PlayerLoadData&>(data);

	characterId_ = static_cast<int>(load_data.player.id);
	gold_ = load_data.wallet.gold;

	// character_id 가 0 이면 행이 아직 없음 -> 첫 저장 시 INSERT
	row_.SetPersisted(load_data.wallet.character_id != 0);
}

void PlayerWallet::Save(std::any data)
{
	auto* save_data = std::any_cast<PlayerSaveData*>(data);

	if (auto record = row_.Flush(buildVO()))
		save_data->wallet = std::move(record);
}

long long PlayerWallet::AddGold(long long amount)
{
	if (amount <= 0)
		return 0;

	gold_ += amount;
	row_.MarkDirty();
	markDirty();
	return amount;
}

bool PlayerWallet::SpendGold(long long amount)
{
	if (amount <= 0 || gold_ < amount)
		return false;

	gold_ -= amount;
	row_.MarkDirty();
	markDirty();
	return true;
}

PlayerWalletVO PlayerWallet::buildVO() const
{
	PlayerWalletVO vo{};
	vo.character_id = characterId_;
	vo.gold = gold_;
	return vo;
}
