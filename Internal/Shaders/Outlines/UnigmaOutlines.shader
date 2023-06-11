Shader "Unigma/UnigmaOutlines"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScaleOuter("Scale Outer Lines", Range(0,100)) = 1
		_ScaleInner("Scale Inner Lines", Range(0,100)) = 1
        _DepthThreshold("Depth Threshold", Range(0,2)) = 1
		_PosThreshold("Position Threshold", Range(0,2)) = 1
        _NormalThreshold("Normal Threshold", Range(0,2)) = 1
        
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

            sampler2D _MainTex, _IsometricDepthNormal, _LineBreak, _IsometricOutlineColor, _IsometricInnerOutlineColor, _IsometricPositions;
            float4 _MainTex_TexelSize, _OuterLines, _InnerLines;
            sampler2D _CameraDepthNormalsTexture;
            float _ScaleOuter, _DepthThreshold, _NormalThreshold, _ScaleInner, _LineBreakage, _PosThreshold;
			float4 _SurfaceNoiseScroll;

            fixed4 frag(v2f i) : SV_Target
            {
				//float4 objectPositions = tex2D(_IsometricPositions, i.uv);
				float4 OutterLineColors = tex2D(_IsometricOutlineColor, i.uv);
				float4 InnerLineColors = tex2D(_IsometricInnerOutlineColor, i.uv);
                
                float3 flowDirection = _SurfaceNoiseScroll.xyz * _SurfaceNoiseScroll.w;
                float3 noiseUV = float3(i.uv.x + _Time.y * flowDirection.x, i.uv.y + _Time.y * flowDirection.y, i.uv.y + _Time.y * flowDirection.z);

				float4 lineBreak = tex2D(_LineBreak, noiseUV);
				float4 mainTex = tex2D(_MainTex, i.uv);
                
                float scaleFloor = floor(_ScaleOuter * 0.5);
                float scaleCeil = ceil(_ScaleOuter * 0.5);

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


                float posFiniteDifference3 = length(pos1 - pos0);
                float posFiniteDifference4 = length(pos3 - pos2);
                float edgePos = sqrt(pow(posFiniteDifference3, 2) + pow(posFiniteDifference4, 2)) * 100;
                float posThreshold = _PosThreshold * pos0.a;
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

                
                float edge = max(edgeDepth, edgePos);
                
                float4 FinalColor = lerp(0, InnerLineColors, edgeNormal);
                FinalColor = step(_LineBreakage, lineBreak.r) * FinalColor;
                FinalColor = lerp(FinalColor, OutterLineColors, edge);
				FinalColor = lerp(mainTex, FinalColor, FinalColor.a);
                //FinalColor = lerp(mainTex, FinalColor, lineBreak.r);
                return FinalColor;
            }
            ENDCG
        }
    }
}
