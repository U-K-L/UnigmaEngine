Shader "Unlit/UnigmaBackground"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SkyboxTexture("Texture", 2D) = "white" {}
		_Dithering("Dithering", Range(0, 1)) = 0.5
		_DitherTexture("Dither Texture", 2D) = "white" {}
		_BottomColor("Bottom Color", Color) = (0.0, 0.0, 0.0, 1.0)
		_TopColor("Top Color", Color) = (0.0, 0.0, 0.0, 1.0)
		_Brightness("brightness", Range(0, 1)) = 1.0
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
                float4 vertex : SV_POSITION;
                float4 screenPosition : TEXCOORD1;
            };

            sampler2D _MainTex, _UnigmaDepthShadowsMap, _SkyboxTexture, _DitherTexture, _UnigmaComposite, _UnigmaDepthMap, _UnigmaBackgroundColor, _UnigmaFluidsFinal;
            float4 _MainTex_ST, _DitherTexture_TexelSize, _BottomColor, _TopColor;
			float _Dithering, _Brightness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPosition = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 fluidFinal = tex2D(_UnigmaFluidsFinal, i.uv);
				fixed4 compositeCol = tex2D(_UnigmaComposite, i.uv);
				fixed4 skyboxTexture = tex2D(_SkyboxTexture, i.uv);
                fixed4 _UnigmaDepthShadows = tex2D(_UnigmaDepthShadowsMap, i.uv);
                float lightedAreas = step(length(compositeCol.xyz), 0.00001);
                float unlightAreas = step(lightedAreas, 0.01);

                fixed4 col = tex2D(_UnigmaBackgroundColor, i.uv);

                return col;
                
                //return _UnigmaDepthShadows.r;
				float4 gradientYcolor = float4(0.9, 0.85, 0.92, 1.0);
                //Make gradient lerp from color
                gradientYcolor = lerp(_BottomColor, _TopColor, 1.0-i.uv.y );

                float blackWhiteGradientBandsMask1 = step(frac((1.0 * (100.1*(1.0 - i.uv.y*1.25)) - i.uv.y) * (i.uv.y * 2)), 0.31) * step(i.uv.y, 0.35);
                float blackWhiteGradientBandsMask2 = step(i.uv.y, 0.35) + step(0.75, i.uv.y);//step(frac((1.0 - i.uv.y) * (i.uv.y * 300)), 0.0021);
                float4 DitherMask = blackWhiteGradientBandsMask1  *blackWhiteGradientBandsMask2;
				//return DitherMask*10000;
                

                float blackWhiteGradientBandsMask3 = frac((1.0 - i.uv.y) * (i.uv.y * 2));
                //return blackWhiteGradientBandsMask3;
                
                //value from the dither pattern
                float2 screenPos = i.screenPosition.xy / i.screenPosition.w;
                float2 ditherCoordinate = screenPos * _ScreenParams.xy * _DitherTexture_TexelSize.xy*0.9;
                
                float ditherValue = tex2D(_DitherTexture, ditherCoordinate).r;

                //combine dither pattern with texture value to get final result
                float dith = step(ditherValue, blackWhiteGradientBandsMask3);
                
                float ditherMask2 =  dith + DitherMask;

                float4 colorDither = (lerp(gradientYcolor, _TopColor, ditherMask2)* _Brightness) + skyboxTexture;

                //if(_UnigmaDepthShadows.r < 0.0000001)
                //   return (colorDither *lightedAreas) + col * unlightAreas;

                float4 backGroundColor = col * unlightAreas;

                float4 fluidBg = lerp(colorDither, fluidFinal * 0.9 + colorDither*0.25, min(1, fluidFinal.w*60)) * lightedAreas;
                return fluidBg + backGroundColor;
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

            sampler2D _MainTex, _UnigmaComposite;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                float2 uv = float2(i.uv.x, 1.0-i.uv.y);
                fixed4 col = tex2D(_MainTex, uv);
                return col;
            }
            ENDCG
        }

    }
}
