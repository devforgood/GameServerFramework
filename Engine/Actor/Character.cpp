#include "Character.h"
#include "Player.h"
#include "World.h"
#include "LogHelper.h"
#include "Skill.h"
#include "Vector3.h"
#include "Common.h"
#include "SkillFactory.h"
#include "Map.h"
#include "INavMovement.h"

Character::Character(Map* map) : Actor(map)
{
	// todo : load skills from DB table
	auto skills = ResourceLoader::Instance().GetSkills();
	for (const auto& skill : skills)
	{
		skills_[skill.first] = SkillFactory::Create(skill.first);
	}

	isInputLocked_ = false;
	gameObjectType_ = syncnet::GameObjectType::GameObjectType_Character;


	auto& entityManager = map_->systemManager_->GetEntityManager();
	entityManager.AddComponent(entityId_, engine::TimerComponent());
}

Character::~Character() 
{
	for (auto& skill : skills_)
	{
		delete skill.second;
	}
	skills_.clear();
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

void Character::use_skill(const syncnet::UseSkill* msg)
{
	float serverClientTimeOffset = map_->world_->timeStamp_->getServerClientTimeOffset(msg->timestamp());

	// Implement skill usage logic here
	LOG.info("Character {} using skill {} at position ({}, {}, {}) serverClientTimeOffset {} timestamp {}"
		, playerId_, msg->skillId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z(), serverClientTimeOffset, msg->timestamp());

	auto itr = skills_.find(msg->skillId());

	if (itr != skills_.end())
	{
		Skill* skill = itr->second;
		int result = skill->cast_skill(this, msg, serverClientTimeOffset);
		if (result == 0)
		{
			LOG.info("Skill {} cast successfully by character {}", msg->skillId(), playerId_);
		}
		else
		{
			LOG.error("Failed to cast skill {} by character {}. Error code: {}", msg->skillId(), playerId_, result);
		}

	}
	else
	{
		LOG.error("Skill {} not found for character {}", msg->skillId(), playerId_);
	}
}

void Character::Update(float dt)
{
	Actor::Update(dt);
	for(auto itr = skills_.begin(); itr != skills_.end(); ++itr)
	{
		Skill* skill = itr->second;
		skill->update(dt);
	}
}

bool Character::Init(Vector3& pos)
{
	// Initialize character-specific properties here
	LOG.info("Character {} initialized", playerId_);

	float speed = 4.5f;

	int agent_id = map_->GetNavMap()->AddAgent(Vector3(pos).pos(), speed);
	if (agent_id < 0)
	{
		LOG.error("OnAddAgent error in Map.addAgent()");
		return false;
	}

	if (map_->actorMap_.find(agent_id) != map_->actorMap_.end())
	{
		LOG.error("OnAddAgent error already exist in actorMap_");
		return false;
	}

	this->SetPosition(pos.x, pos.y, pos.z);
	this->actorId_ = agent_id;
	this->speed = speed;
	auto& entityManager = map_->systemManager_->GetEntityManager();
	entityManager.GetComponent<engine::StateComponent>(entityId_).ActorID = agent_id;

	return true;
}
