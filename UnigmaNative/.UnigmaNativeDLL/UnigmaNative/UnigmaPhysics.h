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

PhysicsObject* pObjs;
int pObjsSize;
void CalculatePosition();

extern "C" {
	extern UNIGMANATIVE_API void* PhysicsObjects;
	extern UNIGMANATIVE_API void SetUpPhysicsArray(void* PhysicsObjectsArray, int size);
}