#include "Skill.h"
#include "Actor.h"
#include "World.h"
#include "Vector3.h"
#include "LogHelper.h"

Skill::Skill()
	: actor_(nullptr), skillId_(0), targetPos_(nullptr), isCasting_(false), duration_(0.0f)
{
}

Skill::~Skill()
{
	if(targetPos_)
	{
		delete targetPos_;
		targetPos_ = nullptr;
	}
}

