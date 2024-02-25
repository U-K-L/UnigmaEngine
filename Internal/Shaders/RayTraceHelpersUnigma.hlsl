#define RUNITY_PI 3.14159265359


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

bool addReservoirSamplePath(inout ReservoirPath reservoir, inout ReservoirPath newReservoir, float weight, float c, float2 randSeed)
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
    float roughness = 10.02;

    float3 specular = GGX_times_NdotL(V, L, surface.normal, max(roughness, kMinRoughness), roughness);

    return float4(specular, ndotL);
}

float3 GetTargetFunctionSurface(Surface surface, float3 samplePosition, float3 sampleRadiance)
{
    float4 BRDF = ComputeBRDFGI(surface, samplePosition);
    float3 reflectedRadiance = sampleRadiance * (BRDF.w * surface.color.xyz + BRDF.xyz);

    return reflectedRadiance;
}
