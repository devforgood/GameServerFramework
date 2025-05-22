#pragma once

#include "syncnet_generated.h"

class Vector3
{
private:
	float pos_[3];

public:
	Vector3(const syncnet::Vec3* pos)
	{
		pos_[0] = pos->x() * -1;
		pos_[1] = pos->y();
		pos_[2] = pos->z();
	}

	float* pos()
	{
		return pos_;
	}

	float x() { return pos_[0]; }
	float y() { return pos_[1]; }
	float z() { return pos_[2]; }

	static std::shared_ptr<syncnet::Vec3> of(const float* pos)
	{
		return std::make_shared< syncnet::Vec3>(pos[0] * -1, pos[1], pos[2]);
	}
};