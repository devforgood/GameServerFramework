#include "PlayerLocation.h"

#include "PlayerLoadData.h"
#include "PlayerSaveData.h"

void PlayerLocation::Load(std::any data)
{
	const auto& load_data = std::any_cast<const PlayerLoadData&>(data);

	characterId_ = load_data.player.id;
	mapId_ = load_data.location.map_id;
	x_ = load_data.location.x;
	y_ = load_data.location.y;
	z_ = load_data.location.z;

	// character_id 가 0 이면 행이 아직 없음 -> 첫 저장 시 INSERT
	row_.SetPersisted(load_data.location.character_id != 0);
}

void PlayerLocation::Save(std::any data)
{
	auto* save_data = std::any_cast<PlayerSaveData*>(data);

	if (auto record = row_.Flush(buildVO()))
		save_data->location = std::move(*record);
}

void PlayerLocation::Remember(int mapId, float x, float y, float z)
{
	if (mapId <= 0)
		return;

	mapId_ = mapId;
	x_ = x;
	y_ = y;
	z_ = z;

	row_.MarkDirty();
	markDirty();
}

bool PlayerLocation::TryGet(int& outMapId, float& outX, float& outY, float& outZ) const
{
	if (mapId_ == 0)
		return false;

	outMapId = mapId_;
	outX = static_cast<float>(x_);
	outY = static_cast<float>(y_);
	outZ = static_cast<float>(z_);
	return true;
}

PlayerLocationVO PlayerLocation::buildVO() const
{
	PlayerLocationVO vo{};
	vo.character_id = characterId_;
	vo.map_id = mapId_;
	vo.x = x_;
	vo.y = y_;
	vo.z = z_;
	return vo;
}
