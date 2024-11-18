// UnigmaNative.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "UnigmaNative.h"
#include <iostream>
#include "../glm/glm.hpp"
#include "Vector.h"
#include "../Game/UnigmaGameManager.h"


UnigmaGameManager* GameManager;
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
    DebugPrint("Native Plugin started, Unigma\n");
    programRunning = true;

    GameManager = new UnigmaGameManager();
    UnigmaGameManager::SetInstance(GameManager);
    GameManager->Create();

    //UnigmaGameManager& GameManager = UnigmaGameManager::GetInstance();
    //CreateDebugConsole();
    //UnigmaThread* consoleThread = new UnigmaThread(CreateDebugConsole);
    //consoleThread->thread.detach(); // Detach the thread to run independently
    //RedirectStandardIO();

}

UNIGMANATIVE_API void EndProgram()
{
    DebugPrint("Native Plugin ended, Unigma");
    programRunning = false;
    //FreeDebugConsole();
}



// This is the constructor of a class that has been exported.
CUnigmaNative::CUnigmaNative()
{
    return;
}

// Declare the debug window thread function
DWORD WINAPI DebugWindowThread(LPVOID lpParam);

void CreateDebugConsole()
{
    // First, detach from any existing console
    FreeConsole();

    // Now, try to allocate a new console
    if (AllocConsole()) {
        // Redirect standard input, output, and error streams to the new console
        FILE* fp;
        freopen_s(&fp, "CONIN$", "r", stdin);
        freopen_s(&fp, "CONOUT$", "w", stdout);
        freopen_s(&fp, "CONOUT$", "w", stderr);

        // Set the title of the console window
        SetConsoleTitle(L"Debug Console");

        // Set console text color (white text on black background)
        HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
        if (hConsole != INVALID_HANDLE_VALUE) {
            SetConsoleTextAttribute(hConsole, FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE | FOREGROUND_INTENSITY);
        }

        std::ios::sync_with_stdio(); // Synchronize C++ streams with C I/O

        // Print a message to ensure the console is working
        std::cout << "Debug console started in a separate thread." << std::endl;

        // Keep the console open and responsive
        std::string input;
        while (programRunning) {

        }

        // Free the console when done
        FreeConsole();
    }
    else {
        DWORD dwError = GetLastError();
        std::cerr << "Failed to create debug console. Error: " << dwError << std::endl;
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

