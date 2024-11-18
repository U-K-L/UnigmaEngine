#include "UnigmaSceneManager.h"
#include "GlobalObjects.h"

UnigmaSceneManager::UnigmaSceneManager()
{

}

UnigmaSceneManager::~UnigmaSceneManager()
{
}

void UnigmaSceneManager::Update()
{
	//Updates all scenes.
	for(auto scene = GlobalScenes.begin(); scene != GlobalScenes.end(); scene++)
	{
		if(scene->second.IsActive && scene->second.IsCreated) //double check.
		{
			//Update scene.
			scene->second.Update();
		}
	}
}

//This actually starts the scene as if it were active.
void UnigmaSceneManager::Start()
{
	//Starts all scenes.
	for(auto scene = GlobalScenes.begin(); scene != GlobalScenes.end(); scene++)
	{
		if(scene->second.IsActive && scene->second.IsCreated && !scene->second.IsStarted)
		{
			//Start scene.
			scene->second.Start();
		}
	}

}

//Just creates a scene, but doesn't do anything with it.
void UnigmaSceneManager::CreateScene(std::string sceneName)
{
	UnigmaScene scene;
	scene.Name = sceneName;
	scene.IsActive = false;
	scene.CreateScene();

	std::cout << "Scene " << sceneName << " created" << std::endl;
	scene.IsCreated = true;
	AddScene(scene);

}

//Loads a scene from JSON file.
void UnigmaSceneManager::LoadScene(std::string sceneName)
{
	//Checks if already exists.
	if(GlobalScenes.count(sceneName) == 0)
	{
		std::cout << "Scene with name " << sceneName << " does not exist" << std::endl;
		return;
	}

	//Loads scene via Json.

	//TODO: LOAD JSON.
}

void UnigmaSceneManager::AddScene(UnigmaScene scene)
{
	if(GlobalScenes.count(scene.Name) > 0)
	{
		std::cout << "Scene with name " << scene.Name << " already exists" << std::endl;
		return;
	}

	GlobalScenes.insert( std::pair<std::string, UnigmaScene>(scene.Name, scene));
}

UnigmaScene* UnigmaSceneManager::GetCurrentScene()
{
	return CurrentScene;
}

void UnigmaSceneManager::SetCurrentScene(UnigmaScene scene)
{
		CurrentScene = &scene;
}


