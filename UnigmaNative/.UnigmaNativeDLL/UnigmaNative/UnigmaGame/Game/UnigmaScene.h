#pragma once
#include <iostream>
#include "UnigmaGameObject.h"

class UnigmaScene
{
	public:
	UnigmaScene();
	~UnigmaScene();

	void Update();
	void Start();
	void CreateScene();
	void AddGameObject(UnigmaGameObject gameObject);

	uint32_t Index;
	std::vector<uint32_t> GameObjectsIndex;
};