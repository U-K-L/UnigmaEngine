Shader "UnigmaPP/Painting"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_KernelSize("Kernel Size (N)", Int) = 7
		_Distance("Distance", Range(0,2)) = 1
		_MinDistance("Minimum Distance", Range(0,2)) = 0
		_distancePow("Variation by Distance", Range(0,100)) = 0
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

				sampler2D _MainTex, _CameraDepthTexture, _ShadowMapTexture;
				sampler2D_half _CameraMotionVectorsTexture;

				float2 _MainTex_TexelSize;
				float _Distance;
				float _MinDistance;
				float _distancePow;

				int _KernelSize;
				struct region
				{
					float3 mean;
					float variance;
				};


				half4 VectorToColor(float2 mv) { // Convert a motion vector into RGBA color.
					half phi = atan2(mv.x, mv.y);
					half hue = (phi / UNITY_PI + 1) * 0.5;

					half r = abs(hue * 6 - 3) - 1;
					half g = 2 - abs(hue * 6 - 2);
					half b = 2 - abs(hue * 6 - 4);
					half a = length(mv);

					return saturate(half4(r, g, b, a));
				}
				//Gets the region to perform kuwahara filter

				region calcRegion(int2 lower, int2 upper, int samples, float2 uv, float depth, float motion)
				{
					region r;
					float3 sum = 0.0;
					float3 squareSum = 0.0;

					int x = lower.x;
					int y = lower.y;
					//for (int x = lower.x; x <= upper.x; ++x)

					//if(depth > _MinDistance){

						for (int a = -10; a <= 10; ++a)
						{
							if (x > upper.x)
							{
								x = lower.x;
								break;
							}
							++x;

							//for (int y = lower.y; y <= upper.y; ++y)
							for (int b = -10; b <= 10; ++b)
							{
								if (y > upper.y)
								{
									y = lower.y;
									break;
								}
								++y;

								fixed2 offset = fixed2(_MainTex_TexelSize.x * x, _MainTex_TexelSize.y * y);
								fixed3 tex = tex2D(_MainTex, uv + offset);

								sum += tex;
								squareSum += tex * tex;

								
							
							}

							
						
						}
					//}
					fixed3 tex2 = tex2D(_MainTex, uv);
					r.mean = lerp(sum / samples, tex2, min(depth, 1));
					//Checks if distance is greater than the depth calculation, then if so blur image.
					//if (step(_Distance, abs((1 - depth))) < 0.00001 && abs(1-depth) > _MinDistance)
						r.mean = sum / samples;


					float3 variance = abs( (squareSum / samples) - (pow(r.mean, motion+1)));
					r.variance = length(variance);

					if(depth < _MinDistance)
					{
						r.mean = tex2;//lerp(tex2, r.mean, depth*_distancePow);
						r.variance = 1;
					}

					return r;
				}

				fixed4 frag(v2f_img i) : SV_Target
				{
					
					half2 motion = tex2D(_CameraMotionVectorsTexture, i.uv);
					half4 motionColor = VectorToColor(motion);
					float motionValue = 1 + ( (length(motionColor)) * 10);

					float depth = tex2D(_CameraDepthTexture, i.uv).r;
					depth = Linear01Depth(depth);
					depth = depth * (_ProjectionParams.z);
					depth *= 0.055;

					int upper = (_KernelSize - 1) * 0.5;
					int lower = -upper;
					int samples = (upper + 1) * (upper + 1);

					region regionA = calcRegion(int2(lower, lower), int2(0, 0), samples, i.uv, depth, motionValue);
					region regionB = calcRegion(int2(0, lower), int2(upper, 0), samples, i.uv, depth, motionValue);
					region regionC = calcRegion(int2(lower, 0), int2(0, upper), samples, i.uv, depth, motionValue);
					region regionD = calcRegion(int2(0, 0), int2(upper, upper), samples, i.uv, depth, motionValue);

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
					col = lerp(col, regionC.mean, testVal*factor); //+ (0.02*lerp(col, length(motionColor), length(motionColor)))*(motionColor > 0);
					minVar = lerp(minVar, regionC.variance, testVal);

					// Text region D.
					testVal = step(regionD.variance, minVar);
					col = lerp(col, regionD.mean, testVal*factor);

					fixed4 image = fixed4(col, 1.0);
					return image;
				}
				ENDCG
			}
		}
}