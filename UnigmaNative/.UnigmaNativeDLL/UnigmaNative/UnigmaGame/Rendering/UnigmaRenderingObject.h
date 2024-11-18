#pragma once
#include <string>
#include <cstdint> // For uint32_t
#include <vector>
#include <../glm//glm.hpp>


struct Vertex {
	glm::vec3 pos = glm::vec3(0.0f);
	glm::vec3 color = glm::vec3(1.0f);              // Default color (white)
	glm::vec2 texCoord = glm::vec2(0.0f);
	glm::vec3 normal = glm::vec3(0.0f, 0.0f, 1.0f); // Default normal (pointing along +Z axis)

	bool operator==(const Vertex& other) const {
		return pos == other.pos && color == other.color && texCoord == other.texCoord && normal == other.normal;
	}
};

struct UnigmaRenderingStruct
{
	std::vector<Vertex> vertices;
	std::vector<uint32_t> indices;
	UnigmaRenderingStruct()
		: vertices({ Vertex

				{{-0.5f, -0.5f, 0.0f}, {1.0f, 0.0f, 0.0f}, {0.0f, 0.0f}, {0.0f, 0.0f, 1.0f}},
				{{0.5f, -0.5f, 0.0f}, {0.0f, 1.0f, 0.0f}, {1.0f, 0.0f}, {0.0f, 0.0f, 1.0f}},
				{{0.5f, 0.5f, 0.0f}, {0.0f, 0.0f, 1.0f}, {1.0f, 1.0f}, {0.0f, 0.0f, 1.0f}},
				{{-0.5f, 0.5f, 0.0f}, {1.0f, 1.0f, 1.0f}, {0.0f, 1.0f}, {0.0f, 0.0f, 1.0f}}
			}
		),
		indices({ 0, 1, 2, 2, 3, 0 })
	{
	}
};