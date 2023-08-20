Shader "Hidden/FluidNormalBuffer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            float4x4 _ProjectionToWorld, _CameraInverseProjection;
            float2 _MainTex_TexelSize;

            float3 getEyePos(sampler2D depthText, float2 uv)
            {
                float depth = tex2D(depthText, uv);
                float4 clipSpacePos = float4(uv * 2.0 - 1.0, depth, 1.0);
                float4 viewSpacePos = mul(_CameraInverseProjection, clipSpacePos);
                return viewSpacePos.xyz / viewSpacePos.w;
            }

            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 fluids = tex2D(_MainTex, i.uv);
                float3 eyeSpacePos = getEyePos(_MainTex, i.uv);
                // calculate differences
                float3 ddx = getEyePos(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0)) - eyeSpacePos;
                float3 ddx2 = eyeSpacePos - getEyePos(_MainTex, i.uv + float2(-_MainTex_TexelSize.x, 0));
                if (abs(ddx.z) > abs(ddx2.z)) {
                    ddx = ddx2;
                }

                float3 ddy = getEyePos(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y)) - eyeSpacePos;
                float3 ddy2 = eyeSpacePos - getEyePos(_MainTex, i.uv + float2(0, -_MainTex_TexelSize.y));
                if (abs(ddy2.z) < abs(ddy.z)) {
                    ddy = ddy2;
                }
                // calculate normal
                float3 normal = cross(ddx, ddy);
                normal = normalize(normal);

				float4 finalImage = float4(normal, 1.0);
                
                return finalImage;
            }
            ENDCG
        }
    }
}
