#include "UnigmaGameManager.h"
#include "GlobalObjects.h"
#include "UnigmaScene.h"



UnigmaGameManager::UnigmaGameManager()
{
	/*
	//Set the first object name for testing. Its a char array so convert std string to char array
	std::string name = "TestObject";
	for (int i = 0; i < name.length(); i++)
	{
		GameObjects[0].name[i] = name[i];
	}
	*/

	UnigmaScene scene = UnigmaScene();

	scene.CreateScene();

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