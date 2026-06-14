#pragma once
#include <vector>
#include "syncnet_generated.h"


namespace gamedata
{
	struct Level; // Forward declaration of gamedata::Level
}

class Level
{
public:
	const gamedata::Level* gamedata; // Pointer to gamedata for Level information
};
