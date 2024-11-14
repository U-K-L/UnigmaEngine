#include "UnigmaGameManager.h"


UnigmaGameObject GameObjects[MAX_NUM_GAMEOBJECTS];
UnigmaGameManager::UnigmaGameManager()
{
	//Set the first object name for testing. Its a char array so convert std string to char array
	std::string name = "TestObject";
	for (int i = 0; i < name.length(); i++)
	{
		GameObjects[0].name[i] = name[i];
	}
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