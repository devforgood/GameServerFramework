#include "Actor.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "World.h"
#include "Common.h"

void Actor::init()
{
	auto& entityManager = world_->system_manager_->GetEntityManager();
	entity_id_ = entityManager.CreateEntity();

	entityManager.AddComponent(entity_id_, Engine::TimerComponent());
	entityManager.AddComponent(entity_id_, Engine::StateComponent());
	entityManager.AddComponent(entity_id_, Engine::PositionComponent());
}

void Actor::clear()
{
	auto& entityManager = world_->system_manager_->GetEntityManager();
	entityManager.DestroyEntity(entity_id_);
}

void Actor::update(float dt)
{
	GameObject::update(dt);

	// front vector 업데이트가 필요한 경우 여기서 처리
	// 예: 이동 중일 때 이동 방향으로 front vector 업데이트
	if (speed > 0) {
		Vector3 velocity(get_vecter2_x(), 0, get_vecter2_y());
		if (velocity.length() > 0.001f) {
			front_vector_ = velocity.normalized();
		}
	}
}

void Actor::set_position(float x, float y, float z)
{
	position_.set(x, y, z);
	add_changed_flag(static_cast<long>(GameObjectChangeType::Position));
	auto& entityManager = world_->system_manager_->GetEntityManager();
	Engine::PositionComponent& transform = entityManager.GetComponent<Engine::PositionComponent>(entity_id_);
	transform.x = x;
	transform.y = y;
	transform.z = z;
};


void Actor::SetState(syncnet::AIState state) {
	GameObject::SetState(state);
	auto& entityManager = world_->system_manager_->GetEntityManager();
	entityManager.GetComponent<Engine::StateComponent>(entity_id_).stateID = static_cast<int>(state);
}

void Actor::set_changed(long flag)
{
	GameObject::set_changed(flag);
	auto& entityManager = world_->system_manager_->GetEntityManager();
	entityManager.GetComponent<Engine::StateComponent>(entity_id_).changeFlag = get_changed_flag();
}


flatbuffers::Offset<syncnet::ActorInfo> Actor::get_actor_info(flatbuffers::FlatBufferBuilder& _fbb, long flag)
{ 
	syncnet::Vec3 pos(position_.convert_x(), position_.convert_y(), position_.convert_z());
	syncnet::ActorState state(this->state());
	syncnet::ActorHealth health(this->health_);

	syncnet::Vec3* posPtr = &pos;
	syncnet::ActorState* statePtr = changed_flag(flag, static_cast<long>(GameObjectChangeType::State)) ? &state : nullptr;
	syncnet::ActorHealth* healthPtr = changed_flag(flag, static_cast<long>(GameObjectChangeType::Health)) ? &health : nullptr;

	//LOG.debug("Actor::get_actor_info: agent_id={}, pos=({}, {}, {}), type={}, state={}, health={}, is_input_locked={}",
	//	this->agent_id(), pos.x(), pos.y(), pos.z(), this->type(), this->state_, this->health_, this->is_input_locked_);

	return syncnet::CreateActorInfo(
		_fbb, 
		this->agent_id(), 
		posPtr,
		this->type(), 
		statePtr, 
		healthPtr, 
		this->is_input_locked_
	);
}