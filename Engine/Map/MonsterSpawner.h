#pragma once

#include <functional>
#include <vector>

#include "gamedata.h"

class RandomUtil;

//---------------------------------------------------------------------------------------
// monster_spawn 마커 하나를 "몬스터 count 마리를 유지하는 스포너"로 다룬다.
//
// 예전에는 마커 하나당 몬스터 하나를 서버 기동 시 한 번 세우는 것이 전부였다. 그래서
// 그 몬스터가 죽으면 서버를 다시 띄우기 전까지 그 자리는 영원히 비어 있었다(마커의
// spawn_interval/spawn_delay 는 데이터에만 있고 아무도 읽지 않았다).
//
// 여기서 마커의 네 값을 전부 쓴다.
//   count           유지할 마리 수(0 이하면 1로 본다 — 필드가 없는 옛 데이터 호환)
//   radius          마커 중심에서 흩뿌릴 반경(0 이면 마커 지점에 정확히 세운다)
//   spawn_interval  부족분을 다시 채우기까지 기다리는 초(0 이면 리스폰 없음)
//   spawn_delay     맵이 열린 뒤 최초 스폰까지 기다리는 초
//
// 맵/네비메시/액터를 전혀 모른다. "살아 있는가" 와 "세워라" 를 콜백으로 받으므로
// 가짜 구현만 끼우면 스폰 타이밍 규칙을 단위 테스트로 고정할 수 있다.
//---------------------------------------------------------------------------------------
class MonsterSpawner
{
public:
	using Marker = gamedata::MapSpawnPointsMonsterSpawn;

	// 액터가 아직 살아 있는지(존재하고 체력이 남았는지). Map 이 답한다.
	using IsAliveFn = std::function<bool(int actor_id)>;

	// 실제 스폰. 좌표는 클라이언트 좌표계(Map.json 기준)다.
	// 성공하면 actor id, 실패하면 음수를 돌려준다.
	using SpawnFn = std::function<int(const Marker& marker, double x, double y, double z)>;

	// 맵 데이터의 monster_spawn 마커로 그룹을 구성한다. 이전 상태는 버린다.
	// random 이 nullptr 이면 내부 폴백 난수원을 쓴다(단위 테스트 등).
	void Build(const gamedata::Map* map_data, RandomUtil* random);

	// spawn_delay 가 없는 그룹을 곧바로 count 만큼 채운다(맵이 열리는 시점).
	// 스폰한 마리 수를 돌려준다.
	int SpawnInitial(const SpawnFn& spawn);

	// 사망/제거된 개체를 정리하고, 주기가 찬 그룹의 부족분을 채운다.
	// 이번 틱에 새로 세운 마리 수를 돌려준다.
	int Update(float delta_seconds, const IsAliveFn& is_alive, const SpawnFn& spawn);

	// 스포너가 관리 중인(살아 있다고 보는) 몬스터 수.
	int AliveCount() const;

	// 마커가 요구하는 총 마리 수.
	int DesiredCount() const;

	size_t GroupCount() const { return groups_.size(); }

	void Clear() { groups_.clear(); }

	// 마커 값 해석. 데이터 없이도 검증할 수 있도록 노출한다.
	static int  ResolveCount(const Marker& marker);
	static float ResolveRespawnSeconds(const Marker& marker);
	static float ResolveSpawnDelay(const Marker& marker);

private:
	struct Group
	{
		const Marker* marker = nullptr;
		int desired = 1;
		float respawn_seconds = 0.0f;
		float delay_remain = 0.0f;
		float respawn_acc = 0.0f;
		bool initial_done = false;
		std::vector<int> alive;
	};

	// 마커 중심 원 안의 한 점. radius 가 0 이면 마커 지점 그대로.
	void ScatterPosition(const Group& group, double& out_x, double& out_y, double& out_z);

	// 부족분을 채운다. 스폰한 수를 돌려준다.
	int Fill(Group& group, const SpawnFn& spawn);

	std::vector<Group> groups_;
	RandomUtil* random_ = nullptr;
};
