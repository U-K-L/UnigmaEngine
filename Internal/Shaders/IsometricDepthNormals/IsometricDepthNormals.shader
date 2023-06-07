Shader "Hidden/IsometricDepthNormals"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale("Scale", Range(0,1)) = 1
        _DepthThreshold("Depth Threshold", Range(0,2)) = 1
        _NormalThreshold("Normal Threshold", Range(0,2)) = 1
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _CameraDepthNormalsTexture;
            float _Scale, _DepthThreshold, _NormalThreshold;

            fixed4 frag (v2f i) : SV_Target
            {
                //read depthnormal
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

                float4 depthnormal0 = tex2D(_CameraDepthNormalsTexture, bottomLeft);
                float4 depthnormal1 = tex2D(_CameraDepthNormalsTexture, topRight);
                float4 depthnormal2 = tex2D(_CameraDepthNormalsTexture, bottomRight);
                float4 depthnormal3 = tex2D(_CameraDepthNormalsTexture, topLeft);

                float3 normal0, normal1, normal2, normal3;
                float depth0, depth1, depth2, depth3;

                DecodeDepthNormal(depthnormal0, depth0, normal0);
                DecodeDepthNormal(depthnormal1, depth1, normal1);
                DecodeDepthNormal(depthnormal2, depth2, normal2);
                DecodeDepthNormal(depthnormal3, depth3, normal3);
                
                float depthFiniteDifference3 = depthnormal1.r - depthnormal0.r;
                float depthFiniteDifference4 = depthnormal3.r - depthnormal2.r;

                float edgeDepth = sqrt(pow(depthFiniteDifference3, 2) + pow(depthFiniteDifference4, 2)) * 100;
                float depthThreshold = _DepthThreshold * depthnormal0;
                edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

                float depthFiniteDifference0 = depth1 - depth0;
                float depthFiniteDifference1 = depth3 - depth2;

                float3 normalFiniteDifference0 = normal1 - normal0;
                float3 normalFiniteDifference1 = normal3 - normal2;

                float edgeDepth2 = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
                float depthThreshold2 = _DepthThreshold * depthnormal0;
                edgeDepth2 = edgeDepth2 > depthThreshold ? 1 : 0;
                
                float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
                edgeNormal = edgeNormal > _NormalThreshold ? 1 : 0;

                float4 coloredEdges = float4(edgeDepth2, 0, edgeNormal- edgeDepth2, 1);
                float edge = max(edgeDepth2, edgeNormal);
                edge = max(edgeDepth, edge);
                return coloredEdges;
            }
            ENDCG
        }
    }
}
