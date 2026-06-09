#include "Vector3.h"
#include "syncnet_generated.h"

std::shared_ptr<syncnet::Vec3> Vector3::of(const float* pos)
{
    return std::make_shared<syncnet::Vec3>(convert_x(pos[0]), convert_y(pos[1]), convert_z(pos[2]));
}