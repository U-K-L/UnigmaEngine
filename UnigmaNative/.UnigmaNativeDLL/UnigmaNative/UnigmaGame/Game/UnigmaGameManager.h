#pragma once
#include <iostream>
#include "UnigmaGameObject.h"
#include "../Core/UnigmaNative.h"

#define MAX_NUM_GAMEOBJECTS 8192

void AddGameObject(UnigmaGameObject gameObject);
void RemoveGameObject(uint32_t ID);

class UnigmaGameManager
{
	public:
	UnigmaGameManager();
	~UnigmaGameManager();

	void Update();
	void Start();
};

extern "C" {
	UNIGMANATIVE_API UnigmaGameObject* GetGameObject(uint32_t ID);
}