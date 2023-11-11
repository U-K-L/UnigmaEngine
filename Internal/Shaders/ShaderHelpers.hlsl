#ifndef SHADER_HELPERS_INCLUDED
#define SHADER_HELPERS_INCLUDED

#define UNITY_PI 3.14159265359

#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc"

#define IDENTITY_MATRIX float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)

#define NOISE_SIMPLEX_1_DIV_289 0.00346020761245674740484429065744f
float mod289(float x) {
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}

float2 mod289(float2 x) {
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}

float3 mod289(float3 x) {
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}

float4 mod289(float4 x) {
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}

float2 hash2(float2 p)
{   // procedural white noise	
    return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);
}


//Iquilezles, raises value slightly, m = threshold (anything above m stays unchanged).
// n = value when 0
// x = value input.
float almostIdentity(float x, float m, float n) {
    if (x > m) return x;

    const float a = 2.0 * n - m;
    const float b = 2.0 * m - 3.0 * n;
    const float t = x / m;
    return (a * t + b) * t * t + n;
}

// ( x*34.0 + 1.0 )*x =
// x*x*34.0 + x
float permute(float x) {
    return mod289(
        x * x * 34.0 + x
    );
}

float3 permute(float3 x) {
    return mod289(
        x * x * 34.0 + x
    );
}

float4 permute(float4 x) {
    return mod289(
        x * x * 34.0 + x
    );
}



float taylorInvSqrt(float r) {
    return 1.79284291400159 - 0.85373472095314 * r;
}

float4 taylorInvSqrt(float4 r) {
    return 1.79284291400159 - 0.85373472095314 * r;
}

uint clz(uint b)
{
    uint len = b ? (31 - floor(log2(b))) : 32;
	return len;
}


float4 grad4(float j, float4 ip)
{
    const float4 ones = float4(1.0, 1.0, 1.0, -1.0);
    float4 p, s;
    p.xyz = floor(frac(j * ip.xyz) * 7.0) * ip.z - 1.0;
    p.w = 1.5 - dot(abs(p.xyz), ones.xyz);

    // GLSL: lessThan(x, y) = x < y
    // HLSL: 1 - step(y, x) = x < y
    s = float4(
        1 - step(0.0, p)
        );

    // Optimization hint Dolkar
    // p.xyz = p.xyz + (s.xyz * 2 - 1) * s.www;
    p.xyz -= sign(p.xyz) * (p.w < 0);

    return p;
}

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

