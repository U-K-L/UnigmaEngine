#define UnigmaToonShadow


Texture2D<float4> _MainTex, _UnigmaNormal, _NormalMap;
SamplerState sampler_MainTex, sampler_UnigmaNormal, sampler_NormalMap;
float4 _MainColor;
float4 _NormalMap_ST;
float4 _Shadow;
float4 _Highlight;
float4 _Thresholds;
float _Smoothness, _LightAbsorbtion, _ReceiveShadow;
float4 _ShadowColors;
            
[shader("closesthit")]
void MyHitShader(inout Payload payload : SV_RayPayload, AttributeData attributes : SV_IntersectionAttributes)
{
    float2 uvs = GetUVs(attributes) * _NormalMap_ST.xy;
    float3 normals = GetNormals(attributes);
    //float3 worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz);



    //Get Texture.
    float3 tangent = GetTangent(attributes);
    float3 bitangent = normalize(cross(normals, tangent));

    //Create tagents from normal maps...
    float3 worldNormal = mul(ObjectToWorld3x4(), normals);
    float3 worldTangent = mul(ObjectToWorld3x4(), tangent);

    float3 binormal = cross(normals, tangent); // *input.tangent.w;
    float3 worldBinormal = mul(ObjectToWorld3x4(), binormal);

    float3 N = normalize(worldNormal);
    float3 T = normalize(worldTangent);
    float3 B = normalize(worldBinormal);

    float3 tangentNormal = _NormalMap.SampleLevel(sampler_NormalMap, uvs, 0).xyz;
    tangentNormal = normalize(tangentNormal * 2 - 1);
    float3x3 TBN = float3x3(normalize(T), normalize(B), normalize(N));
    TBN = transpose(TBN);
    float3 worldNormalTBN = mul(TBN, tangentNormal);

    float3x3 tangentMatrix = transpose(float3x3(tangent, bitangent, normals));

                //Get from Texture2D
                //normals = _UnigmaNormal.SampleLevel(sampler_UnigmaNormal, payload.uv, 0).xyz;
    float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);

                //Combine tangent normals with vertex normals
    worldNormal = worldNormalTBN * _NormalMap_ST.z + normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz) * _NormalMap_ST.w;
                
    float3 rgbNormalMap = tex.xzy * 2 - 1;

                //Wacky tests to set it to certain types of normal maps.
    if (_NormalMap.SampleLevel(sampler_NormalMap, uvs, 0).w <= 0) //If there isn't a normal map texture.
        worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz); //Set to regular world normal based on vertices.
    if (_MainTex.SampleLevel(sampler_MainTex, uvs, 0).w > 0) //If there is a RGB normal map.
    {
        worldNormal = normalize(mul(ObjectToWorld3x4(), float4(rgbNormalMap, 0)).xyz); //Set to RGB normal map.
    }

                

                //payload.normal = float4(worldNormal, 1);

                //Make normal based on physical light characteristics.

    float2 rxy = randGaussian(float3(payload.pixel.xyy), rand(payload.pixel.x));
    float2 rxz = randGaussian(float3(payload.pixel.xy + 2452, payload.pixel.x), rand(payload.pixel.y));
    float3 ruv = float3(rxy, rxz.x);
                
    float3 diffuse = RandomPointOnHemisphere(payload.pixel, worldNormal.xyz, payload.pixel) * 0.01;
    float3 specular = reflect(WorldRayDirection().xyz, worldNormal.xyz);

    float lightAbsorbed = _LightAbsorbtion < ruv.x;

    payload.normal = float4(specular, _ReceiveShadow);


    float3 position = WorldRayOrigin() + WorldRayDirection() * (RayTCurrent() - 0.00001);
                //float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);

    payload.distance = RayTCurrent();
    payload.color = float4(1, 1, _Smoothness, InstanceID());
                //Act as color
    float3 lightDirAbsolute = normalize(_WorldSpaceLightPos0.xyz);
    float3 lightDir = normalize(lightDirAbsolute);

    float NdotL = dot(worldNormal, lightDir);

    float4 midTones = _MainColor * step(_Thresholds.x, NdotL);
    float4 shadows = _Shadow * step(NdotL, _Thresholds.y);
    float4 highlights = _Highlight * step(_Thresholds.z, NdotL);

    float4 finalColor = max(midTones, shadows);
    finalColor = max(finalColor, highlights);
    float distSquared = max(0.01, min(1, 1 / (RayTCurrent() * RayTCurrent())));
                //Make different reflection models.
    payload.direction = finalColor; // *distSquared;//_Midtone* distSquared;//float4(normals, 1

                //Put this into uv and pixel
    payload.uv = _ShadowColors.xy;
    payload.pixel = _ShadowColors.zw;
                /*
                if(InstanceID() == payload.color.w)
                    //Incode self-shadows as y
                    
                else
                    //Encode cast shadows as x.
                    payload.color = float4(1,0,0, InstanceID());
                    */
                //payload.color = 1;
}