#pragma once
#include <cmath>
#include <memory>
#include "syncnet_generated.h"

class Vector3
{
public:
	float x;
	float y;
	float z;

	// 생성자
	Vector3() : x(0), y(0), z(0) {}
	Vector3(float x, float y, float z) : x(x), y(y), z(z) {}
	Vector3(const Vector3& other) : x(other.x), y(other.y), z(other.z) {}
	Vector3(const syncnet::Vec3* vec3) : x(convert_x(vec3->x())), y(convert_y(vec3->y())), z(convert_z(vec3->z())) {}

	// 연산자 오버로딩
	Vector3 operator+(const Vector3& other) const {
		return Vector3(x + other.x, y + other.y, z + other.z);
	}

	Vector3 operator-(const Vector3& other) const {
		return Vector3(x - other.x, y - other.y, z - other.z);
	}

	Vector3 operator*(float scalar) const {
		return Vector3(x * scalar, y * scalar, z * scalar);
	}

	Vector3 operator/(float scalar) const {
		if (scalar != 0) {
			return Vector3(x / scalar, y / scalar, z / scalar);
		}
		return *this;
	}

	// 벡터의 길이
	float length() const {
		return std::sqrt(x * x + y * y + z * z);
	}

	// 정규화된 벡터 반환
	Vector3 normalized() const {
		float len = length();
		if (len > 0.0001f) {
			return *this / len;
		}
		return *this;
	}

	// 내적
	float dot(const Vector3& other) const {
		return x * other.x + y * other.y + z * other.z;
	}

	// 외적
	Vector3 cross(const Vector3& other) const {
		return Vector3(
			y * other.z - z * other.y,
			z * other.x - x * other.z,
			x * other.y - y * other.x
		);
	}

	// 좌표계 변환 메서드들
	static float convert_x(float x) { return x * -1; }  // 클라이언트와 서버 간의 x축 방향이 반대
	static float convert_y(float y) { return y; }
	static float convert_z(float z) { return z; }

	// 정적 상수 벡터들
	static const Vector3 zero() { return Vector3(0, 0, 0); }
	static const Vector3 one() { return Vector3(1, 1, 1); }
	static const Vector3 up() { return Vector3(0, 1, 0); }
	static const Vector3 down() { return Vector3(0, -1, 0); }
	static const Vector3 right() { return Vector3(1, 0, 0); }
	static const Vector3 left() { return Vector3(-1, 0, 0); }
	static const Vector3 forward() { return Vector3(0, 0, 1); }
	static const Vector3 back() { return Vector3(0, 0, -1); }

	float* pos()
	{
		return &x;
	}

	static std::shared_ptr<syncnet::Vec3> of(const float* pos);

	float convert_x() const { return convert_x(x); }
	float convert_y() const { return convert_y(y); }
	float convert_z() const { return convert_z(z); }

	float get_vecter2_x() const { return x; }
	float get_vecter2_y() const { return z; }
	void set(float x, float y, float z) {
		this->x = x;
		this->y = y;
		this->z = z;
	}

	static bool equal(float x1, float y1, float z1, float x2, float y2, float z2) 
	{
		return x1 == x2 && y1 == y2 && z1 == z2;
	}
};