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
    
};

struct AttributeData
{
    float2 barycentrics;
    float distance;
    float3 position;
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