#include "UnigmaScene.h"
#include "../glm/glm.hpp"
#include "GlobalObjects.h"
#include "UnigmaGameManager.h"

UnigmaScene::UnigmaScene()
{
}

UnigmaScene::~UnigmaScene()
{
}

void UnigmaScene::Update()
{

	/*
	//Make objects in scene spin in a circle.
	//Get chrono time.
	auto currentTime = std::chrono::high_resolution_clock::now();
	auto duration = currentTime.time_since_epoch();
	auto millis = std::chrono::duration_cast<std::chrono::milliseconds>(duration).count();

	//Get the sin and cos of the time.
	float sinTime = sin(millis);
	float cosTime = cos(millis);

	//Update all game objects in the scene.
	for(auto gameObjectIndex = GameObjectsIndex.begin(); gameObjectIndex != GameObjectsIndex.end(); gameObjectIndex++)
	{
		//Get the game object.
		UnigmaGameObject gameObject = GameObjects[*gameObjectIndex];

		//Update the game object.
		gameObject.transform.position += 250.0f*glm::vec3(sinTime*0.0001, cosTime*0.0001, 0);

		//Update the game object in the global array.
		GameObjects[*gameObjectIndex] = gameObject;
	}
	*/
}

void UnigmaScene::Start()
{
}

void UnigmaScene::AddGameObject(UnigmaGameObject& gameObject)
{
	//Add the game object to Game Manager global GameObjects array. And ensure proper indexing.
	gameObject.ID = GameObjectsIndex.size();
	GameObjectsIndex.push_back(gameObject.ID);
	GameObjects[gameObject.ID] = gameObject;
}

void UnigmaScene::CreateScene()
{
	/* Faux scene layout.
	* GameObject: Name: Kanaloa, Position: (0, 0, 0)
	* GameObject: Name: Sunny, Position: (1, 1, 1)
	* 
	* 
	*/

	UnigmaGameObject Kanaloa = UnigmaGameObject();
	strcpy(Kanaloa.name, "Kanaloa");
	Kanaloa.transform.position = glm::vec3(0, 0, 0);

	UnigmaGameObject Sunny = UnigmaGameObject();
	strcpy(Sunny.name, "Sunny");
	Sunny.transform.position = glm::vec3(1, 1, 1);

	AddGameObject(Kanaloa);
	AddGameObject(Sunny);

	//Create Rendering Objects as well.
	UnigmaGameManager* gameManager = UnigmaGameManager::instance;

	gameManager->RenderingManager->CreateRenderingObject(Kanaloa);
	gameManager->RenderingManager->CreateRenderingObject(Sunny);

	std::cout << "Scene created" << std::endl;



}