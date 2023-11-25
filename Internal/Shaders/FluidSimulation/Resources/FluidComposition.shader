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
        _DeepestWaterColor("Deepest Water Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_BlendSmooth("Normal Smoothing", Range(0, 10)) = 0.5
		_Spread("Spread", Range(0, 10)) = 0.5
		_EdgeWidth("Edge Width", Range(0, 10)) = 0.5
        [Normal]_DisplacementTex("Displacement Map", 2D) = "white"{}
        [Normal]_DisplacementTexInner("Displacement Map inside of water", 2D) = "white"{}
        _Intensity("Intensity of displacement", Range(0, 2)) = 1
        _UnderWaterTexture("UnderWater Texture", 2D) = "white" {}
		_SpecularPower("Specular Power", Range(0, 100)) = 10.0
		_SpecularIntensity("Specular Intensity", Range(0, 10)) = 1.0
		_FresnelPower("Fresnel Power", Range(0, 10)) = 1.0
		_DensityThickness("Density Thickness", Float) = 1.0
        _OutlineThickness("Outline Thickness", Range(0,1)) = 0.05
        _CausticTex("Caustic Main Texture", 2D) = "white" {}
        _CausticTile("Caustic Tile Texture", 2D) = "white" {}
        _CausticNoise("Caustic Noise Texture", 2D) = "white" {}
        _NoiseScaleCaustic("Noise Scale, used for caustic texture", Vector) = (1.0, 1.0, 1.0, 1.0)
		_Speed("Speed of caustic animation", Float) = 1.0
		_CausticScale("Scale of caustic animation", Float) = 1.0
		_CausticColor("Color of caustic animation", Color) = (1.0, 1.0, 1.0, 1.0)
		_CausticIntensity("Intensity of caustic", Range(0, 20)) = 1
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
            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
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

			sampler2D _SurfaceMap, _CausticTex, _CausticTile, _CausticNoise, _CurlMap, _VelocityMap, _ColorFieldNormalMap, _MainTex, _UnigmaFluids, _UnigmaFluidsDepth, _UnigmaFluidsNormals, _NoiseTex, _DensityMap, _DisplacementTex, _DisplacementTexInner, _SideTexture, _TopTexture, _FrontSideTexture, _UnderWaterTexture;
            float2 _UnigmaFluids_TexelSize, _UnigmaFluidsNormals_TexelSize, _MainTex_TexelSize;
			float _BlurFallOff, _BlurRadius, _DepthMaxDistance, _BlendSmooth, _Spread, _EdgeWidth, _Intensity, _DensityThickness, _OutlineThickness;
			float _CausticIntensity, _CausticScale, _Speed, _ScaleX, _ScaleY, _SpecularPower, _SpecularIntensity, _FresnelPower;
            float4x4 _ProjectionToWorld, _CameraInverseProjection;
            float4 _DeepWaterColor, _NoiseScale, _ShallowWaterColor, _DeepestWaterColor, _NoiseScaleCaustic, _CausticColor;


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
                for (int x = -3; x <= 3; x++) {
                    for (int y = -3; y <= 3; y++) {
                        averageColor += normalize(tex2D(inputTex, uv + float2(x, y) * _UnigmaFluidsNormals_TexelSize).xyz);
                    }
                }
                averageColor /= 36;
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
                return averageColor;
            }
            
            fixed4 triplanar(float3 blendNormal, float4 texturex, float4 texturey, float4 texturez)
            {
                float4 triplanartexture = texturez;
                triplanartexture = lerp(triplanartexture, texturex, blendNormal.x);
                triplanartexture = lerp(triplanartexture, texturey, blendNormal.y);
                return triplanartexture;
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
                fixed4 particleNormalMap = tex2D(_ColorFieldNormalMap, i.uv);
				fixed4 velocityMap = tex2D(_VelocityMap, i.uv);
				fixed4 surfaceMap = tex2D(_SurfaceMap, i.uv);
                fixed4 curlMap = tex2D(_CurlMap, i.uv);
                
				fixed4 underWaterTex = tex2D(_UnderWaterTexture, distortionGrabPass *2);

                float3 fluidNormalsAvg = ModalFilter(_UnigmaFluidsNormals, i.uv);
				//velocityMap.xyz = ModalFilter(_VelocityMap, i.uv);
                
                //Triplanar
//------------------------------------------------------------

                float3 worldNormalVec = fluidNormalsAvg;
                float speed = _Time.x * _Speed;
                float3 blendNormal = saturate(pow(worldNormalVec * _BlendSmooth, 4));
                float3 worldPos = fluids.xyz;

                _NoiseScale.xyz *= _NoiseScale.w;
                float4 xn = tex2D(_SideTexture, (worldPos.zy * _NoiseScale.x) - (speed));
                float4 yn = tex2D(_TopTexture, (worldPos.zx * _NoiseScale.y) - (speed));
                float4 zn = tex2D(_FrontSideTexture, (worldPos.xy * _NoiseScale.z) - (speed));
                float4 noisetexture = zn;
                noisetexture = lerp(noisetexture, xn, blendNormal.x);
                noisetexture = lerp(noisetexture, yn, blendNormal.y);

                _NoiseScaleCaustic.xyz *= _NoiseScaleCaustic.w;
                float4 xx = tex2D(_CausticNoise, float2(worldPos.zy * _NoiseScaleCaustic.x) - (speed));
                float4 yy = tex2D(_CausticNoise, float2(worldPos.xz * _NoiseScaleCaustic.y) - (speed));
                float4 zz = tex2D(_CausticNoise, float2(worldPos.xy * _NoiseScaleCaustic.z) - (speed));

                float triCaustic = triplanar(blendNormal, xx, yy, zz);

                float4 xc = tex2D(_CausticTex, float2((worldPos.z + triCaustic) * _CausticScale, (worldPos.y) * (_CausticScale / 4)));
                float4 zc = tex2D(_CausticTex, float2((worldPos.x + triCaustic) * _CausticScale, (worldPos.y) * (_CausticScale / 4)));
                float4 yc = tex2D(_CausticTex, (float2(worldPos.x + triCaustic, worldPos.z + triCaustic)) * _CausticScale);
                
                float4 causticsTex = triplanar(blendNormal, xc, yc, zc);


                float secScale = _CausticScale * 0.6;
                float4 xc2 = tex2D(_CausticTile, float2((worldPos.z - triCaustic) * secScale, (worldPos.y) * (secScale / 4)));
                float4 zc2 = tex2D(_CausticTile, float2((worldPos.x - triCaustic) * secScale, (worldPos.y) * (secScale / 4)));
                float4 yc2 = tex2D(_CausticTile, float2(worldPos.x - triCaustic, worldPos.z - triCaustic) * secScale);

                float4 causticsTex2 = triplanar(blendNormal, xc2, yc2, zc2);

                float4 xc3 = tex2D(_CausticTex, float2((worldPos.z - triCaustic) * secScale, (worldPos.y) * (secScale / 4)));
                float4 zc3 = tex2D(_CausticTex, float2((worldPos.x - triCaustic) * secScale, (worldPos.y) * (secScale / 4)));
                float4 yc3 = tex2D(_CausticTex, float2(worldPos.x - triCaustic, worldPos.z - triCaustic) * secScale);

                float4 causticsTex3 = triplanar(blendNormal, xc3, yc3, zc3);

                // combining
                causticsTex *= causticsTex2+ causticsTex3;
                causticsTex *= _CausticIntensity * _CausticColor;
                

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
                float edgeDepth = sqrt(pow(depthFiniteDifference3, 2) + pow(depthFiniteDifference4, 2));
                float depthThreshold = 0.1 * depthnormal0.w;
                edgeDepth = edgeDepth > _OutlineThickness ? 1 : 0;

                
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
                edgeNormal = edgeNormal > 1.0 ? 1 : 0;


                scaleFloor = floor(1 * 0.5);
                scaleCeil = ceil(1 * 0.5);

                bottomLeft = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleFloor;
                topRight = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleCeil;
                bottomRight = i.uv + float2(_MainTex_TexelSize.x * scaleCeil, -_MainTex_TexelSize.y * scaleFloor);
                topLeft = i.uv + float2(-_MainTex_TexelSize.x * scaleFloor, _MainTex_TexelSize.y * scaleCeil);


                normal0 = tex2D(_UnigmaFluidsDepth, bottomLeft + (_Intensity * 0.01) * diplacementNormals.rg).y;
                normal1 = tex2D(_UnigmaFluidsDepth, topRight + (_Intensity * 0.01) * diplacementNormals.rg).y;
                normal2 = tex2D(_UnigmaFluidsDepth, bottomRight + (_Intensity * 0.01) * diplacementNormals.rg).y;
                normal3 = tex2D(_UnigmaFluidsDepth, topLeft + (_Intensity * 0.01) * diplacementNormals.rg).y;

                //return normal0;
                normalFiniteDifference0 = normal1.xyz - normal0.xyz;
                normalFiniteDifference1 = normal3.xyz - normal2.xyz;

                float edgeInner = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
                edgeInner = smoothstep(0.00199, 0.074, edgeInner)*fluids.w;//edgeInner > 0.099 - edgeInner ? 1 : 0;
                

                float edge = max(edgeDepth, edgeInner);


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

                //Determine how if on side or on top.

                float normDotNoise = dot(worldNormalVec + (noisetexture.y + (noisetexture * 0.5)), worldNormalVec.y);
                //Checks if higher then top.
                //float4 topTextureResult = step(_Spread + _EdgeWidth, normDotNoise) * topTexture;
                //Side
                //float4 sideTextureResult = step(normDotNoise, _Spread) * sideTexture;

                //float4 result = (topTextureResult) + sideTextureResult;

                //Create diffuse surface.

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - fluids.xyz);

                float NdotL = saturate(dot(particleNormalMap.xyz, _WorldSpaceLightPos0.xyz)) * 0.5 + 0.5;
                //NdotL = step(0.705, NdotL);
                float4 diffuse = saturate(NdotL * _LightColor0);
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                float3 specular =  _LightColor0 * pow(DotClamped(reflect(-lightDir, fluidNormalsAvg.xyz), viewDir), _SpecularPower) * _SpecularIntensity;
                diffuse += float4(specular, 1);//float4(ShadeSH9(half4(fluidNormalsAvg.xyz, 1)), 1);
                
				float fresnel = saturate(dot(fluidNormalsAvg.xyz, viewDir));
				fresnel = pow(fresnel, _FresnelPower);
                diffuse += fresnel;


                float4 waterDeepness = lerp(_ShallowWaterColor, _DeepWaterColor, 13.95*densityMap);
                float waterDepthDifference = saturate((1.0 - frac(fluids.w)) / _DepthMaxDistance);
                float4 waterColor = lerp(_ShallowWaterColor, _DeepWaterColor, waterDepthDifference);
				waterColor = lerp(waterColor, waterDeepness, 1.0- (densityMap) );
				waterColor = lerp(waterColor, _DeepestWaterColor, 1.0 - smoothstep(0.785, 0.05, densityMap));
                //waterColor = lerp(waterColor, waterDeepness, 1.0 - i.uv.y);


                float4 waterSpecular = diffuse + waterColor;//lerp(waterColor, 1, step(0.85 + (0.05 * sin(_Time.x * 10)), diffuse.x));
                float4 result = (min(2.5 * NdotL + 0.55, 1.05) * waterColor) + edge;
                
                //------------------------------------------------------------
                // 
                //Create causatics


 




                //Voronoi noise.
				float voronoi = 0;
				float cells = 0;
				float uVoronoi = 0;
				float uCells = 0;
                float simplexN = snoise(float3(i.uv, 1));
                
                float2 animatedUV = animateUVs(worldPos.zx * _NoiseScale.z, 0.25);
                animatedUV += float2(1, 1);
                float2 twirl = Twirl(i.uv, float2(0, 0), 5.25, float2(0, 0), 0.55);
                float2 distortion = UnpackNormal(tex2D(_DisplacementTex, twirl)).xy;
                

                float2 uv = i.uv + distortion*0.01 + animatedUV;//i.screenPosition.xy / i.screenPosition.w;
                uv += float2(0.0001 * _Time.y, 0.00001 * _Time.y);
                uv = fmod(uv, 0.1);//float2((uv.x % 1.001), (uv.y % 1.001));

                float2 positionalPoint = ClosetCell(uv, 10);
                float2 circlePos = uv - positionalPoint;

                float dist = distance(i.uv, ClosetLineCell(i.uv));

                float x = dist;// the input value
                float PatternResult = sin(2.5 * (1.0 - (1.0 / sqrt(1.0 + pow(x, 3.0))))) + 0.01555
                    * (cos(760.0 * (1.0 - (1.0 / sqrt(1.0 + pow(x, 3.0))))) + 1.0);

                float circleDistort = 40 * smoothstep(0, 1, distance(i.uv, float2(0.5, 0.5)));
                float radiusIntensity = 0.5 * PatternResult * circleDistort;

                float circleResult = sdCircle(circlePos, 0.01 * radiusIntensity);
                float circleGrid = smoothstep(0, 0.0025, circleResult);

                //Use different SDFs.
                float di = 1.2 * cos(_Time.y + 3.9);
                float sdfResult = sdStar5(circlePos, 0.01 * radiusIntensity, 2.0);//sdHeart(circlePos, _Radius * radiusIntensity);
                float sdfGrid = smoothstep(0, 0.0025, sdfResult);


                F1Unity_Voronoi_float(animatedUV + i.uv + simplexN *0.015 * simplexN, UNITY_PI, 7, voronoi, cells, 50);
                float3 iVoronoi = Ivoronoi((animatedUV + i.uv + simplexN * 0.015 * simplexN )*10);
                iVoronoi = 1.0 - smoothstep(0.02, 0.05, iVoronoi);
                float distFromEdge = Unity_Voronoi_float(animatedUV + i.uv + distortion * 0.014 * simplexN, 0, 7, uVoronoi, uCells);

				float voronoiDiff = voronoi - uVoronoi;
                
                float4 smoothVoronoi1 = smoothVoronoi((animatedUV + i.uv + simplexN * 0.015 * simplexN) * 32, 0.095 * (1.0 - diplacementNormals.x*1.2));
                float4 smoothVoronoi2 = smoothVoronoi((animatedUV + i.uv + simplexN * 0.015 * simplexN) * 32, 0);
                float4 smoothVoronoi3 = smoothVoronoi((animatedUV + i.uv + simplexN * 0.015 * simplexN) * 12, 0.395 * (1.0 - diplacementNormals.x * 1.2));
                
                float causaticPattern = lerp(0, 1, smoothstep(0.0001, 0.25, (smoothVoronoi2.x - smoothVoronoi1.x) * 1000 * (1.0 - simplexN * 0.5)));
                
				float4 causaticColor = lerp(0, waterColor, causaticPattern*5);
                //causaticColor = lerp(causaticColor, _DeepWaterColor, 1.0 - causaticColor*5);
                
				float voronoiRamped = lerp(0, 1, step(0.35, voronoi));
				voronoiRamped = lerp(voronoi, voronoiRamped, step(0.35, voronoiDiff));
				//float4 colorVoronois = lerp(waterColor, 1, step(0.15, voronoi));
                




                float finalMask = lerp(sdfGrid, circleGrid, 0.00000001255 * radiusIntensity);
                finalMask = max(step(1, 1 - finalMask), 0);
                //finalMask = lerp(underWaterTex + causaticColor * 0.45, finalMask*100, finalMask);

                //return underWaterTex + causaticColor*0.45;
                float4 CausaticFinal = finalMask + causaticColor;

                
                

                float atteunuationDensity = min(0.155,saturate(_DensityThickness * densityMap.z) * (exp(densityMap.z * 75 * fluidsDepth.z) - 1.0));
                float4 grabPass = lerp(distortedOriginalImage, result, 0.55);
                fixed4 finalImage = lerp(originalImage, grabPass, step(0.65, fluidsDepth.w));
                fixed4 cleanFluidSingleColor = lerp(distortedOriginalImage, _ShallowWaterColor * fluidsDepth.w,0);
				cleanFluidSingleColor = lerp(distortedOriginalImage, _DeepWaterColor* fluidsDepth.w, atteunuationDensity + 0.15);
                
				cleanFluidSingleColor = lerp(originalImage, cleanFluidSingleColor, step(0.5, fluidsDepth.w));

                float surface = smoothstep(0.05, 0.155, fluidsDepth.y);
                float4 fluidColorFinal = cleanFluidSingleColor + fluids.w*waterSpecular*0.12;
                //return fluidColorFinal;
                //return diffuse;
                float4 colorField = float4((particleNormalMap.xyz * 0.5 + 0.5) * fluids.w, fluids.w);
                float4 colorFieldLerp = lerp(distortedOriginalImage * fluids.w, colorField, atteunuationDensity + 0.35);
                float4 colorSurfaceFluid = fluidColorFinal + edge + colorField * 0.0935 + surface * 0.0756;
                //return causaticPattern;
                float4 causaticLerpTop = lerp(distortedOriginalImage, causticsTex * fluids.w* step(0.0000001, fluidsDepth.y), 0.55);
                float4 causaticLerpSide = lerp(distortedOriginalImage, causticsTex * fluids.w * step(0.000000, fluidsDepth.y), 0.25);
                float4 causaticLerp = causaticLerpSide * 0.5 + causaticLerpTop*0.55;
                float4 colorLerping = lerp(colorSurfaceFluid, causaticLerp * step(0.00001, fluids.w), 0.45 * step(0.00001, fluids.w) * step(0.0000001, fluidsDepth.y));
				
                //return densityMap;
                //return surface;
                return colorLerping;
                //return edgeInner;
                //return fluidsDepth.y;
                float curly = length(curlMap);
                //return step(9.2, curly);
                float foam = smoothstep(8.89, 10.0, fluidsDepth.x);
                //return foam;
                float4 waterWithFoam = colorLerping;
                waterWithFoam.xyz += float3(foam, foam, foam);
                
                //return originalImage;
                //return fluidsDepth;
                //return curlMap;
                //return waterWithFoam;//lerp(colorLerping, float4(1,1,1,1), foam);
                //return edgeNormal;
                //return waterSpecular;
                //return fluidsNormal;
                //return float4(fluidsDepth.z, fluidsDepth.z, fluidsDepth.z,1);
                //return cleanFluidSingleColor + edgeDepth +float4((particleNormalMap.xyz * 0.5 + 0.5) * fluids.w, fluids.w) * 0.25;
              
                //return finalImage;
                //return lerp(finalImage, lerp(finalImage, finalImage + CausaticFinal * fluids.w, fluids.w *0.25), step(0.5, blendNormal.y));
                //return _DensityThickness * fluidsDepth.z;
                //return float4(fluids.w*fluids.xyz*0.5 + 0.5, fluids.w);
                //return finalImage;
                //return diffuse*fluids.w;//float4(ShadeSH9(half4(normalize(fluidNormalsAvg.xyz), 1)), 1);
                // good
                //return noisetexture;
                //return particleNormalMap;
                //return velocityMap;
                //return fluids.w * curlMap;
                //return float4((particleNormalMap.xyz * 0.5 + 0.5),1);
				//return step(2.0, fluidsDepth.x);
                //return smoothstep(0.0225, 0.0295, fluidsDepth.y);
                //return edge;
                //return surfaceMap;
            }
            ENDCG
        }
    }
}
