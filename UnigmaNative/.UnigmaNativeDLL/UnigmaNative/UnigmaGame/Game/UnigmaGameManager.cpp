#pragma once
#include "UnigmaGameManager.h"
#include "GlobalObjects.h"

UnigmaGameManager* UnigmaGameManager::instance = nullptr; // Define the static member

UnigmaGameManager::UnigmaGameManager()
{

}

void UnigmaGameManager::Create()
{
	//Create managers.
	SceneManager = new UnigmaSceneManager();
	RenderingManager = new UnigmaRenderingManager();

	std::cout << "UnigmaGameManager created" << std::endl;

	SceneManager->CreateScene("DefaultScene");

	IsCreated = true;
}

void UnigmaGameManager::Start()
{
	SceneManager->Start();
}

void UnigmaGameManager::Update()
{
	SceneManager->Update();
	RenderingManager->Update();
}

UnigmaGameManager::~UnigmaGameManager()
{
}

UNIGMANATIVE_API UnigmaGameObject* GetGameObject(uint32_t ID)
{
	return &GameObjects[ID];
	if(GameObjects[ID].isCreated)
	{
		return &GameObjects[ID];
	}
	else
	{
		return nullptr;
	}
}

// Function to get the size of the RenderingObjects vector
UNIGMANATIVE_API uint32_t GetRenderObjectsSize() {
    return UnigmaGameManager::instance->RenderingManager->RenderingObjects.size();
}

// Function to get an element from the RenderingObjects vector by index
UNIGMANATIVE_API UnigmaRenderingStruct* GetRenderObjectAt(uint32_t index) {
    auto& renderObjects = UnigmaGameManager::instance->RenderingManager->RenderingObjects;
    if (index < renderObjects.size()) {
        return &renderObjects[index];
    }
    else {
        return nullptr; // Return nullptr if index is out of bounds
    }
}
