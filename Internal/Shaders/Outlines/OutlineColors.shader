Shader "Unigma/OutlineColors"
{
    Properties
    {
		_ThicknessTexture("Outline Thickness texture", 2D) = "black" {}
		_OutlineColor("Color", Color) = (1,1,1,1)
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

            float4 _OutlineColor, _ThicknessTexture_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _ThicknessTexture);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            sampler2D _ThicknessTexture;

            fixed4 frag(v2f i) : SV_Target
            {
				float4 texcol = tex2D(_ThicknessTexture, i.uv);
                float thickness = 1;//dot(texcol, texcol) / 3.0;
                float4 finalOutput = float4(_OutlineColor.xyz, thickness);
				return finalOutput* thickness;
            }
            ENDCG
        }

        //Pass 2.
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

            float4 _OutlineInnerColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineInnerColor;
            }
            ENDCG
        }
    }
}
