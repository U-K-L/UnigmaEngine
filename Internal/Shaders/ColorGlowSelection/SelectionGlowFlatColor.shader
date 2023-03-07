Shader "Unlit/SelectionGlowFlatColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _Prepass;
            sampler2D _SelectionPrePassTex;
            sampler2D _SelectionBlurredTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            half3 Sample (float2 uv) {
			    return tex2D(_MainTex, uv).rgb;
	        }

		    half3 SampleBox (float2 uv, float delta) {
			    float4 o = _MainTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
			    half3 s =
				    Sample(uv + o.xy) + Sample(uv + o.zy) +
				    Sample(uv + o.xw) + Sample(uv + o.zw);
			    return s * 0.25f;
		    }

            fixed4 frag (v2f i) : SV_Target
            {
                return half4(SampleBox(i.uv, 1), 1);
            }
            ENDCG
        }

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _Prepass;
            sampler2D _SelectionPrePassTex;
            sampler2D _SelectionBlurredTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            half3 Sample (float2 uv) {
			    return tex2D(_MainTex, uv).rgb;
	        }

		    half3 SampleBox (float2 uv, float delta) {
			    float4 o = _MainTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
			    half3 s =
				    Sample(uv + o.xy) + Sample(uv + o.zy) +
				    Sample(uv + o.xw) + Sample(uv + o.zw);
			    return s * 0.25f;
		    }

            fixed4 frag (v2f i) : SV_Target
            {
                return half4(SampleBox(i.uv, 0.5), 1);
            }
            ENDCG
        }
    }
}
