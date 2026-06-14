#pragma once
#include <memory>
#include "syncnet_generated.h"

class GameObject;
class World;
class Map;
class Player;
class Actor;

class ActorFactory
{
public:
	static std::shared_ptr<Actor> CreateActor(Map* map, std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos);
};

