#pragma once
#include <cmath>

inline float ManhattanDistance(const float* v1, const float* v2)
{
	return abs(v2[0] - v1[0]) + abs(v2[1] - v1[1]) + abs(v2[2] - v1[2]);
}