#define RUNITY_PI 3.14159265359

#include "UnityCG.cginc"
Texture2D<float4> _UnigmaBlueNoise;
//Struct definitions.
struct Sample
{
    float3 x0;
    float3 x1;
    float3 x2;
    float weight;
};

struct UnigmaLight
{
    float3 position;
    float emission;
    float3 area;
    float3 color;
};

struct Vertex
{
	float3 position;
	float3 normal;
	float2 uv;
};

struct Payload
{
    float4 color;
    float3 direction;
    float distance;
    float4 normal;
    float2 pixel;
    float2 uv;
    
};

struct AttributeData
{
    float2 barycentrics;
    float distance;
    float3 position;
};

struct Reservoir
{
    uint Y; //Index most important light.
    float W; //light weight
    float wSum; // weight summed.
    float M; //Number of total lights for this reservoir.
    float pHat;
    float3 x1;
    int age;
};

struct ReservoirPath
{
    float wSum; // weight summed.
    float M; //Number of total lights for this reservoir.
    float3 radiance;
    float3 position;
    float3 normal;

};

struct Surface
{
    float3 position; //Comes from the initial trace. Sample x2.
    float3 normal; //Sample x2.
    float3 viewDir; //Store in sample x2.
    float3 color; // x2 sample. x2 being secondary surface.
    float emittance; //possible light.
};

struct UnigmaDispatchInfo
{
    int FrameCount;
};


float3x3 inverse(float3x3 m)
{
    float a00 = m[0][0], a01 = m[0][1], a02 = m[0][2];
    float a10 = m[1][0], a11 = m[1][1], a12 = m[1][2];
    float a20 = m[2][0], a21 = m[2][1], a22 = m[2][2];

    float b01 = a22 * a11 - a12 * a21;
    float b11 = -a22 * a10 + a12 * a20;
    float b21 = a21 * a10 - a11 * a20;

    float det = a00 * b01 + a01 * b11 + a02 * b21;

    return float3x3(b01, (-a22 * a01 + a02 * a21), (a12 * a01 - a02 * a11),
                            b11, (a22 * a00 - a02 * a20), (-a12 * a00 + a02 * a10),
                            b21, (-a21 * a00 + a01 * a20), (a11 * a00 - a01 * a10)) / det;
}

float euclideanNorm(float3x3 matrixA)
{
    float sum = length(matrixA[0]);
    sum += length(matrixA[1]);
    sum += length(matrixA[2]);
    
    return sum;

}

float euclideanNorm(float4x4 matrixA)
{
    float sum = length(matrixA[0]);
    sum += length(matrixA[1]);
    sum += length(matrixA[2]);
    sum += length(matrixA[3]);
    
    return sum;
}

// Expands a 10-bit integer into 30 bits
// by inserting 2 zeros after each bit.
unsigned int expandBits(unsigned int v)
{
    v = (v * 0x00010001u) & 0xFF0000FFu;
    v = (v * 0x00000101u) & 0x0F00F00Fu;
    v = (v * 0x00000011u) & 0xC30C30C3u;
    v = (v * 0x00000005u) & 0x49249249u;
    return v;
}

// Calculates a 30-bit Morton code for the
// given 3D point located within the unit cube [0,1].
unsigned int morton3D(float x, float y, float z)
{
    x = min(max(x * 1024.0f, 0.0f), 1023.0f);
    y = min(max(y * 1024.0f, 0.0f), 1023.0f);
    z = min(max(z * 1024.0f, 0.0f), 1023.0f);
    unsigned int xx = expandBits((unsigned int)x);
    unsigned int yy = expandBits((unsigned int)y);
    unsigned int zz = expandBits((unsigned int)z);
    return xx * 4 + yy * 2 + zz;
}

uint MortonX(uint x)
{
    x = (x | (x << 8)) & 0x00FF00FF;
    x = (x | (x << 4)) & 0x0F0F0F0F;
    x = (x | (x << 2)) & 0x33333333;
    x = (x | (x << 1)) & 0x55555555;
    return x;
}


uint IntegerCompact(uint x)
{
    x = (x & 0x11111111) | ((x & 0x44444444) >> 1);
    x = (x & 0x03030303) | ((x & 0x30303030) >> 2);
    x = (x & 0x000F000F) | ((x & 0x0F000F00) >> 4);
    x = (x & 0x000000FF) | ((x & 0x00FF0000) >> 8);
    return x;
}

uint ZCurveToLinearIndex(uint2 xy)
{
    return MortonX(xy[0]) | (MortonX(xy[1]) << 1);
}


