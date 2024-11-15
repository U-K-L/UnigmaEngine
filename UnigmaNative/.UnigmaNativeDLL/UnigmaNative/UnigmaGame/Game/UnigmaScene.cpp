#include "UnigmaScene.h"
#include "../glm/glm.hpp"
#include "GlobalObjects.h"

UnigmaScene::UnigmaScene()
{
}

UnigmaScene::~UnigmaScene()
{
}

void UnigmaScene::Update()
{
}

void UnigmaScene::Start()
{
}

void UnigmaScene::AddGameObject(UnigmaGameObject gameObject)
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
}