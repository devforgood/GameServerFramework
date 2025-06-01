#include "Actor.h"
#include "Vector3.h"

void Actor::update(float dt)
{
	GameObject::update(dt);
}

flatbuffers::Offset<syncnet::ActorInfo> Actor::get_actor_info(flatbuffers::FlatBufferBuilder& _fbb, GameObjectChangeType flag)
{ 
	syncnet::Vec3 pos(Vector3::convert_x(x), Vector3::convert_y(y), Vector3::convert_z(z));
	syncnet::ActorState state(this->state_);
	syncnet::ActorHealth health(this->health_);

	syncnet::Vec3* posPtr = &pos;
	syncnet::ActorState* statePtr = changed_flag(flag, GameObjectChangeType::State) ? &state : nullptr;
	syncnet::ActorHealth* healthPtr = changed_flag(flag, GameObjectChangeType::Health) ? &health : nullptr;

	return syncnet::CreateActorInfo(_fbb, this->agent_id(), posPtr, this->type(), statePtr, healthPtr, this->is_input_locked_);
}