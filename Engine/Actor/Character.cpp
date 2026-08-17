#include "Character.h"
#include "Player.h"
#include "World.h"
#include "LogHelper.h"
#include "Skill.h"
#include "Vector3.h"
#include "Common.h"
#include "Map.h"
#include "INavMovement.h"

Character::Character(Map* map) : Actor(map)
{
	// 스킬은 여기서 채우지 않는다. 플레이어가 실제로 배운 목록(PlayerSkill)을
	// Player::Possess 가 실어 준다 — 예전에는 여기서 전체 스킬 테이블을 등록해
	// 아무나 모든 스킬을 쓸 수 있었다.
	isInputLocked_ = false;
	gameObjectType_ = syncnet::GameObjectType::GameObjectType_Character;


	auto& entityManager = map_->systemManager_->GetEntityManager();
	entityManager.AddComponent(entityId_, engine::TimerComponent());
}

Character::~Character()
{
	LOG.info("Character {} destroyed", playerId_);
}

bool Character::PreCreate(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("OnAddAgent error: player is nullptr");
		return false;
	}

	if (player->GetCharacter() != nullptr)
	{
		LOG.error("OnAddAgent error: player already has a character");
		return false;
	}

	return true;
}

bool Character::PostCreate(std::shared_ptr<Player> player, std::shared_ptr<GameObject> game_object)
{
	auto character = std::dynamic_pointer_cast<Character>(game_object);
	if (player == nullptr || character == nullptr)
	{
		LOG.error("OnAddAgent error in Character::after_create()");
		return false;
	}

	player->Possess(character);
	return true;
}

CastResult Character::use_skill(const syncnet::UseSkill* msg)
{
	if (msg == nullptr || msg->pos() == nullptr)
		return CastResult::CasterInvalid;

	float serverClientTimeOffset = map_->world_->timeStamp_->getServerClientTimeOffset(msg->timestamp());

	LOG.info("Character {} using skill {} at position ({}, {}, {}) serverClientTimeOffset {} timestamp {}"
		, playerId_, msg->skillId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z(), serverClientTimeOffset, msg->timestamp());

	CastContext ctx;
	ctx.skillId = msg->skillId();
	ctx.targetActorId = msg->targetId();
	ctx.targetPos = Vector3(msg->pos());
	ctx.clientDuration = static_cast<float>(msg->duration());
	ctx.serverClientTimeOffset = serverClientTimeOffset;

	CastResult result = skillSet_.TryCast(this, ctx);
	if (result == CastResult::Success)
	{
		LOG.info("Skill {} cast successfully by character {}", msg->skillId(), playerId_);
	}
	else
	{
		LOG.debug("Skill {} cast rejected for character {}. Reason code: {}",
			msg->skillId(), playerId_, static_cast<int>(result));
	}

	return result;
}

void Character::Update(float dt)
{
	Actor::Update(dt);
	skillSet_.Update(this, dt);
}

bool Character::Init(Vector3& pos)
{
	// Initialize character-specific properties here
	LOG.info("Character {} initialized", playerId_);

	float speed = 4.5f;

	int actor_id = map_->GetNavMap()->AddAgent(Vector3(pos).pos(), speed);
	if (actor_id < 0)
	{
		LOG.error("OnAddAgent error in Map.addAgent()");
		return false;
	}

	if (map_->actorMap_.find(actor_id) != map_->actorMap_.end())
	{
		LOG.error("OnAddAgent error already exist in actorMap_");
		return false;
	}

	this->SetPosition(pos.x, pos.y, pos.z);
	this->actorId_ = actor_id;
	this->speed = speed;
	auto& entityManager = map_->systemManager_->GetEntityManager();
	entityManager.GetComponent<engine::StateComponent>(entityId_).ActorID = actor_id;

	return true;
}
