#include "NonPlayerCharacter.h"
#include "Character.h"
#include "Common.h"
#include "EventMessage.h"
#include "INavMovement.h"
#include "LogHelper.h"
#include "Map.h"
#include "Player.h"
#include "PlayerEventBrokerProxy.h"
#include "Vector3.h"
#include <cmath>

namespace
{
	// 추종 목표를 다시 잡는 주기(초). 매 틱 다시 잡아도 결과가 거의 같다.
	constexpr float kFollowInterval = 0.5f;

	// 목적지 도착으로 인정하는 거리(서버 좌표, xz).
	constexpr float kArriveRadius = 4.0f;

	// 호위 대상을 따라갈 때 유지하는 거리. 0 이면 플레이어와 겹쳐 서로 밀어낸다.
	constexpr float kFollowKeepDistance = 2.0f;

	// 도착/사망을 알릴 반경. 근처에 있던 플레이어가 함께 한 것으로 본다.
	constexpr float kNotifyRadius = 40.0f;

	float DistanceSqXZ(const Vector3& a, const Vector3& b)
	{
		const float dx = a.x - b.x;
		const float dz = a.z - b.z;
		return dx * dx + dz * dz;
	}
}

NonPlayerCharacter::NonPlayerCharacter(Map* map)
	: Actor(map)
{
	gameObjectType_ = syncnet::GameObjectType::GameObjectType_Npc;
	state_ = syncnet::AIState::AIState_Patrol;
}

void NonPlayerCharacter::SetData(const gamedata::Npc* data)
{
	data_ = data;
	if (data_ == nullptr)
		return;

	if (data_->hp > 0)
		SetHealth(data_->hp);

	// 목적지는 전역 마커 id(게이트 또는 player_spawn)다. 스폰 시점에 한 번만 풀어 둔다 —
	// 매 틱 데이터를 다시 뒤질 이유가 없다.
	if (data_->escort_dest_id != 0)
	{
		const gamedata::Map* destMap = nullptr;
		syncnet::Vec3 destPos(0, 0, 0);
		if (Map::ResolveGateTarget(data_->escort_dest_id, destMap, destPos)
			&& destMap != nullptr && map_ != nullptr && destMap->id == map_->GetMapId())
		{
			destination_ = Vector3(&destPos);
			hasDestination_ = true;
		}
		else
		{
			// 다른 맵의 마커를 목적지로 두면 영원히 도착하지 못한다. 데이터 검증이
			// 막아 주지만, 통과해 들어온 경우에도 조용히 굴러가지 않게 남긴다.
			LOG.error("NPC {} 의 escort_dest_id {} 를 이 맵({})에서 찾지 못했습니다.",
				data_->id, data_->escort_dest_id, map_ != nullptr ? map_->GetMapId() : 0);
		}
	}
}

int NonPlayerCharacter::GetDataId() const
{
	return data_ != nullptr ? data_->id : 0;
}

bool NonPlayerCharacter::Init(Vector3& pos)
{
	const float speed = (data_ != nullptr && data_->move_speed > 0.0)
		? static_cast<float>(data_->move_speed) : 3.0f;

	const int actor_id = map_->GetNavMap()->AddAgent(pos.pos(), speed);
	if (actor_id < 0)
	{
		LOG.error("NonPlayerCharacter::Init 실패: navmesh 에 에이전트를 추가하지 못했습니다.");
		return false;
	}

	actorId_ = actor_id;
	SetPosition(pos.x, pos.y, pos.z);
	spawnPos_ = pos;
	this->speed = speed;
	return true;
}

void NonPlayerCharacter::Update(float dt)
{
	Actor::Update(dt);

	if (dead_)
	{
		// 리스폰 대기. 되살아나도 도착 기록은 지운다 — 새로 호위를 시작하는 셈이다.
		if (respawnAcc_ > 0.0f)
		{
			respawnAcc_ -= dt;
			if (respawnAcc_ <= 0.0f)
			{
				respawnAcc_ = 0.0f;
				dead_ = false;
				arrived_ = false;
				SetHealth(data_ != nullptr && data_->hp > 0 ? data_->hp : 100);
				state_ = syncnet::AIState::AIState_Patrol;
				map_->GetNavMap()->TeleportAgent(GetActorId(), spawnPos_.pos());
				SetPosition(spawnPos_.x, spawnPos_.y, spawnPos_.z);
				AddChangedFlag(static_cast<long>(GameObjectChangeType::All));
			}
		}
		return;
	}

	if (GetHealth() <= 0)
	{
		NotifyDead();
		return;
	}

	if (!hasDestination_)
		return; // 호위 대상이 아니면 제자리에 서 있는다

	followAcc_ += dt;
	if (followAcc_ >= kFollowInterval)
	{
		followAcc_ = 0.0f;
		FollowNearestPlayer();
	}

	CheckArrival();
}

