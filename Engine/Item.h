#pragma once
#include <vector>
#include "syncnet_generated.h"


namespace gamedata
{
	class Item; // Forward declaration of gamedata::Item
}

class Item
{
public:
	const gamedata::Item* gamedata; // Pointer to gamedata for Item information
};
