#pragma once

#include "Component.h"
#include "DbChangeTracker.h"
#include "./SQL/generated/vo.h"

//---------------------------------------------------------------------------------------
// 마지막 접속 위치(맵 + 좌표).
//
// 예전에는 World 의 인메모리 맵(lastLocations_)에만 있어서, 서버를 재시작하면 전원이
// 시작 지점으로 돌아갔다. 이제 캐릭터 행과 함께 영속화한다.
//
// 좌표는 서버 좌표계 그대로 저장한다(클라 좌표계 변환은 프로토콜 경계에서만 한다).
//---------------------------------------------------------------------------------------
class PlayerLocation : public ComponentBase<PlayerLocation>
{
public:
	virtual void Load(std::any data) override;
	virtual void Save(std::any data) override;

	// 저장할 위치를 갱신한다. 캐릭터가 살아있는 동안 주기 저장/종료 저장이 이 값을 쓴다.
	void Remember(int mapId, float x, float y, float z);

	// 저장된 위치가 있으면 true 와 함께 값을 채운다.
	// map_id 가 0 이면 아직 저장된 적이 없다(첫 접속).
	bool TryGet(int& outMapId, float& outX, float& outY, float& outZ) const;

	bool HasSavedLocation() const { return mapId_ != 0; }

private:
	VOPlayerLocation buildVO() const;

	DbRowTracker<VOPlayerLocation> row_;
	long long characterId_ = 0;
	int mapId_ = 0;
	double x_ = 0.0;
	double y_ = 0.0;
	double z_ = 0.0;
};
