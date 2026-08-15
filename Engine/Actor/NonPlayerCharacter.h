#pragma once
#include "Actor.h"

namespace gamedata
{
	struct Npc;
}

// 월드에 실제로 존재하는 NPC.
//
// npc.json 의 NPC 가 전부 여기 오지는 않는다. 위치와 상호작용 반경만 필요한 NPC(퀘스트를
// 주는 마을 사람 등)는 데이터로 남고 클라가 씬에 배치한다 — 액터로 만들어 봐야 매 틱 도는
// 비용만 늘고 얻는 것이 없다. **hp 가 0 보다 큰 NPC 만** 액터가 된다. 맞을 수 있다는 것은
// 곧 죽을 수 있다는 뜻이고, 그때부터는 서버가 상태를 들고 있어야 하기 때문이다.
//
// 호위(escort_dest_id 가 있는 NPC)는 가까운 플레이어를 따라간다. 플레이어가 목적지까지
// 데려가면 도착을 알리고, 도중에 죽으면 사망을 알린다 — 둘 다 주변 플레이어 전원에게 간다
// (몬스터 처치 크레딧과 같은 규칙: 근처에 있던 사람이 함께 한 것으로 본다).
class NonPlayerCharacter : public Actor
{
public:
	explicit NonPlayerCharacter(Map* map);

	virtual bool Init(Vector3& pos) override;
	virtual void Update(float dt) override;

	// 이 액터가 어느 NPC 인지(npc.json id). 스폰 직후 Map 이 새긴다.
	void SetData(const gamedata::Npc* data);
	const gamedata::Npc* GetData() const { return data_; }
	int GetDataId() const;

	// 호위 NPC 는 캐릭터가 아니지만 몬스터의 표적이 되어야 한다. 그래야 "지키다"가
	// 실제 위험이 있는 목표가 된다.
	virtual bool IsMonsterTarget() const override { return true; }

	// 사망 처리(최초 1회만 알린다). 체력이 0 이하로 떨어지면 Update 가 부른다.
	void NotifyDead();

	bool IsDead() const { return dead_; }

private:
	// 가장 가까운 플레이어 캐릭터를 따라간다. 따라갈 대상이 없으면 제자리에 선다.
	void FollowNearestPlayer();

	// 목적지에 닿았는지 확인하고, 닿았으면 주변 플레이어에게 알린다(최초 1회).
	void CheckArrival();

	// 반경 안의 플레이어 전원에게 이벤트를 발행한다.
	template <typename TEvent>
	void PublishNearby(const TEvent& make_event) const;

	const gamedata::Npc* data_ = nullptr;

	// 도착/사망은 각각 한 번만 알린다. 매 틱 알리면 진행도가 순식간에 채워진다.
	bool arrived_ = false;
	bool dead_ = false;

	// 목적지(서버 좌표). escort_dest_id 를 스폰 시점에 한 번 풀어 둔다.
	Vector3 destination_;
	bool hasDestination_ = false;

	// 스폰 지점. 죽은 뒤 여기로 되살아난다.
	Vector3 spawnPos_;

	// 추종 목표를 매 틱 다시 계산할 이유가 없다(플레이어는 그 사이 몇 cm 움직인다).
	float followAcc_ = 0.0f;

	// 사망 후 남은 리스폰 시간(초). 0 이면 대기 중이 아니다.
	float respawnAcc_ = 0.0f;
};