// Converts a linear to a 2D position following a Z-curve pattern.
uint2 LinearIndexToZCurve(uint index)
{
    return uint2(
        IntegerCompact(index),
        IntegerCompact(index >> 1));
}

// 32 bit Jenkins hash
uint JenkinsHash(uint a)
{
    // http://burtleburtle.net/bob/hash/integer.html
    a = (a + 0x7ed55d16) + (a << 12);
    a = (a ^ 0xc761c23c) ^ (a >> 19);
    a = (a + 0x165667b1) + (a << 5);
    a = (a + 0xd3a2646c) ^ (a << 9);
    a = (a + 0xfd7046c5) + (a << 3);
    a = (a ^ 0xb55a4f09) ^ (a >> 16);
    return a;
}

uint GetRandomSeed(uint2 pixelLocation, uint _Offset)
{
    uint Zindex = ZCurveToLinearIndex(pixelLocation);
    uint seed = JenkinsHash(Zindex);
    //Offset seed by time
    seed += _Time.y *1000;
    //Offset seed by frame
    seed += _Offset;
    return seed;
}

uint murmur3(uint seed, uint index)
{
#define ROT32(x, y) ((x << y) | (x >> (32 - y)))

    // https://en.wikipedia.org/wiki/MurmurHash
    uint c1 = 0xcc9e2d51;
    uint c2 = 0x1b873593;
    uint r1 = 15;
    uint r2 = 13;
    uint m = 5;
    uint n = 0xe6546b64;

    uint hash = seed;
    uint k = index++;
    k *= c1;
    k = ROT32(k, r1);
    k *= c2;

    hash ^= k;
    hash = ROT32(hash, r2) * m + n;

    hash ^= 4;
    hash ^= (hash >> 16);
    hash *= 0x85ebca6b;
    hash ^= (hash >> 13);
    hash *= 0xc2b2ae35;
    hash ^= (hash >> 16);

#undef ROT32

    return hash;
}


float sampleUniformRng(uint seed, uint index = 1)
{
    uint v = murmur3(seed, index);
    const uint one = asuint(1.f);
    const uint mask = (1 << 23) - 1;
    return asfloat((mask & v) | one) - 1.f;
}

float QualityRand(uint seed)
{
    return sampleUniformRng(seed);
}


float rand(uint seed)
{

    uint3 id = DispatchRaysIndex();
    uint3 dim = DispatchRaysDimensions();
    float2 zSeed = LinearIndexToZCurve(seed);
    
    float val = asfloat(zSeed.x);
    float3 co = float3(val, val, val);
    float2 rSeed = frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);

    val = asfloat(zSeed.y);
    co = float3(val, val, val);
    rSeed.y = frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
	rSeed *= dim.xy;

    float2 textureSize = float2(1024, 1024);
    float2 UV = ((rSeed.xy + float2(0.5, 0.5)) / float2(dim.x, dim.y)) * 2 - 1;
    float2 index = ((UV * textureSize.xy + textureSize.xy) / 2) - 0.5f;
    
    return _UnigmaBlueNoise[index].r;//QualityRand(seed);
}


float4x4 saturationMatrix(float saturation)
{
    float3 luminance = float3(0.3086, 0.6094, 0.0820);

    float oneMinusSat = 1.0 - saturation;

    float3 red = luminance.x * oneMinusSat;
    red += float3(saturation, 0, 0);

    float3 green = luminance.y * oneMinusSat;
    green += float3(0, saturation, 0);

    float3 blue = luminance.z * oneMinusSat;
    blue += float3(0, 0, saturation);

    return float4x4(red, 0,
        green, 0,
        blue, 0,
        0, 0, 0, 1);
}

float Luminance(float3 rgb)
{
    return dot(rgb, float3(0.2126f, 0.7152f, 0.0722f));
}

float4x4 contrastMatrix(float contrast)
{
    float t = (1.0 - contrast) / 2.0;

    return float4x4(contrast, 0, 0, 0,
        0, contrast, 0, 0,
        0, 0, contrast, 0,
        t, t, t, 1);

}


float2 GetUVs(AttributeData attributes)
{
    //Gets the triangle in question. First by getting its index then the order the vertices are in.
    uint primitiveIndex = PrimitiveIndex();
    uint3 triangleIndicies = UnityRayTracingFetchTriangleIndices(primitiveIndex);
    Vertex v0, v1, v2;

    //Get the attributes of this vertex.
    v0.uv = UnityRayTracingFetchVertexAttribute2(triangleIndicies.x, kVertexAttributeTexCoord0); //tex coordinate.
    v1.uv = UnityRayTracingFetchVertexAttribute2(triangleIndicies.y, kVertexAttributeTexCoord0);
    v2.uv = UnityRayTracingFetchVertexAttribute2(triangleIndicies.z, kVertexAttributeTexCoord0);

    //Get the barycentric coordinates for this position.
    float3 barycentrics = float3(1.0 - attributes.barycentrics.x - attributes.barycentrics.y, attributes.barycentrics.x, attributes.barycentrics.y);

    return v0.uv * barycentrics.x + v1.uv * barycentrics.y + v2.uv * barycentrics.z;
}

