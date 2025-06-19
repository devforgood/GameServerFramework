#include "Actor.h"
#include "Vector3.h"
#include "LogHelper.h"

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

flatbuffers::Offset<syncnet::ActorInfo> Actor::get_actor_info(flatbuffers::FlatBufferBuilder& _fbb, long flag)
{ 
	syncnet::Vec3 pos(position_.convert_x(), position_.convert_y(), position_.convert_z());
	syncnet::ActorState state(this->state_);
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