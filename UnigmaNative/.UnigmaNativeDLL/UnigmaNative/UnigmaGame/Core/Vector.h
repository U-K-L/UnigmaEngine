#pragma once
#include "../glm/glm.hpp"
struct Vector3
{
	float x, y, z;

    Vector3 operator+(const Vector3& other) const {
        return { x + other.x, y + other.y, z + other.z };
    }

    Vector3 operator*(const Vector3& other) const {
        return { x * other.x, y * other.y, z * other.z };
    }

    Vector3 operator+(float value) const {
        return { x + value, y + value, z + value };
    }

    Vector3 operator*(float value) const {
        return { x * value, y * value, z * value };
    }

    glm::vec3 toGlmVec3() const {
        return glm::vec3(x, y, z);
    }
};