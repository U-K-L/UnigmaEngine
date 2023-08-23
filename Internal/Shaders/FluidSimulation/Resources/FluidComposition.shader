// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

Shader "Hidden/FluidComposition"
{
    Properties
    {
        _NoiseTex("Noise Texture", 2D) = "white" {}
		_NoiseScale("Noise Scale, used for noise texture", Vector) = (1.0, 1.0, 1.0, 1.0)
        _MainTex ("Texture", 2D) = "white" {}
		_DepthMaxDistance("Maximum distance for depth, used for depth buffer", Float) = 100.0
        _ShallowWaterColor("Water Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_DeepWaterColor("Water Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_BlendSmooth("Normal Smoothing", Range(0, 10)) = 0.5
		_Spread("Spread", Range(0, 10)) = 0.5
		_EdgeWidth("Edge Width", Range(0, 10)) = 0.5
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "../../ShaderHelpers.hlsl"

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

            sampler2D _MainTex, _UnigmaFluids, _UnigmaFluidsDepth, _UnigmaFluidsNormals, _NoiseTex;
            float2 _UnigmaFluids_TexelSize, _UnigmaFluidsNormals_TexelSize;
			float _BlurFallOff, _BlurRadius, _DepthMaxDistance, _BlendSmooth, _Spread, _EdgeWidth;
			float _ScaleX, _ScaleY;
            float4x4 _ProjectionToWorld, _CameraInverseProjection;
            float4 _DeepWaterColor, _NoiseScale, _ShallowWaterColor;


            float bilateralFilter(sampler2D depthSampler, float2 texcoord)
            {
                float depth = tex2D(depthSampler, texcoord).w;
                float sum = 0;
                float wsum = 0;
                float blurScale = 1.0 / _BlurRadius;
                for (float x = -_BlurRadius; x <= _BlurRadius; x += 1.0) {
                    float tex = tex2D(depthSampler, texcoord + _UnigmaFluids_TexelSize * x * float2(_ScaleX, _ScaleY)).w;
                    // spatial domain
                    float r = x * blurScale;
                    float r2 = (tex - depth) * _BlurFallOff;
                    float w = exp(-r * r);
                    float g = exp(-r2 * r2);
                    sum += tex * w * g;
                    wsum += w * g;
                }
                if (wsum > 0.0) {
                    sum /= wsum;
                }
                return sum;
            }

            
            /*
            float3 GetNormalFromDepth(sampler2D depth, float2 uv)
            {
                float3 eps = float3(0.0001, 0.0, 0.0);
	            //Sample the distance field at the point and at a small offset.
                float3 n = float3(
		            tex2D(depth,eps.x + uv.x) - tex2D(depth,eps.x - uv.x),
		            tex2D(depth,eps.y + uv.y) - tex2D(depth,eps.y - uv.y));

    
                return normalize(n);
            }
            */
            // inspired by keijiro's depth inverse projection
            // https://github.com/keijiro/DepthInverseProjection
            // constructs view space ray at the far clip plane from the screen uv
            // then multiplies that ray by the linear 01 depth
            float3 viewSpacePosAtScreenUV(float2 uv)
            {
                float3 viewSpaceRay = mul(_ProjectionToWorld, float4(uv * 2.0 - 1.0, 1.0, 1.0) * _ProjectionParams.z);
                float rawDepth = bilateralFilter(_UnigmaFluids, uv);
                return viewSpaceRay * Linear01Depth(rawDepth);
            }
            
            float3 viewSpacePosAtPixelPosition(float2 vpos)
            {
                float2 uv = vpos * _UnigmaFluids_TexelSize;
                return viewSpacePosAtScreenUV(uv);
            

            }
            // base on János Turánszki's Improved Normal Reconstruction
            // https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
            // this is a minor optimization over the original, using only 2 comparisons instead of 8
            // at the cost of two additional vector subtractions
            // sharpness of 3 tap with better handling of depth disparities
            // worse artifacts on convex edges than either 3 tap or 4 tap

            // unity's compiled fragment shader stats: 62 math, 5 tex
            half3 viewNormalAtPixelPosition(float2 vpos)
            {
                // get current pixel's view space position
                half3 viewSpacePos_c = viewSpacePosAtPixelPosition(vpos + float2( 0.0, 0.0));

                // get view space position at 1 pixel offsets in each major direction
                half3 viewSpacePos_l = viewSpacePosAtPixelPosition(vpos + float2(-1.0, 0.0));
                half3 viewSpacePos_r = viewSpacePosAtPixelPosition(vpos + float2( 1.0, 0.0));
                half3 viewSpacePos_d = viewSpacePosAtPixelPosition(vpos + float2( 0.0,-1.0));
                half3 viewSpacePos_u = viewSpacePosAtPixelPosition(vpos + float2( 0.0, 1.0));

                // get the difference between the current and each offset position
                half3 l = viewSpacePos_c - viewSpacePos_l;
                half3 r = viewSpacePos_r - viewSpacePos_c;
                half3 d = viewSpacePos_c - viewSpacePos_d;
                half3 u = viewSpacePos_u - viewSpacePos_c;

                // pick horizontal and vertical diff with the smallest z difference
                half3 hDeriv = abs(l.z) < abs(r.z) ? l : r;
                half3 vDeriv = abs(d.z) < abs(u.z) ? d : u;

                // get view space normal from the cross product of the two smallest offsets
                half3 viewNormal = normalize(cross(hDeriv, vDeriv));

                return viewNormal;
            }
            
            float3 getEyePos(sampler2D depthText, float2 uv)
            {
                float depth = bilateralFilter(depthText, uv);
				float4 clipSpacePos = float4(uv * 2.0 - 1.0, depth, 1.0);
				float4 viewSpacePos = mul(_CameraInverseProjection, clipSpacePos);
				return viewSpacePos.xyz / viewSpacePos.w;
            }

            //Looks at a kernel of 3x3 and return the color with the most in that kernal. modal filter

            float3 ModalFilter(float2 uv)
            {
                //Get the average, then look for the pixel closes to said average.
                float3 color = float3(0, 0, 0);
                float3 maxColor = float3(0, 0, 0);
                float3 averageColor = float3(0, 0, 0);
                for (int x = -1; x <= 1; x++) {
                    for (int y = -1; y <= 1; y++) {
                        averageColor += tex2D(_UnigmaFluidsNormals, uv + float2(x, y) * _UnigmaFluidsNormals_TexelSize).xyz;
                    }
                }
                averageColor / 9;
                float minDist = 10000;
                float3 finalColor = float3(0, 0, 0);
                for (int x = -1; x <= 1; x++) {
                    for (int y = -1; y <= 1; y++) {
                        float3 c = tex2D(_UnigmaFluidsNormals, uv + float2(x, y) * _UnigmaFluidsNormals_TexelSize).xyz;
                        float dist = distance(averageColor, c);
                        if (minDist > dist)
                        {
                            minDist = dist;
                            finalColor = tex2D(_UnigmaFluidsNormals, uv + float2(x, y) * _UnigmaFluidsNormals_TexelSize).xyz;
                        }

                    }
                }
				//finalColor.r = 1.0 * step(finalColor.r, 1.0-0.995);
                //finalColor.g = 1.0 * step(finalColor.g, 1.0-0.995);
                //finalColor.b = 1.0 * step(finalColor.b, 1.0-0.995);
                return finalColor;
            }
            
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 fluids = tex2D(_UnigmaFluids, i.uv);
			    fixed4 fluidsDepth = tex2D(_UnigmaFluidsDepth, i.uv);
			    fixed4 fluidsNormal = tex2D(_UnigmaFluidsNormals, i.uv);
                fixed4 originalImage = tex2D(_MainTex, i.uv);

                float3 fluidNormalsAvg = ModalFilter(i.uv);

                //Triplanar
//------------------------------------------------------------

                float3 worldNormalVec = fluidNormalsAvg;
                float3 blendNormal = saturate(pow(worldNormalVec * _BlendSmooth, 4));
                float3 worldPos = fluids.xyz;

                _NoiseScale.xyz *= _NoiseScale.w;
                float4 xn = tex2D(_NoiseTex, worldPos.zy * _NoiseScale.x);
                float4 yn = tex2D(_NoiseTex, worldPos.zx * _NoiseScale.y);
                float4 zn = tex2D(_NoiseTex, worldPos.xy * _NoiseScale.z);
                float4 noisetexture = zn;
                noisetexture = lerp(noisetexture, xn, blendNormal.x);
                noisetexture = lerp(noisetexture, yn, blendNormal.y);
                
                //Create diffuse surface.

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - fluids.xyz);

                float4 NdotL = saturate(dot(fluidNormalsAvg.xyz, _WorldSpaceLightPos0.xyz));


                float waterDepthDifference = saturate( (1.0 - frac(fluids.w)) / _DepthMaxDistance);
                float4 waterColor = lerp(_ShallowWaterColor, _DeepWaterColor, waterDepthDifference);
				waterColor = lerp(_ShallowWaterColor, _DeepWaterColor, 1.0 - i.uv.y);
                
                float4 waterSpecular = lerp(waterColor, 1, step(0.15, NdotL));




                //Determine how if on side or on top.
                /*
                float normDotNoise = dot(worldNormalVec + (noisetexture.y + (noisetexture * 0.5)), worldNormalVec.y);
                //Checks if higher then top.
                float4 topTextureResult = step(_Spread + _EdgeWidth, normDotNoise) * topTexture;
                //Side
                float4 sideTextureResult = step(normDotNoise, _Spread) * sideTexture;

                float4 result = (topTextureResult) + sideTextureResult;
                */
                float4 result = waterColor;
                
                //------------------------------------------------------------



                
                fixed4 finalImage = lerp(originalImage, result, step(0.95, fluidsDepth.w) * 0.7);


                return finalImage;
            }
            ENDCG
        }
    }
}
