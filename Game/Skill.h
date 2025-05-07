#pragma once
#include <vector>
#include "syncnet_generated.h"

enum skillProperties
{
	SKILL_NONE = 0,
	SKILL_JUMP = 1,
};

class Actor; // Forward declaration
class Skill
{
private:
	int skill_id_;
	std::vector<int> skill_properties_;

public:

	int cast_skill(Actor * actor, int skill_id, const syncnet::Vec3* target_pos);
};

