#pragma once
#include "Component.h"

class PlayerQuest : public ComponentBase<PlayerQuest>
{
	virtual void Save(std::any data) override;
};

