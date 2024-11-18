#pragma once
#include <iostream>
#include "UnigmaGameObject.h"
#include "../Core/UnigmaNative.h"
#include "UnigmaSceneManager.h"
#include "../Rendering/UnigmaRenderingManager.h"

// Function declarations.
void AddGameObject(UnigmaGameObject gameObject);
void RemoveGameObject(uint32_t ID);

class UnigmaGameManager
{
public:
    static UnigmaGameManager* instance;

    static void SetInstance(UnigmaGameManager* app)
    {
        std::cout << "Setting Game Manager instance" << std::endl;
        instance = app;
    }

    // Delete copy constructor and assignment operator to prevent copies.
    UnigmaGameManager(const UnigmaGameManager&) = delete;
    UnigmaGameManager& operator=(const UnigmaGameManager&) = delete;

    void Update();
    void Start();
    void Create();

    UnigmaRenderingManager* RenderingManager;
    UnigmaSceneManager* SceneManager;
    UnigmaGameManager();

    ~UnigmaGameManager();

    bool IsCreated;
};

extern "C" {
    UNIGMANATIVE_API UnigmaGameObject* GetGameObject(uint32_t ID);

    // Function to get the size of the RenderingObjects vector
    UNIGMANATIVE_API uint32_t GetRenderObjectsSize();

    // Function to get an element from the RenderingObjects vector by index
    UNIGMANATIVE_API UnigmaRenderingStruct* GetRenderObjectAt(uint32_t index);
}
