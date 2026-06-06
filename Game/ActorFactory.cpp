#include "ActorFactory.h"
#include "GameObject.h"
#include "Actor.h"
#include "Player.h"
#include "Character.h"
#include "Monster.h"
#include "World.h"
#include "LogHelper.h"
#include "Vector3.h"
#include "Common.h"
#include "Map.h"
#include "../Engine/GridManager.h"

std::shared_ptr<Actor> ActorFactory::CreateActor(Map* map, std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	Vector3 target_pos(pos);
	std::shared_ptr<Actor> actor;

	switch (type)
	{
	case syncnet::GameObjectType::GameObjectType_Character:
		actor = std::make_shared<Character>(map);
		break;
	case syncnet::GameObjectType::GameObjectType_Monster:
		actor = std::make_shared<Monster>(map);
		break;
	default:
		LOG.error("OnAddAgent error unsupported GameObjectType {}", static_cast<int>(type));
		return nullptr;
	}

	if (actor->pre_create(player) == false)
	{
		LOG.error("OnAddAgent error in Actor::before_create()");
		return nullptr;
	}

	if (actor->init(target_pos) == false)
	{
		LOG.error("OnAddAgent error in Actor::init()");
		return nullptr;
	}

	if (actor->post_create(player, actor) == false)
	{
		LOG.error("OnAddAgent error in Actor::after_create()");
		return nullptr;
	}

	map->grid_manager_->add(actor.get());

	return actor;
}
