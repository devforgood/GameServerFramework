#pragma once
#include <vector>
#include "Map.h"

class game_session;
class Monster;
namespace syncnet
{
	struct Vec3;
}

class World
{
private:

	Map* map_;
	Monster* monster_;
public:

	void Init();
	void update(float deltaTime);

	Map* map() { return map_; }

	void SendWorldState(game_session* session);


	std::vector<syncnet::Vec3> raycasts;

};

