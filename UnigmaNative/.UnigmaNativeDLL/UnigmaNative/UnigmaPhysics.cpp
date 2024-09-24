#include "pch.h"
#include "framework.h"
#include "UnigmaNative.h"
#include "UnigmaPhysics.h"
#include <iostream>
#include "glm/glm.hpp"
#include "Vector.h"

UNIGMANATIVE_API Vector3 GetPhysicsPosition(void* PhysicsObjectsArray, int size)
{
	PhysicsObject* pObjs = (PhysicsObject*)PhysicsObjectsArray;



	for (int i = 0; i < size; i++)
	{
		glm::vec3 currentPos = glm::vec3(pObjs[i].position.x, pObjs[i].position.y, pObjs[i].position.z);
		glm::vec3 newPos = currentPos + glm::vec3(0, -2, 0);
		pObjs[i].position = { newPos.x, newPos.y, newPos.z};
	}

	Vector3 position = pObjs[0].position;

	return position;
}