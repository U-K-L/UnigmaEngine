Shader "Hidden/BubbleGumShader"
{
    Properties
    {
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
            float4 _SurfaceNoiseScroll, _RefracStr;

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

            fixed4 frag (v2f i) : SV_Target
            {

                float3 flowDirection = _SurfaceNoiseScroll.xyz * _SurfaceNoiseScroll.w;
                float3 noiseUV = float3(i.uv.x + _Time.y * flowDirection.x, i.uv.y + _Time.y * flowDirection.y, i.uv.y + _Time.y * flowDirection.z);



                float2 textureCoordinate = i.screenPosition.xy / i.screenPosition.w;

                float XUV = textureCoordinate.x * _RefracStr.x + _Time.y * _RefracStr.y;
                float YUV = textureCoordinate.y * _RefracStr.x + _Time.y * _RefracStr.y;
                float2 screenPosUV = textureCoordinate;
                screenPosUV.y += cos(XUV + YUV) * _RefracStr.z * cos(YUV);
                screenPosUV.x += sin(XUV - YUV) * _RefracStr.z * sin(YUV);

                //Create paintery effect for that under the water.
                float2 uv = i.screenPosition.xy / i.screenPosition.w;
                float3 diplacementNormals = UnpackNormal(tex2D(_DisplacementTex, screenPosUV));
                float2 distortion = uv + ((_Intensity * 0.01) * diplacementNormals.rg);

                float2 backgroundUVs = (screenPosUV + noiseUV)*0.75;

                float2 posUV = textureCoordinate;
                float cutoff = tex2D(_TransitionTexture, ((screenPosUV - 0.5) * (_Transition*50) +0.5)).r;//sdHeart(posUV, _Transition);

                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 tex = tex2D(_BackgroundTexture, backgroundUVs);
                tex.b *= 1.5;

                col = lerp(col, tex, step(cutoff, 0 ));
                col = lerp(tex, col, step(_Transition, 0.999));
                return col;
            }
            ENDCG
        }
    }
}
