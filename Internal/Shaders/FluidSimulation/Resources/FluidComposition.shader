// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

Shader "Hidden/FluidComposition"
{
    Properties
    {
        _NoiseTex("Noise Texture", 2D) = "white" {}
		_NoiseScale("Noise Scale, used for noise texture", Vector) = (1.0, 1.0, 1.0, 1.0)
		_SideTexture("Texture for the sides", 2D) = "white" {}
        _FrontSideTexture("Texture for the front sides", 2D) = "white" {}
		_TopTexture("Texture for the top", 2D) = "white" {}
        _MainTex ("Texture", 2D) = "white" {}
		_DepthMaxDistance("Maximum distance for depth, used for depth buffer", Float) = 100.0
        _ShallowWaterColor("Shallow Water Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_DeepWaterColor("Deep Water Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_BlendSmooth("Normal Smoothing", Range(0, 10)) = 0.5
		_Spread("Spread", Range(0, 10)) = 0.5
		_EdgeWidth("Edge Width", Range(0, 10)) = 0.5
        [Normal]_DisplacementTex("Displacement Map", 2D) = "white"{}
        [Normal]_DisplacementTexInner("Displacement Map inside of water", 2D) = "white"{}
        _Intensity("Intensity of displacement", Range(0, 2)) = 1
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

			sampler2D _MainTex, _UnigmaFluids, _UnigmaFluidsDepth, _UnigmaFluidsNormals, _NoiseTex, _DensityMap, _DisplacementTex, _DisplacementTexInner, _SideTexture, _TopTexture, _FrontSideTexture;
            float2 _UnigmaFluids_TexelSize, _UnigmaFluidsNormals_TexelSize, _MainTex_TexelSize;
			float _BlurFallOff, _BlurRadius, _DepthMaxDistance, _BlendSmooth, _Spread, _EdgeWidth, _Intensity;
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

            float3 ModalFilter(sampler2D inputTex, float2 uv)
            {
                //Get the average, then look for the pixel closes to said average.
                float3 color = float3(0, 0, 0);
                float3 maxColor = float3(0, 0, 0);
                float3 averageColor = float3(0, 0, 0);
                for (int x = -5; x <= 5; x++) {
                    for (int y = -5; y <= 5; y++) {
                        averageColor += tex2D(inputTex, uv + float2(x, y) * _UnigmaFluidsNormals_TexelSize).xyz;
                    }
                }
                averageColor / 9;
                float minDist = 10000;
                float3 finalColor = float3(0, 0, 0);
                for (int x = -5; x <= 5; x++) {
                    for (int y = -5; y <= 5; y++) {
                        float3 c = tex2D(inputTex, uv + float2(x, y) * _UnigmaFluidsNormals_TexelSize).xyz;
                        float dist = distance(averageColor, c);
                        if (minDist > dist)
                        {
                            minDist = dist;
                            finalColor = tex2D(inputTex, uv + float2(x, y) * _UnigmaFluidsNormals_TexelSize).xyz;
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
                float2 screenPosUV = i.uv;
                float XUV = screenPosUV.x * 0.25 + _Time.y * 0.15;
                float YUV = screenPosUV.y * 0.25 + _Time.y * 0.15;

                screenPosUV.y += cos(XUV + YUV) * 0.25 * cos(YUV);
                screenPosUV.x += sin(XUV - YUV) * 0.25 * sin(YUV);
                
                //Create paintery effect for that under the water.
                float2 duv = i.uv;
                float3 diplacementNormals = UnpackNormal(tex2D(_DisplacementTex, screenPosUV));
                float3 diplacementNormalsInner = UnpackNormal(tex2D(_DisplacementTexInner, screenPosUV));
                float2 distortionBlob = duv + ((_Intensity * 0.01) * diplacementNormals.rg);
                float2 distortionGrabPass = duv + ((_Intensity * 0.025) * diplacementNormalsInner.rg);
                float2 distortionGrabPass2 = duv + ((_Intensity * 0.035) * diplacementNormalsInner.rg);
                
                fixed4 fluids = tex2D(_UnigmaFluids, i.uv);
			    fixed4 fluidsDepth = tex2D(_UnigmaFluidsDepth, distortionBlob);
			    fixed4 fluidsNormal = tex2D(_UnigmaFluidsNormals, distortionBlob);
                fixed4 originalImage = tex2D(_MainTex, i.uv);
                fixed4 distortedOriginalImage = tex2D(_MainTex, distortionGrabPass);
				fixed4 densityMap = tex2D(_DensityMap, distortionGrabPass2);

                float3 fluidNormalsAvg = ModalFilter(_UnigmaFluidsNormals, i.uv);

                //Triplanar
//------------------------------------------------------------

                float3 worldNormalVec = fluidNormalsAvg;
                float3 blendNormal = saturate(pow(worldNormalVec * _BlendSmooth, 4));
                float3 worldPos = fluids.xyz;

                _NoiseScale.xyz *= _NoiseScale.w;
                float4 xn = tex2D(_SideTexture, worldPos.zy * _NoiseScale.x);
                float4 yn = tex2D(_TopTexture, worldPos.zx * _NoiseScale.y);
                float4 zn = tex2D(_FrontSideTexture, worldPos.xy * _NoiseScale.z);
                float4 noisetexture = zn;
                noisetexture = lerp(noisetexture, xn, blendNormal.x);
                noisetexture = lerp(noisetexture, yn, blendNormal.y);
               

                //Create Lines.
                float scaleFloor = floor(1 * 0.5);
                float scaleCeil = ceil(1 * 0.5);

                float2 bottomLeft = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleFloor;
                float2 topRight = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleCeil;
                float2 bottomRight = i.uv + float2(_MainTex_TexelSize.x * scaleCeil, -_MainTex_TexelSize.y * scaleFloor);
                float2 topLeft = i.uv + float2(-_MainTex_TexelSize.x * scaleFloor, _MainTex_TexelSize.y * scaleCeil);


                float4 depthnormal0 = tex2D(_UnigmaFluidsDepth, bottomLeft);
                float4 depthnormal1 = tex2D(_UnigmaFluidsDepth, topRight);
                float4 depthnormal2 = tex2D(_UnigmaFluidsDepth, bottomRight);
                float4 depthnormal3 = tex2D(_UnigmaFluidsDepth, topLeft);


                float depthFiniteDifference3 = depthnormal1.a - depthnormal0.a;
                float depthFiniteDifference4 = depthnormal3.a - depthnormal2.a;
                float edgeDepth = sqrt(pow(depthFiniteDifference3, 2) + pow(depthFiniteDifference4, 2)) * 100;
                float depthThreshold = 0.1 * depthnormal0;
                edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

                /*
                float4 pos0 = tex2D(_IsometricPositions, bottomLeft);
                float4 pos1 = tex2D(_IsometricPositions, topRight);
                float4 pos2 = tex2D(_IsometricPositions, bottomRight);
                float4 pos3 = tex2D(_IsometricPositions, topLeft);


                float posFiniteDifference3 = length(pos1 - pos0);
                float posFiniteDifference4 = length(pos3 - pos2);
                float edgePos = sqrt(pow(posFiniteDifference3, 2) + pow(posFiniteDifference4, 2)) * 100;
                float posThreshold = _PosThreshold * pos0.a;
                edgePos = edgePos > posThreshold ? 1 : 0;
                //float edgeMask = length(depthnormal0 + depthnormal1 + depthnormal2 + depthnormal3) > 0.01 ? 1 : 0;
                                */
                scaleFloor = floor(1 * 0.5);
                scaleCeil = ceil(1 * 0.5);

                bottomLeft = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleFloor;
                topRight = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleCeil;
                bottomRight = i.uv + float2(_MainTex_TexelSize.x * scaleCeil, -_MainTex_TexelSize.y * scaleFloor);
                topLeft = i.uv + float2(-_MainTex_TexelSize.x * scaleFloor, _MainTex_TexelSize.y * scaleCeil);


                float4 normal0 = tex2D(_UnigmaFluidsNormals, bottomLeft + (_Intensity * 0.01) * diplacementNormals.rg);
                float4 normal1 = tex2D(_UnigmaFluidsNormals, topRight + (_Intensity * 0.01) * diplacementNormals.rg);
                float4 normal2 = tex2D(_UnigmaFluidsNormals, bottomRight + (_Intensity * 0.01) * diplacementNormals.rg);
                float4 normal3 = tex2D(_UnigmaFluidsNormals, topLeft + (_Intensity * 0.01) * diplacementNormals.rg);

                float3 normalFiniteDifference0 = normal1.xyz - normal0.xyz;
                float3 normalFiniteDifference1 = normal3.xyz - normal2.xyz;

                float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
                edgeNormal = edgeNormal > 0.055 ? 1 : 0;


                float edge = max(edgeDepth, edgeNormal);
                /*

                bottomLeft = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleFloor;
                topRight = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleCeil;
                bottomRight = i.uv + float2(_MainTex_TexelSize.x * scaleCeil, -_MainTex_TexelSize.y * scaleFloor);
                topLeft = i.uv + float2(-_MainTex_TexelSize.x * scaleFloor, _MainTex_TexelSize.y * scaleCeil);

                
                float4 uv0 = tex2D(_UnigmaFluids, bottomLeft);
                float4 uv1 = tex2D(_UnigmaFluids, topRight);
                float4 uv2 = tex2D(_UnigmaFluids, bottomRight);
                float4 uv3 = tex2D(_UnigmaFluids, topLeft);
                
                //noisetexture0
                xn = tex2D(_SideTexture, uv0.zy * _NoiseScale.x);
                yn = tex2D(_TopTexture, uv0.zx * _NoiseScale.y);
                zn = tex2D(_FrontSideTexture, uv0.xy * _NoiseScale.z);
                float3 noisetexture0 = zn;
                noisetexture0 = lerp(noisetexture0, xn, blendNormal.x);
                noisetexture0 = lerp(noisetexture0, yn, blendNormal.y);

                //noisetexture1
                xn = tex2D(_SideTexture, uv1.zy * _NoiseScale.x);
                yn = tex2D(_TopTexture, uv1.zx * _NoiseScale.y);
                zn = tex2D(_FrontSideTexture, uv1.xy * _NoiseScale.z);
                float3 noisetexture1 = zn;
                noisetexture1 = lerp(noisetexture1, xn, blendNormal.x);
                noisetexture1 = lerp(noisetexture1, yn, blendNormal.y);

				//noisetexture2
				xn = tex2D(_SideTexture, uv2.zy * _NoiseScale.x);
				yn = tex2D(_TopTexture, uv2.zx * _NoiseScale.y);
				zn = tex2D(_FrontSideTexture, uv2.xy * _NoiseScale.z);
				float3 noisetexture2 = zn;
				noisetexture2 = lerp(noisetexture2, xn, blendNormal.x);
				noisetexture2 = lerp(noisetexture2, yn, blendNormal.y);
                
				//noisetexture3
				xn = tex2D(_SideTexture, uv3.zy * _NoiseScale.x);
				yn = tex2D(_TopTexture, uv3.zx * _NoiseScale.y);
				zn = tex2D(_FrontSideTexture, uv3.xy * _NoiseScale.z);
				float3 noisetexture3 = zn;
				noisetexture3 = lerp(noisetexture3, xn, blendNormal.x);
				noisetexture3 = lerp(noisetexture3, yn, blendNormal.y);
                
                
                

                float3 uvFiniteDifference0 = noisetexture1.xyz - noisetexture0.xyz;
                float3 uvFiniteDifference1 = noisetexture3.xyz - noisetexture2.xyz;

                float edgeUV= sqrt(dot(uvFiniteDifference0, uvFiniteDifference0) + dot(uvFiniteDifference1, uvFiniteDifference1));
                edgeUV = edgeUV > 0.0001 ? 1 : 0;
                */
                //Determine how if on side or on top.
                /*
                float normDotNoise = dot(worldNormalVec + (noisetexture.y + (noisetexture * 0.5)), worldNormalVec.y);
                //Checks if higher then top.
                float4 topTextureResult = step(_Spread + _EdgeWidth, normDotNoise) * topTexture;
                //Side
                float4 sideTextureResult = step(normDotNoise, _Spread) * sideTexture;

                float4 result = (topTextureResult) + sideTextureResult;
                */
                //Create diffuse surface.

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - fluids.xyz);

                float4 NdotL = saturate(dot(fluidNormalsAvg.xyz, _WorldSpaceLightPos0.xyz));


                float4 waterDeepness = lerp(_ShallowWaterColor, _DeepWaterColor, densityMap *0.225);
                float waterDepthDifference = saturate((1.0 - frac(fluids.w)) / _DepthMaxDistance);
                float4 waterColor = lerp(_ShallowWaterColor, _DeepWaterColor, waterDepthDifference);
				waterColor = lerp(waterColor, waterDeepness, 1.0- (densityMap*0.55) );
                //waterColor = lerp(waterColor, waterDeepness, 1.0 - i.uv.y);


                float4 waterSpecular = lerp(waterColor, 1, step(0.85 + (0.05 * sin(_Time.x * 10)), NdotL));
                float4 result = waterColor + edge;
                
                //------------------------------------------------------------



                float4 grabPass = lerp(distortedOriginalImage, result, 0.55);
                fixed4 finalImage = lerp(originalImage, grabPass, step(0.65, fluidsDepth.w));


                return finalImage;
            }
            ENDCG
        }
    }
}
