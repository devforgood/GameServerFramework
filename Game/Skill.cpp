#include "Skill.h"
#include "Actor.h"
#include "World.h"
#include "Vector3.h"
#include "LogHelper.h"

Skill::Skill()
	: actor_(nullptr), skill_id_(0), target_pos_(nullptr), is_casting_(false), duration_(0.0f)
{
}

Skill::~Skill()
{
	if(target_pos_)
	{
		delete target_pos_;
		target_pos_ = nullptr;
	}
}

