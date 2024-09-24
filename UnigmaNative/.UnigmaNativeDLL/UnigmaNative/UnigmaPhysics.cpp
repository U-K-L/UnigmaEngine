#include "pch.h"
#include "framework.h"
#include "UnigmaNative.h"
#include "UnigmaPhysics.h"
#include <iostream>
#include "glm/glm.hpp"
#include "Vector.h"
#include <thread>
#include <mutex>
using namespace std;

thread t1;
UNIGMANATIVE_API void SetUpPhysicsArray(void* PhysicsObjectsArray, int size)
{
	pObjs = (PhysicsObject*)PhysicsObjectsArray;
	pObjsSize = size;
	//Create a new thread.
	t1 = thread(CalculatePosition);
	t1.detach();
}

void CalculatePosition()
{
	while (programRunning)
	{
		for (int i = 0; i < pObjsSize; i++)
		{
			pObjs[i].position.y += -0.0125f;
		}
		std::this_thread::sleep_for(std::chrono::milliseconds(16));
	}
}