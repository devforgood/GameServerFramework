#pragma once

#include "syncnet_generated.h"

class Vector3Converter
{
private:
	float pos_[3];

public:
	Vector3Converter(const syncnet::Vec3* pos)
	{
		pos_[0] = pos->x() * -1;
		pos_[1] = pos->y();
		pos_[2] = pos->z();
	}

	float* pos()
	{
		return pos_;
	}
};