float4x4 inverse2(in float4x4 m)
{
    return float4x4(
        m[0][0], m[1][0], m[2][0], 0.0,
        m[0][1], m[1][1], m[2][1], 0.0,
        m[0][2], m[1][2], m[2][2], 0.0,
        -dot(m[0].xyz, m[3].xyz),
        -dot(m[1].xyz, m[3].xyz),
        -dot(m[2].xyz, m[3].xyz),
        1.0);
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

float sphIntersectExtruded(float3 ro, float3 rd, float4 sph)
{
    float3 oc = ro - sph.xyz; //Position with center added.
    float b = dot(oc, rd);
    float c = dot(oc, oc) - (sph.w * sph.w);
    float h = b * b - c;
    if (h < 0.0) return -1.0;
    h = sqrt(h);
    float t1 = -b - h;
	float3 p2 = ro + rd * t1;
	float3 p = p2 - sph.xyz;
    c = dot(oc, oc) - (sph.w * sph.w)*p.y*15.0f*sin(_Time.x*25.0);
	h = b * b - c;
	if (h < 0.0) return -1.0;
	h = sqrt(h);
    return -b - h;
}

// axis aligned box centered at the origin, with size boxSize
float2 boxIntersection(in float3 ro, in float3 rd, in float3 boxSize, in float4x4 txx, out float3 outNormal)
{
    //Convert to local space of the box
    float3 rdd = (mul(txx, float4(rd, 0.0)) ).xyz;
    float3 roo = (mul(txx, float4 (ro, 1.0))).xyz;
    
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
    //float3 rdd = (mul(txx, float4(rd, 0.0))).xyz;
    //float3 roo = (mul(txx, float4 (ro, 1.0))).xyz;
    float3 rdd = rd;//float3(rd.x + txi._m30, rd.y + txi._m31, rd.z + txi._m32);//(mul(txx, float4(rd, 0.0)) ).xyz;
    float3 roo = float3(ro.x + txx._m30, ro.y + txx._m31, ro.z + txx._m32);//(mul(txx, float4 (ro, 1.0))).xyz;
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
    float4 res = (tN > 0.0) ? float4(tN, step(float3(tN, tN, tN), t1)) :
        float4(tF, step(t2, float3(tF, tF, tF)));

    // add sign to normal and convert to ray space
    res.yzw = (mul(txi, float4(-sign(rdd) * res.yzw, 0.0))).xyz;

    return res;
}

bool IntersectBox(in float3 roo, in float3 rdd, float3 boxmin, float3 boxmax, out float tnear, out float tfar)
{
    // compute intersection of ray with all six bbox planes
    float3 invR = 1.0 / rdd;
    float3 tbot = invR * (boxmin.xyz - roo);
    float3 ttop = invR * (boxmax.xyz - roo);
    // re-order intersections to find smallest and largest on each axis
    float3 tmin = min(ttop, tbot);
    float3 tmax = max(ttop, tbot);
    // find the largest tmin and the smallest tmax
    float2 t0 = max(tmin.xx, tmin.yz);
    tnear = max(t0.x, t0.y);
    t0 = min(tmax.xx, tmax.yz);
    tfar = min(t0.x, t0.y);
    // check for hit
    bool hit;
    if ((tnear > tfar))
        hit = false;
    else
        hit = true;
    return hit;
}

float opSmoothUnion2(float d1, float d2, float k)
{
    float h = max(k - abs(d1 - d2), 0.0);
    return min(d1, d2) - h * h * 0.25 / k;
}

float opSmoothSubtraction(float d1, float d2, float k)
{
    return -opSmoothUnion2(d1, -d2, k);
}

float opSmoothIntersection(float d1, float d2, float k)
{
    return -opSmoothUnion2(-d1, -d2, k);
}

float Droplets(float sphereSDF, float dropletSDF)
{
    float finalValue = 0.0;
    float resultSphere = opSmoothIntersection(sphereSDF, dropletSDF, 0.51);

    finalValue = resultSphere;//smoothstep(0.2, 0.22, resultSphere);
    return finalValue;
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

float hash1(float n) { return frac(sin(n) * 43758.5453); }

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

float rand2dTo1d(float2 value, float2 dotDir = float2(12.9898, 78.233)) {
    float2 smallValue = sin(value);
    float random = dot(smallValue, dotDir);
    random = frac(sin(random) * 143758.5453);
    return random;
}

float2 rand2dTo2d(float2 value) {
    return float2(
        rand2dTo1d(value, float2(12.989, 78.233)),
        rand2dTo1d(value, float2(39.346, 11.135))
        );
}

float2 voronoiNoise(float2 value) {

    //From ronja Tutorials. Gets the distance of each point to generate voronoi Noise.
    float2 baseCell = floor(value);
    float minDistToCell = 10;
    float2 closestCell;
    [unroll]
    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            float2 cell = baseCell + float2(x, y);
            float2 cellPosition = cell + rand2dTo2d(cell);
            float2 toCell = cellPosition - value;
            float distToCell = length(toCell);
            if (distToCell < minDistToCell) {
                minDistToCell = distToCell;
                closestCell = cell;
            }

        }
    }
    float random = rand2dTo1d(closestCell);
    return float2(minDistToCell, random);
}

float2 unity_voronoi_noise_randomVector(float2 UV, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y * +offset) * 0.5 + 0.5, cos(UV.x * offset) * 0.5 + 0.5);
}

float Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            //Find the nearest point to color the grid.
            float2 lattice = float2(x, y); //The point.
            float2 offset = lattice - f + unity_voronoi_noise_randomVector(lattice + g, AngleOffset); //Randomly generated point.
            float d = distance(lattice + offset, f); //The distance between them.
            if (d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = res.x;
                Cells = res.y;
            }
        }
    }

    float DistFromCenter = sqrt(Out);
    float DistFromEdge = 8.0f;
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            //Find the nearest point to color the grid.
            float2 lattice = float2(x, y); //The point.
            float2 offset = lattice - f + unity_voronoi_noise_randomVector(lattice + g, AngleOffset); //Randomly generated point.
            
            float distToEdge = dot(0.5 * (offset + res.xy), normalize(offset - res.xy));
            
            DistFromEdge = min(DistFromEdge, distToEdge);
        }
    }

    return DistFromEdge;
}

// The parameter w controls the smoothness
float4 smoothVoronoi(in float2 x, float w)
{
    float2 n = floor(x);
    float2 f = frac(x);

    float4 m = float4(8.0, 0.0, 0.0, 0.0);
    for (int j = -2; j <= 2; j++)
        for (int i = -2; i <= 2; i++)
        {
            float2 g = float2(float(i), float(j));
            float2 o = hash2(n + g);

            // animate
            o = 0.5 + 0.5 * sin(_Time.x + 6.2831 * o);

            // distance to cell		
            float d = length(g - f + o);

            // cell color
            float3 col = 0.5 + 0.5 * sin(hash1(dot(n + g, float2(7.0, 113.0))) * 2.5 + 3.5 + float3(2.0, 3.0, 0.0));
            // in linear space
            col = col * col;

            // do the smooth min for colors and distances		
            float h = smoothstep(-1.0, 1.0, (m.x - d) / w);
            m.x = lerp(m.x, d, h) - h * (1.0 - h) * w / (1.0 + 3.0 * w); // distance
            m.yzw = lerp(m.yzw, col, h) - h * (1.0 - h) * w / (1.0 + 3.0 * w); // color
        }

    return m;
}

