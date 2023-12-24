Shader "Unigma/UnigmaOutlines"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BackgroundTexture("Background Texture", 2D) = "white" {}
        _ScaleOuter("Scale Outer Lines", Range(0,100)) = 1
        _ScaleInner("Scale Inner Lines", Range(0,100)) = 1
        _ScaleShadow("Scale Shadow Lines", Range(0,100)) = 1
        _ScaleWhiteOutline("Scale of white outline", Range(0, 100)) = 1
        _DepthThreshold("Depth Threshold", Range(0,2)) = 1
        _PosThreshold("Position Threshold", Range(0,2)) = 1
        _NormalThreshold("Normal Threshold", Range(0,2)) = 1
        _ShadowOutlineColor("Shadow Outline Color", Color) = (0,0,0,1)
        
		_InnerLines("Inner lines color", Color) = (0,0,0,1)
		_OuterLines("Outer lines color", Color) = (0,0,0,1)
		_LineBreak("Line break texture", 2D) = "white" {}
		_LineBreakage("Scale of line breakage", Range(0,1)) = 0.15
		_SurfaceNoiseScroll("Surface noise scroll", Vector) = (0,0,0,0)
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
            #include "../ShaderHelpers.hlsl"

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

            sampler2D _UnigmaGlobalIllumination, _BackgroundTexture, _MainTex, _IsometricDepthNormal, _LineBreak, _IsometricOutlineColor, _IsometricInnerOutlineColor, _IsometricPositions, _UnigmaDepthShadowsMap;
            float4 _MainTex_TexelSize, _OuterLines, _InnerLines, _ShadowOutlineColor;
            sampler2D _CameraDepthNormalsTexture;
            float _ScaleOuter, _ScaleWhiteOutline, _ScaleShadow, _DepthThreshold, _NormalThreshold, _ScaleInner, _LineBreakage, _PosThreshold;
			float4 _SurfaceNoiseScroll;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 GlobalIllumination = tex2D(_UnigmaGlobalIllumination, i.uv);
                fixed4 BackgroundTexture = tex2D(_BackgroundTexture, i.uv);
                fixed4 _UnigmaDepthShadows = tex2D(_UnigmaDepthShadowsMap, i.uv);
				float4 OutterLineColors = tex2D(_IsometricOutlineColor, i.uv);
				float4 InnerLineColors = tex2D(_IsometricInnerOutlineColor, i.uv);
                
                float3 flowDirection = _SurfaceNoiseScroll.xyz * _SurfaceNoiseScroll.w;
                float3 noiseUV = float3(i.uv.x + _Time.y * flowDirection.x, i.uv.y + _Time.y * flowDirection.y, i.uv.y + _Time.y * flowDirection.z);

				float4 lineBreak = tex2D(_LineBreak, noiseUV);
				float4 mainTex = tex2D(_MainTex, i.uv);
                
				float OuterScale = OutterLineColors.a * 5 + _ScaleOuter;
                float scaleFloor = floor(OuterScale * 0.5);
                float scaleCeil = ceil(OuterScale * 0.5);

                float2 bottomLeft = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleFloor;
                float2 topRight = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleCeil;
                float2 bottomRight = i.uv + float2(_MainTex_TexelSize.x * scaleCeil, -_MainTex_TexelSize.y * scaleFloor);
                float2 topLeft = i.uv + float2(-_MainTex_TexelSize.x * scaleFloor, _MainTex_TexelSize.y * scaleCeil);


                float4 depthnormal0 = tex2D(_IsometricDepthNormal, bottomLeft);
                float4 depthnormal1 = tex2D(_IsometricDepthNormal, topRight);
                float4 depthnormal2 = tex2D(_IsometricDepthNormal, bottomRight);
                float4 depthnormal3 = tex2D(_IsometricDepthNormal, topLeft);

                
                float depthFiniteDifference3 = depthnormal1.a - depthnormal0.a;
                float depthFiniteDifference4 = depthnormal3.a - depthnormal2.a;
                float edgeDepth = sqrt(pow(depthFiniteDifference3, 2) + pow(depthFiniteDifference4, 2)) * 100;
                float depthThreshold = _DepthThreshold * depthnormal0;
                edgeDepth = edgeDepth > depthThreshold ? 1 : 0;
                
                float4 pos0 = tex2D(_IsometricPositions, bottomLeft);
                float4 pos1 = tex2D(_IsometricPositions, topRight);
                float4 pos2 = tex2D(_IsometricPositions, bottomRight);
                float4 pos3 = tex2D(_IsometricPositions, topLeft);


                float posFiniteDifference3 = abs(pos1.a - pos0.a);//length(pos1 - pos0);
                float posFiniteDifference4 = abs(pos3.a - pos2.a);//length(pos3 - pos2);
                float edgePos = sqrt(pow(posFiniteDifference3, 2) + pow(posFiniteDifference4, 2)) * 100;
                float posThreshold = _PosThreshold;
                edgePos = edgePos > posThreshold ? 1 : 0;
                //float edgeMask = length(depthnormal0 + depthnormal1 + depthnormal2 + depthnormal3) > 0.01 ? 1 : 0;
                
                scaleFloor = floor(_ScaleInner * 0.5);
                scaleCeil = ceil(_ScaleInner * 0.5);

                bottomLeft = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleFloor;
                topRight = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleCeil;
                bottomRight = i.uv + float2(_MainTex_TexelSize.x * scaleCeil, -_MainTex_TexelSize.y * scaleFloor);
                topLeft = i.uv + float2(-_MainTex_TexelSize.x * scaleFloor, _MainTex_TexelSize.y * scaleCeil);


                float4 normal0 = tex2D(_IsometricDepthNormal, bottomLeft);
                float4 normal1 = tex2D(_IsometricDepthNormal, topRight);
                float4 normal2 = tex2D(_IsometricDepthNormal, bottomRight);
                float4 normal3 = tex2D(_IsometricDepthNormal, topLeft);

                float3 normalFiniteDifference0 = normal1.xyz - normal0.xyz;
                float3 normalFiniteDifference1 = normal3.xyz - normal2.xyz;
                
                float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
                edgeNormal = edgeNormal > _NormalThreshold ? 1 : 0;


                //Get shadow outlines.
                
                scaleFloor = floor(_ScaleShadow * 0.5);
                scaleCeil = ceil(_ScaleShadow * 0.5);

                bottomLeft = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleFloor;
                topRight = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleCeil;
                bottomRight = i.uv + float2(_MainTex_TexelSize.x * scaleCeil, -_MainTex_TexelSize.y * scaleFloor);
                topLeft = i.uv + float2(-_MainTex_TexelSize.x * scaleFloor, _MainTex_TexelSize.y * scaleCeil);

                float4 shadow0 = tex2D(_UnigmaDepthShadowsMap, bottomLeft);
                float4 shadow1 = tex2D(_UnigmaDepthShadowsMap, topRight);
                float4 shadow2 = tex2D(_UnigmaDepthShadowsMap, bottomRight);
                float4 shadow3 = tex2D(_UnigmaDepthShadowsMap, topLeft);


                float2 shadowFiniteDifference3 = shadow1.yz - shadow0.yz;
                float2 shadowFiniteDifference4 = shadow3.yz - shadow2.yz;
                float edgeShadow = sqrt(dot(shadowFiniteDifference3, shadowFiniteDifference3) + dot(shadowFiniteDifference4, shadowFiniteDifference4));
                float depthThresholdShadow = _DepthThreshold * shadow0;
                edgeShadow = edgeShadow > depthThresholdShadow ? 1 : 0;

                //UnigmaDepth. For scene vs background.
                scaleFloor = floor(_ScaleWhiteOutline * 0.5);
                scaleCeil = ceil(_ScaleWhiteOutline * 0.5);

                bottomLeft = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleFloor;
                topRight = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleCeil;
                bottomRight = i.uv + float2(_MainTex_TexelSize.x * scaleCeil, -_MainTex_TexelSize.y * scaleFloor);
                topLeft = i.uv + float2(-_MainTex_TexelSize.x * scaleFloor, _MainTex_TexelSize.y * scaleCeil);

                shadow0 = tex2D(_UnigmaDepthShadowsMap, bottomLeft);
                shadow1 = tex2D(_UnigmaDepthShadowsMap, topRight);
                shadow2 = tex2D(_UnigmaDepthShadowsMap, bottomRight);
                shadow3 = tex2D(_UnigmaDepthShadowsMap, topLeft);


                shadowFiniteDifference3 = shadow1.r - shadow0.r;
                shadowFiniteDifference4 = shadow3.r - shadow2.r;
                float edgeUnigmaDepth = sqrt(pow(shadowFiniteDifference3, 2) + pow(shadowFiniteDifference4, 2)) * 100;
                depthThresholdShadow = _DepthThreshold * shadow0;
                edgeUnigmaDepth = edgeUnigmaDepth > 0.999 ? 1 : 0;
                
                float edge = max(edgeDepth, edgePos);
                
                //Delete where edge is present.
                edgeUnigmaDepth = edgeUnigmaDepth * step(edge, 0.01);
                float4 FinalColor = mainTex;

                //Order matters here.
                //First place the shadow line as it has the least priority.
                FinalColor = lerp(FinalColor, float4(_ShadowOutlineColor.xyz, 1), edgeShadow * _ShadowOutlineColor.w);
                FinalColor = lerp(FinalColor, InnerLineColors, edgeNormal *step(0.001, InnerLineColors.w));
                FinalColor = step(_LineBreakage, lineBreak.r) * FinalColor;

                //return InnerLineColors;
                //And make it optional!
                //FinalColor = lerp(FinalColor, BackgroundTexture, step(_UnigmaDepthShadows.r, 0.01));
                FinalColor = lerp(FinalColor, float4(OutterLineColors.xyz, 1), edge * step(0.001, OutterLineColors.w));
				FinalColor = lerp(mainTex, FinalColor, FinalColor.a);
                
                float shadows = _UnigmaDepthShadows.y;
                float3 shadowStrength = 0.115 * step(0.001,shadows) * float3(0.55,1, 0.55);
                //FinalColor = lerp(mainTex, FinalColor, lineBreak.r);
                //White outline added.
                FinalColor = float4(FinalColor.xyz - shadowStrength, FinalColor.w) + edgeUnigmaDepth;

                return lerp(FinalColor, FinalColor + GlobalIllumination*0.1, min(1, GlobalIllumination.w));
            }
            ENDCG
        }

    }
}
