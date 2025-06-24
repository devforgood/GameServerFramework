#include "RandomUtil.h"

float RandomUtil::GetRandomFloat(float min, float max) {
    boost::random::uniform_real_distribution<float> dist(min, max);
    return dist(rng_);
}

double RandomUtil::GetRandomDouble(double min, double max) {
    boost::random::uniform_real_distribution<double> dist(min, max);
    return dist(rng_);
}