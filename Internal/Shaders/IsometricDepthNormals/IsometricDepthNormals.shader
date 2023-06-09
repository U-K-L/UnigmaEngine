Shader "Hidden/IsometricDepthNormals"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale("Scale", Range(0,1)) = 1
        _DepthThreshold("Depth Threshold", Range(0,2)) = 1
        _NormalThreshold("Normal Threshold", Range(0,2)) = 1
        
		_InnerLines("Inner lines color", Color) = (0,0,0,1)
		_OuterLines("Outer lines color", Color) = (0,0,0,1)
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

            sampler2D _MainTex, _OutlineMap;
            float4 _MainTex_TexelSize, _OuterLines, _InnerLines;
            sampler2D _CameraDepthNormalsTexture;
            float _Scale, _DepthThreshold, _NormalThreshold;

            fixed4 frag (v2f i) : SV_Target
            {
                //read depthnormal
				float4 mainTex = tex2D(_MainTex, i.uv);
                float4 sampleTex = tex2D(_OutlineMap, i.uv);
                float4 normalTex = float4(sampleTex.xyz, 1);
                float4 depthTex = float4(sampleTex.w, sampleTex.w, sampleTex.w, 1);
                float4 depthnormal = tex2D(_CameraDepthNormalsTexture, i.uv);
                
                float3 normal;
                float depth;
                DecodeDepthNormal(depthnormal, depth, normal);

                depth = depth * _ProjectionParams.z;
                depth *= 0.1;

                float scaleFloor = floor(_Scale * 0.5);
                float scaleCeil = ceil(_Scale * 0.5);

                float2 bottomLeft = i.uv - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleFloor;
                float2 topRight = i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * scaleCeil;
                float2 bottomRight = i.uv + float2(_MainTex_TexelSize.x * scaleCeil, -_MainTex_TexelSize.y * scaleFloor);
                float2 topLeft = i.uv + float2(-_MainTex_TexelSize.x * scaleFloor, _MainTex_TexelSize.y * scaleCeil);


                float4 depthnormal0 = tex2D(_OutlineMap, bottomLeft);
                float4 depthnormal1 = tex2D(_OutlineMap, topRight);
                float4 depthnormal2 = tex2D(_OutlineMap, bottomRight);
                float4 depthnormal3 = tex2D(_OutlineMap, topLeft);

                
                float depthFiniteDifference3 = depthnormal1.a - depthnormal0.a;
                float depthFiniteDifference4 = depthnormal3.a - depthnormal2.a;
                float edgeDepth = sqrt(pow(depthFiniteDifference3, 2) + pow(depthFiniteDifference4, 2)) * 100;
                float depthThreshold = _DepthThreshold * depthnormal0;
                edgeDepth = edgeDepth > depthThreshold ? 1 : 0;
                //float edgeMask = length(depthnormal0 + depthnormal1 + depthnormal2 + depthnormal3) > 0.01 ? 1 : 0;
                

                float3 normalFiniteDifference0 = depthnormal1.xyz - depthnormal0.xyz;
                float3 normalFiniteDifference1 = depthnormal3.xyz - depthnormal2.xyz;
                
                float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
                edgeNormal = edgeNormal > _NormalThreshold ? 1 : 0;

                
                float edge = max(edgeDepth, edgeNormal);
                
                float4 FinalColor = lerp(0, _InnerLines + mainTex, edgeNormal);
                FinalColor = lerp(FinalColor, _OuterLines, edgeDepth);
				FinalColor = lerp(mainTex, FinalColor, FinalColor.a);
                return FinalColor;
            }
            ENDCG
        }
    }
}
