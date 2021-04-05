#include "World.h"

void World::Init()
{
	m_map = new Map();
	m_map->Init();

}

void World::update(float deltaTime)
{
	m_map->update(deltaTime);
}
