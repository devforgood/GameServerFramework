#include "GameObjectFactory.h"
#include "GameObject.h"
#include "Player.h"
#include "Character.h"
#include "Monster.h"
#include "World.h"
#include "LogHelper.h"
#include "Vector3.h"
#include "Common.h"

std::shared_ptr<GameObject> GameObjectFactory::CreateGameObject(World* world, std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	Vector3 target_pos(pos);
	std::shared_ptr<GameObject> game_object;
	switch (type)
	{
	case syncnet::GameObjectType::GameObjectType_Character:
	{
		// 이미 생성된 캐릭터가 있는지 확인하고, 있다면 해당 캐릭터를 반환하도록 수정 필요
		if (player->character() != nullptr)
		{
			LOG.error("OnAddAgent error: player already has a character");
			return nullptr;
		}

		std::shared_ptr<Character> character = std::make_shared<Character>(world);
		game_object = character;
		if (game_object->init(target_pos) == false)
		{
			LOG.error("OnAddAgent error in Character::init()");
			return nullptr;
		}
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