//Smooth Version
void F1Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells, float smoothness)
{
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);
    float resX = 0.0;

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            //Find the nearest point to color the grid.
            float2 lattice = float2(x, y); //The point.
            float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset); //Randomly generated point.
            float d = distance(lattice + offset, f); //The distance between them.

            resX += 1.0 / pow(d, smoothness);
            if (d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = pow(1.0 / resX, 1.0 / 16.0);
                Cells = res.y;
            }
        }
    }
}

float3 Ivoronoi(in float2 x)
{
    float2 n = floor(x);
    float2 f = frac(x);

    //----------------------------------
    // first pass: regular voronoi
    //----------------------------------
    float2 mg, mr;

    float md = 8.0;
    for (int j = -1; j <= 1; j++)
        for (int i = -1; i <= 1; i++)
        {
            float2 g = float2(float(i), float(j));
            float2 o = hash2(n + g);
            o = 0.5 + 0.5 * sin(10*_Time.x + 6.2831 * o);
            float2 r = g + o - f;
            float d = dot(r, r);

            if (d < md)
            {
                md = d;
                mr = r;
                mg = g;
            }
        }

    //----------------------------------
    // second pass: distance to borders
    //----------------------------------
    md = 8.0;
    for (int j = -2; j <= 2; j++)
        for (int i = -2; i <= 2; i++)
        {
            float2 g = mg + float2(float(i), float(j));
            float2 o = hash2(n + g);
            o = 0.5 + 0.5 * sin(10 * _Time.x + 6.2831 * o);
            float2 r = g + o - f;

            if (dot(mr - r, mr - r) > 0.00001)
                md = min(md, dot(0.5 * (mr + r), normalize(r - mr)));
        }

    return float3(md, mr);
}

float snoise(float3 v)
{
    const float2 C = float2(
        0.166666666666666667, // 1/6
        0.333333333333333333 // 1/3
        );
    const float4 D = float4(0.0, 0.5, 1.0, 2.0);
    // First corner
    float3 i = floor(v + dot(v, C.yyy));
    float3 x0 = v - i + dot(i, C.xxx);
    // Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);
    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
    float3 x3 = x0 - D.yyy; // -1.0+3.0*C.x = -0.5 = -D.y
    // Permutations
    i = mod289(i);
    float4 p = permute(
        permute(
            permute(
                i.z + float4(0.0, i1.z, i2.z, 1.0)
            ) + i.y + float4(0.0, i1.y, i2.y, 1.0)
        ) + i.x + float4(0.0, i1.x, i2.x, 1.0)
    );
    // Gradients: 7x7 points over a square, mapped onto an octahedron.
    // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    float n_ = 0.142857142857; // 1/7
    float3 ns = n_ * D.wyz - D.xzx;
    float4 j = p - 49.0 * floor(p * ns.z * ns.z); // mod(p,7*7)
    float4 x_ = floor(j * ns.z);
    float4 y_ = floor(j - 7.0 * x_); // mod(j,N)
    float4 x = x_ * ns.x + ns.yyyy;
    float4 y = y_ * ns.x + ns.yyyy;
    float4 h = 1.0 - abs(x) - abs(y);
    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);
    //float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
    //float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, float4(0, 0, 0, 0));
    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
    float3 p0 = float3(a0.xy, h.x);
    float3 p1 = float3(a0.zw, h.y);
    float3 p2 = float3(a1.xy, h.z);
    float3 p3 = float3(a1.zw, h.w);
    //Normalise gradients
    float4 norm = rsqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;
    // Mix final noise value
    float4 m = max(0.5 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    m = m * m;
    return 105.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
}

//Returns the sawtooth function.
float sawTooth(float speed, float t) {
    float cot = 1 / tan((speed * t * UNITY_PI) / 2);
    float s = -(1 / UNITY_PI) * atan(cot) + 0.5;
    return s;
}

float2 animateUVs(float2 uv, float speed) {

    //uv *= -abs(sin(_Time.r * speed));
    //uv *= sawTooth(speed, _Time.r);
    float progress = frac((2 + ((3 + _Time.r) % 7)) * speed * 0.0001);
    return uv * (progress * 10000);
}

