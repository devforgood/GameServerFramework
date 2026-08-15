#pragma once
#include <vector>
#include "syncnet_generated.h"


namespace gamedata
{
	struct Dialog; // Forward declaration of gamedata::Dialog
}

class Dialog
{
public:
	const gamedata::Dialog* gamedata; // Pointer to gamedata for Dialog information
};
