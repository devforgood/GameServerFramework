#include "BaseSkill.h"
#include "Actor.h"
#include "World.h"
#include "Vector3.h"
#include "LogHelper.h"

BaseSkill::BaseSkill()
	: actor_(nullptr), skill_id_(0), target_pos_(nullptr), is_casting_(false), duration_(0.0f)
{
}

BaseSkill::~BaseSkill()
{
	if(target_pos_)
	{
		delete target_pos_;
		target_pos_ = nullptr;
	}
}

