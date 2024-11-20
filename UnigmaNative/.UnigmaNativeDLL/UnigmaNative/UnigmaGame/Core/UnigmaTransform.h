#pragma once
#define GLM_FORCE_DEFAULT_ALIGNED_GENTYPES
#include <../glm/glm.hpp>
#include <iostream>
struct UnigmaTransform
{
	glm::mat4 transformMatrix;
	glm::vec3 position;
	int pad;
	glm::vec3 rotation;
	int pad2;
	UnigmaTransform() : transformMatrix(1.0f), position(0.0f), rotation(0.0f)
	{
		UpdatePosition();
	}
	// Override the assignment operator to copy from RenderObject's transformMatrix
	UnigmaTransform& operator=(float rObjTransform[16]) {
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				transformMatrix[i][j] = rObjTransform[i * 4 + j];
			}
		}
		return *this;
	}

	// Overload assignment operator for setting position
	UnigmaTransform& operator=(const glm::vec3& newPosition) {
		position = newPosition;
		UpdatePosition(); // Automatically update transformMatrix when position is set
		return *this;
	}

	void UpdatePosition()
	{
		transformMatrix[3][0] = position.x;
		transformMatrix[3][1] = position.y;
		transformMatrix[3][2] = position.z;
	}

	void Print() {
		std::cout << "Transform Matrix:\n";
		for (int i = 0; i < 4; i++) {
			std::cout << "[";
			for (int j = 0; j < 4; j++) {
				std::cout << transformMatrix[i][j] << " ";
			}
			std::cout << "]\n";
		}
	}
};