#pragma once
#include "Map.h"

class game_session;
class Monster;
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
};

