Shader "Hidden/StarPatternShader"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _TransitionTexture("Transition Texture", 2D) = "white" {}
        _BackgroundTexture("Background Texture", 2D) = "white" {}
        _Transition("Transition Slider", Range(0,1)) = 0
        _SurfaceNoiseScroll("Surface Noise Scroll Amount", Vector) = (0.03, 0.03, 0, 0)
        _RefracStr("Refractance Strength, amp, speed.", Vector) = (12,2.0,0.002,1)
        _Intensity("Intensity of displacement", Range(0, 2)) = 1
        [Normal]_DisplacementTex("Displacement Map", 2D) = "white"{}
    }
    SubShader
    {
        // No culling or depth
		Cull Off ZWrite On ZTest LEqual
        //Add transparency
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPosition : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPosition = ComputeScreenPos(o.vertex);
                return o;
            }

            sampler2D _MainTex, _TransitionTexture, _DisplacementTex, _BackgroundTexture;
            float _Transition, _Intensity;
            float4 _SurfaceNoiseScroll, _RefracStr, _Color;

            float sdCircle(float2 p, float r)
            {
                return length(p) - r;
            }
            float2 dot2(float2 p)
            {
                return dot(p, p);
            }
            float sdHeart(float2 p, float r)
            {
                p.x = abs(p.x);

                if (p.y + p.x > 1.0)
                    return sqrt(dot2(p - float2(0.25, 0.75))) - sqrt(2.0) / 4.0;
                return sqrt(min(dot2(p - float2(0.00, 1.00)),
                    dot2(p - 0.5 * max(p.x + p.y, 0.0)))) * sign(p.x - p.y);
            }

            float4 vec4(float3 vec)
            {
				return float4(vec.x, vec.y, vec.z, 1);
            }
            

            float4 vec4(float vec)
            {
                return float4(vec, vec, vec, 1);
            }
            
            fixed4 frag(v2f i) : SV_Target{

                //Get a circle given the current coordinates of the uv.
				float circle = sdCircle(i.uv, 0.5);
                
                return vec4(circle);
            }
                
            ENDCG
        }
    }
}
