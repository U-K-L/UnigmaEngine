// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'

Shader "Hidden/FluidCompositionRaster"
{
    Properties
    {
        _NoiseTex("Noise Texture", 2D) = "white" {}
		_NoiseScale("Noise Scale, used for noise texture", Vector) = (1.0, 1.0, 1.0, 1.0)
		_TopTexture("Texture for the top", 2D) = "white" {}
        _MainTex ("Texture", 2D) = "white" {}
		_DepthMaxDistance("Maximum distance for depth, used for depth buffer", Float) = 100.0
        _ShallowWaterColor("Shallow Water Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_DeepWaterColor("Deep Water Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _BrightWaterColor("Brighter Water Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _DeepestWaterColor("Deepest Water Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Threshold("NdotL thresholds", Vector) = (1.0, 1.0, 1.0, 1.0)
		_BlendSmooth("Normal Smoothing", Range(0, 10)) = 0.5
		_Spread("Spread", Range(0, 10)) = 0.5
		_EdgeWidth("Edge Width", Range(0, 10)) = 0.5
        [Normal]_DisplacementTex("Displacement Map", 2D) = "white"{}
        [Normal]_DisplacementTexInner("Displacement Map inside of water", 2D) = "white"{}
        _Intensity("Intensity of displacement", Range(0, 2)) = 1
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

        _FoamIntensity("Foam Intensity", Vector) = (4.0,1.0,1.0,1.0)
        _SurfaceNoise("Surface Noise", 2D) = "white" {}
        _SurfaceNoiseCutoff("Surface Noise Cutoff", Range(0, 1)) = 0.777
        _SurfaceNoiseScroll("Surface Noise Scroll Amount", Vector) = (0.03, 0.03, 0, 0)
        _AirVisibility("Air visibility", Range(0, 1)) = 0
        _EdgeNormalThreshold("Edge Normal Threshold", Range(0, 1)) = 0.8

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

			sampler2D _UnigmaNormal, _UnigmaMotionID, _UnigmaBackgroundColor, _UnigmaDepthShadowsMap, _SurfaceMap, _CausticTex, _CausticTile, _CausticNoise, _CurlMap, _VelocityMap, _ColorFieldNormalMap, _MainTex, _UnigmaFluids, _UnigmaFluidsDepth, _UnigmaFluidsNormals, _NoiseTex, _DensityMap, _DisplacementTex, _DisplacementTexInner, _TopTexture;
            float2 _UnigmaFluids_TexelSize, _UnigmaFluidsNormals_TexelSize, _MainTex_TexelSize;
			float _BlurFallOff, _BlurRadius, _DepthMaxDistance, _BlendSmooth, _Spread, _EdgeWidth, _Intensity, _DensityThickness, _OutlineThickness;
			float _CausticIntensity, _CausticScale, _Speed, _ScaleX, _ScaleY, _SpecularPower, _SpecularIntensity, _FresnelPower;
            float4x4 _ProjectionToWorld, _CameraInverseProjection;
            float4 _DeepWaterColor, _NoiseScale, _ShallowWaterColor, _DeepestWaterColor, _NoiseScaleCaustic, _CausticColor, _BrightWaterColor, _Threshold;
            
            sampler2D _SurfaceNoise;
            float4 _SurfaceNoise_ST;
            float _SurfaceNoiseCutoff;
            float4 _SurfaceNoiseScroll;
            float4 _FoamIntensity;
            float _AirVisibility, _EdgeNormalThreshold;

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
                fixed4 unigmaDepth = tex2D(_UnigmaDepthShadowsMap, i.uv);
				fixed4 unigmaBackground = tex2D(_UnigmaBackgroundColor, i.uv);
                fixed4 unigmaMotion = tex2D(_UnigmaMotionID, i.uv);
                fixed4 unigmaNormal = tex2D(_UnigmaNormal, i.uv);

                //return unigmaMotion;
                //return fluidsNormal;
                //return curlMap;
				//return unigmaBackground;
                //return unigmaDepth.z*100;
                //return fluidsDepth;
                //return lerp(fluids.w, unigmaDepth.z, step(fluids.w, unigmaDepth.z))*10;
                //return densityMap;
                float heatMapIntensity = fluidsDepth.x;
                //heatMapIntensity *= 0.0251;
                heatMapIntensity = min(heatMapIntensity, 1);


                float fluidsSceneDepth = fluidsDepth.w;
                fluidsDepth.w = (fluidsDepth.w) * step(0, fluidsDepth.w) * 0.00255;

                fluidsDepth.w = fluidsDepth.w * step(0.09, fluidsDepth.w);
                //return 1;
                float3 fluidNormalsAvg = fluidsNormal;
                
                //return fluidsDepth.w;
                //Triplanar Fluid Surfaces.
//------------------------------------------------------------

                float3 worldNormalVec = fluidNormalsAvg;
                float speed = _Time.x * _Speed;
                float3 blendNormal = saturate(pow(worldNormalVec * _BlendSmooth, 4));
                float3 worldPos = fluids.xyz;

                float2 screenPosUV2 = worldPos.zx;
                float XUV2 = screenPosUV.x * 0.25 + _Time.y * _Speed;
                float YUV2 = screenPosUV.y * 0.25 + _Time.y * _Speed;

                screenPosUV2.y += cos(XUV + YUV) * 0.25 * cos(YUV);
                screenPosUV2.x += sin(XUV - YUV) * 0.25 * sin(YUV);

                _NoiseScale.xyz *= _NoiseScale.w;
                float4 xn = tex2D(_TopTexture, (worldPos.zy * _NoiseScale.x) - (speed));
                float4 yn = tex2D(_TopTexture, (screenPosUV2 * _NoiseScale.y) - (speed));
                float4 zn = tex2D(_TopTexture, (worldPos.xy * _NoiseScale.z) - (speed));
                float4 noisetexture = zn;
                noisetexture = lerp(noisetexture, xn, blendNormal.x);
                noisetexture = lerp(noisetexture, yn, blendNormal.y);

                _NoiseScaleCaustic.xyz *= _NoiseScaleCaustic.w;
                float4 xx = tex2D(_CausticNoise, float2(worldPos.zy * _NoiseScaleCaustic.x) - (speed));
                float4 yy = tex2D(_CausticNoise, float2(worldPos.xz * _NoiseScaleCaustic.y) - (speed));
                float4 zz = tex2D(_CausticNoise, float2(worldPos.xy * _NoiseScaleCaustic.z) - (speed));

                float triCaustic = triplanar(blendNormal, xx, yy, zz);

                float4 xc = tex2D(_TopTexture, float2((worldPos.z + triCaustic) * _CausticScale, (worldPos.y) * (_CausticScale / 4)));
                float4 zc = tex2D(_TopTexture, float2((worldPos.x + triCaustic) * _CausticScale, (worldPos.y) * (_CausticScale / 4)));
                float4 yc = tex2D(_TopTexture, (float2(worldPos.x + triCaustic, worldPos.z + triCaustic)) * _CausticScale);

                float4 causticsTex = triplanar(blendNormal, xc, yc, zc);


                float secScale = _CausticScale * 0.6;
                float4 xc2 = tex2D(_CausticTile, float2((worldPos.z - triCaustic) * secScale, (worldPos.y) * (secScale / 4)));
                float4 zc2 = tex2D(_CausticTile, float2((worldPos.x - triCaustic) * secScale, (worldPos.y) * (secScale / 4)));
                float4 yc2 = tex2D(_CausticTile, float2(worldPos.x - triCaustic, worldPos.z - triCaustic) * secScale);

                float4 causticsTex2 = triplanar(blendNormal, xc2, yc2, zc2);

                float4 xc3 = tex2D(_TopTexture, float2((worldPos.z - triCaustic) * secScale, (worldPos.y) * (secScale / 4)));
                float4 zc3 = tex2D(_TopTexture, float2((worldPos.x - triCaustic) * secScale, (worldPos.y) * (secScale / 4)));
                float4 yc3 = tex2D(_TopTexture, float2(worldPos.x - triCaustic, worldPos.z - triCaustic) * secScale);

                float4 causticsTex3 = triplanar(blendNormal, xc3, yc3, zc3);

                // combining
                causticsTex *= causticsTex2 + causticsTex3;
                //causticsTex *= _CausticIntensity * _CausticColor;

                //return causticsTex;


                //Triplanar
//------------------------------------------------------------

                speed *= 5;
                float3 worldNormalVecMap = unigmaNormal.xyz;
                float3 blendNormalMap = saturate(pow(worldNormalVecMap * _BlendSmooth, 4));
                float3 worldPosMap = unigmaMotion.xyz;

                float4 xnm = tex2D(_TopTexture, (worldPosMap.zy * _NoiseScale.x) - (speed));
                float4 ynm = tex2D(_TopTexture, (screenPosUV2 * _NoiseScale.y) - (speed));
                float4 znm = tex2D(_TopTexture, (worldPosMap.xy * _NoiseScale.z) - (speed));
                float4 noisetextureMap = zn;
                noisetextureMap = lerp(noisetextureMap, xnm, blendNormalMap.x);
                noisetextureMap = lerp(noisetextureMap, ynm, blendNormalMap.y);

                float4 xxm = tex2D(_CausticNoise, float2(worldPosMap.zy * _NoiseScaleCaustic.x) - (speed));
                float4 yym = tex2D(_CausticNoise, float2(worldPosMap.xz * _NoiseScaleCaustic.y) - (speed));
                float4 zzm = tex2D(_CausticNoise, float2(worldPosMap.xy * _NoiseScaleCaustic.z) - (speed));

                float triCausticMap = triplanar(blendNormalMap, xxm, yym, zzm);

                float4 xcm = tex2D(_CausticTex, float2((worldPosMap.z + triCaustic) * _CausticScale, (worldPosMap.y) * (_CausticScale / 4)));
                float4 zcm = tex2D(_CausticTex, float2((worldPosMap.x + triCaustic) * _CausticScale, (worldPosMap.y) * (_CausticScale / 4)));
                float4 ycm = tex2D(_CausticTex, (float2(worldPosMap.x + triCaustic, worldPosMap.z + triCaustic)) * _CausticScale);

                float4 causticsTexMap = triplanar(blendNormalMap, xcm, ycm, zcm);

                float4 xc2m = tex2D(_CausticTile, float2((worldPosMap.z - triCaustic) * secScale, (worldPosMap.y) * (secScale / 4)));
                float4 zc2m = tex2D(_CausticTile, float2((worldPosMap.x - triCaustic) * secScale, (worldPosMap.y) * (secScale / 4)));
                float4 yc2m = tex2D(_CausticTile, float2(worldPosMap.x - triCaustic, worldPosMap.z - triCaustic) * secScale);

                float4 causticsTex2Map = triplanar(blendNormalMap, xc2m, yc2m, zc2m);

                float4 xc3m = tex2D(_CausticTex, float2((worldPosMap.z - triCaustic) * secScale, (worldPosMap.y) * (secScale / 4)));
                float4 zc3m = tex2D(_CausticTex, float2((worldPosMap.x - triCaustic) * secScale, (worldPosMap.y) * (secScale / 4)));
                float4 yc3m = tex2D(_CausticTex, float2(worldPosMap.x - triCaustic, worldPosMap.z - triCaustic) * secScale);

                float4 causticsTex3Map = triplanar(blendNormalMap, xc3m, yc3m, zc3m);

                // combining
                causticsTexMap *= causticsTex2Map + causticsTex3Map;
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

                float3 normalFiniteDifference0 = normal1.yzz - normal0.yzz;
                float3 normalFiniteDifference1 = normal3.yzz - normal2.yzz;

                

                float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
                edgeNormal = edgeNormal > _EdgeNormalThreshold ? 0.98 : 0;
                edgeNormal *= fluidsDepth.y * step(fluidsNormal.y, 0.1);

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
                edge = max(edgeNormal, edge);

                //Create diffuse surface.

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - fluids.xyz);

                float NdotL = saturate(dot(fluidsNormal.xyz, _WorldSpaceLightPos0.xyz)) * 0.5 + 0.5;

                float4 midTones = _ShallowWaterColor * step(_Threshold.x, NdotL) * step(NdotL, _Threshold.w);
                float4 shadowLight = _DeepWaterColor * step(NdotL, _Threshold.y);
                float4 highlights = _BrightWaterColor * step(_Threshold.z, NdotL);

                float4 midHighLowLight = max(midTones, shadowLight);
                midHighLowLight = max(midHighLowLight, highlights);


                //NdotL = step(0.705, NdotL);
                float4 diffuse = midHighLowLight;//saturate(NdotL * _LightColor0);
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                float3 specular = _LightColor0 * pow(DotClamped(reflect(-lightDir, fluidNormalsAvg.xyz), viewDir), _SpecularPower) * _SpecularIntensity;

                
                float lightIntensitySpec = specular.r;

                if(lightIntensitySpec <= 0.5 && lightIntensitySpec > 0.081)
                    lightIntensitySpec = 0.15;
                if(lightIntensitySpec > 0.5 && lightIntensitySpec < 0.95)
                    lightIntensitySpec = 0.35;
                if(lightIntensitySpec > 0.95)
                    lightIntensitySpec = 1;

                diffuse += float4(lightIntensitySpec, lightIntensitySpec, lightIntensitySpec, 1);//float4(ShadeSH9(half4(fluidNormalsAvg.xyz, 1)), 1);

                float fresnel = saturate(dot(fluidNormalsAvg.xyz, viewDir));
                fresnel = pow(fresnel, _FresnelPower);
                diffuse += fresnel;

                float4 waterDeepness = lerp(_ShallowWaterColor, _DeepWaterColor, 13.95 * 0.25);
                float waterDepthDifference = saturate((1.0 - frac(fluidsDepth.w)) / _DepthMaxDistance);
                float4 waterColor = lerp(_ShallowWaterColor, _DeepWaterColor, waterDepthDifference);
                waterColor = lerp(waterColor, waterDeepness, 1.0 - (0.25));
                //waterColor = lerp(waterColor, _DeepestWaterColor, 1.0 - smoothstep(0.785, 0.05, 0.25));



                float3 halfVector = normalize(_WorldSpaceLightPos0 + viewDir);
                float NdotH = dot(worldNormalVec, halfVector);
                //Calculate the Animation:
                float3 flowDirection = _SurfaceNoiseScroll.xyz * _SurfaceNoiseScroll.w;
                float3 noiseUV = float3(i.uv.x + _Time.y * flowDirection.x, i.uv.y + _Time.y * flowDirection.y, i.uv.y + _Time.y * flowDirection.z);

                //Calculate noise:
                //Get random value based on time and position.
				float randomVal = snoise(worldPos + noiseUV)*0.1;
                float noise = tex2D(_SurfaceNoise, noiseUV.xy).r;
                float surfaceNoiseSample2 = snoise(worldPos * _FoamIntensity.x + noiseUV);
                float surfaceNoiseSample = (snoise(worldPos * _FoamIntensity.y + noiseUV) - surfaceNoiseSample2);//snoise(i.worldPos * 7 + noiseUV);//_TextureInfluence.w * ((snoise(i.worldPos * 7 + noiseUV) * _TextureInfluence.x) - ((noise / 2.0) * _TextureInfluence.y) + (surfaceNoiseSample2 * _TextureInfluence.z));

				float noiseMask = noise > _SurfaceNoiseCutoff ? 1 : 0;
                float4 surfaceNoise = smoothstep(_SurfaceNoiseCutoff, _SurfaceNoiseCutoff + 0.1, surfaceNoiseSample);//surfaceNoiseSample > _SurfaceNoiseCutoff ? _SparkleFoamColor* noiseMask : 0;
                //float surfaceNoiseEmit = pow(NdotH+ NdotH, 1.5)*25 * fluidsDepth.y * tex2D(_SurfaceNoise, float2(worldPos.x * 10, worldPos.z *5)).r*10* step(abs(rand(worldPos + noiseUV)).r, 0.978*  fluidsDepth.y*  fluidsDepth.y) * surfaceNoise;

                float noiseHeatMap = length(worldPos)*100 * step(abs(rand(worldPos + noiseUV)).r, 0.978*  fluidsDepth.y*  fluidsDepth.y);
                if(curlMap.w == 1)
                    waterColor = float4(0.8, 0.96, 1, 1);

                if(curlMap.w == 3)
                    waterColor = float4(0.75, 0.75, 0.75, 1);

                float4 waterSpecular =  waterColor + diffuse;

                float atteunuationDensity = min(0.0155,saturate(_DensityThickness * densityMap.z) * (exp(densityMap.z * 75 * fluidsDepth.z) - 1.0));

                fixed4 cleanFluidSingleColor = lerp(distortedOriginalImage, _DeepWaterColor * fluidsDepth.w, atteunuationDensity + 0.15);



                float heatMapSmoothed = pow(smoothstep(0.35, 1, heatMapIntensity), 0.5);
                //return densityMap;
                //return fluids.w;
                //return atteunuationDensity;

                cleanFluidSingleColor = lerp(originalImage, cleanFluidSingleColor, step(0.01, fluidsDepth.w));



                float surface = smoothstep(0.05, 0.155, fluidsDepth.y);

                

                float4 fluidColorFinal = 0.52*cleanFluidSingleColor + fluidsDepth.w * waterSpecular * 0.3171575;

                float4 colorField = float4((particleNormalMap.xyz * 0.5 + 0.5) * fluidsDepth.w, fluidsDepth.w);
                float4 colorFieldLerp = lerp(distortedOriginalImage * fluidsDepth.w, colorField, atteunuationDensity + 0.35);
                float4 colorSurfaceFluid = 0.1715*heatMapIntensity + fluidColorFinal + colorFieldLerp * 0.0935 + surface * 0.0756;


                float4 causaticLerpTop = lerp(distortedOriginalImage, causticsTex * fluids.w * step(0.0000001, fluidsDepth.y), 0.55);
                float4 causaticLerpSide = lerp(distortedOriginalImage, causticsTex * fluids.w * step(0.000000, fluidsDepth.y), 0.25);
                float4 causaticLerp = causaticLerpSide * 0.15 + causaticLerpTop * 0.825;

                float4 colorLerping = lerp(colorSurfaceFluid* waterColor, causaticLerp * step(0.00001, fluidsDepth.w), 0.0465 * step(0.00001, fluids.w) * step(0.0000001, fluidsDepth.y));

                float4 finalColorWater = colorSurfaceFluid* waterColor;

                float4 fluidColorFinalNoLight =  0.7*((0.52*cleanFluidSingleColor + fluidsDepth.w) + edge + colorFieldLerp * 0.0935 + surface * 0.0756) * waterColor;
                //return causticsTex;
                //_CausticIntensity
                float whiteCausatic = smoothstep(0.9, 0.95, length(causticsTex.xyz) / sqrt(3));
                //heatMapSmoothed *= abs(surfaceNoiseSample);
                //heatMapSmoothed = lerp(heatMapSmoothed,1, max(0, min(1, noiseHeatMap)));

                if(heatMapSmoothed <= 0.5 && heatMapSmoothed > 0.01)
                    heatMapSmoothed = 0.15;
                if(heatMapSmoothed > 0.5 && heatMapSmoothed < 0.95)
                    heatMapSmoothed = 0.35;
                if(heatMapSmoothed > 0.95)
                    heatMapSmoothed = 1;

                colorLerping = lerp(finalColorWater, finalColorWater + causticsTex*0.1075  + whiteCausatic*0.02182, smoothstep(0.01, 0.6925, fluidsNormal.y));
                
                colorLerping += 0.0910875*smoothstep(0.9, 0.95, length(causticsTexMap.xyz) / sqrt(3)) + causticsTexMap*0.0571;


                float4 occulusion = lerp(originalImage, colorLerping, step(0.001, fluidsDepth.w));

                return occulusion;

                if(particleNormalMap.w == 1)
                    return lerp(distortedOriginalImage, fluidColorFinalNoLight, _AirVisibility);
                //return curlMap;
                
            }

            ENDCG
        }
    }
}
