Shader "Unlit/IsometricDepthNormals"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Fade("Fade camera", Range(0,100000)) = 1
		_NormalAmount("Normal amount", Range(0,50)) = 1
		_DepthAmount("Depth amount", Range(0,50)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
				float depthGen : TEXCOORD1;
				float3 normal : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float _Fade, _NormalAmount, _DepthAmount;

            v2f vert (appdata v)
            {
                v2f o;
                float4 vertexProgjPos = mul(UNITY_MATRIX_MV, v.vertex);
                o.depthGen = saturate((-vertexProgjPos.z - _ProjectionParams.y) / (_Fade + 0.001));
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return float4(pow(i.normal, _NormalAmount), pow(i.depthGen, _DepthAmount));
            }
            ENDCG
        }
    }
}
