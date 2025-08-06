#pragma once
#include "syncnet_generated.h"

class World;
class Map;
class Vector3;

enum class GameObjectChangeType
{
	None = 0,
	Position = 1 << 0,
	State = 1 << 1,
	Health = 1 << 2,
	InputLocked = 1 << 3, // 입력 잠금 상태
	All = Position | State | Health | InputLocked
};

class send_message; // Forward declaration

class GameObject {
protected:
	Map* map_;

private:
	syncnet::AIState state_;
	long change_flag_;

public:
	GameObject(Map* map) : map_(map), change_flag_(0)
	{
	}
	virtual ~GameObject() = default; // 반드시 virtual 소멸자를 추가
	virtual void update(float dt) {};
	virtual void set_position(float x, float y, float z) {};
	virtual bool is_changed_position(float x, float y, float z) { return false; }
	virtual bool is_changed() { return false; }
	bool changed_flag(long flag) { return (get_changed_flag() & flag) != 0; }
	bool changed_flag(long myself_flag, long flag) { return  (myself_flag & flag) != 0; }
	virtual void set_changed(long flag) { change_flag_ = flag; }
	virtual void reset_changed() { set_changed(static_cast<long>(GameObjectChangeType::None)); }
	long get_changed_flag() { return change_flag_; }
	void add_changed_flag(long flag) { set_changed(get_changed_flag() | flag); }

	virtual syncnet::GameObjectType type() { return syncnet::GameObjectType::GameObjectType_Monster; }
	virtual int agent_id() { return -1; }
	virtual bool init(Vector3& pos) { return false; }

	Map* map() { return map_; }
	syncnet::AIState state() { return state_; }
	virtual void SetState(syncnet::AIState state) { 
		add_changed_flag(static_cast<long>(GameObjectChangeType::State));
		state_ = state; 
	}
	virtual int health() { return 0; }

	// CreateActorInfo 호출시 필요한 정보 생성
	virtual flatbuffers::Offset<syncnet::ActorInfo> get_actor_info(flatbuffers::FlatBufferBuilder& _fbb, long flag) {return 0;}
};

