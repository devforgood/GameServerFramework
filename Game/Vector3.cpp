#include "Vector3.h"
#include "syncnet_generated.h"

float* Vector3::pos()
{
    static float pos_[3];
    pos_[0] = x;
    pos_[1] = y;
    pos_[2] = z;
    return pos_;
}

std::shared_ptr<syncnet::Vec3> Vector3::of(const float* pos)
{
    return std::make_shared<syncnet::Vec3>(convert_x(pos[0]), convert_y(pos[1]), convert_z(pos[2]));
} 