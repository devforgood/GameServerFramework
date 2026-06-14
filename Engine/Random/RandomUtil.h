#pragma once
#include <boost/random.hpp>

class RandomUtil
{
private:
	boost::random::mt19937 rng_;

public:
	float GetRandomFloat(float min, float max);
	double GetRandomDouble(double min, double max);

};

