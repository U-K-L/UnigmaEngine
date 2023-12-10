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

            sampler2D _MainTex, _UnigmaFluids, _UnigmaFluidsDepth, _DistancesMap;
            float4x4 _ProjectionToWorld, _CameraInverseProjection;
            float2 _MainTex_TexelSize, _UnigmaFluidsDepth_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                    float4 vertex : SV_POSITION;
                float4 projPos : TEXCOORD2;
                float3 camRelativeWorldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.projPos = ComputeScreenPos(o.vertex);
                o.camRelativeWorldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz - _WorldSpaceCameraPos;

                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            sampler2D _CameraDepthTexture;
            float4 _CameraDepthTexture_TexelSize;

            float3 rayFromScreenUV(in float2 uv, in float4x4 InvMatrix)
            {
                float x = uv.x * 2.0 - 1.0;
                float y = uv.y * 2.0 - 1.0;
                float4 position_s = float4(x, y, 1.0, 1.0);
                return mul(InvMatrix, position_s * _ProjectionParams.z);
            }

            float getRawDepth(float2 uv) { return tex2D(_UnigmaFluidsDepth, uv).w; }


            float3 viewSpacePosAtPixelPosition(v2f i, float2 pos)
            {
                float rawDepth = tex2D(_MainTex, i.uv + pos * _MainTex_TexelSize.xy);//float rawDepth = 
                float2 uv = i.uv + pos * _MainTex_TexelSize.xy;
                float3 ray = rayFromScreenUV(uv, unity_CameraInvProjection);
                return ray * Linear01Depth(rawDepth);
            }

            float3 viewSpacePosAtScreenUV(float2 uv)
            {
                float3 viewSpaceRay = mul(unity_CameraInvProjection, float4(uv * 2.0 - 1.0, 1.0, 1.0) * _ProjectionParams.z);
                float rawDepth = getRawDepth(uv);
                return viewSpaceRay * Linear01Depth(rawDepth);
            }

            half3 viewNormalAtPixelPosition(float2 vpos)
            {
                // screen uv from vpos
                float2 uv = vpos * _MainTex_TexelSize.xy;

                // current pixel's depth
                float c = getRawDepth(uv);

                // get current pixel's view space position
                half3 viewSpacePos_c = viewSpacePosAtScreenUV(uv);

                // get view space position at 1 pixel offsets in each major direction
                half3 viewSpacePos_l = viewSpacePosAtScreenUV(uv + float2(-1.0, 0.0) * _MainTex_TexelSize.xy);
                half3 viewSpacePos_r = viewSpacePosAtScreenUV(uv + float2(1.0, 0.0) * _MainTex_TexelSize.xy);
                half3 viewSpacePos_d = viewSpacePosAtScreenUV(uv + float2(0.0, -1.0) * _MainTex_TexelSize.xy);
                half3 viewSpacePos_u = viewSpacePosAtScreenUV(uv + float2(0.0, 1.0) * _MainTex_TexelSize.xy);


                // get the difference between the current and each offset position
                half3 l = viewSpacePos_c - viewSpacePos_l;
                half3 r = viewSpacePos_r - viewSpacePos_c;
                half3 d = viewSpacePos_c - viewSpacePos_d;
                half3 u = viewSpacePos_u - viewSpacePos_c;

                // get depth values at 1 & 2 pixels offsets from current along the horizontal axis
                half4 H = half4(
                    getRawDepth(uv + float2(-1.0, 0.0) * _MainTex_TexelSize.xy),
                    getRawDepth(uv + float2(1.0, 0.0) * _MainTex_TexelSize.xy),
                    getRawDepth(uv + float2(-2.0, 0.0) * _MainTex_TexelSize.xy),
                    getRawDepth(uv + float2(2.0, 0.0) * _MainTex_TexelSize.xy)
                    );

                // get depth values at 1 & 2 pixels offsets from current along the vertical axis
                half4 V = half4(
                    getRawDepth(uv + float2(0.0, -1.0) * _MainTex_TexelSize.xy),
                    getRawDepth(uv + float2(0.0, 1.0) * _MainTex_TexelSize.xy),
                    getRawDepth(uv + float2(0.0, -2.0) * _MainTex_TexelSize.xy),
                    getRawDepth(uv + float2(0.0, 2.0) * _MainTex_TexelSize.xy)
                    );

                // current pixel's depth difference from slope of offset depth samples
                // differs from original article because we're using non-linear depth values
                // see article's comments
                half2 he = abs((2 * H.xy - H.zw) - c);
                half2 ve = abs((2 * V.xy - V.zw) - c);

                // pick horizontal and vertical diff with the smallest depth difference from slopes
                half3 hDeriv = he.x < he.y ? l : r;
                half3 vDeriv = ve.x < ve.y ? d : u;

                // get view space normal from the cross product of the best derivatives
                half3 viewNormal = normalize(cross(hDeriv, vDeriv));

                return viewNormal;
            }

            fixed4 frag(v2f i) : SV_Target
            {



                float3 normalWorld = viewNormalAtPixelPosition(i.vertex.xy);

                return normalWorld.xyzz;
            }


            /*
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



            float3 getEyePos(sampler2D depthText, float2 uv)
            {
                float depth = tex2D(depthText, uv).w;
                float4 clipSpacePos = float4(uv * 2.0 - 1.0, depth, 1.0);
                float4 viewSpacePos = mul(_CameraInverseProjection, clipSpacePos);
                return viewSpacePos.xyz / viewSpacePos.w;
            }

            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 fluids = tex2D(_UnigmaFluidsDepth, i.uv);
                fixed4 colorFieldGrad = tex2D(_UnigmaFluids, i.uv);
                float3 eyeSpacePos = getEyePos(_UnigmaFluidsDepth, i.uv);

                float width = 1920;
                float height = 1080;
                float offset = 0.5 / width;

                float2 uv = float2(i.uv.x + offset, i.uv.y);
                float2 uv2 = float2(i.uv.x - offset, i.uv.y);
                // calculate differences
                float3 ddx = getEyePos(_UnigmaFluidsDepth, i.uv + float2(_UnigmaFluidsDepth_TexelSize.x, 0)) - eyeSpacePos;
                float3 ddx2 = eyeSpacePos - getEyePos(_UnigmaFluidsDepth, i.uv + float2(-_UnigmaFluidsDepth_TexelSize.x, 0));
                if (abs(ddx.z) > abs(ddx2.z)) {
                    ddx = ddx2;
                }

                float3 ddy = getEyePos(_UnigmaFluidsDepth, i.uv + float2(0, _UnigmaFluidsDepth_TexelSize.y)) - eyeSpacePos;
                float3 ddy2 = eyeSpacePos - getEyePos(_UnigmaFluidsDepth, i.uv + float2(0, -_UnigmaFluidsDepth_TexelSize.y));
                if (abs(ddy2.z) < abs(ddy.z)) {
                    ddy = ddy2;
                }
                // calculate normal
                float3 normal = cross(ddx, ddy);
                normal = normalize(normal);

				float3 hardNormals = float3(1, 0, 0) * step( 0.31, normal.x);
                hardNormals += float3(0, 1, 0) * step(0.71, normal.y);
                hardNormals += float3(0, 0, 1) * step(0.31, normal.z);

				float4 finalImage = lerp(float4(hardNormals, 1.0), float4(normal, 1.0), 0.65);
                return float4(normal.xyz, 1);
            }
            */
            ENDCG
        }
    }
}
