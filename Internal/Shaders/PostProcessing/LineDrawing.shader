Shader "UnigmaPP/LineDrawing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_LineColors("LineColor", Color) = (0,0,0,0)
		_LineWeight("LineWeight", Range(0,10)) = 1
		_FadingColor("LineColor", Color) = (0,0,0,0)
		_FadingIntensity("Fading Intensity", Range(0,1)) = 0.75
		_PaperColor("Paper Color", Color) = (1,1,1,1)
		_PaperBackground("Is Paper Background?", Range(0,1)) = 0
    }
    SubShader
    {

		Tags{
				"RenderType" = "Opaque"
			}

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
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex, _CameraDepthTexture;
			float2 _MainTex_TexelSize;
			float _LineWeight;
			float4 _LineColors;
			float4 _FadingColor;
			float _FadingIntensity;
			float4 _PaperColor;
			float _PaperBackground;

			float3 sobel(float2 uv, float depth) {
				float x = 0;
				float y = 0;
				if(_FadingIntensity > 0.0)
				{
					_LineWeight = max(_LineWeight * depth, 0.25);
					_LineWeight = min(_LineWeight, 10);
				}

				float2 texelSize = (_LineWeight)*_MainTex_TexelSize*0.1;

				//Kernal for computing edges of images.
				//Computing the gradient of the image to detect edges:
				//X direction kernel. [-1 | 0 | 1]
				//					  [-2 | 0 | 2]
				//					  [-1 | 0 | 1] //This computes the derivative in the x direction.
				//If the sum is more than 0, or very large then an edge is detected.
				x += tex2D(_MainTex, uv + float2(-texelSize.x, -texelSize.y)) * -1.0;
				x += tex2D(_MainTex, uv + float2(-texelSize.x, 0)) * -2.0;
				x += tex2D(_MainTex, uv + float2(-texelSize.x, texelSize.y)) * -1.0;

				x += tex2D(_MainTex, uv + float2(texelSize.x, -texelSize.y)) *  1.0;
				x += tex2D(_MainTex, uv + float2(texelSize.x, 0)) *  2.0;
				x += tex2D(_MainTex, uv + float2(texelSize.x, texelSize.y)) *  1.0;

				//Y direction kernel. [-1 |-2 |-1]
				//					  [ 0 | 0 | 0]
				//					  [ 1 | 2 | 1] //The transpose of the X direction.
				y += tex2D(_MainTex, uv + float2(-texelSize.x, -texelSize.y)) * -1.0;
				y += tex2D(_MainTex, uv + float2(0, -texelSize.y)) * -2.0;
				y += tex2D(_MainTex, uv + float2(texelSize.x, -texelSize.y)) * -1.0;

				y += tex2D(_MainTex, uv + float2(-texelSize.x, texelSize.y)) *  1.0;
				y += tex2D(_MainTex, uv + float2(0, texelSize.y)) *  2.0;
				y += tex2D(_MainTex, uv + float2(texelSize.x, texelSize.y)) *  1.0;

				//Combines the two directions by taking the pythagoren theorem.
				//Total magnitude. How big is the edge at this location. 
				float mag = sqrt(x*x + y * y);
				float angle = atan2(y,x);
				return float3(mag, angle, 0);
			}

            fixed4 frag (v2f i) : SV_Target
            {
				float depth = tex2D(_CameraDepthTexture, i.uv).r;
				depth = Linear01Depth(depth);
				depth = depth * (_ProjectionParams.z);
				depth *= 0.015; //Scales the depth so that lighter line weights closer.
				//Gets the screen texture.
				fixed4 col = tex2D(_MainTex, i.uv);
				//Gets the lines based on the sobel kernel.
				float3 sobelColors;

				if (_PaperBackground > 0) {
					sobelColors = (col*(1-_PaperColor.a))+_PaperColor - sobel(i.uv, depth).xxx*(1 - _LineColors);
					if (depth > 10 && _FadingIntensity > 0.0)
						return (col*(1 - _PaperColor.a)) +_PaperColor;
				}
				else {
					 sobelColors = (col)+(_FadingColor*col*depth*_FadingIntensity) - (sobel(i.uv, depth).xxx*(1-_LineColors));
					if (depth > 10 && _FadingIntensity > 0.0)
						return col;
				}


				return fixed4(sobelColors, 1.0);
            }
            ENDCG
        }
    }
}