//Creates a twirling effect for texture.
float2 Twirl(float2 UV, float2 Center, float Strength, float2 Offsets, float speed)
{
    Center += (Strength * 0.5);
    float2 delta = (UV * Strength) - Center;
    float angle = (_Time.y * speed) + Strength * length(delta);
    float x = cos(angle) * delta.x - sin(angle) * delta.y;
    float y = sin(angle) * delta.x + cos(angle) * delta.y;
    return float2(x + Center.x + Offsets.x, y + Center.y + Offsets.y);
}

float sdStar5(float2 p, float r, float rf)
{
    const float2 k1 = float2(0.809016994375, -0.587785252292);
    const float2 k2 = float2(-k1.x, k1.y);
    p.x = abs(p.x);
    p -= 2.0 * max(dot(k1, p), 0.0) * k1;
    p -= 2.0 * max(dot(k2, p), 0.0) * k2;
    p.x = abs(p.x);
    p.y -= r;
    float2 ba = rf * float2(-k1.y, k1.x) - float2(0, 1);
    float h = clamp(dot(p, ba) / dot(ba, ba), 0.0, r);
    return length(p - ba * h) * sign(p.y * ba.x - p.x * ba.y);
}

float sdCircle(float2 p, float r)
{
    return length(p) - r;
}

float sdMoon(float2 p, float d, float ra, float rb)
{
    p.y = abs(p.y);
    float a = (ra * ra - rb * rb + d * d) / (2.0 * d);
    float b = sqrt(max(ra * ra - a * a, 0.0));
    if (d * (p.x * b - p.y * a) > d * d * max(b - p.y, 0.0))
        return length(p - float2(a, b));
    return max((length(p) - ra),
        -(length(p - float2(d, 0)) - rb));
}
float2 dot2(float2 p)
{
    return dot(p, p);
}
float sdHeart(float2 p, float r)
{
    p.x = abs(p.x);

    if (p.y + p.x > 1.0)
        return sqrt(dot2(p - float2(0.25, 0.75))) - sqrt(2.0) / 4.0;
    return sqrt(min(dot2(p - float2(0.00, 1.00)),
        dot2(p - 0.5 * max(p.x + p.y, 0.0)))) * sign(p.x - p.y);
}

float4 vec4(float3 vec)
{
    return float4(vec.x, vec.y, vec.z, 1);
}


float4 vec4(float vec)
{
    return float4(vec, vec, vec, 1);
}

float LineCluster(float p, float spacing, float count)
{
    float result = step(frac(p * count), spacing);

    return result;
}


float PointGrid(float2 p)
{
    float2 vecSteps = step(frac(p * 10), float2(ddx(p.x), ddy(p.y)) * 10);
    float result = vecSteps.x * vecSteps.y;
    return result;
}

float2 ClosetCell(float2 p, int _count)
{
    float minDistToCell = 100;
    float2 finalCell = p;
    for (int i = 0; i < _count; i++)
    {
        for (int j = 0; j < _count; j++)
        {
            float2 currentPoint = float2((1.0 / _count) * i, (1.0 / _count) * j);
            float dist = distance(currentPoint, p);
            if (dist < minDistToCell)
            {
                minDistToCell = dist;
                finalCell = currentPoint;
            }
        }

    }

    return finalCell;
}

float2 ClosetLineCell(float2 p)
{
    float minDistToCell = 100;
    float2 finalCell = p;
    const int count = 10;
    for (int i = 0; i < count; i++)
    {
        float2 currentPoint = float2(1 - ((1.0 / count) * i), (1.0 / count) * i);
        float dist = distance(currentPoint, p);
        if (dist < minDistToCell)
        {
            minDistToCell = dist;
            finalCell = currentPoint;
        }

    }

    return finalCell;
}


float opU(float d1, float d2)
{
    return min(d1, d2);
}

float2 sminN(float a, float b, float k, float n)
{
    float h = max(k - abs(a - b), 0.0) / k;
    float m = pow(h, n) * 0.5;
    float s = m * k / n;
    return (a < b) ? float2(a - s, m) : float2(b - s, 1.0 - m);
}

// polynomial smooth min
float smin(float a, float b, float k)
{
    float h = max(k - abs(a - b), 0.0) / k;
    return min(a, b) - h * h * k * (1.0 / 4.0);
}


float opSmoothUnion(float d1, float d2, float k) {
    float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) - k * h * (1.0 - h);
}

float3 gsmin(in float4 a, in float4 b, in float k)
{
    float h = max(k - abs(a.x - b.x), 0.0);
    float m = 0.25 * h * h / k;
    float n = 0.50 * h / k;
    return float3(min(a.x, b.x) - m,
        lerp(a.yzw, b.yzw, (a.x < b.x) ? n : 1.0 - n).xy);
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