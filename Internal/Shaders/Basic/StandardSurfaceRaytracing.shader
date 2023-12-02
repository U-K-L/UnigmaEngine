Shader "Custom/StandardSurfaceRaytracing"
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
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }


    SubShader
    {
        Pass
        {
            Name "MyRaytraceShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../RayTraceHelpersUnigma.hlsl"


        float RaySphereIntersect(float3 orig, float3 dir, float radius)
        {
            float a = dot(dir, dir);
            float b = 2 * dot(orig, dir);
            float c = dot(orig, orig) - radius * radius;
            float delta2 = b * b - 4 * a * c;
            float t = -1.0f;

            if (delta2 >= 0)
            {
                float t0 = (-b + sqrt(delta2)) / (2 * a);
                float t1 = (-b - sqrt(delta2)) / (2 * a);

                // Get the smallest root larger than 0 (t is in object space);
                t = max(t0, t1);

                if (t0 >= 0)
                    t = min(t, t0);

                if (t1 >= 0)
                    t = min(t, t1);

                float3 localPos = orig + t * dir;

                float3 worldPos = mul(ObjectToWorld(), float4(localPos, 1));

                t = length(worldPos - WorldRayOrigin());
            }

            return t;
        }
        [shader("closesthit")]
        void MyHitShader(inout Payload payload : SV_RayPayload,
            AttributeData attributes : SV_IntersectionAttributes)
        {
            payload.color = 0;//float4(normals, 1);

        }

        ENDHLSL
    }

}
