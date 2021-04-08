#pragma once
#include "Map.h"

class World
{
private:

	Map* m_map;

public:

	void Init();
	void update(float deltaTime);

	Map* map() { return m_map; }
};

