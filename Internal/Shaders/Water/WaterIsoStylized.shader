// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unigma/WaterIsoStylized"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WaterMainColor("The main Water Color", Color) = (1,1,1,1)
        //Depth values:
        _DepthMaxDistance("Depth Max Distance", Float) = 1
        _DepthGradiantShallow("Depth Gradient Shallow", Color) = (1,1,1,1)
        _DepthGradiantDeep("Depth Gradient Deep", Color) = (1,1,1,1)
        _DepthDistanceAxis("Axis of depth", Vector) = (0.0,0.0,0.0,1.0)

        _SurfaceNoise("Surface Noise", 2D) = "white" {}
        _SurfaceNoiseCutoff("Surface Noise Cutoff", Range(0, 1)) = 0.777
        _FoamMaxDistance("Foam Maximum Distance", Float) = 0.4
        _FoamMinDistance("Foam Minimum Distance", Float) = 0.04
        _SurfaceNoiseScroll("Surface Noise Scroll Amount", Vector) = (0.03, 0.03, 0, 0)

        _RefracStr("Refractance Strength, amp, speed.", Vector) = (12,2.0,0.002,1)
        _TextureInfluence("Snoise, Map, Snoise2", Vector) = (1.0,1.0,1.0,1.0)

        _LineArtColor("Line art Color", Color) = (0,0,0,1)

        _LineThickness("Line Thickness, ZY, XZ, XY, All", Vector) = (0.0, 0.0, 0.0, 0.0)
        _SparkleFoamColor("Brightest foam color", Color) = (1,1,1,1)
        
        [Normal]_DisplacementTex("Displacement Map", 2D) = "white"{}
        _Intensity ("Intensity of displacement", Range(0, 2)) = 1
        _Scale ("Scale of texture", Range(0,2)) = 1


    }
    SubShader
    {
        Tags
        {
	        "Queue" = "Transparent+2000"
        }
        LOD 200
        GrabPass { "_WaterIsoBackground" }
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 noiseUV : TEXCOORD6;
                UNITY_FOG_COORDS(1)
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
                float3 viewNormal : TEXCOORD4;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 screenPosition : TEXCOORD3;
                float4 position : TEXCOORD7;
                SHADOW_COORDS(5)
            };

            sampler2D _MainTex, _WaterIsoBackground;
            float4 _MainTex_ST;
            float4 _DepthGradiantDeep;
            float4 _DepthGradiantShallow;
            float _DepthMaxDistance;
            sampler2D _CameraDepthTexture;
            sampler2D _CameraNormalsTexture;

            sampler2D _SurfaceNoise;
            float4 _SurfaceNoise_ST;
            float _SurfaceNoiseCutoff;

            float _FoamMaxDistance;
            float _FoamMinDistance;
            float4 _SurfaceNoiseScroll;
            float4 _DepthDistanceAxis;

            float4 _WaterMainColor, _SparkleFoamColor;
            float4 _RefracStr;
            float4 _TextureInfluence;

            float4 _LineThickness;
            float4 _LineArtColor;

            sampler2D _DisplacementTex;
            float _Intensity;
            float _Scale;

            v2f vert (appdata v)
            {
                v2f o;
                float3 p = v.vertex.xyz;

			    float k = 2 * UNITY_PI / 2.0;
			    p.y = 0.5 * sin(k * (p.x - 1.0 * _Time.y));

                o.position = v.vertex;
                o.noiseUV = TRANSFORM_TEX(v.uv, _SurfaceNoise);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;
                o.normal = v.normal;
                o.viewNormal = COMPUTE_VIEW_NORMAL;
                o.worldPos = mul( unity_ObjectToWorld, float4( v.vertex.xyz, 1.0 ) ).xyz;

                //if(length(o.worldNormal.zy) > 0.1)
                   // v.vertex.xyz = p;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPosition = ComputeScreenPos(o.pos);
                UNITY_TRANSFER_FOG(o,o.vertex);
                TRANSFER_SHADOW(o)
                return o;
            }
            #define NOISE_SIMPLEX_1_DIV_289 0.00346020761245674740484429065744f
            float mod289(float x) {
                return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
            }
 
            float2 mod289(float2 x) {
                return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
            }
 
            float3 mod289(float3 x) {
                return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
            }
 
            float4 mod289(float4 x) {
                return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
            }
 
             //Iquilezles, raises value slightly, m = threshold (anything above m stays unchanged).
            // n = value when 0
            // x = value input.
            float almostIdentity(float x, float m, float n){
                if(x > m) return x;

                const float a = 2.0*n - m;
                const float b = 2.0*m - 3.0*n;
                const float t = x/m;
                return (a*t + b)*t*t + n;
            }
 
            // ( x*34.0 + 1.0 )*x =
            // x*x*34.0 + x
            float permute(float x) {
                return mod289(
                    x*x*34.0 + x
                );
            }
 
            float3 permute(float3 x) {
                return mod289(
                    x*x*34.0 + x
                );
            }
 
            float4 permute(float4 x) {
                return mod289(
                    x*x*34.0 + x
                );
            }
 
 
 
            float taylorInvSqrt(float r) {
                return 1.79284291400159 - 0.85373472095314 * r;
            }
 
            float4 taylorInvSqrt(float4 r) {
                return 1.79284291400159 - 0.85373472095314 * r;
            }
 
 
 
            float4 grad4(float j, float4 ip)
            {
                const float4 ones = float4(1.0, 1.0, 1.0, -1.0);
                float4 p, s;
                p.xyz = floor( frac(j * ip.xyz) * 7.0) * ip.z - 1.0;
                p.w = 1.5 - dot( abs(p.xyz), ones.xyz );
 
                // GLSL: lessThan(x, y) = x < y
                // HLSL: 1 - step(y, x) = x < y
                s = float4(
                    1 - step(0.0, p)
                );
 
                // Optimization hint Dolkar
                // p.xyz = p.xyz + (s.xyz * 2 - 1) * s.www;
                p.xyz -= sign(p.xyz) * (p.w < 0);
 
                return p;
            }

            float snoise(float3 v)
            {
                const float2 C = float2(
                    0.166666666666666667, // 1/6
                    0.333333333333333333 // 1/3
                );
                const float4 D = float4(0.0, 0.5, 1.0, 2.0);
            // First corner
                float3 i = floor(v + dot(v, C.yyy));
                float3 x0 = v - i + dot(i, C.xxx);
            // Other corners
                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1 - g;
                float3 i1 = min(g.xyz, l.zxy);
                float3 i2 = max(g.xyz, l.zxy);
                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
                float3 x3 = x0 - D.yyy; // -1.0+3.0*C.x = -0.5 = -D.y
            // Permutations
                i = mod289(i);
                float4 p = permute(
                    permute(
                        permute(
                                i.z + float4(0.0, i1.z, i2.z, 1.0)
                        ) + i.y + float4(0.0, i1.y, i2.y, 1.0)
                    ) + i.x + float4(0.0, i1.x, i2.x, 1.0)
                );
            // Gradients: 7x7 points over a square, mapped onto an octahedron.
            // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
                float n_ = 0.142857142857; // 1/7
                float3 ns = n_ * D.wyz - D.xzx;
                float4 j = p - 49.0 * floor(p * ns.z * ns.z); // mod(p,7*7)
                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_); // mod(j,N)
                float4 x = x_ * ns.x + ns.yyyy;
                float4 y = y_ * ns.x + ns.yyyy;
                float4 h = 1.0 - abs(x) - abs(y);
                float4 b0 = float4(x.xy, y.xy);
                float4 b1 = float4(x.zw, y.zw);
                //float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
                //float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
                float4 s0 = floor(b0) * 2.0 + 1.0;
                float4 s1 = floor(b1) * 2.0 + 1.0;
                float4 sh = -step(h, float4(0, 0, 0, 0));
                float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
                float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
                float3 p0 = float3(a0.xy, h.x);
                float3 p1 = float3(a0.zw, h.y);
                float3 p2 = float3(a1.xy, h.z);
                float3 p3 = float3(a1.zw, h.w);
            //Normalise gradients
                float4 norm = rsqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
                p0 *= norm.x;
                p1 *= norm.y;
                p2 *= norm.z;
                p3 *= norm.w;
            // Mix final noise value
                float4 m = max(0.5 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
                m = m * m;
                return 105.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
            }
            //Converts linear depth to logirthimic depth found in perspective cameras.
            //On mobile the depth is reversed.
            //https://forum.unity.com/threads/getting-scene-depth-z-buffer-of-the-orthographic-camera.601825/#post-4966334
            float OrthoDepth(float rawDepth)
            {
                float persp = LinearEyeDepth(rawDepth);
                float z = _ProjectionParams.z;
                float ortho = (z-_ProjectionParams.y)*(1-rawDepth)+_ProjectionParams.y;
                return lerp(persp,ortho,unity_OrthoParams.w);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {


                //Get Normals:
                float4 worldNormal = float4(i.worldNormal * 0.5 + 0.5, 1);
                float3 existingNormal = tex2Dproj(_CameraNormalsTexture, UNITY_PROJ_COORD(i.screenPosition));
                float3 normalDot = saturate(dot(existingNormal, i.viewNormal));

                //Calculate the Animation:
                float3 flowDirection =  _SurfaceNoiseScroll.xyz * _SurfaceNoiseScroll.w;
                float3 noiseUV = float3(i.uv.x + _Time.y * flowDirection.x, i.uv.y + _Time.y * flowDirection.y, i.uv.y + _Time.y * flowDirection.z);
                float2 screenPosUV = i.screenPosition.xy/i.screenPosition.w;

                //Wavy water distortion.
                float XUV = screenPosUV.x*_RefracStr.x+_Time.y*_RefracStr.y;
	            float YUV = screenPosUV.y*_RefracStr.x+_Time.y*_RefracStr.y;
	            screenPosUV.y += cos(XUV+YUV)*_RefracStr.z*cos(YUV);
	            screenPosUV.x += sin(XUV-YUV)*_RefracStr.z*sin(YUV);

                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                //Create paintery effect for that under the water.
                float2 uv = i.screenPosition.xy/i.screenPosition.w;
                float3 diplacementNormals = UnpackNormal(tex2D(_DisplacementTex, screenPosUV) );
                float2 distortion = uv + ((_Intensity*0.01) * diplacementNormals.rg);
                
                fixed3 underWater = tex2D(_WaterIsoBackground, distortion).xyz;

                float noise = tex2D(_SurfaceNoise, noiseUV.xy).r;
                float surfaceNoiseSample2 = snoise(i.worldPos*70 + noiseUV);
                float surfaceNoiseSample = _TextureInfluence.w*((snoise(i.worldPos*7 + noiseUV)*_TextureInfluence.x) - ((noise/2.0)*_TextureInfluence.y ) + (surfaceNoiseSample2*_TextureInfluence.z));

                //Calculate Depth.
                float existingDepth01 = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPosition)).r;
                float existingDepthLinear = LinearEyeDepth(existingDepth01);
                float trueDepth = OrthoDepth(existingDepth01);
                float4 depthAxis = i.screenPosition * _DepthDistanceAxis;
                float depthDiff = trueDepth - (depthAxis.x + depthAxis.y + depthAxis.z + depthAxis.w)/length(_DepthDistanceAxis);
                //depthDiff = min(1, depthDiff);

                //Calculate Depth Colors.
                float waterDepthDiff = saturate(depthDiff / _DepthMaxDistance);
                float4 waterColor = lerp(_DepthGradiantShallow, _DepthGradiantDeep, waterDepthDiff);


                //Calculate noise:
                float surfaceNoise = surfaceNoiseSample > _SurfaceNoiseCutoff ? 1 : 0;

                //Calculate foam:
                float foamDistance = lerp(_FoamMaxDistance, _FoamMinDistance, normalDot);
                float foamDepthDifference01 = saturate(depthDiff / foamDistance);
                float surfaceNoiseCutoff = foamDepthDifference01 * _SurfaceNoiseCutoff;
                float4 surfaceNoiseDepth = surfaceNoiseSample > surfaceNoiseCutoff ? _SparkleFoamColor : 0;

                if(length(i.normal.x > 0.1))
                {
                    float t_water = max(min( 1-(abs(i.position.x*i.position.x)*abs(i.position.y)*i.position.z*i.position.z*50.0), 1.0), 0.0);
                    waterColor.xyz = lerp(waterColor.xyz,waterColor.xyz*waterColor.xyz*0.85,  t_water);
                }
                

                //Add results
                
                float4 underWaterMerge = float4(waterColor.xyz * underWater + surfaceNoiseDepth, 1.0);
                float4 result = lerp(waterColor + surfaceNoiseDepth, underWaterMerge, 0.5);


                //Calculate outline:

                //World space planes.
                float2 uvX = i.worldPos.zy; // x facing plane
                float2 uvY = i.worldPos.xz; // y facing plane
                float2 uvZ = i.worldPos.xy; // z facing plane

                //Local space planes.
                //float period = sin(_Time.y+i.position.x)*_LineThickness.x;
                float2 uvx = i.position.zy;
                float2 uvy = i.position.xz;
                float2 uvz = i.position.xy;

                //float4 topColor = length(i.normal.y);
                //float4 frontColor = length(i.normal.z);
                //float4 sideColor =  length(i.normal.x);
                
                if(_LineThickness.w > 0.0)
                {
                    float3 cutOffLine = _LineThickness.xyz * _LineThickness.w;
                    float4 lineColor = _LineArtColor;//lerp(_LineArtColor, result, surfaceNoiseSample*abs(sin(_Time.y*i.position.z))*2.0);//float4(_LineArtColor.xyz * surfaceNoiseSample, 1.0);
                    if(length(uvx) >cutOffLine.x)
                        return lineColor;
                    if(length(uvy) > cutOffLine.y)
                        return lineColor;
                    if(length(uvz) > cutOffLine.z - 0.005)
                        return lineColor;         
                }


                return result;
            }
            ENDCG
        }

    }
}
