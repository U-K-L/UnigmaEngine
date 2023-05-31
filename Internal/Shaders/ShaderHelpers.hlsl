#ifndef SHADER_HELPERS_INCLUDED
#define SHADER_HELPERS_INCLUDED

#define UNITY_PI 3.14159265359

#include "UnityCG.cginc"

struct NVector
{
    float nvector;
};


float3 TransformToWorldSpace(float4x4 _LocalToWorld, float3 p)
{
    float3 worldPos = mul(_LocalToWorld, float4(p, 1)).xyz;
    return worldPos;
}

float3 GetTriangleCenter(float3 a, float3 b, float3 c)
{
    return (a+b+c) / 3.0;
}

float2 GetTriangleCenter(float2 a, float2 b, float2 c)
{
    return (a+b+c) / 3.0;
}

float3 GetTriangleNormal(float3 a, float3 b, float3 c)
{
    return normalize(cross(b-a, c-a));
}

void GetTriangleNormalAndTSMatrix(float3 a, float3 b, float3 c, out float3 normal, out float3x3 tangentTransform) {

    float3 tangent = normalize(b - a);
    normal = normalize(cross(tangent, c - a));
    //normal = float3(0, 0, 1);
    float3 bitangent = normalize(cross(tangent, normal));
    tangentTransform = transpose(float3x3(tangent, bitangent, normal));
}

float rand(float3 co)
{
    return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
}

// Returns a pseudorandom number. By Ronja Böhringer
float rand(float4 value) {
    float4 smallValue = sin(value);
    float random = dot(smallValue, float4(12.9898, 78.233, 37.719, 09.151));
    random = frac(sin(random) * 143758.5453);
    return random;
}

float rand(float3 pos, float offset) {
    return rand(float4(pos, offset));
}

float randNegative1to1(float3 pos, float offset) {
    return rand(pos, offset) * 2 - 1;
}

// Construct a rotation matrix that rotates around the provided axis, sourced from:
// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
float3x3 AngleAxis3x3(float angle, float3 axis)
{
    float c, s;
    sincos(angle, s, c);

    float t = 1 - c;
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    return float3x3(
        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
        t * x * z - s * y, t * y * z + s * x, t * z * z + c
        );
}

//Need the plane aka triangle as input.
float3 PathAlongTangent(float3 a, float3 b, float3 c, float3 vertPos, float3 tripos, float3 target)
{
    //Will take the position and the new position and move the position to the new position within tangent space.

    //Get the random movement.
	float3 offsetTS = float3(sin(_Time.y), cos(_Time.x), sin(_Time.z))  + target;
    
    //Creating a tangent space matrix is easy. We create a change of basis such that the x axis and y axis exists on a 2D plane at the point.
    //Basically it's the plane of the triangle, lastly the Z axis will point out the triangle.
    //This is just the tangent, bitangent, and normal.
    float3 tangent = normalize(b - a);
    float3 normal = normalize(cross(tangent, c - a));
    float3 bitangent = normalize(cross(tangent, normal));
    
    //We zero out the Z axis so that it doesn't leave the plane. Since we're adding in random values we need this.
	float3x3 tangentSpace = transpose(float3x3(tangent, bitangent, float3(0,0,0)));
	//float3 tangentSpacePos = mul(tangentSpace, vertPos);
	float3 tagentSpaceTarget = mul(tangentSpace, offsetTS);
    
    return vertPos + tagentSpaceTarget;
}

void MatrixMultiply(uint3 id, int _Cols, StructuredBuffer<float> A, StructuredBuffer<float> B, RWStructuredBuffer<float> result, int _Transpose, int _Batch)
{
    int x = id.x + (id.z * 65535);
    int y = id.y + (id.z * 65535);
    
    int vectorIndex = (x + y * _Cols);
    if (vectorIndex > result.Length)
        return;
    //Got to use a for loop for many reasons. 
    //For one, the threads act without gauranteed sequentiality. Therefore revisiting the same index does not work.
    float sum = 0;
    for (int i = 0; i < _Cols; i++)
    {
        if (_Transpose == 0) //No transpose
            sum += A[(y * _Cols) + i] * B[(i * _Cols) + x];
		else if (_Transpose == 1) // A^T X B
			sum += A[(i * _Cols) + y] * B[(i * _Cols) + x];
		else if (_Transpose == 2) // A X B^T
            sum += A[(y * _Cols) + i] * B[(x * _Cols) + i];
		else if (_Transpose == 3) // A^T X B^T
			sum += A[(i * _Cols) + y] * B[(x * _Cols) + i];
    }
    result[vectorIndex] = sum;
}

void FastDotProduct(uint3 id, int _Cols, StructuredBuffer<float> A, StructuredBuffer<float> B, RWStructuredBuffer<float> result)
{
    int currentIndex = id.x + (id.y * _Cols) + (id.z * _Cols * _Cols);
    
	if (currentIndex > result.Length)
		return;
	result[currentIndex] = A[currentIndex] * B[currentIndex];
}

void SumProduct(uint3 id, int _Cols, StructuredBuffer<float> tmp, RWStructuredBuffer<float> result, int _Batch, int _BatchSize, int _BufferSize)
{
	int startingIndex = id.x * _BatchSize + (id.y * _BatchSize * _BatchSize) + (id.z * _BatchSize * _BatchSize * _BatchSize);
    int currentIndex = id.x + id.y + id.z;
    
    if (startingIndex > _BufferSize)
        return;
    
    float sum = 0;
    
    for (int i = startingIndex; (i < startingIndex + _BatchSize) && (i < startingIndex + _Cols); i++)
    {
        sum += tmp[i];
    }
        
    result[currentIndex] = sum;
}

#endif