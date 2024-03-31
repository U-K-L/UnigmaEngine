Shader "Unlit/UngimaWaterV2"
{

    Properties
    {
        _MainTex("Texture", 2D) = "black" {}
        _Fade("Fade camera", Range(0,100000)) = 1
        _NormalAmount("Normal amount", Range(0,50)) = 1
        _DepthAmount("Depth amount", Range(0,50)) = 1
        _ObjectID("Object ID", Int) = 0
		_WaterColor("Water Color", Color) = (0.80, 0.94, 1.0, 1.0)
		_Smootheness("Smootheness", Range(0,1)) = 1.0
        _SurfaceNoise("Surface Noise", 2D) = "white" {}
        _SurfaceNoiseCutoff("Surface Noise Cutoff", Range(0, 1)) = 0.777
        _SurfaceNoiseScroll("Surface Noise Scroll Amount", Vector) = (0.03, 0.03, 0, 0)
        _RefracStr("Refractance Strength, amp, speed.", Vector) = (12,2.0,0.002,1)
		_DisplacementTex("Normal Distortion", 2D) = "white" {}
		_Intensity("Intensity", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque+12" }
        LOD 200
        GrabPass { "_WaterIsoBackgroundV2" }
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
            
     Pass
        {
            name "RenderNormals"
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
                float3 normal : NORMAL;
                float3 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float depthGen : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float3 T : TEXCOORD3;
                float3 B : TEXCOORD4;
                float3 N : TEXCOORD5;
                float3 worldNormal : TEXCOORD6;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Fade, _NormalAmount, _DepthAmount;

            v2f vert(appdata v)
            {
                v2f o;
                float4 vertexProgjPos = mul(UNITY_MATRIX_MV, v.vertex);
                o.depthGen = saturate((-vertexProgjPos.z - _ProjectionParams.y) / (_Fade + 0.001));
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;//UnityObjectToWorldNormal(v.normal);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                float3 worldNormal = mul((float3x3)unity_ObjectToWorld, v.normal);
                float3 worldTangent = mul((float3x3)unity_ObjectToWorld, v.tangent);

                float3 binormal = cross(v.normal, v.tangent.xyz); // *input.tangent.w;
                float3 worldBinormal = mul((float3x3)unity_ObjectToWorld, binormal);

                // and, set them
                o.N = normalize(worldNormal);
                o.T = normalize(worldTangent);
                o.B = normalize(worldBinormal);
                return o;
            }

            //Make entire shader a shader pass in the future.
            fixed4 frag(v2f i) : SV_Target
            {
                //Get texture data
                return float4(i.worldNormal, 1.0);
                float3 tangentNormal = tex2D(_MainTex, i.uv).xyz;
                float3 rgbNormalMap = tangentNormal.xzy * 2 - 1;
                rgbNormalMap = UnityObjectToWorldNormal(rgbNormalMap);
                return float4(rgbNormalMap, 1);
                return float4(i.worldNormal, 1.0);
                tangentNormal = normalize(tangentNormal * 2 - 1);
                float3x3 TBN = float3x3(normalize(i.T), normalize(i.B), normalize(i.N));
                TBN = transpose(TBN);
                float3 worldNormal = mul(TBN, tangentNormal);
                if (tex2D(_MainTex, i.uv).w <= 0)
                    return float4(i.worldNormal, 1.0);
                return float4(worldNormal, 1);


                return float4(i.worldNormal, 1.0);
            }
            ENDCG
        }
            
     Pass
        {
            name "RenderPosition"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
                // make fog work
                #pragma multi_compile_fog

                #include "UnityCG.cginc"
                #include "../ShaderHelpers.hlsl"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    float3 normal : NORMAL;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    float4 vertex : SV_POSITION;
                    float depthGen : TEXCOORD1;
                    float3 normal : TEXCOORD2;
                    float3 worldPos : TEXCOORD3;
                    float4 projPos : TEXCOORD4;
                    float3 camRelativeWorldPos : TEXCOORD5;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                float _Fade, _NormalAmount, _DepthAmount;
                UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

                v2f vert(appdata v)
                {
                    v2f o;
                    float4 vertexProgjPos = mul(UNITY_MATRIX_MV, v.vertex);
                    o.depthGen = saturate((-vertexProgjPos.z - _ProjectionParams.y) / (_Fade + 0.001));
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.projPos = ComputeScreenPos(o.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    o.normal = UnityObjectToWorldNormal(v.normal);
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    o.camRelativeWorldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz - _WorldSpaceCameraPos;
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float2 screenUV = i.projPos.xy / i.projPos.w;

                    // sample depth texture
                    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);

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
                    worldPos = mul(unity_CameraToWorld, float4(i.worldPos, 1.0));

                    float4 finalColor = float4(i.worldPos, 1.0);
                    return finalColor;
                }
                ENDCG
            }
         
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
                float4 vertex : SV_POSITION;
                float4 pos : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
            };

            sampler2D _MainTex, _UnigmaWaterReflections, _WaterIsoBackgroundV2, _CameraDepthNormalsTexture, _UnigmaDepthShadowsMap, _DisplacementTex;
            float4 _MainTex_ST, _WaterColor;


            sampler2D _SurfaceNoise;
            float4 _SurfaceNoise_ST;
            float _SurfaceNoiseCutoff;
            float4 _RefracStr;
            float4 _SurfaceNoiseScroll;
            float _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPosition = ComputeScreenPos(o.pos);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                
                //Calculate the Animation:
                float3 flowDirection = _SurfaceNoiseScroll.xyz * _SurfaceNoiseScroll.w;
                float3 noiseUV = float3(i.uv.x + _Time.y * flowDirection.x, i.uv.y + _Time.y * flowDirection.y, i.uv.y + _Time.y * flowDirection.z);
                float2 screenPosUV = i.screenPosition.xy / i.screenPosition.w;

                //Wavy water distortion.
                float XUV = screenPosUV.x * _RefracStr.x + _Time.y * _RefracStr.y;
                float YUV = screenPosUV.y * _RefracStr.x + _Time.y * _RefracStr.y;
                screenPosUV.y += cos(XUV + YUV) * _RefracStr.z * cos(YUV);
                screenPosUV.x += sin(XUV - YUV) * _RefracStr.z * sin(YUV);

                //Create paintery effect for that under the water.
                float2 uv = i.screenPosition.xy / i.screenPosition.w;
                float3 diplacementNormals = UnpackNormal(tex2D(_DisplacementTex, screenPosUV));
                float2 distortion = uv + ((_Intensity * 0.01) * diplacementNormals.rg);

				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthNormalsTexture, i.screenPosition.xy / i.screenPosition.w).r;

                fixed4 _UnigmaDepthShadows = tex2D(_UnigmaDepthShadowsMap, uv);
                fixed4 underWater = tex2D(_WaterIsoBackgroundV2, distortion);
                return lerp(underWater, _WaterColor, _UnigmaDepthShadows.r*20);//float4(_UnigmaDepthShadows.r, _UnigmaDepthShadows.r, _UnigmaDepthShadows.r, 1);//lerp(_WaterColor, underWater , depth);
            }
            ENDCG
        }
        
        Pass
        {
            Name "DepthShadowsRaytracingShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../RayTraceHelpersUnigma.hlsl"

            Texture2D<float4> _MainTex, _DisplacementTex, _SurfaceNoise;
			SamplerState sampler_MainTex, sampler_DisplacementTex, sampler_SurfaceNoise;
            float4 _WaterColor;
            float _Smoothness;

            float4 _SurfaceNoise_ST;
            float _SurfaceNoiseCutoff;
            float4 _RefracStr;
            float4 _SurfaceNoiseScroll;
            float _Intensity;

            [shader("closesthit")]
            void MyHitShader(inout Payload payload : SV_RayPayload,
                AttributeData attributes : SV_IntersectionAttributes)
            {
                float2 uvs = GetUVs(attributes);
                float3 normals = GetNormals(attributes);
                float3 worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz);
                payload.normal = float4(worldNormal + 0.0 * WorldRayDirection(), 1);


                //Calculate the Animation:
                float3 flowDirection = _SurfaceNoiseScroll.xyz * _SurfaceNoiseScroll.w;
                float3 noiseUV = float3(uvs.x + _Time.y * flowDirection.x, uvs.y + _Time.y * flowDirection.y, uvs.y + _Time.y * flowDirection.z);
                float2 screenPosUV = uvs;//i.screenPosition.xy / i.screenPosition.w;

                //Wavy water distortion.
                float XUV = screenPosUV.x * _RefracStr.x + _Time.y * _RefracStr.y;
                float YUV = screenPosUV.y * _RefracStr.x + _Time.y * _RefracStr.y;
                screenPosUV.y += cos(XUV + YUV) * _RefracStr.z * cos(YUV);
                screenPosUV.x += sin(XUV - YUV) * _RefracStr.z * sin(YUV);

                //Create paintery effect for that under the water.
                //float2 uv = i.screenPosition.xy / i.screenPosition.w;
                //_NormalMap.SampleLevel(sampler_NormalMap, uvs, 0).xyz;
                float3 diplacementNormals = UnpackNormal(_DisplacementTex.SampleLevel(sampler_DisplacementTex, screenPosUV, 0));
                //float2 distortion = uv + ((_Intensity * 0.01) * diplacementNormals.rg);
                
                payload.normal.xyz += ((_Intensity * 0.1) * diplacementNormals);
                float3 position = WorldRayOrigin() + WorldRayDirection() * (RayTCurrent() - 0.00001);
                float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);

                payload.distance = RayTCurrent();
                payload.color = 1;//float4(1, 1, _Smoothness, InstanceID());

                float4 finalColor = _WaterColor;
                float distSquared = min(1, 1 / (RayTCurrent() * RayTCurrent()));
                payload.direction = finalColor;//_Midtone* distSquared;//float4(normals, 1);
                /*
                if(InstanceID() == payload.color.w)
                    //Incode self-shadows as y

                else
                    //Encode cast shadows as x.
                    payload.color = float4(1,0,0, InstanceID());
                    */
                    //payload.color = 1;
                }

                ENDHLSL
            }
    }
}
