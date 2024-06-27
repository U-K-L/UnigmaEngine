Shader "Custom/WaterSurface"
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
        #pragma surface surf Standard

#pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
#pragma editor_sync_compilation
#pragma target 4.5
        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        #include "../../FluidHelpers.hlsl"
#ifdef SHADER_API_D3D11
        StructuredBuffer<Particle> _Particles;
#endif
        struct Input {
            float3 worldPos;
            float2 uv_MainTex;
        };

        float _Smoothness;

        void ConfigureSurface(Input input, inout SurfaceOutputStandard surface) {
            surface.Albedo = saturate(input.worldPos * 0.5 + 0.5);
            surface.Smoothness = _Smoothness;
        }

        void ConfigureProcedural() {
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
            //UNITY_SETUP_INSTANCE_ID(v);
            //float3 position = float3(sin(_Time.x) * 100, unity_InstanceID, cos(_Time.x)*100);
            float3 position = _Particles[unity_InstanceID].position;
            unity_ObjectToWorld = 0.0;
            unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
            unity_ObjectToWorld._m00_m11_m22 = 0.220875;
#endif
        }


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
    FallBack "Diffuse"
}
