// UnigmaNative.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "UnigmaNative.h"
#include <iostream>
#include "glm/glm.hpp"
#include "Vector.h"

bool programRunning = true;
// This is an example of an exported variable
DLLEXPORT UNIGMANATIVE_API int nUnigmaNative=0;

// This is an example of an exported function.
DLLEXPORT UNIGMANATIVE_API int fnUnigmaNative(void)
{
    return 0;
}


DLLEXPORT UNIGMANATIVE_API Vector3 GetSquared(void* x)
{
    Vector3* vectors = (Vector3*)x;
    Vector3 vec = vectors[2];//*(x + vec3Size * 2);

    glm::vec3 dotProd(vec.x, vec.y, vec.z);
    float dotResult = glm::dot(dotProd, dotProd);

    Vector3 result = { dotResult,dotResult,dotResult };

    return result;
}


UNIGMANATIVE_API void EndProgram()
{
    programRunning = false;
}


// This is the constructor of a class that has been exported.
CUnigmaNative::CUnigmaNative()
{
    return;
}

