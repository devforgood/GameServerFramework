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
		return std::make_shared< syncnet::Vec3>(convert_x(pos[0]), convert_y(pos[1]), convert_z(pos[2]));
	}

	static float convert_x(float x)
	{
		return x * -1;
	}
	static float convert_y(float y)
	{
		return y;
	}
	static float convert_z(float z)
	{
		return z;
	}

};