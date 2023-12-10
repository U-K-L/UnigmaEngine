Shader "Unlit/ScreenSpaceNormals"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            sampler2D customDepthTextureUnigma, camDepthTextureUnigma;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.projPos = ComputeScreenPos(o.vertex);
                o.camRelativeWorldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz - _WorldSpaceCameraPos;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
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

            float getRawDepth(float2 uv) { return tex2D(customDepthTextureUnigma, uv); }


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

            float3 getWorldPos(v2f i, float2 screenUV)
            {


                // sample depth texture
                float depth = SAMPLE_DEPTH_TEXTURE(_MainTex, screenUV);

                // get linear depth from the depth
                float sceneZ = LinearEyeDepth(depth);

                // calculate the view plane vector
                // note: Something like normalize(i.camRelativeWorldPos.xyz) is what you'll see other
                // examples do, but that is wrong! You need a vector that at a 1 unit view depth, not
                // a1 unit magnitude.
                float3 viewPlane = i.camRelativeWorldPos.xyz / dot(i.camRelativeWorldPos.xyz, unity_WorldToCamera._m20_m21_m22);

                // calculate the world position
                // multiply the view plane by the linear depth to get the camera relative world space position
                // add the world space camera position to get the world space position from the depth texture
                float3 worldPos = viewPlane * sceneZ + _WorldSpaceCameraPos;

                return worldPos;
            }
            float4x4 _ProjectionToWorld, _CameraInverseProjection;
            float3 getEyePos(sampler2D depthText, float2 uv)
            {
                float depth = tex2D(depthText, uv).w;
                float4 clipSpacePos = float4(uv * 2.0 - 1.0, depth, 1.0);
                float4 viewSpacePos = mul(_CameraInverseProjection, clipSpacePos);
                return viewSpacePos.xyz / viewSpacePos.w;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                float3 vpl = viewSpacePosAtPixelPosition(i,float2(-1, 0));
                float3 vpr = viewSpacePosAtPixelPosition(i,float2(1, 0));
                float3 vpd = viewSpacePosAtPixelPosition(i,float2(0,-1));
                float3 vpu = viewSpacePosAtPixelPosition(i,float2(0, 1));

                float3 viewNormal = normalize(-cross(vpu - vpd, vpr - vpl));
                float3 WorldNormal = mul((float3x3)unity_MatrixInvV, viewNormal);

                // if needed, this will detect the sky
                //float rawDepth2 = _CameraDepthTexture.Load(int3(i.vertex.xy, 0)).r;
                // if (rawDepth == 0.0)
                    // WorldNormal = float3(0,0,0);
                float rawDepth = tex2D(_MainTex, i.vertex.xy);
                //rawDepth = tex2D(_CameraDepthTexture, i.uv);
                //return float4(i.vertex.xy, 1, 0);
                //return float4(i.uv, 1, 0);

                float2 screenUV = i.projPos.xy / i.projPos.w;

                float3 worldPos = getWorldPos(i, screenUV);

                float3 ddxl = getWorldPos(i, screenUV + float2(-_MainTex_TexelSize.x, 0));
                float3 ddxr = getWorldPos(i, screenUV + float2(_MainTex_TexelSize.x, 0));
                float3 ddxd = getWorldPos(i, screenUV + float2(0, -_MainTex_TexelSize.y));
                float3 ddxu = getWorldPos(i, screenUV + float2(0, _MainTex_TexelSize.y));


                float3 viewNormal2 = normalize(-cross(ddxu - ddxd, ddxr - ddxl));
                float3 WorldNormal2 = mul((float3x3)unity_MatrixInvV, viewNormal2);


                float3 normalWorld = viewNormalAtPixelPosition(i.vertex.xy);


                float4 camDepth = tex2D(camDepthTextureUnigma, i.uv).r;
                float4 outputDepth = tex2D(_MainTex, i.uv).r;

                float ddxx = camDepth.r - (outputDepth.r);//abs(camDepth.r * 1.7451 - (outputDepth.r)).r;
                float4 ddxy = abs(outputDepth - outputDepth) * 100000;
                float4 ddxyT = abs(camDepth.z - camDepth.y) * 1000;
                float4 ddxyTM = abs(outputDepth.z - camDepth.z)*100;
                float2 screenPP = viewSpacePosAtScreenUV(i.uv);


                /*
                fixed4 fluids = tex2D(_MainTex, i.uv);
                fixed4 colorFieldGrad = tex2D(_MainTex, i.uv);
                float3 eyeSpacePos = getEyePos(_MainTex, i.uv);

                float width = 1920;
                float height = 1080;
                float offset = 0.5 / width;

                float2 uv = float2(i.uv.x + offset, i.uv.y);
                float2 uv2 = float2(i.uv.x - offset, i.uv.y);
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

                float3 hardNormals = float3(1, 0, 0) * step(0.31, normal.x);
                hardNormals += float3(0, 1, 0) * step(0.71, normal.y);
                hardNormals += float3(0, 0, 1) * step(0.31, normal.z);

                float4 finalImage = lerp(float4(hardNormals, 1.0), float4(normal, 1.0), 0.65);
                return float4(normal.xyz, 1);
                */

                //return ddxy;
                //return ddxx*1;//float4(screenPP.xy,1,1) * outputDepth;
                //return worldPos.xyzz;
                //return rawDepth;
                //return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, float4(screenUV, 0.0, 0.0))*10;
                //return SAMPLE_DEPTH_TEXTURE(_MainTex, float4(screenUV, 0.0, 0.0))*10;
                //return tex2D(_MainTex, i.uv).r;
                //return tex2D(CameraDepthOutput, i.uv);
                //return float4(ddxyTM.xy, tex2D(_MainTex, i.uv).zw);
                //return tex2D(customDepthTextureUnigma, i.uv);
                return normalWorld.xyzz;
            }
            ENDCG
        }
    }
}
