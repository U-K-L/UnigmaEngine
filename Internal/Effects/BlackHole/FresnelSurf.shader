Shader "Unigma/FresnelSurf"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_Emission("Emission", Range(0,100)) = 1
		_InnerStr("Inner Radius", Range(-1,1)) = 0.5
		_OutterStr("Outter Radius", Range(0,2)) = 1

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
			float3 worldNormal;
			float3 viewDir;
			INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		float _Emission;
		float _InnerStr;
		float _OutterStr;

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

			//Apply effect of fresnal.
			_Emission *= _Emission * _Emission;
			float fresnel = ((-_Emission+ (_Emission*_InnerStr)) - (dot(IN.worldNormal, IN.viewDir) *(_Emission +  (_Emission* _OutterStr) )));
			//fresnel = saturate(1-fresnel); //Clamps value between 0 and 1.
			o.Emission = (_Emission + fresnel)*(_Color*3);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
