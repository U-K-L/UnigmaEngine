struct Vertex
{
    float2 texcoord;
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
    v0.texcoord = UnityRayTracingFetchVertexAttribute2(triangleIndicies.x, kVertexAttributeTexCoord0); //tex coordinate.
    v1.texcoord = UnityRayTracingFetchVertexAttribute2(triangleIndicies.y, kVertexAttributeTexCoord0);
    v2.texcoord = UnityRayTracingFetchVertexAttribute2(triangleIndicies.z, kVertexAttributeTexCoord0);

    //Get the barycentric coordinates for this position.
    float3 barycentrics = float3(1.0 - attributes.barycentrics.x - attributes.barycentrics.y, attributes.barycentrics.x, attributes.barycentrics.y);

    return v0.texcoord * barycentrics.x + v1.texcoord * barycentrics.y + v2.texcoord * barycentrics.z;
}