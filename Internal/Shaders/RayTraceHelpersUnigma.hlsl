struct Vertex
{
	float3 position;
	float3 normal;
	float2 uv;
};

struct Payload
{
    float4 color;
    
};

struct AttributeData
{
    float2 barycentrics;
    float2 texcoord;
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