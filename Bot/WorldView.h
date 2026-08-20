#pragma once

#include <cmath>
#include <cstdint>
#include <unordered_map>

#include "syncnet_generated.h"

namespace bot
{
	// 클라이언트 좌표계 벡터. 봇은 서버 좌표계로 변환할 일이 없다
	// (서버가 UpdateActorNotify 에 이미 클라 좌표로 실어 보낸다).
	struct Vec3
	{
		float x = 0.0f;
		float y = 0.0f;
		float z = 0.0f;

		Vec3() = default;
		Vec3(float in_x, float in_y, float in_z) : x(in_x), y(in_y), z(in_z) {}
		explicit Vec3(const syncnet::Vec3& v) : x(v.x()), y(v.y()), z(v.z()) {}

		syncnet::Vec3 ToNet() const { return syncnet::Vec3(x, y, z); }
	};

	// 이동 판정은 수평 거리로만 한다. 서버가 내비메시 위로 y 를 보정하기 때문에
	// 높이차를 섞으면 실제로는 붙어 있는데 사거리 밖으로 판정된다.
	inline float DistanceSqXZ(const Vec3& a, const Vec3& b)
	{
		const float dx = a.x - b.x;
		const float dz = a.z - b.z;
		return dx * dx + dz * dz;
	}

	inline float DistanceXZ(const Vec3& a, const Vec3& b)
	{
		return std::sqrt(DistanceSqXZ(a, b));
	}

	struct ActorSnapshot
	{
		int actor_id = 0;
		syncnet::GameObjectType type = syncnet::GameObjectType::GameObjectType_Monster;
		Vec3 pos;
		syncnet::AIState state = syncnet::AIState::AIState_Patrol;
		int health = 0;
		bool has_health = false;
		bool input_locked = false;
		double last_update = 0.0;

		bool IsDead() const
		{
			return state == syncnet::AIState::AIState_Dead
				|| state == syncnet::AIState::AIState_Destroyed
				|| (has_health && health <= 0);
		}
	};

	// 한 봇이 보는 세계. UpdateActorNotify 를 접어 넣어 유지한다.
	//
	// 주의: 서버는 '변경된 필드만' 보낸다(Map::SendPendingViews). 시야 진입 때만 전체
	// 스냅샷이고, 이후 갱신에는 state/health 가 빠져 있을 수 있다. 빠진 필드를 기본값으로
	// 덮어쓰면 살아 있는 몬스터가 매 틱 체력 0 으로 보이므로, 없는 필드는 이전 값을 남긴다.
	//
	// 봇 하나가 통째로 소유하며 그 봇을 돌리는 워커 스레드에서만 접근한다.
	class WorldView
	{
	public:
		struct ApplyResult
		{
			int entered = 0;
			int updated = 0;
			int removed = 0;

			// 살아 있던 몬스터가 이번 갱신에서 죽은 것으로 바뀐 수.
			int monster_deaths = 0;

			// 내 캐릭터가 이번 갱신에서 죽은 것으로 바뀌었는지.
			bool self_died = false;
		};

		// self_actor_id 는 self_died 판정에만 쓴다. 0 도 유효한 actor id 이므로,
		// 아직 캐릭터가 없으면 kNoSelf(-1) 를 넘긴다.
		static constexpr int kNoSelf = -1;
		ApplyResult Apply(const syncnet::UpdateActorNotify* notify, double now, int self_actor_id);

		void Clear() { actors_.clear(); }

		const ActorSnapshot* Find(int actor_id) const;

		// 반경 안에서 가장 가까운 살아 있는 몬스터. 없으면 0.
		int FindNearestMonster(const Vec3& from, float radius) const;

		size_t Size() const { return actors_.size(); }

		const std::unordered_map<int, ActorSnapshot>& Actors() const { return actors_; }

	private:
		std::unordered_map<int, ActorSnapshot> actors_;
	};
}
