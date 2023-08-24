#ifndef SHADER_HELPERS_INCLUDED
#define SHADER_HELPERS_INCLUDED

#define UNITY_PI 3.14159265359

#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc"

#define IDENTITY_MATRIX float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)

float4x4 inverse(float4x4 m) {
    float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
    float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
    float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
    float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];

    float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
    float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
    float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
    float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

    float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
    float idet = 1.0f / det;

    float4x4 ret;

    ret[0][0] = t11 * idet;
    ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
    ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
    ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;

    ret[1][0] = t12 * idet;
    ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
    ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
    ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;

    ret[2][0] = t13 * idet;
    ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
    ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
    ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;

    ret[3][0] = t14 * idet;
    ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
    ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
    ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;

    return ret;
}

struct NVector
{
    float nvector;
};

float sdot(float3 x, float3 y, float f = 1.0f)
{
    return saturate(dot(x, y) * f);
}

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

float3 PointTangentToNormal(float3 p, float3 normal) {

    float3 helper = float3(1, 0, 0);
    if (abs(normal.x) > 0.99f)
        helper = float3(0, 0, 1);
    float3 tangent = normalize(cross(normal, helper));
    float3 binormal = normalize(cross(normal, tangent));
    return mul(p, float3x3(tangent, binormal, normal));
}

float SphereSDF(float3 p, float r)
{
	float d = length(p) - r;
    return d;
}

//Intersectors -- https://iquilezles.org/articles/intersectors/
float sphIntersect(float3 ro, float3 rd, float4 sph)
{
    float3 oc = ro - sph.xyz;
    float b = dot(oc, rd);
    float c = dot(oc, oc) - sph.w * sph.w;
    float h = b * b - c;
    if (h < 0.0) return -1.0;
    h = sqrt(h);
    return -b - h;
}

// axis aligned box centered at the origin, with size boxSize
float2 boxIntersection(in float3 ro, in float3 rd, in float3 boxSize, in float4x4 txx, out float3 outNormal)
{
    //Convert to local space of the box
    float3 rdd = (mul(txx, float4(rd, 0.0)) ).xyz;
    float roo = (mul(txx, float4 (ro, 1.0))).xyz;
    
    float3 m = 1.0 / rd; // can precompute if traversing a set of aligned boxes
    float3 n = m * ro;   // can precompute if traversing a set of aligned boxes
    float3 k = abs(m) * boxSize;
    float3 t1 = -n - k;
    float3 t2 = -n + k;
    float tN = max(max(t1.x, t1.y), t1.z);
    float tF = min(min(t2.x, t2.y), t2.z);
    
    if (tN > tF || tF < 0.0) return float2(-1.0, -1.0); // no intersection
    outNormal = (tN > 0.0) ? step(float3(tN, tN, tN), t1) : // ro ouside the box
    step(t2, float3(tF, tF, tF));  // ro inside the box
    outNormal *= -sign(rd);
    return float2(tN, tF);
}

// https://iquilezles.org/articles/boxfunctions
float4 boxIntersection2(in float3 ro, in float3 rd, in float4x4 txx, in float4x4 txi, in float3 rad)
{
    // convert from ray to box space
    float3 rdd = (mul(txx, float4(rd, 0.0))).xyz;
    float roo = (mul(txx, float4 (ro, 1.0))).xyz;

    // ray-box intersection in box space
    float3 m = 1.0 / rdd;
    // more robust
    float3 k = float3(rdd.x >= 0.0 ? rad.x : -rad.x, rdd.y >= 0.0 ? rad.y : -rad.y, rdd.z >= 0.0 ? rad.z : -rad.z);
    float3 t1 = (-roo - k) * m;
    float3 t2 = (-roo + k) * m;
    
    float tN = max(max(t1.x, t1.y), t1.z);
    float tF = min(min(t2.x, t2.y), t2.z);

    // no intersection
    if (tN > tF || tF < 0.0) return -1.0;

    // use this instead if your rays origin can be inside the box
    float4 res = (tN > 0.0) ? float4(tN, step(tN, t1)) :
        float4(tF, step(t2, tF));

    // add sign to normal and convert to ray space
    res.yzw = (mul(txi, float4(-sign(rdd) * res.yzw, 0.0))).xyz;

    return res;
}



float3 depthWorldPosition(float2 uv, float z, float4x4 InvVP)
{
    float x = uv.x * 2.0f - 1.0f;
    float y = (1.0 - uv.y) * 2.0f - 1.0f;
    float4 position_s = float4(x, y, z, 1.0f);
    float4 position_v = mul(InvVP, position_s);
    return position_v.xyz / position_v.w;
}

float3 GetSphereNormal(float3 p, float r)
{
	float3 eps = float3(0.0001, 0.0, 0.0);
	//Sample the distance field at the point and at a small offset.
	float3 n = float3(
		SphereSDF(p + eps.xyy, r) - SphereSDF(p - eps.xyy, r),
		SphereSDF(p + eps.yxy, r) - SphereSDF(p - eps.yxy, r),
		SphereSDF(p + eps.yyx, r) - SphereSDF(p - eps.yyx, r));
    
	return normalize(n);
}

