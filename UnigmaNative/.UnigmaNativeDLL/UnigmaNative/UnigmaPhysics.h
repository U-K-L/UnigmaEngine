#pragma once
#include "Vector.h"
struct PhysicsObject
{
    unsigned int objectId;
    Vector3 position;
    Vector3 velocity;
    Vector3 acceleration;
    float strength;
    float kelvin;
    float radius;

};