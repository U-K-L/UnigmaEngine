#pragma once
#include <iostream>
#include "UnigmaScene.h"

class UnigmaSceneManager
{
	public:
	UnigmaSceneManager();
	~UnigmaSceneManager();

	void Update();
	void Start();
	void CreateScene(std::string sceneName);
	void AddScene(UnigmaScene scene);
	void LoadScene(std::string sceneName);

	uint32_t Index;
	std::vector<uint32_t> ScenesIndex;

	UnigmaScene* GetCurrentScene();
	void SetCurrentScene(UnigmaScene scene);

	private:
		UnigmaScene* CurrentScene;
};
