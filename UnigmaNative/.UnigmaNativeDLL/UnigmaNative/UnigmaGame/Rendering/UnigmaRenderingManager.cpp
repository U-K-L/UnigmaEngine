#include "UnigmaRenderingManager.h"

UnigmaRenderingManager::UnigmaRenderingManager()
{

}

UnigmaRenderingManager::~UnigmaRenderingManager()
{
}

void UnigmaRenderingManager::Update()
{
}

void UnigmaRenderingManager::Start()
{
}

void UnigmaRenderingManager::CreateRenderingObject(UnigmaGameObject& gameObject)
{
	UnigmaRenderingStruct renderingObject = UnigmaRenderingStruct();
	gameObject.RenderID = RenderingObjects.size();
	RenderingObjects.push_back(renderingObject);
}
