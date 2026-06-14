#include "Actor.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "World.h"
#include "Common.h"
#include "Map.h"

void Actor::Init()
{
	auto& entityManager = map_->systemManager_->GetEntityManager();
	entityId_ = entityManager.CreateEntity();

	entityManager.AddComponent(entityId_, engine::TimerComponent());
	entityManager.AddComponent(entityId_, engine::StateComponent());
	entityManager.AddComponent(entityId_, engine::PositionComponent());
}

void Actor::Clear()
{
	auto& entityManager = map_->systemManager_->GetEntityManager();
	entityManager.DestroyEntity(entityId_);
}

void Actor::Update(float dt)
{
	GameObject::Update(dt);

	// front vector 업데이트가 필요한 경우 여기서 처리
	// 예: 이동 중일 때 이동 방향으로 front vector 업데이트
	if (speed > 0) {
		Vector3 velocity(GetVecter2X(), 0, GetVecter2Y());
		if (velocity.length() > 0.001f) {
			frontVector_ = velocity.normalized();
		}
	}
}

void Actor::SetPosition(float x, float y, float z)
{
	position_.set(x, y, z);
	AddChangedFlag(static_cast<long>(GameObjectChangeType::Position));
	auto& entityManager = map_->systemManager_->GetEntityManager();
	engine::PositionComponent& transform = entityManager.GetComponent<engine::PositionComponent>(entityId_);
	transform.x = x;
	transform.y = y;
	transform.z = z;
};


void Actor::SetState(syncnet::AIState state) {
	AddChangedFlag(static_cast<long>(GameObjectChangeType::State));
	state_ = state;
	auto& entityManager = map_->systemManager_->GetEntityManager();
	entityManager.GetComponent<engine::StateComponent>(entityId_).stateID = static_cast<int>(state);
}

void Actor::SetChangedFlag(long flag)
{
	changeFlag_ = flag;
	auto& entityManager = map_->systemManager_->GetEntityManager();
	entityManager.GetComponent<engine::StateComponent>(entityId_).changeFlag = GetChangedFlag();
}


flatbuffers::Offset<syncnet::ActorInfo> Actor::GetActorInfo(flatbuffers::FlatBufferBuilder& _fbb, long flag)
{ 
	syncnet::Vec3 pos(position_.convert_x(), position_.convert_y(), position_.convert_z());
	syncnet::ActorState state(this->GetState());
	syncnet::ActorHealth health(this->health_);

	syncnet::Vec3* posPtr = &pos;
	syncnet::ActorState* statePtr = IsChangedFlag(flag, static_cast<long>(GameObjectChangeType::State)) ? &state : nullptr;
	syncnet::ActorHealth* healthPtr = IsChangedFlag(flag, static_cast<long>(GameObjectChangeType::Health)) ? &health : nullptr;

	//LOG.debug("Actor::get_actor_info: agent_id={}, pos=({}, {}, {}), type={}, state={}, health={}, is_input_locked={}",
	//	this->agent_id(), pos.x(), pos.y(), pos.z(), this->type(), this->state_, this->health_, this->isInputLocked_);

	return syncnet::CreateActorInfo(
		_fbb, 
		this->GetActorId(), 
		posPtr,
		this->GetType(), 
		statePtr, 
		healthPtr, 
		this->isInputLocked_
	);
}