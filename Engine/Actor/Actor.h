#pragma once
#include "GameObject.h"
#include "SendMessage.h"
#include "Vector3.h"
#include "IGridActor.h"

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
	int actorId_ = -1;
	bool isInputLocked_ = false;
	Vector3 frontVector_;    // 캐릭터가 바라보는 방향
	float rotationSpeed_ = 5.0f; // 회전 속도 (초당 회전 각도)
	Vector3 position_;        // 위치 정보를 protected로 이동

	// 전투 스탯. 전부 데이터에서 온다 —
	//   캐릭터: level.json 의 해당 레벨 행(hp/attack/defense)
	//   몬스터/NPC: monster.json / npc.json 의 행
	// 데이터가 없으면 아래 기본값으로 남는다(레벨 1 캐릭터와 같은 수준).
	int health_ = 100;
	int maxHealth_ = 100;
	int attack_ = 10;
	int defense_ = 0;

	int lastAttackerActorId_ = -1; // 마지막으로 데미지를 입힌 액터 ID (킬 판정용)
	syncnet::GameObjectType gameObjectType_;
	int32_t entityId_ = -1; // 엔티티 ID (필요시 사용)

	Map* map_;
	syncnet::AIState state_;
	long changeFlag_;

public:
	Actor(Map* map) : map_(map), frontVector_(0, 0, 1)  // 초기 방향은 z축 양의 방향
	{
		Init();
	}

	virtual ~Actor() 
	{
		Clear();
	}

	bool IsChangedFlag(long flag) { return (GetChangedFlag() & flag) != 0; }
	bool IsChangedFlag(long myself_flag, long flag) { return  (myself_flag & flag) != 0; }
	virtual void ResetChangedFlag() { SetChangedFlag(static_cast<long>(GameObjectChangeType::None)); }
	long GetChangedFlag() { return changeFlag_; }
	void AddChangedFlag(long flag) { SetChangedFlag(GetChangedFlag() | flag); }

	virtual bool PreCreate(std::shared_ptr<Player> player) { return true; }
	virtual bool PostCreate(std::shared_ptr<Player> player, std::shared_ptr<GameObject> game_object) { return true; }

	Map* GetMap() { return map_; }
	syncnet::AIState GetState() { return state_; }

	void Init();
	virtual bool Init(Vector3& pos) { return false; }
	void Clear();

	virtual void Update(float dt) override;

	int GetActorId() { return actorId_; }

	virtual void SetPosition(float x, float y, float z);

	virtual bool IsCharacter() const
	{
		return gameObjectType_ == syncnet::GameObjectType::GameObjectType_Character;
	}

	// 기본적으로 플레이어 캐릭터만 몬스터의 표적이 된다. 호위 NPC 처럼 캐릭터가 아니면서
	// 사냥당해야 하는 액터가 이것만 따로 켠다(IGridActor 의 설명 참고).
	virtual bool IsMonsterTarget() const override
	{
		return IsCharacter();
	}

	virtual void SetGridX(int gridX)
	{
		this->gridX = gridX; 
	}
	virtual void SetGridY(int gridY)
	{
		this->gridY = gridY; 
	}
	virtual int GetGridX() const
	{ 
		return gridX; 
	}
	virtual int GetGridY() const
		{
		return gridY;
	}
	virtual void SetGridSlot(int slot) override { gridSlot = slot; }
	virtual int GetGridSlot() const override { return gridSlot; }
	virtual float GetVector2X() const
	{
		return GetVecter2X();
	}
	virtual float GetVector2Y() const
	{
		return GetVecter2Y();
	}

	virtual int GetActorId() const override
	{
		return actorId_;
	}

	virtual void SetLastAttacker(int attacker_actor_id) override
	{
		lastAttackerActorId_ = attacker_actor_id;
	}

	// 위치 얻기
	const Vector3& GetPosition() const { return position_; }

	// front vector 관련 메서드
	const Vector3& GetFrontVector() const { return frontVector_; }
	void SetFrontVector(const Vector3& front) { frontVector_ = front.normalized(); }
	void SetFrontVector(float x, float y, float z) { frontVector_ = Vector3(x, y, z).normalized(); }

	// front vector를 각도로 변환 (도 단위, 0도는 동쪽, 90도는 북쪽)
	float GetFrontAngleDegrees() const
	{
		// front vector를 2D 평면에서 각도로 변환 (표준 수학 좌표계)
		// 표준 수학 좌표계: atan2(y, x) 사용 (0도는 동쪽, 90도는 북쪽)
		float angle_rad = std::atan2(frontVector_.z, frontVector_.x);
		float angle_deg = angle_rad * 180.0f / 3.14159f;
		
		// 각도를 0-360도 범위로 정규화
		if (angle_deg < 0) {
			angle_deg += 360.0f;
		}
		
		return angle_deg;
	}

	// 목표 지점을 향해 front vector를 회전
	void RotateToTarget(const Vector3& target_pos, float dt)
	{
		Vector3 to_target = (target_pos - GetPosition()).normalized();
		to_target.y = 0; // y축 회전만 고려
		to_target = to_target.normalized();

		// 현재 front vector와 목표 방향 사이의 각도 계산
		float dot = frontVector_.dot(to_target);
		float angle = std::acos(dot);

		// 회전 방향 결정 (외적 사용)
		Vector3 cross = frontVector_.cross(to_target);
		float rotation_direction = (cross.y > 0) ? 1.0f : -1.0f;

		// 부드러운 회전 적용
		float rotation_amount = rotationSpeed_ * dt;
		if (angle > rotation_amount)
		{
			// 현재 front vector를 rotation_amount만큼 회전
			float cos_theta = std::cos(rotation_amount * rotation_direction);
			float sin_theta = std::sin(rotation_amount * rotation_direction);
			float new_x = frontVector_.x * cos_theta - frontVector_.z * sin_theta;
			float new_z = frontVector_.x * sin_theta + frontVector_.z * cos_theta;
			frontVector_ = Vector3(new_x, 0, new_z).normalized();
		}
		else
		{
			// 목표 방향에 도달
			frontVector_ = to_target;
		}
	}

	// 회복은 최대 체력을 넘지 않는다.
	virtual void IncrementHealth(int amount)
	{
		health_ += amount;
		if (health_ > maxHealth_)
			health_ = maxHealth_;
		AddChangedFlag(static_cast<long>(GameObjectChangeType::Health));
	}

	// 체력은 0 아래로 내려가지 않는다. 예전에는 음수로 계속 내려가서,
	// 사망 후 회복하면 0 에 닿기까지 여러 번 맞아야 했다.
	virtual void DecrementHealth(int amount)
	{
		health_ -= amount;
		if (health_ < 0)
			health_ = 0;
		AddChangedFlag(static_cast<long>(GameObjectChangeType::Health));
	}

	virtual bool IsChangedPosition(float x, float y, float z) 
	{ 
		return (this->position_.x != x || this->position_.y != y || this->position_.z != z);
	}

	virtual bool IsChanged() 
	{ 
		return GetChangedFlag() != static_cast<long>(GameObjectChangeType::None)
			&& !isInputLocked_; // if input is locked, we don't want to change the position
	}
	virtual int GetHealth() { return health_; }

	virtual void SetHealth(int health)
	{
		health_ = health;
		AddChangedFlag(static_cast<long>(GameObjectChangeType::Health));
	}

	// --- 전투 스탯 ---

	int GetMaxHealth() const { return maxHealth_; }
	int GetAttack() const { return attack_; }
	int GetDefense() const override { return defense_; }

	bool IsDead() const { return health_ <= 0; }

	// 스탯을 한 번에 적용한다(데이터 로드/레벨업 시점).
	// resetHealth 가 true 면 체력을 최대치로 채운다(스폰/부활).
	// false 면 현재 체력을 유지하되 새 최대치를 넘지 않게 자른다(레벨업).
	void SetCombatStats(int maxHealth, int attack, int defense, bool resetHealth)
	{
		maxHealth_ = maxHealth > 0 ? maxHealth : 1;
		attack_ = attack;
		defense_ = defense;

		if (resetHealth)
			SetHealth(maxHealth_);
		else if (health_ > maxHealth_)
			SetHealth(maxHealth_);
	}

	// 마지막으로 데미지를 입힌 액터(킬러) ID
	int GetLastAttackerActorId() const { return lastAttackerActorId_; }

	virtual flatbuffers::Offset<syncnet::ActorInfo> GetActorInfo(flatbuffers::FlatBufferBuilder& _fbb, long flag);

	bool IsInputLocked() const { return isInputLocked_; }
	void SetInputLocked(bool locked) 
	{ 
		isInputLocked_ = locked; 
		AddChangedFlag(static_cast<long>(GameObjectChangeType::InputLocked));
	}

	float GetVecter2X() const { return position_.get_vecter2_x(); }
	float GetVecter2Y() const { return position_.get_vecter2_y(); }

	virtual syncnet::GameObjectType GetType() { return gameObjectType_; }

	virtual void SetState(syncnet::AIState state);

	virtual void SetChangedFlag(long flag);

public:
	int gridX = -1;
	int gridY = -1;
	int gridSlot = -1; // 소속 셀 vector 안의 인덱스(GridManager 전용)
	float speed = 0.0f;
};

