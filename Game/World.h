#pragma once
#include "Map.h"

class game_session;
class World
{
private:

	Map* m_map;

public:

	void Init();
	void update(float deltaTime);

	Map* map() { return m_map; }

	void SendWorldState(game_session* session);
};

