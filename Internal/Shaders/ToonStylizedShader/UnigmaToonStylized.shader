Shader "Unigma/UnigmaToonStylized"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
	    _Midtone("Midtone", Color) = (1,1,1,1)
		_Shadow("Shadow", Color) = (1,1,1,1)
		_Highlight("Highlight", Color) = (1,1,1,1)
		_Thresholds("Light thresholds", Vector) = (0.2, 0.4, 0.6, 0.8)
        
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent"
        "LightMode" = "ForwardBase" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _Midtone;
			float4 _Shadow;
			float4 _Highlight;
			float4 _Thresholds;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //Three colors shadow, midtone, highlight.
                //Each of this colors are on different normals of a percieved box ...
                //The normals are NOT interpolated and it is a flat shading.
                float4 normals = float4(i.normal, 1);
                float3 lightDirAbsolute = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightDir = normalize(lightDirAbsolute);
                
				float NdotL = dot(i.normal, lightDir);
                
				float4 midTones = _Midtone * step(_Thresholds.x, NdotL);
				float4 shadows = _Shadow * step(NdotL, _Thresholds.y);
				float4 highlights = _Highlight * step(_Thresholds.z, NdotL);
                
				float4 finalColor = max(midTones, shadows);
				finalColor = max(finalColor, highlights);
                
                return finalColor;
                
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
