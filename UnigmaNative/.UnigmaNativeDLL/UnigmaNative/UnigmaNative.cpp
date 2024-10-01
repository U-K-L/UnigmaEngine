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

UNIGMANATIVE_API void StartProgram()
{
    DebugPrint("Native Plugin started, Unigma");
    programRunning = true;
    CreateDebugConsole();
    RedirectStandardIO();

}

UNIGMANATIVE_API void EndProgram()
{
    DebugPrint("Native Plugin ended, Unigma");
    programRunning = false;
    FreeDebugConsole();
}


// This is the constructor of a class that has been exported.
CUnigmaNative::CUnigmaNative()
{
    return;
}

void CreateDebugConsole()
{
    if (!AllocConsole())
    {
        // Handle error
        DWORD dwError = GetLastError();
        // You can choose to log this error or handle it as needed
    }
}

void FreeDebugConsole()
{
    FreeConsole();
}

void DebugPrint(const char* format, ...)
{
    va_list args;
    va_start(args, format);
    vprintf(format, args);
    va_end(args);
}

void RedirectStandardIO()
{
    FILE* fp;

    freopen_s(&fp, "CONIN$", "r", stdin);
    freopen_s(&fp, "CONOUT$", "w", stdout);
    freopen_s(&fp, "CONOUT$", "w", stderr);

    // Optional: Synchronize C++ streams with C I/O
    std::ios::sync_with_stdio();
}