void NonPlayerCharacter::FollowNearestPlayer()
{
	if (map_ == nullptr)
		return;

	const float range = (data_ != nullptr && data_->follow_range > 0.0)
		? static_cast<float>(data_->follow_range) : 12.0f;
	const float rangeSq = range * range;

	const Vector3& myPos = GetPosition();
	Character* nearest = nullptr;
	float nearestSq = rangeSq;

	for (const auto& player : map_->GetPlayers())
	{
		if (player == nullptr)
			continue;
		auto& character = player->GetCharacter();
		if (character == nullptr || character->GetHealth() <= 0)
			continue;

		const float distSq = DistanceSqXZ(myPos, character->GetPosition());
		if (distSq < nearestSq)
		{
			nearestSq = distSq;
			nearest = character.get();
		}
	}

	if (nearest == nullptr)
	{
		// 아무도 없으면 멈춘다. 계속 마지막 목표로 걸어가면 혼자 목적지에 도착해 버린다.
		map_->GetNavMap()->Stop(GetActorId());
		return;
	}

	// 겹치지 않도록 조금 뒤에 선다.
	const Vector3& targetPos = nearest->GetPosition();
	Vector3 toMe = myPos - targetPos;
	toMe.y = 0.0f;
	const float len = std::sqrt(toMe.x * toMe.x + toMe.z * toMe.z);

	Vector3 dest = targetPos;
	if (len > 0.001f)
	{
		dest.x = targetPos.x + (toMe.x / len) * kFollowKeepDistance;
		dest.z = targetPos.z + (toMe.z / len) * kFollowKeepDistance;
	}

	map_->GetNavMap()->SetMoveTarget(GetActorId(), dest.pos(), false);
}

void NonPlayerCharacter::CheckArrival()
{
	if (arrived_ || !hasDestination_)
		return;

	if (DistanceSqXZ(GetPosition(), destination_) > kArriveRadius * kArriveRadius)
		return;

	arrived_ = true;
	map_->GetNavMap()->Stop(GetActorId());

	const int npcId = GetDataId();
	PublishNearby([npcId](int playerId) { return EventNpcEscorted{ playerId, npcId }; });

	LOG.info("NPC {} 호위 완료(목적지 마커 {}).", npcId,
		data_ != nullptr ? data_->escort_dest_id : 0);
}

void NonPlayerCharacter::NotifyDead()
{
	if (dead_)
		return;

	dead_ = true;
	state_ = syncnet::AIState::AIState_Dead;
	AddChangedFlag(static_cast<long>(GameObjectChangeType::State));

	const int npcId = GetDataId();
	PublishNearby([npcId](int playerId) { return EventNpcDead{ playerId, npcId }; });

	respawnAcc_ = (data_ != nullptr && data_->respawn_seconds > 0)
		? static_cast<float>(data_->respawn_seconds) : 30.0f;

	LOG.info("NPC {} 사망. {}초 뒤 리스폰.", npcId, respawnAcc_);
}

template <typename TEvent>
void NonPlayerCharacter::PublishNearby(const TEvent& make_event) const
{
	if (map_ == nullptr)
		return;

	const Vector3& myPos = GetPosition();
	const float radiusSq = kNotifyRadius * kNotifyRadius;

	for (const auto& player : map_->GetPlayers())
	{
		if (player == nullptr)
			continue;

		auto& character = player->GetCharacter();
		if (character == nullptr)
			continue;
		if (DistanceSqXZ(myPos, character->GetPosition()) > radiusSq)
			continue;

		if (auto* broker = player->GetComponent<PlayerEventBroker>())
			broker->publish(make_event(static_cast<int>(player->GetPlayerId())));
	}
}
