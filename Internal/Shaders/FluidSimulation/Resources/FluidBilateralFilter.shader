Shader "Hidden/FluidBilateralFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScaleX("Scale X", Float) = 1.0
        _ScaleY("Scae Y", Float) = 1.0
        _BlurFallOff("Blur Falloff", Float) = 0.25
        _BlurRadius("Blur Radius", Float) = 5.0
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
            #pragma target 4.5
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

            sampler2D _MainTex, _UnigmaFluids, _DensityMap, _SurfaceMap, _VelocityMap;
            float2 _UnigmaFluids_TexelSize;
            float _BlurFallOff, _BlurRadius;
            float _ScaleX, _ScaleY;
            float4x4 _ProjectionToWorld, _CameraInverseProjection;


            float bilateralFilterDepth(sampler2D depthSampler, float2 texcoord)
            {
                float4 tex1 = tex2D(depthSampler, texcoord);
                float depth = tex1.w;
                float sum = 0;
                float wsum = 0;
                float blurScale = 1.0 / _BlurRadius;
                for (float x = -_BlurRadius; x <= _BlurRadius; x += 1.0) {
                    float tex = tex2D(depthSampler, texcoord + _UnigmaFluids_TexelSize * x * float2(_ScaleX, _ScaleY)).w;
                    // spatial domain
                    float r = x * blurScale;
                    float r2 = (tex - depth) * _BlurFallOff;
                    float w = exp(-r * r);
                    float g = exp(-r2 * r2);
                    sum += tex * w * g;
                    wsum += w * g;
                }
                if (wsum > 0.0) {
                    sum /= wsum;
                }
                return sum;
            }


            float bilateralFilterDensity(sampler2D depthSampler, float2 texcoord)
            {
                float4 tex1 = tex2D(depthSampler, texcoord);
                float depth = tex1.w;
                float sum = 0;
                float wsum = 0;
                float blurScale = 1.0 / _BlurRadius;
                for (float x = -_BlurRadius; x <= _BlurRadius; x += 1.0) {
                    float tex = tex2D(depthSampler, texcoord + _UnigmaFluids_TexelSize * x * float2(_ScaleX, _ScaleY)).z;
                    // spatial domain
                    float r = x * blurScale;
                    float r2 = (tex - depth) * _BlurFallOff;
                    float w = exp(-r * r);
                    float g = exp(-r2 * r2);
                    sum += tex * w * g;
                    wsum += w * g;
                }
                if (wsum > 0.0) {
                    sum /= wsum;
                }
                return sum;
            }
            

			
            
            float bilateralFilterSurface(sampler2D depthSampler, float2 texcoord)
            {
                float blurRadiusSurface = 10.80;
                float2 blurScaleSurface = float2(1.30, 0.65);
                blurScaleSurface *= float2(_ScaleX, _ScaleY);
                float4 tex1 = tex2D(depthSampler, texcoord);
                float depth = tex1.w;
                float sum = 0;
                float wsum = 0;
                float blurScale = 1.0 / blurRadiusSurface;
                for (float x = -blurRadiusSurface; x <= blurRadiusSurface; x += 1.0) {
                    float tex = tex2D(depthSampler, texcoord + _UnigmaFluids_TexelSize * x * float2(blurScaleSurface.x, blurScaleSurface.y)).y;
                    // spatial domain
                    float r = x * blurScale;
                    float r2 = (tex - depth) * _BlurFallOff;
                    float w = exp(-r * r);
                    float g = exp(-r2 * r2);
                    sum += tex * w * g;
                    wsum += w * g;
                }
                if (wsum > 0.0) {
                    sum /= wsum;
                }
                return sum;
            }

            float bilateralFilterVelocity(sampler2D depthSampler, float2 texcoord)
            {
                float blurRadiusSurface = 3.087;
                float2 blurScaleSurface = float2(1.95, 1.25);
                blurScaleSurface *= float2(_ScaleX, _ScaleY);
                float4 tex1 = tex2D(depthSampler, texcoord);
                float depth = tex1.w;
                float sum = 0;
                float wsum = 0;
                float blurScale = 1.0 / blurRadiusSurface;
                for (float x = -blurRadiusSurface; x <= blurRadiusSurface; x += 1.0) {
                    float tex = tex2D(depthSampler, texcoord + _UnigmaFluids_TexelSize * x * float2(blurScaleSurface.x, blurScaleSurface.y)).x;
                    // spatial domain
                    float r = x * blurScale;
                    float r2 = (tex - depth) * _BlurFallOff;
                    float w = exp(-r * r);
                    float g = exp(-r2 * r2);
                    sum += tex * w * g;
                    wsum += w * g;
                }
                if (wsum > 0.0) {
                    sum /= wsum;
                }
                return sum;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float4 filter = 0.0; 
                //Depth.
                filter.w = bilateralFilterDepth(_MainTex, i.uv);
                //Surface
                filter.y = bilateralFilterSurface(_MainTex, i.uv);
                //Velocity
                filter.x = bilateralFilterVelocity(_MainTex, i.uv);
                //Density.
                filter.z = bilateralFilterDensity(_MainTex, i.uv);
                return filter;
            }
                
            ENDCG
        }
    }
}
