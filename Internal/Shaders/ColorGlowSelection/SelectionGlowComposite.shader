Shader "Unlit/SelectionGlowComposite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Prepass ("Texture", 2D) = "white" {}
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 prepassText = tex2D(_SelectionPrePassTex, i.uv);
                fixed4 blurrText = tex2D(_SelectionBlurredTex, i.uv);
                float4 screenText = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                float4 result = screenText * prepassText;
                float4 mask = (1-step(0.0001, prepassText.r));
                float4 subtract = mask*blurrText;
                /*
                if(length(subtract) > 0)
                    return subtract*screenText;
                return screenText;
                */
                return blurrText;
            }
            ENDCG
        }
    }
}
