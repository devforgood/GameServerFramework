#pragma once
#include "GameObject.h"
#include "SendMessage.h"
#include "Vector3.h"
#include "..\Engine\IGridActor.h"

class World;
class Map;
class Vector3;
class Player;

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

class Actor : public GameObject, public IGridActor
{
protected:
	int agent_id_ = -1;
	bool is_input_locked_ = false;
	Vector3 front_vector_;    // 캐릭터가 바라보는 방향
	float rotation_speed_ = 5.0f; // 회전 속도 (초당 회전 각도)
	Vector3 position_;        // 위치 정보를 protected로 이동
	int health_ = 100;
	long last_attacker_player_id_ = -1; // 마지막으로 데미지를 입힌 플레이어 ID (킬 판정용)
	syncnet::GameObjectType game_object_type_;
	int32_t entity_id_ = -1; // 엔티티 ID (필요시 사용)

	Map* map_;
	syncnet::AIState state_;
	long change_flag_;

public:
	Actor(Map* map) : map_(map), front_vector_(0, 0, 1)  // 초기 방향은 z축 양의 방향
	{
		init();
	}

	virtual ~Actor() 
	{
		clear();
	}

	bool changed_flag(long flag) { return (get_changed_flag() & flag) != 0; }
	bool changed_flag(long myself_flag, long flag) { return  (myself_flag & flag) != 0; }
	virtual void reset_changed() { set_changed(static_cast<long>(GameObjectChangeType::None)); }
	long get_changed_flag() { return change_flag_; }
	void add_changed_flag(long flag) { set_changed(get_changed_flag() | flag); }

	virtual bool pre_create(std::shared_ptr<Player> player) { return true; }
	virtual bool post_create(std::shared_ptr<Player> player, std::shared_ptr<GameObject> game_object) { return true; }

	Map* map() { return map_; }
	syncnet::AIState state() { return state_; }

	void init();
	virtual bool init(Vector3& pos) { return false; }
	void clear();

	virtual void update(float dt) override;

	int agent_id() { return agent_id_; }

	virtual void set_position(float x, float y, float z);

	virtual bool isCharacter() const
	{
		return game_object_type_ == syncnet::GameObjectType::GameObjectType_Character;
	}

	virtual void setGridX(int gridX)
	{
		this->gridX = gridX; 
	}
	virtual void setGridY(int gridY)
	{
		this->gridY = gridY; 
	}
	virtual int getGridX() const
	{ 
		return gridX; 
	}
	virtual int getGridY() const
		{ 
		return gridY; 
	}
	virtual float getVector2X() const
	{
		return get_vecter2_x();
	}
	virtual float getVector2Y() const
	{
		return get_vecter2_y();
	}
	virtual int getAgentID() const
	{ 
		return agent_id_; 
	}

	virtual void decrementHealth(int amount)
	{
		decrement_health(amount);
	}

	virtual void setLastAttackerPlayerId(long player_id) override
	{
		last_attacker_player_id_ = player_id;
	}

	// 이 액터를 조종하는 플레이어 ID. 플레이어 캐릭터가 아니면 -1.
	virtual long player_id() const { return -1; }

	// 위치 얻기
	const Vector3& get_position() const { return position_; }

	// front vector 관련 메서드
	const Vector3& get_front_vector() const { return front_vector_; }
	void set_front_vector(const Vector3& front) { front_vector_ = front.normalized(); }
	void set_front_vector(float x, float y, float z) { front_vector_ = Vector3(x, y, z).normalized(); }

	// front vector를 각도로 변환 (도 단위, 0도는 동쪽, 90도는 북쪽)
	float get_front_angle_degrees() const
	{
		// front vector를 2D 평면에서 각도로 변환 (표준 수학 좌표계)
		// 표준 수학 좌표계: atan2(y, x) 사용 (0도는 동쪽, 90도는 북쪽)
		float angle_rad = std::atan2(front_vector_.z, front_vector_.x);
		float angle_deg = angle_rad * 180.0f / 3.14159f;
		
		// 각도를 0-360도 범위로 정규화
		if (angle_deg < 0) {
			angle_deg += 360.0f;
		}
		
		return angle_deg;
	}

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
		add_changed_flag(static_cast<long>(GameObjectChangeType::Health));
	}

	virtual void decrement_health(int amount) 
	{
		health_ -= amount;
		add_changed_flag(static_cast<long>(GameObjectChangeType::Health));
	}

	virtual bool is_changed_position(float x, float y, float z) 
	{ 
		return (this->position_.x != x || this->position_.y != y || this->position_.z != z);
	}

	virtual bool is_changed() 
	{ 
		return get_changed_flag() != static_cast<long>(GameObjectChangeType::None)
			&& !is_input_locked_; // if input is locked, we don't want to change the position
	}
	virtual int health() { return health_; }

	// 마지막으로 데미지를 입힌 플레이어(킬러) ID
	long last_attacker_player_id() const { return last_attacker_player_id_; }

	virtual flatbuffers::Offset<syncnet::ActorInfo> get_actor_info(flatbuffers::FlatBufferBuilder& _fbb, long flag);

	bool is_input_locked() const { return is_input_locked_; }
	void set_input_locked(bool locked) 
	{ 
		is_input_locked_ = locked; 
		add_changed_flag(static_cast<long>(GameObjectChangeType::InputLocked));
	}

	float get_vecter2_x() const { return position_.get_vecter2_x(); }
	float get_vecter2_y() const { return position_.get_vecter2_y(); }

	virtual syncnet::GameObjectType type() { return game_object_type_; }

	virtual void SetState(syncnet::AIState state);

	virtual void set_changed(long flag);

public:
	int gridX = -1;
	int gridY = -1;
	float speed = 0.0f;
};

