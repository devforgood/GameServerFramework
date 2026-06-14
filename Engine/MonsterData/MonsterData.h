#pragma once
#include <vector>
#include "syncnet_generated.h"


namespace gamedata
{
	struct MonsterData; // Forward declaration of gamedata::MonsterData
}

class MonsterData
{
public:
	const gamedata::MonsterData* gamedata; // Pointer to gamedata for MonsterData information
};
