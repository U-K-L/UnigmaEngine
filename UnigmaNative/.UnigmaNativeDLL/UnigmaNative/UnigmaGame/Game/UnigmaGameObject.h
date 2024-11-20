#pragma once
#include <iostream>
#include <vector>
#include "../Core/UnigmaTransform.h"

#define MAX_NUM_COMPONENTS 32
#pragma pack(push, 1) // Sets 1-byte alignment
struct UnigmaGameObject
{
    UnigmaTransform transform;
    char _pad[4]; // Padding to align the next member on a 4-byte boundary
    char name[32];
    uint32_t ID;
    uint32_t RenderID;
    bool isActive;
    bool isCreated;
    char _pad2[6];
    uint16_t components[MAX_NUM_COMPONENTS];

    // Default constructor
    UnigmaGameObject()
        : ID(0), RenderID(0), isActive(false), isCreated(false)
    {
        // Initialize 'name' to an empty string
        memset(name, 0, sizeof(name));

        // Initialize 'components' array to zero
        //memset(components, 0, sizeof(components));
    }
};
#pragma pack(pop) // Restores the previous alignment