//When no seed is provided simply use time.x.
float rand()
{
    float3 co = float3(_Time.x, _Time.x, _Time.x);
    return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
}

float rand(float val)
{
	float3 co = float3(val, val, val);
    return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
}


float rand(float2 co)
{
    return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
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

//Box–Muller transform: https://developer.nvidia.com/gpugems/gpugems3/part-vi-gpu-computing/chapter-37-efficient-random-number-generation-and-application
float2 randGaussian(float3 pos, float offset) {
	float u1 = rand(pos, offset);
	float u2 = rand(pos, offset + 1);
	float theta = 2 * UNITY_PI * u1;
	float rho = 0.164955 * sqrt(-2 * log(abs(u2) + 0.01));
	float z0 = rho * cos(theta) + 0.5;
    float z1 = rho * sin(theta) + 0.5;
    z0 = max(z0, 0);
	z0 = min(z0, 1);
	z1 = max(z1, 0);
	z1 = min(z1, 1);
	return float2(z0, z1);
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

float3 RandomPointInTriangle(float3 a, float3 b, float3 c, float2 r)
{
    float3 p = (1 - sqrt(r.x)) * a + (sqrt(r.x) * (1 - r.y)) * b + (r.y * sqrt(r.x)) * c;
    return p;
}

//Need to supply normal so that hemisphere is oriented with the normal.
//Let's use the cosine weighted sampling.
//We map a square onto a disk then project that disk onto a hemisphere.
float3 RandomPointOnHemisphere(float2 pixel, float3 normal, float2 seed, float radius = 1.0, float power = 1.0)
{
	float2 xy = randGaussian(float3(pixel + seed, seed.y), rand(seed.x));
    float2 xz = randGaussian(float3(pixel + seed + 2452, seed.x), rand(seed.y));
    float3 uv = float3(xy, xz.x);
    
	float theta = acos(pow(1 - uv.x, 1.0 / (power + 1.0)));
	float phi = 2 * UNITY_PI * uv.y;

	float3 dir = float3(sin(theta) * cos(phi), sin(theta) * sin(phi), cos(theta));

    //Quick Guass test
    //float3 gaussianDistrib = float3(uv.x, uv.y, uv.z); //Range -1, 1.
    //float3 prandom =  normalize(gaussianDistrib) * radius;
    
	//Transform this direction to be on the hemisphere with the provided normal.
    float3 transformedDir = PointTangentToNormal(dir, normal);
    return transformedDir;
    
}

//Use crammer's rule to solve for the barycentric coordinates of a point in a triangle.
//From Real-Time Collision Detection (Christer Ericson)
float3 Barycentric(float3 a, float3 b, float3 c, float3 p)
{
	float3 v0 = b - a, v1 = c - a, v2 = p - a;
	float d00 = dot(v0, v0);
	float d01 = dot(v0, v1);
	float d11 = dot(v1, v1);
	float d20 = dot(v2, v0);
	float d21 = dot(v2, v1);
	float denom = d00 * d11 - d01 * d01;
	float3 bary;
	bary.y = (d11 * d20 - d01 * d21) / denom;
	bary.z = (d00 * d21 - d01 * d20) / denom;
	bary.x = 1.0 - bary.y - bary.z;
	return bary;
}

//Need the plane aka triangle as input.
float3 PathAlongTangent(float3 a, float3 b, float3 c,float3 target)
{
	float3 offsetTS = target;

    
    //Creating a tangent space matrix is easy. We create a change of basis such that the x axis and y axis exists on a 2D plane at the point.
    //Basically it's the plane of the triangle, lastly the Z axis will point out the triangle.
    //This is just the tangent, bitangent, and normal.
    float3 tangent = normalize(b - a);
    float3 normal = normalize(cross(tangent, c - a));
    float3 bitangent = normalize(cross(tangent, normal));
	float3x3 tangentSpace = transpose(float3x3(tangent, bitangent, normal));
	float3 tangentSpaceTarget = mul(tangentSpace, offsetTS);
    
    return tangentSpaceTarget;
}

float3x3 AlignToNormal(float3 a, float3 b, float3 c)
{
    float3 normal = GetTriangleNormal(a, b, c);
    //Create tangent.
    float3 tangent = normalize(c - a);
    //Create bitangent.
    float3 bitangent = normalize(cross(tangent, normal));
    //Create new basis matrix. Transpose to match Unity column row order.
    float3x3 LocalToWorldMatrixNormal = transpose(float3x3(tangent, normal, bitangent));
    return LocalToWorldMatrixNormal;
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
    
    for (int i = startingIndex; (i < startingIndex + _BatchSize) && (i < startingIndex + _BufferSize); i++)
    {
        sum += result[i];
    }
        
    result[startingIndex] = sum;
}

void Add(uint3 id, int _Cols, StructuredBuffer<float> A, StructuredBuffer<float> B, RWStructuredBuffer<float> result)
{
	int currentIndex = id.x + (id.y * _Cols) + (id.z * _Cols * _Cols);

	if (currentIndex > result.Length)
		return;
	result[currentIndex] = A[currentIndex] + B[currentIndex];
}


#endif