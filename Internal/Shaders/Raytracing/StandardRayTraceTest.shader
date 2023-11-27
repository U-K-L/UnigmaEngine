Shader "Custom/StandardRayTraceTest"
{

    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Pass
        {
            Name "MyRaytraceShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyHitShader
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../RayTraceHelpersUnigma.hlsl"
            


            Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;

            [shader("intersection")]
            void IntersectionMain()
            {
                float3 ro = ObjectRayOrigin();
                float3 rd = normalize(ObjectRayDirection());
                AttributeData attr;
                attr.barycentrics = float2(0, 0);
				float4 sphere = float4(0, 0, 0, 0.5);
                float t1 = sphIntersect(ro, rd, sphere);
                ReportHit(t1, 0, attr);
            }
            
            [shader("closesthit")]
            void MyHitShader(inout Payload payload : SV_RayPayload,
                AttributeData attributes : SV_IntersectionAttributes)
            {
                float2 uvs = GetUVs(attributes);
                float3 normals = GetNormals(attributes);
                //float3 worldNormal = mul((float4x4)unity_ObjectToWorld, float4(normals, 0)).xyz;

				float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);

                payload.color = float4(normals, 1);
                
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
