Shader "Unlit/DepthShadowsRaytracingShader"
{

    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Pass
        {
            Name "DepthShadowsRaytracingShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../../RayTraceHelpersUnigma.hlsl"

            Texture2D<float4> _MainTex;
            SamplerState sampler_MainTex;

            [shader("closesthit")]
            void MyHitShader(inout Payload payload : SV_RayPayload,
                AttributeData attributes : SV_IntersectionAttributes)
            {
                float2 uvs = GetUVs(attributes);
                float3 normals = GetNormals(attributes);
                //float3 worldNormal = mul((float4x4)unity_ObjectToWorld, float4(normals, 0)).xyz;

                float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);

                //payload.distance = attributes.distance;
                //payload.color = float4(attributes.position, PrimitiveIndex());
                payload.color = 1;
            }

            ENDHLSL
        }
    }
}
