#pragma once
#include "Vector.h"
#include <thread>
#include <mutex>
struct PhysicsObject
{
    unsigned int objectId;
    Vector3 position;
    Vector3 velocity;
    Vector3 acceleration;
    float strength;
    float kelvin;
    float radius;
    int collisionPrimitivesStart;
    int collisionPrimitivesCount;
    glm::mat4 localToWorld;
};

struct CollisionPrimitive
{
    Vector3 position;
    Vector3 normal;
    Vector3 force;
};

std::thread PhysicsMainThread;

int* CollisionIndices;
int CollisionIndicesSize;
CollisionPrimitive* CollisionPrimitives;
int CollisionPrimitivesSize;
PhysicsObject* pObjs;
int pObjsSize;
void CaculatePhysicsForces();
void CaculateAcceleration(float deltaTime);
void CaculateVelocity(float deltaTime);
bool CheckObjectAgainstAllCollisions(PhysicsObject objectA);
bool TriangleTriangleIntersectionTest(int tri1Index, int tri2Index, const CollisionPrimitive* primitives, const PhysicsObject& object1, const PhysicsObject& object2);
glm::vec3 crossProduct(const glm::vec3& a, const glm::vec3& b);
bool CheckObjectCollisions(PhysicsObject objectA, PhysicsObject objectB);
bool TriangleCollision();

extern "C" {

	extern UNIGMANATIVE_API void* PhysicsObjects;
	extern UNIGMANATIVE_API void SetUpPhysicsBuffers(void* PhysicsObjectsArray, int PhysicsObjectsArraySize,
                                                     void* CollisionPrims,      int CollisionPrimsSize,
                                                     void* CollisionIndices,    int CollisionIndicesSize);
    extern UNIGMANATIVE_API Vector3 CheckObjectCollisionsTest(int objectAId);
}