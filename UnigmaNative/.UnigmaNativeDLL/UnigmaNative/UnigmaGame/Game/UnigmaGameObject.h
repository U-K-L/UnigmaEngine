#pragma once
#include <iostream>
#include <vector>
#include "../Core/UnigmaTransform.h"

#define MAX_NUM_COMPONENTS 32
struct UnigmaGameObject
{
	UnigmaTransform transform;
	char name[32];
	uint32_t ID;
	uint32_t RenderID;
	bool isActive;
	bool isCreated;
	uint16_t components[MAX_NUM_COMPONENTS];
};
