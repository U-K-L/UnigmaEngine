#include "pch.h"
#include "framework.h"
#include "UnigmaNative.h"
#include "UnigmaPhysics.h"
#include <iostream>
#include "glm/glm.hpp"
#include "Vector.h"

using namespace std;


UNIGMANATIVE_API void SetUpPhysicsArray(void* PhysicsObjectsArray, int size)
{
	//Get our physics objects.
	pObjs = (PhysicsObject*)PhysicsObjectsArray;
	pObjsSize = size;

	//Create a new thread.
	PhysicsMainThread = thread(CaculatePhysicsForces); //Begin calculation of physics forces.
	PhysicsMainThread.detach(); //Let it go and do its own thing.
}

void CaculatePhysicsForces()
{
    /*
	auto lastTime = chrono::high_resolution_clock::now();
	auto currentTime = chrono::high_resolution_clock::now();
	chrono::duration<float> elapsedTime = currentTime - lastTime;
	
	while (programRunning)
	{
		lastTime = currentTime; // Store the previous time before updating.
		currentTime = chrono::high_resolution_clock::now(); //Update current time.

		//Get the difference of the time to get delta.
		elapsedTime = currentTime - lastTime;
		float deltaTime = elapsedTime.count();

		//Perform physics calculations.
		CaculateAcceleration(deltaTime);
		CaculateVelocity(deltaTime);

		std::this_thread::sleep_for(std::chrono::milliseconds(32));
	}
    */
}

void CaculateAcceleration(float deltaTime)
{
	Vector3 gravity = { 0, -0.98f, 0 };
	for (int i = 0; i < pObjsSize; i++)
	{
		pObjs[i].acceleration = pObjs[i].acceleration + gravity * deltaTime;
	}
}

void CaculateVelocity(float deltaTime)
{
	for (int i = 0; i < pObjsSize; i++)
	{
		pObjs[i].velocity = pObjs[i].velocity + pObjs[i].acceleration * deltaTime;
	}
}

bool TriangleCollision()
{
    for (int i = 0; i < pObjsSize; i++)
    {
        PhysicsObject objectA = pObjs[i];

        for (int j = 0; j < pObjsSize; j++)
        {
            PhysicsObject objectB = pObjs[j];

            if (objectA.objectId == objectB.objectId)
                continue;
            if (CheckObjectCollisions(objectA, objectB))
                return true;
        }
    }

    return false;
}

bool CheckObjectCollisions(PhysicsObject objectA, PhysicsObject objectB)
{
    for (int i = objectA.collisionPrimitivesStart; i < objectA.collisionPrimitivesStart + objectA.collisionPrimitivesCount; i++)
    {

        for (int j = objectB.collisionPrimitivesStart; j < objectB.collisionPrimitivesStart + objectB.collisionPrimitivesCount; j++)
        {

            if (TriangleTriangleIntersectionTest(i, j, CollisionPrimitives))
                return true;
        }
    }

    return false;
}

glm::vec3 crossProduct(const glm::vec3& a, const glm::vec3& b) {
    return glm::cross(a, b);
}

bool TriangleTriangleIntersectionTest(int tri1Index, int tri2Index, const CollisionPrimitive* primitives) {
    // Assuming CollisionIndices contains the indices of the vertices, with three indices for each triangle
    glm::vec3 p1 = primitives[CollisionIndices[tri1Index * 3 + 0]].position.toGlmVec3();
    glm::vec3 p2 = primitives[CollisionIndices[tri1Index * 3 + 1]].position.toGlmVec3();
    glm::vec3 p3 = primitives[CollisionIndices[tri1Index * 3 + 2]].position.toGlmVec3();
    glm::vec3 q1 = primitives[CollisionIndices[tri2Index * 3 + 0]].position.toGlmVec3();
    glm::vec3 q2 = primitives[CollisionIndices[tri2Index * 3 + 1]].position.toGlmVec3();
    glm::vec3 q3 = primitives[CollisionIndices[tri2Index * 3 + 2]].position.toGlmVec3();

    glm::vec3 p1p2 = p2 - p1;
    glm::vec3 p2p3 = p3 - p2;
    glm::vec3 q1q2 = q2 - q1;
    glm::vec3 q2q3 = q3 - q2;

    glm::vec3 axes[] = {
        crossProduct(p1p2, q1q2),
        crossProduct(p1p2, q2q3),
        crossProduct(p2p3, q1q2),
        crossProduct(p2p3, q2q3),
        crossProduct(q1q2, p1p2),
        crossProduct(q1q2, p2p3)
    };

    for (int i = 0; i < 6; i++) {
        glm::vec3 axis = glm::normalize(axes[i]);
        float p1ProjMin = glm::dot(axis, p1);
        float p1ProjMax = p1ProjMin;

        float verticesProj[] = {
            glm::dot(axis, p2),
            glm::dot(axis, p3),
            glm::dot(axis, q1),
            glm::dot(axis, q2),
            glm::dot(axis, q3)
        };

        for (int j = 0; j < 5; j++) {
            p1ProjMin = std::min(p1ProjMin, verticesProj[j]);
            p1ProjMax = std::max(p1ProjMax, verticesProj[j]);
        }

        float q1ProjMin = glm::dot(axis, q1);
        float q1ProjMax = q1ProjMin;
        for (int j = 3; j < 5; j++) {
            q1ProjMin = std::min(q1ProjMin, verticesProj[j]);
            q1ProjMax = std::max(q1ProjMax, verticesProj[j]);
        }

        if (p1ProjMax < q1ProjMin || q1ProjMax < p1ProjMin)
            return false;
    }
    return true;
}


