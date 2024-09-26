#include "pch.h"
#include "framework.h"
#include "UnigmaNative.h"
#include "UnigmaPhysics.h"
#include <iostream>
#include "glm/glm.hpp"
#include "Vector.h"

using namespace std;


UNIGMANATIVE_API void SetUpPhysicsBuffers(void* PhysicsObjectsArray, int PhysicsObjectsArraySize,
                                     void* collisionPrims, int collisionPrimsSize,
                                     void* collisionIndices, int collisionIndicesSize)
{
	//Get our physics objects.
	pObjs = (PhysicsObject*)PhysicsObjectsArray;
	pObjsSize = PhysicsObjectsArraySize;

    //Get collisionPrimitives.
    CollisionPrimitives = (CollisionPrimitive*)collisionPrims;
    CollisionPrimitivesSize = collisionPrimsSize;

    //Get indices.
    CollisionIndices = (int*)collisionIndices;
    CollisionIndicesSize = collisionIndicesSize;

	//Create a new thread.
	PhysicsMainThread = thread(CaculatePhysicsForces); //Begin calculation of physics forces.
	PhysicsMainThread.detach(); //Let it go and do its own thing.
}

UNIGMANATIVE_API Vector3 CheckObjectCollisionsTest(int objectAId)
{
    PhysicsObject objectA = pObjs[objectAId];
    for (int j = 0; j < pObjsSize; j++)
    {
        PhysicsObject objectB = pObjs[j];

        if (objectA.objectId == objectB.objectId)
            continue;
        if (CheckObjectCollisions(objectA, objectB))
            return objectA.position;
    }

    return {-12, -12, -12};
}

void CaculatePhysicsForces()
{
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
        bool didCollide = CheckObjectAgainstAllCollisions(pObjs[i]);
        if (didCollide)
            pObjs[i].velocity = pObjs[i].velocity + pObjs[i].acceleration * deltaTime;
        else
            pObjs[i].velocity = { 0,0,0 };
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

bool CheckObjectAgainstAllCollisions(PhysicsObject objectA)
{
    for (int j = 0; j < pObjsSize; j++)
    {
        PhysicsObject objectB = pObjs[j];

        if (objectA.objectId == objectB.objectId)
            continue;
        if (CheckObjectCollisions(objectA, objectB))
            return true;
    }
}

bool CheckObjectCollisions( PhysicsObject objectA,  PhysicsObject objectB)
{
    // Calculate the number of triangles for each object
    int triangleCountA = objectA.collisionPrimitivesCount / 3;
    int triangleCountB = objectB.collisionPrimitivesCount / 3;

    // Loop over the triangle numbers for objectA
    for (int aTri = 0; aTri < triangleCountA; aTri++)
    {
        // Loop over the triangle numbers for objectB
        for (int bTri = 0; bTri < triangleCountB; bTri++)
        {
            // Call TriangleTriangleIntersectionTest with triangle numbers
            if (TriangleTriangleIntersectionTest(aTri, bTri, CollisionPrimitives, objectA, objectB))
                return true;
        }
    }

    return false;
}


glm::vec3 crossProduct(const glm::vec3& a, const glm::vec3& b) {
    return glm::cross(a, b);
}

bool TriangleTriangleIntersectionTest(int tri1Index, int tri2Index, const CollisionPrimitive* primitives, const PhysicsObject& object1, const PhysicsObject& object2) {
    // Calculate the starting indices in CollisionIndices for each triangle
    int idx1 = object1.collisionPrimitivesStart + tri1Index * 3;
    int idx2 = object2.collisionPrimitivesStart + tri2Index * 3;

    // Access the indices of the vertices for each triangle
    int i1 = CollisionIndices[idx1 + 0];
    int i2 = CollisionIndices[idx1 + 1];
    int i3 = CollisionIndices[idx1 + 2];

    int j1 = CollisionIndices[idx2 + 0];
    int j2 = CollisionIndices[idx2 + 1];
    int j3 = CollisionIndices[idx2 + 2];

    // Transform the local positions to world space using localToWorld matrices
    glm::vec3 p1 = glm::vec3(object1.localToWorld * glm::vec4(primitives[i1].position.toGlmVec3(), 1.0f));
    glm::vec3 p2 = glm::vec3(object1.localToWorld * glm::vec4(primitives[i2].position.toGlmVec3(), 1.0f));
    glm::vec3 p3 = glm::vec3(object1.localToWorld * glm::vec4(primitives[i3].position.toGlmVec3(), 1.0f));

    glm::vec3 q1 = glm::vec3(object2.localToWorld * glm::vec4(primitives[j1].position.toGlmVec3(), 1.0f));
    glm::vec3 q2 = glm::vec3(object2.localToWorld * glm::vec4(primitives[j2].position.toGlmVec3(), 1.0f));
    glm::vec3 q3 = glm::vec3(object2.localToWorld * glm::vec4(primitives[j3].position.toGlmVec3(), 1.0f));

    // Compute edge vectors
    glm::vec3 p1p2 = p2 - p1;
    glm::vec3 p1p3 = p3 - p1;
    glm::vec3 q1q2 = q2 - q1;
    glm::vec3 q1q3 = q3 - q1;

    // Axes to test
    glm::vec3 axes[13];
    int axisCount = 0;

    // Triangle normals
    glm::vec3 n1 = glm::cross(p1p2, p1p3);
    glm::vec3 n2 = glm::cross(q1q2, q1q3);

    // Add triangle normals to axes
    axes[axisCount++] = n1;
    axes[axisCount++] = n2;

    // Edges of triangles
    glm::vec3 edges1[3] = { p1p2, p1p3, p3 - p2 };
    glm::vec3 edges2[3] = { q1q2, q1q3, q3 - q2 };

    // Compute cross products of edges to get axes
    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            glm::vec3 axis = glm::cross(edges1[i], edges2[j]);
            if (glm::length(axis) > 1e-6f) {
                axes[axisCount++] = axis;
            }
        }
    }

    // Perform SAT test
    for (int i = 0; i < axisCount; i++) {
        glm::vec3 axis = axes[i];

        // Skip near-zero axes
        if (glm::length(axis) < 1e-6f)
            continue;

        // Project triangle 1 onto axis
        float p1Proj = glm::dot(axis, p1);
        float p2Proj = glm::dot(axis, p2);
        float p3Proj = glm::dot(axis, p3);

        float pMin = glm::min(p1Proj, glm::min(p2Proj, p3Proj));
        float pMax = glm::max(p1Proj, glm::max(p2Proj, p3Proj));

        // Project triangle 2 onto axis
        float q1Proj = glm::dot(axis, q1);
        float q2Proj = glm::dot(axis, q2);
        float q3Proj = glm::dot(axis, q3);

        float qMin = glm::min(q1Proj, glm::min(q2Proj, q3Proj));
        float qMax = glm::max(q1Proj, glm::max(q2Proj, q3Proj));

        // Check for separation
        if (pMax < qMin || qMax < pMin)
            return false; // Separating axis found
    }

    return true; // No separating axis found; triangles intersect
}


