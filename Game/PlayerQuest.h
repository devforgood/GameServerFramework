#pragma once
#include "Component.h"

class PlayerQuest : public ComponentBase<PlayerQuest>
{
	virtual void save(std::any data) override;
};

