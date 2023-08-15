Shader "Hidden/FluidComposition"
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

            sampler2D _MainTex, _UnigmaFluids;
            float2 _UnigmaFluids_TexelSize;
            const float filterRadius = 50.0;
            float4x4 _ProjectionToWorld, _CameraInverseProjection;

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
                float rawDepth = tex2D(_UnigmaFluids, uv);
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
                float depth = tex2D(depthText, uv).w;
				float4 clipSpacePos = float4(uv * 2.0 - 1.0, depth, 1.0);
				float4 viewSpacePos = mul(_CameraInverseProjection, clipSpacePos);
				return viewSpacePos.xyz / viewSpacePos.w;
            }

            float bilateralFilter(sampler2D depthSampler, float2 texcoord)
            {
                float depth = tex2D(depthSampler, texcoord).w;
                float sum = 0;
                float wsum = 0;
                float blurScale = 1000.0;
                float blurDepthFalloff = 0.1;
                float2 blurDir = float2(1.0,1.0) * _UnigmaFluids_TexelSize;
                for(float x=-filterRadius; x<=filterRadius; x+=1.0) {
                    blurDir = float2(x / filterRadius,1.0 - (x / filterRadius)) * _UnigmaFluids_TexelSize;
                    float tex = tex2D(depthSampler, texcoord + x*blurDir).w;
                    // spatial domain
                    float r = x * blurScale;
                    float w = exp(-r*r);
                    // range domain
                    float r2 = (tex - depth) * blurDepthFalloff;
                    float g = exp(-r2*r2);
                    sum += tex * w * g;
                    wsum += w * g;
                }
                if (wsum > 0.0) {
                    sum /= wsum;
                }
                return sum;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 fluids = tex2D(_UnigmaFluids, i.uv);
                fixed4 originalImage = tex2D(_MainTex, i.uv);
                //fixed4 finalCol = lerp(originalImage, fluids, fluids.w);

                float3 eyeSpacePos = getEyePos(_UnigmaFluids, i.uv);
                //epsilon = _UnigmaFluids_TexelSize;
                _UnigmaFluids_TexelSize *= 20;
                // calculate differences
                float3 ddx = getEyePos(_UnigmaFluids, i.uv + float2(_UnigmaFluids_TexelSize.x, 0)) - eyeSpacePos;
                float3 ddx2 = eyeSpacePos - getEyePos(_UnigmaFluids, i.uv + float2(-_UnigmaFluids_TexelSize.x, 0));
                if (abs(ddx.z) > abs(ddx2.z)) {
                    ddx = ddx2;
                }
                
                float3 ddy = getEyePos(_UnigmaFluids, i.uv + float2(0, _UnigmaFluids_TexelSize.y)) - eyeSpacePos;
                float3 ddy2 = eyeSpacePos - getEyePos(_UnigmaFluids, i.uv + float2(0, -_UnigmaFluids_TexelSize.y));
                if (abs(ddy2.z) < abs(ddy.z)) {
                    ddy = ddy2;
                }
                // calculate normal
                float3 n = cross(ddx, ddy);
                n = normalize(n);
                
                float3 worldPos = depthWorldPosition(i.uv, fluids.r, _ProjectionToWorld);
                float3 normals = viewNormalAtPixelPosition(i.uv);
                float3 finalCol = lerp(originalImage.xyz, bilateralFilter(_UnigmaFluids, i.uv), max(0,fluids.w));
                
                
                return float4(finalCol,1);
            }
            ENDCG
        }
    }
}
