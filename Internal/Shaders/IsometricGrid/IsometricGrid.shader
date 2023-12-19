Shader "Unlit/IsometricGrid"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OverlayImage("Overlay Image for Testing", 2D) = "white" {}
        _GridOpacity ("Grid Opacity", Range(0,1)) = 0.1
        _GridSize ("Grid Size", Range(0,5.0)) = 1.0
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
            #include "../ShaderHelpers.hlsl"

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

            sampler2D _MainTex, _OverlayImage;
            float4 _MainTex_ST;
            float _GridOpacity, _GridSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {

                fixed4 originalImage = tex2D(_MainTex, i.uv);
                fixed4 overlayImage = tex2D(_OverlayImage, i.uv);
                float2 uv = i.uv - float2(0.5, 0.5);
                uv.y *= 2.0;

                float lThick = 0.05* _GridSize;
                float lAmount = 10.0* _GridSize;
                float rad1 = 0.785398;
                float rad2 = rad1 + UNITY_PI / 2.0;
                float hLine = step(frac((uv.y + uv.x * sin(rad1)) * lAmount), lThick);
                float vLine = step(frac((uv.x + uv.y * sin(rad2)) * lAmount), lThick);

                float pline = cos(rad1) * uv.x + sin(rad1) * uv.y;
                pline = step(frac(abs(pline) * lAmount), lThick);


                float hline = cos(rad2) * uv.x + sin(rad2) * uv.y;
                hline = step(frac(abs(hline) * lAmount), lThick);
                float4 grid = float4(hline, pline, pline, 1.0);

                float4 finalResult = grid;
                return (overlayImage*0.5* _GridOpacity) + _GridOpacity*finalResult + originalImage;
            }
            ENDCG
        }
    }
}
