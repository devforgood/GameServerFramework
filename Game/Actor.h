#pragma once
#include "GameObject.h"
#include "SendMessage.h"
#include "Vector3.h"

class Actor : public GameObject
{
protected:
	int agent_id_ = -1;
	bool is_input_locked_ = false;
	Vector3 front_vector_;    // 캐릭터가 바라보는 방향
	float rotation_speed_ = 5.0f; // 회전 속도 (초당 회전 각도)
	Vector3 position_;        // 위치 정보를 protected로 이동
	int health_ = 100;

public:
	Actor(World* world) : GameObject(world), front_vector_(0, 0, 1)  // 초기 방향은 z축 양의 방향
	{
	}

	virtual ~Actor() {}
	virtual void update(float dt) override;

	virtual int agent_id() override { return agent_id_; }

	virtual void set_position(float x, float y, float z) 
	{
		position_.set(x, y, z);
		change_flag_ |= static_cast<long>(GameObjectChangeType::Position);
	};

	// 위치 얻기
	const Vector3& get_position() const { return position_; }

	// front vector 관련 메서드
	const Vector3& get_front_vector() const { return front_vector_; }
	void set_front_vector(const Vector3& front) { front_vector_ = front.normalized(); }
	void set_front_vector(float x, float y, float z) { front_vector_ = Vector3(x, y, z).normalized(); }

	// 목표 지점을 향해 front vector를 회전
	void rotate_to_target(const Vector3& target_pos, float dt)
	{
		Vector3 to_target = (target_pos - get_position()).normalized();
		to_target.y = 0; // y축 회전만 고려
		to_target = to_target.normalized();

		// 현재 front vector와 목표 방향 사이의 각도 계산
		float dot = front_vector_.dot(to_target);
		float angle = std::acos(dot);

		// 회전 방향 결정 (외적 사용)
		Vector3 cross = front_vector_.cross(to_target);
		float rotation_direction = (cross.y > 0) ? 1.0f : -1.0f;

		// 부드러운 회전 적용
		float rotation_amount = rotation_speed_ * dt;
		if (angle > rotation_amount)
		{
			// 현재 front vector를 rotation_amount만큼 회전
			float cos_theta = std::cos(rotation_amount * rotation_direction);
			float sin_theta = std::sin(rotation_amount * rotation_direction);
			float new_x = front_vector_.x * cos_theta - front_vector_.z * sin_theta;
			float new_z = front_vector_.x * sin_theta + front_vector_.z * cos_theta;
			front_vector_ = Vector3(new_x, 0, new_z).normalized();
		}
		else
		{
			// 목표 방향에 도달
			front_vector_ = to_target;
		}
	}

	virtual void increment_health(int amount) 
	{
		health_ += amount;
		change_flag_ |= static_cast<long>(GameObjectChangeType::Health);
	}

	virtual void decrement_health(int amount) 
	{
		health_ -= amount;
		change_flag_ |= static_cast<long>(GameObjectChangeType::Health);
	}

	virtual bool is_changed_position(float x, float y, float z) 
	{ 
		return (this->position_.x != x || this->position_.y != y || this->position_.z != z);
	}

	virtual bool is_changed() 
	{ 
		return change_flag_ != static_cast<long>(GameObjectChangeType::None)
			&& !is_input_locked_; // if input is locked, we don't want to change the position
	}
	virtual int health() { return health_; }

	virtual flatbuffers::Offset<syncnet::ActorInfo> get_actor_info(flatbuffers::FlatBufferBuilder& _fbb, long flag);

	bool is_input_locked() const { return is_input_locked_; }
	void set_input_locked(bool locked) 
	{ 
		is_input_locked_ = locked; 
		change_flag_ |= static_cast<long>(GameObjectChangeType::InputLocked);
	}

	float get_vecter2_x() const { return position_.get_vecter2_x(); }
	float get_vecter2_y() const { return position_.get_vecter2_y(); }

public:
	int gridX = -1;
	int gridY = -1;
	float speed = 0.0f;
};

