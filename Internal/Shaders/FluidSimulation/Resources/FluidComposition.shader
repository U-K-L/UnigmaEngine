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

			sampler2D _UnigmaBackgroundColor, _UnigmaDepthShadowsMap, _DistancesMap, _DepthBufferTexture, _SurfaceMap, _CausticTex, _CausticTile, _CausticNoise, _CurlMap, _VelocityMap, _ColorFieldNormalMap, _MainTex, _UnigmaFluids, _UnigmaFluidsDepth, _UnigmaFluidsNormals, _NoiseTex, _DensityMap, _DisplacementTex, _DisplacementTexInner, _SideTexture, _TopTexture, _FrontSideTexture, _UnderWaterTexture;
            float2 _UnigmaFluids_TexelSize, _UnigmaFluidsNormals_TexelSize, _MainTex_TexelSize;
			float _BlurFallOff, _BlurRadius, _DepthMaxDistance, _BlendSmooth, _Spread, _EdgeWidth, _Intensity, _DensityThickness, _OutlineThickness;
			float _CausticIntensity, _CausticScale, _Speed, _ScaleX, _ScaleY, _SpecularPower, _SpecularIntensity, _FresnelPower;
            float4x4 _ProjectionToWorld, _CameraInverseProjection;
            float4 _DeepWaterColor, _NoiseScale, _ShallowWaterColor, _DeepestWaterColor, _NoiseScaleCaustic, _CausticColor;

            
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
                fixed4 originalImage = tex2D(_UnigmaBackgroundColor, i.uv);
                fixed4 distortedOriginalImage = tex2D(_UnigmaBackgroundColor, distortionGrabPass);
                fixed4 densityMap = tex2D(_DensityMap, distortionGrabPass2);
                fixed4 particleNormalMap = tex2D(_ColorFieldNormalMap, i.uv);
                fixed4 velocityMap = tex2D(_VelocityMap, i.uv);
                fixed4 surfaceMap = tex2D(_SurfaceMap, i.uv);
                fixed4 curlMap = tex2D(_CurlMap, i.uv);
                fixed4 distanceMap = tex2D(_DistancesMap, i.uv);
                fixed4 unigmaDepth = tex2D(_UnigmaDepthShadowsMap, i.uv);
				fixed4 unigmaBackground = tex2D(_UnigmaBackgroundColor, i.uv);

				//return unigmaBackground;
                //return unigmaDepth.z*100;
                //return fluids.w*100;
                //return lerp(fluids.w, unigmaDepth.z, step(fluids.w, unigmaDepth.z))*10;
                //return densityMap;
                float fluidsSceneDepth = fluids.w;
                fluids.w = (1.0 - fluids.w) * step(0, fluids.w);
                fixed4 underWaterTex = tex2D(_UnderWaterTexture, distortionGrabPass * 2);

                //return 1;
                float3 fluidNormalsAvg = fluidsNormal;

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
                causticsTex *= causticsTex2 + causticsTex3;
                //causticsTex *= _CausticIntensity * _CausticColor;


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
                edgeInner = 0;//smoothstep(0.0604, 0.0624, edgeInner)*fluids.w;//edgeInner > 0.099 - edgeInner ? 1 : 0;


                float edge = max(edgeDepth, edgeInner);

                //Create diffuse surface.

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - fluids.xyz);

                float NdotL = saturate(dot(fluidsNormal.xyz, _WorldSpaceLightPos0.xyz)) * 0.5 + 0.5;


                //NdotL = step(0.705, NdotL);
                float4 diffuse = saturate(NdotL * _LightColor0);
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                float3 specular = _LightColor0 * pow(DotClamped(reflect(-lightDir, fluidNormalsAvg.xyz), viewDir), _SpecularPower) * _SpecularIntensity;
                diffuse += float4(specular, 1);//float4(ShadeSH9(half4(fluidNormalsAvg.xyz, 1)), 1);

                float fresnel = saturate(dot(fluidNormalsAvg.xyz, viewDir));
                fresnel = pow(fresnel, _FresnelPower);
                diffuse += fresnel;


                float4 waterDeepness = lerp(_ShallowWaterColor, _DeepWaterColor, 13.95 * densityMap);
                float waterDepthDifference = saturate((1.0 - frac(fluids.w)) / _DepthMaxDistance);
                float4 waterColor = lerp(_ShallowWaterColor, _DeepWaterColor, waterDepthDifference);
                waterColor = lerp(waterColor, waterDeepness, 1.0 - (densityMap));
                waterColor = lerp(waterColor, _DeepestWaterColor, 1.0 - smoothstep(0.785, 0.05, densityMap));


                float4 waterSpecular = waterColor;//diffuse + waterColor;

                float atteunuationDensity = min(0.0155,saturate(_DensityThickness * densityMap.z) * (exp(densityMap.z * 75 * fluidsDepth.z) - 1.0));

                fixed4 cleanFluidSingleColor = lerp(distortedOriginalImage, _DeepWaterColor * fluidsDepth.w, atteunuationDensity + 0.15);

                //return densityMap;
                //return fluids.w;
                //return atteunuationDensity;

                cleanFluidSingleColor = lerp(originalImage, cleanFluidSingleColor, step(0.01, fluidsDepth.w));

                //return cleanFluidSingleColor;

                float surface = smoothstep(0.05, 0.155, fluidsDepth.y);
                float4 fluidColorFinal = cleanFluidSingleColor + fluids.w * waterSpecular * 0.12;


                float4 colorField = float4((particleNormalMap.xyz * 0.5 + 0.5) * fluids.w, fluids.w);
                float4 colorFieldLerp = lerp(distortedOriginalImage * fluids.w, colorField, atteunuationDensity + 0.35);
                float4 colorSurfaceFluid = fluidColorFinal + edge + colorFieldLerp * 0.0935 + surface * 0.0756;


                float4 causaticLerpTop = lerp(distortedOriginalImage, causticsTex * fluids.w * step(0.0000001, fluidsDepth.y), 0.55);
                float4 causaticLerpSide = lerp(distortedOriginalImage, causticsTex * fluids.w * step(0.000000, fluidsDepth.y), 0.25);
                float4 causaticLerp = causaticLerpSide * 0.15 + causaticLerpTop * 0.825;

                float4 colorLerping = lerp(colorSurfaceFluid* waterColor, causaticLerp * step(0.00001, fluids.w), 0.0465 * step(0.00001, fluids.w) * step(0.0000001, fluidsDepth.y));

                //_CausticIntensity
                //return colorSurfaceFluid;

                float4 occulusion = lerp(colorLerping, originalImage, step(fluidsSceneDepth, unigmaDepth.r));
                //return curlMap;
                return occulusion;
            }

            ENDCG
        }
    }
}
