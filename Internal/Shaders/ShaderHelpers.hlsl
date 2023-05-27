#ifndef SHADER_HELPERS_INCLUDED
#define SHADER_HELPERS_INCLUDED

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



void MatrixMultiply(uint3 id, int _Cols, StructuredBuffer<float> A, StructuredBuffer<float> B, RWStructuredBuffer<float> result, int _Transpose)
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

#endif