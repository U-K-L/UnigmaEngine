#pragma once
#include <iostream>
#include "UnigmaGameObject.h"
#include "GlobalObjects.h"
#include "UnigmaRenderingObject.h"

class UnigmaRenderingManager
{
	public:
	UnigmaRenderingManager();
	~UnigmaRenderingManager();

	void Update();
	void Start();

	std::vector<UnigmaRenderingStruct> RenderingObjects;
	void CreateRenderingObject(UnigmaGameObject& gameObject);
};