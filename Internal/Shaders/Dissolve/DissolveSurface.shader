Shader "Unigma/DissolveSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0

		//Dissolve shader properties.
		_DissolveTexture("Dissolve Texture", 2D) = "white"{}
		_Amount("Amount", Range(0,1)) = 0
		_OutlineColor("Outline Color", color) = (1,1,1)
		_FadedColor("Faded Color", Color) = (1, 1, 1, 1)
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
		sampler2D _DissolveTexture;
		half _Amount;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		fixed3 _OutlineColor;
		fixed4 _FadedColor;

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
			//Get the dissolve texture value.
			half dissolve_value = tex2D(_DissolveTexture, IN.uv_MainTex).r;
			half dissolved = _Amount- dissolve_value;

			//clip(dissolved); //Performs the actual dissolve amount. Discards any pixel less than 0 value.

			if(_Amount < 1)
				o.Emission = _OutlineColor * step(dissolved, 0.13f);
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
			if (dissolved < 0) {
				o.Albedo *= _FadedColor;
				o.Emission = 0;
			}
        }
        ENDCG
    }
    FallBack "Diffuse"
}
