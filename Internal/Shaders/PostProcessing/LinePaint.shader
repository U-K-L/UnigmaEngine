Shader "UnigmaPP/LinePaint"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_KernelSize("Kernel Size (N)", Int) = 7
		_Distance("Distance", Range(0,2)) = 1

		_LineColors("LineColor", Color) = (0,0,0,0)
		_LineWeight("LineWeight", Range(0,10)) = 1
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }

			Pass
			{
				CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag

				#include "UnityCG.cginc"

				sampler2D _MainTex, _CameraDepthTexture;
				float2 _MainTex_TexelSize;
				float _Distance;

				int _KernelSize;
				struct region
				{
					float3 mean;
					float variance;
				};


				//Gets the region to perform kuwahara filter
				region calcRegion(int2 lower, int2 upper, int samples, float2 uv, float depth)
				{
					region r;
					float3 sum = 0.0;
					float3 squareSum = 0.0;

					for (int x = lower.x; x <= upper.x; ++x)
					{
						for (int y = lower.y; y <= upper.y; ++y)
						{
							fixed2 offset = fixed2(_MainTex_TexelSize.x * x, _MainTex_TexelSize.y * y);
							fixed3 tex = tex2D(_MainTex, uv + offset);

							sum += tex;
							squareSum += tex * tex;
						}
					}
					fixed3 tex2 = tex2D(_MainTex, uv);
					r.mean = lerp(sum / samples, tex2, min(depth, 1));
					if (step(_Distance, abs((1 - depth))) < 0.00001)
						r.mean = sum / samples;

					float3 variance = abs((squareSum / samples) - (r.mean * r.mean));
					r.variance = length(variance);
					return r;
				}

				fixed4 frag(v2f_img i) : SV_Target
				{

					float depth = tex2D(_CameraDepthTexture, i.uv).r;
					depth = Linear01Depth(depth);
					depth = depth * (_ProjectionParams.z);
					depth *= 0.055;

					int upper = (_KernelSize - 1) * 0.5;
					int lower = -upper;
					int samples = (upper + 1) * (upper + 1);

					region regionA = calcRegion(int2(lower, lower), int2(0, 0), samples, i.uv, depth);
					region regionB = calcRegion(int2(0, lower), int2(upper, 0), samples, i.uv, depth);
					region regionC = calcRegion(int2(lower, 0), int2(0, upper), samples, i.uv, depth);
					region regionD = calcRegion(int2(0, 0), int2(upper, upper), samples, i.uv, depth);

					float factor = 1;
					fixed3 col = regionA.mean;
					fixed minVar = regionA.variance;
					float testVal;

					// Test region B.
					testVal = step(regionB.variance, minVar);
					col = lerp(col, regionB.mean, testVal);
					minVar = lerp(minVar, regionB.variance, testVal*factor);

					// Test region C.
					testVal = step(regionC.variance, minVar);
					col = lerp(col, regionC.mean, testVal*factor);
					minVar = lerp(minVar, regionC.variance, testVal);

					// Text region D.
					testVal = step(regionD.variance, minVar);
					col = lerp(col, regionD.mean, testVal*factor);

					fixed3 image = tex2D(_MainTex, i.uv);
					return fixed4(col, 1.0);
				}
				ENDCG
			}


			//PASS 2 LINES
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

			v2f vert(appdata v)
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

			float3 sobel(float2 uv, float depth) {
				float x = 0;
				float y = 0;
				_LineWeight = max(_LineWeight / depth, 0.25);
				float2 texelSize = (_LineWeight)*_MainTex_TexelSize*0.1;

				//Kernal for computing edges of images.
				x += tex2D(_MainTex, uv + float2(-texelSize.x, -texelSize.y)) * -1.0;
				x += tex2D(_MainTex, uv + float2(-texelSize.x, 0)) * -2.0;
				x += tex2D(_MainTex, uv + float2(-texelSize.x, texelSize.y)) * -1.0;

				x += tex2D(_MainTex, uv + float2(texelSize.x, -texelSize.y)) *  1.0;
				x += tex2D(_MainTex, uv + float2(texelSize.x, 0)) *  2.0;
				x += tex2D(_MainTex, uv + float2(texelSize.x, texelSize.y)) *  1.0;

				y += tex2D(_MainTex, uv + float2(-texelSize.x, -texelSize.y)) * -1.0;
				y += tex2D(_MainTex, uv + float2(0, -texelSize.y)) * -2.0;
				y += tex2D(_MainTex, uv + float2(texelSize.x, -texelSize.y)) * -1.0;

				y += tex2D(_MainTex, uv + float2(-texelSize.x, texelSize.y)) *  1.0;
				y += tex2D(_MainTex, uv + float2(0, texelSize.y)) *  2.0;
				y += tex2D(_MainTex, uv + float2(texelSize.x, texelSize.y)) *  1.0;

				return sqrt(x*x + y * y);
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float depth = tex2D(_CameraDepthTexture, i.uv).r;
				depth = Linear01Depth(depth);
				depth = depth * (_ProjectionParams.z);
				depth *= 0.15;
				fixed4 col = tex2D(_MainTex, i.uv);
				float3 sobelColors = col - (sobel(i.uv, depth)*(1 - _LineColors));
				return col;//fixed4(sobelColors, 1.0);
			}
			ENDCG
		}
		}





}