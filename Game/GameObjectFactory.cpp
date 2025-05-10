#include "GameObjectFactory.h"
#include "GameObject.h"
#include "Player.h"
#include "Character.h"
#include "Monster.h"
#include "World.h"
#include "LogHelper.h"
#include "GridManager.h"
#include "Vector3.h"

std::shared_ptr<GameObject> GameObjectFactory::CreateGameObject(World* world, std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	Vector3 target_pos(pos);
	std::shared_ptr<GameObject> game_object;
	switch (type)
	{
	case syncnet::GameObjectType::GameObjectType_Character:
	{
		auto itr = world->players_.find(player->player_id());
		if (itr != world->players_.end())
		{
			LOG.info("OnAddAgent already exist in players_");

			itr->second->switch_session(player);
			return nullptr;
		}

		std::shared_ptr<Character> character = std::make_shared<Character>(world);
		game_object = character;
		if (game_object->init(target_pos) == false)
		{
			LOG.error("OnAddAgent error in Character::init()");
			return nullptr;
		}
		world->players_.insert(std::make_pair(player->player_id(), player));
		player->possess(character);
		world->grid_manager_->add(character.get());
		break;
	}
	case syncnet::GameObjectType::GameObjectType_Monster:
		game_object = std::make_shared<Monster>(world);
		if (game_object->init(target_pos) == false)
		{
			LOG.error("OnAddAgent error in Monster::init()");
			return nullptr;
		}
		world->grid_manager_->add((Actor*)game_object.get());
		break;
	}


	return game_object;
}