#pragma once
#include <vector>
#include "syncnet_generated.h"


namespace gamedata
{
	struct Quest; // Forward declaration of gamedata::Quest
}

class Quest
{
public:
	const gamedata::Quest* gamedata; // Pointer to gamedata for Quest information
};

