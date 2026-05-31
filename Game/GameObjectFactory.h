#pragma once
#include <memory>
#include "syncnet_generated.h"

class GameObject;
class World;
class Map;
class Player;

class GameObjectFactory
{
public:
	static std::shared_ptr<GameObject> CreateGameObject(Map* map, std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos);
};