float3 GetNormals(AttributeData attributes)
{
    //Gets the triangle in question. First by getting its index then the order the vertices are in.
    uint primitiveIndex = PrimitiveIndex();
    uint3 triangleIndicies = UnityRayTracingFetchTriangleIndices(primitiveIndex);
    Vertex v0, v1, v2;

    //Get the attributes of this vertex.
    v0.normal = UnityRayTracingFetchVertexAttribute3(triangleIndicies.x, kVertexAttributeNormal); //normal.
    v1.normal = UnityRayTracingFetchVertexAttribute3(triangleIndicies.y, kVertexAttributeNormal);
    v2.normal = UnityRayTracingFetchVertexAttribute3(triangleIndicies.z, kVertexAttributeNormal);

    //Interpolate the normal via the barycentric coordinate system.
    float3 barycentrics = float3(1.0 - attributes.barycentrics.x - attributes.barycentrics.y, attributes.barycentrics.x, attributes.barycentrics.y);

    return v0.normal * barycentrics.x + v1.normal * barycentrics.y + v2.normal * barycentrics.z;
}

float3 GetTangent(AttributeData attributes)
{
    //Gets the triangle in question. First by getting its index then the order the vertices are in.
    uint primitiveIndex = PrimitiveIndex();
    uint3 triangleIndicies = UnityRayTracingFetchTriangleIndices(primitiveIndex);
    Vertex v0, v1, v2;

    //Get the attributes of this vertex.
    v0.normal = UnityRayTracingFetchVertexAttribute3(triangleIndicies.x, kVertexAttributeTangent); //tangent.
    v1.normal = UnityRayTracingFetchVertexAttribute3(triangleIndicies.y, kVertexAttributeTangent);
    v2.normal = UnityRayTracingFetchVertexAttribute3(triangleIndicies.z, kVertexAttributeTangent);

    //Interpolate the normal via the barycentric coordinate system.
    float3 barycentrics = float3(1.0 - attributes.barycentrics.x - attributes.barycentrics.y, attributes.barycentrics.x, attributes.barycentrics.y);

    return v0.normal * barycentrics.x + v1.normal * barycentrics.y + v2.normal * barycentrics.z;
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

void GetTriangleNormalAndTSMatrix(float3 a, float3 b, float3 c, out float3 normal, out float3x3 tangentTransform) {

    float3 tangent = normalize(b - a);
    normal = normalize(cross(tangent, c - a));
    float3 bitangent = normalize(cross(tangent, normal));
    tangentTransform = transpose(float3x3(tangent, bitangent, normal));
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


//Box–Muller transform: https://developer.nvidia.com/gpugems/gpugems3/part-vi-gpu-computing/chapter-37-efficient-random-number-generation-and-application
float2 randGaussian(float3 pos, float offset) {
    float u1 = rand(pos, offset);
    float u2 = rand(pos, offset + 1);
    float theta = 2 * RUNITY_PI * u1;
    float rho = 0.164955 * sqrt(-2 * log(abs(u2) + 0.01));
    float z0 = rho * cos(theta) + 0.5;
    float z1 = rho * sin(theta) + 0.5;
    z0 = max(z0, 0);
    z0 = min(z0, 1);
    z1 = max(z1, 0);
    z1 = min(z1, 1);
    return float2(z0, z1);
}

float3 PointTangentToNormal(float3 p, float3 normal) {

    float3 helper = float3(1, 0, 0);
    if (abs(normal.x) > 0.99f)
        helper = float3(0, 0, 1);
    float3 tangent = normalize(cross(normal, helper));
    float3 binormal = normalize(cross(normal, tangent));
    return mul(p, float3x3(tangent, binormal, normal));
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
    float phi = 2 * RUNITY_PI * uv.y;

    float3 dir = float3(sin(theta) * cos(phi), sin(theta) * sin(phi), cos(theta));

    //Quick Guass test
    //float3 gaussianDistrib = float3(uv.x, uv.y, uv.z); //Range -1, 1.
    //float3 prandom =  normalize(gaussianDistrib) * radius;

    //Transform this direction to be on the hemisphere with the provided normal.
    float3 transformedDir = PointTangentToNormal(dir, normal);
    return transformedDir;
}

float sdot(float3 x, float3 y, float f = 1.0f)
{
    return saturate(dot(x, y) * f);
}

float3 ACESFilm(float3 x)
{
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}

float3 LinearToSRGB(float3 x)
{
    return (x < 0.0031308f) ?
                x * 12.92f :
                pow(x, 1.0f/2.4f) * 1.055f - 0.055f;
}

float3 HDRToOutput(float3 hdr, float exposure)
{
    // Exposure (tune the value to set the overall brightness;
    // positive makes it brighter, while negative makes it darker)
    hdr *= exp2(exposure);

    // Limit saturation to 99% - maps pure colors like (1, 0, 0) to (1, 0.01, 0.01)
    float3 maxComp = max(hdr.b,max(hdr.r, hdr.g));
    hdr = max(hdr, 0.01 * maxComp);

    // Apply tonemapping curve
    float3 ldrLinear = ACESFilm(hdr);

    // Convert to sRGB
    float3 ldrSRGB = LinearToSRGB(hdr);
    return ldrSRGB;
}

void InitiateReservoirPath(inout ReservoirPath reservoir, float3 position, float3 normal, float3 radiance)
{
    reservoir.wSum = 1.0;
    reservoir.M = 1;
    reservoir.radiance = radiance;
    reservoir.position = position;
    reservoir.normal = normal;
}

void CreateSurface(inout Surface surface, float3 position, float3 normal, float3 viewDir, float3 color, float emittance)
{
	surface.position = position;
	surface.normal = normal;
	surface.viewDir = viewDir;
	surface.color = color;
	surface.emittance = emittance;
}

bool addReservoirSamplePath(inout ReservoirPath reservoir, inout ReservoirPath newReservoir, float weight, float c, uint randSeed)
{
    float risWeight = weight * newReservoir.wSum * newReservoir.M;
    reservoir.M += c;
    reservoir.wSum += risWeight;

   
    if (risWeight >= rand(randSeed) * reservoir.wSum)
    {
        reservoir.position = newReservoir.position;
        reservoir.radiance = newReservoir.radiance;
        reservoir.normal = newReservoir.normal;
        return true;
    }

    return false;
}

bool addReservoirSample(inout Reservoir reservoir, uint lightX, float weight, float c, uint randSeed)
{
    reservoir.M += c;
    reservoir.wSum += weight;
    reservoir.pHat = weight;

    if (rand(randSeed) < weight / reservoir.wSum)
    {
        reservoir.Y = lightX;
        return true;
    }

    return false;
}

//Use these as a helper function.
float square(float x)
{
	return x * x;
}

float Schlick_Fresnel(float F0, float VdotH)
{
    return F0 + (1 - F0) * pow(max(1 - VdotH, 0), 5);
}

float3 Schlick_Fresnel(float3 F0, float VdotH)
{
    return F0 + (1 - F0) * pow(max(1 - VdotH, 0), 5);
}

float G_Smith_over_NdotV(float roughness, float NdotV, float NdotL)
{
    float alpha = square(roughness);
    float g1 = NdotV * sqrt(square(alpha) + (1.0 - square(alpha)) * square(NdotL));
    float g2 = NdotL * sqrt(square(alpha) + (1.0 - square(alpha)) * square(NdotV));
    return 2.0 * NdotL / (g1 + g2);
}

float3 GGX_times_NdotL(float3 V, float3 L, float3 N, float roughness, float3 F0)
{
    float3 H = normalize(L + V);

    float NoL = saturate(dot(N, L));
    float VoH = saturate(dot(V, H));
    float NoV = saturate(dot(N, V));
    float NoH = saturate(dot(N, H));

    if (NoL > 0)
    {
        float G = G_Smith_over_NdotV(roughness, NoV, NoL);
        float alpha = square(roughness);
        float D = square(alpha) / (RUNITY_PI * square(square(NoH) * square(alpha) + (1 - square(NoH))));

        float3 F = Schlick_Fresnel(F0, VoH);

        return F * (D * G / 4);
    }
    return 0;
}



float4 ComputeBRDFGI(Surface surface, float3 samplePosition)
{
    //This normal comes from the shader.
    float3 N = surface.normal;
    //view direction is from camera, ignore for now.
    float3 V = surface.viewDir;
    //obvious.
    float3 L = normalize(samplePosition - surface.position);

    float ndotL = dot(surface.normal, -L);

    //remove for surface material later.
	float3 kMinRoughness = 0.01;
    float roughness = 1.02;

    float3 specular = GGX_times_NdotL(V, L, surface.normal, max(roughness, kMinRoughness), roughness);

    return float4(specular, ndotL);
}

float3 GetTargetFunctionSurface(Surface surface, float3 samplePosition, float3 sampleRadiance)
{
    float4 BRDF = ComputeBRDFGI(surface, samplePosition);
    float3 reflectedRadiance = sampleRadiance * (surface.color.xyz + BRDF.xyz);

    return reflectedRadiance;
}

void CreateSample(inout RayDesc ray, inout Payload payload, float3 chosenCameraPosition, float3 chosenDirection)
{
    uint3 id = DispatchRaysIndex();
    uint3 dim = DispatchRaysDimensions();

    //Convert to 0 - 1.
    float2 pixel = ((id.xy + float2(0.5, 0.5)) / float2(dim.x, dim.y)) * 2 - 1;

    ray.Origin = chosenCameraPosition;
    ray.Direction = chosenDirection;
    ray.TMin = 0;
    ray.TMax = 10000;

    payload.color = float4(1, 1, 1, 0);
    payload.distance = 99999;
    payload.direction = chosenDirection;
    payload.pixel = pixel + _Time.xy;
    payload.uv = ((id.xy + float2(0.5, 0.5)) / float2(dim.x, dim.y));
}


float4 AreaLightSample(float3 position, uint seed, UnigmaLight lightSource)
{

    float2 xy = randGaussian(position, rand(seed));
    float2 xz = randGaussian(position, rand(seed+42));
    float3 uv = float3(xy, xz.x);

    float3 area = lightSource.area;

    float4 lightSample = 1.0;
    lightSample.xyz = (uv * area) + lightSource.position;
    lightSample.w = lightSource.emission;

    return lightSample;
}

float GetStylizedLighting(inout Reservoir reservoir, UnigmaLight lightSource, float3 origin, in Payload Sx1Payload, uint seed)
{
    uint lightIndex = reservoir.Y; //This new light index comes from the weighted reservoir we computed.
    float4 lightSample = AreaLightSample(origin, seed, lightSource);
    float3 toLight = normalize(lightSample.xyz - origin);
    float Le = lightSource.emission;

	float NdotL = saturate(dot(Sx1Payload.normal, toLight));
    
    float4 midTones = 0.5 * step(0.2, NdotL);
    float4 shadows = 0 * step(NdotL, 0.4);
    float4 highlights = 1 * step(0.6, NdotL);

    float4 BRDF = max(midTones, shadows);
    BRDF = max(BRDF, highlights);
    
    float Gx = min(50000, 1.0f / (distance(lightSample.xyz, origin)));

    
    return BRDF * Gx * Le;
}

float GetDiffuseLighting(inout Reservoir reservoir, UnigmaLight lightSource, float3 origin, in Payload Sx1Payload, uint seed)
{
    uint lightIndex = reservoir.Y; //This new light index comes from the weighted reservoir we computed.
    float4 lightSample = AreaLightSample(origin, seed, lightSource);
    float3 toLight = normalize(lightSample.xyz - origin);
    //Finally compute the brdf for this light.
    float Gx = min(50000, 1.0f / (distance(lightSample.xyz, origin)));
    float Le = lightSource.emission;
    float BRDF = (1.0f / RUNITY_PI) * sdot(Sx1Payload.normal, toLight);

    //Target function.
    float4 pHat = BRDF * Le * Gx;

    return pHat;
}


float GetTargetFunction(inout Reservoir reservoir, UnigmaLight lightSource, float3 origin, in Payload Sx1Payload, uint seed)
{
	return GetStylizedLighting(reservoir, lightSource, origin, Sx1Payload, seed);
}

float UpdateReservoirWeight(inout Reservoir reservoir, UnigmaLight lightSource, float3 origin, in Payload Sx1Payload, uint seed)
{

    //Target function.
    float4 pHat = GetStylizedLighting(reservoir, lightSource, origin, Sx1Payload, seed);

    reservoir.W = pHat > 0.0 ? (reservoir.wSum / reservoir.M) / pHat : 0.0;
    reservoir.pHat = pHat;

    return pHat;
}

float hash(float n)
{
    return frac(sin(n) * 43758.5453);
}
     
float pNoise(float3 x)
{
        // The noise function returns a value in the range -1.0f -> 1.0f
    float3 p = floor(x);
    float3 f = frac(x);
     
    f = f * f * (3.0 - 2.0 * f);
    float n = p.x + p.y * 57.0 + 113.0 * p.z;
     
    return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
               lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
               lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
               lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
}