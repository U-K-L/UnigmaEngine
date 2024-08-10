Shader "Unigma/FresnelSurf"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_Emission("Emission", Range(0,100)) = 1
		_InnerStr("Inner Radius", Range(-1,1)) = 0.5
		_OutterStr("Outter Radius", Range(0,2)) = 1
        [IntRange] _StencilRef("Stencil Ref Value", Range(0,255)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Stencil
        {
            Ref[_StencilRef]
            Comp Equal
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
                float3 normal : NORMAL;
				float3 tangent : TANGENT;

            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 screenSpace : TEXCOORD3;
                float3 viewDir : TEXCOORD4;
                float3 T : TEXCOORD5;
                float3 B : TEXCOORD6;
                float3 N : TEXCOORD7;
            };

            half _Glossiness;
            half _Metallic;
            fixed4 _Color;
		    float _Emission;
		    float _InnerStr;
		    float _OutterStr;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;

                o.screenSpace = ComputeScreenPos(o.vertex);
                o.viewDir = WorldSpaceViewDir(v.vertex);

                o.normal = UnityObjectToWorldNormal(v.normal);


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

            fixed4 frag(v2f i) : SV_Target
            {

			    //Apply effect of fresnal.
			    _Emission *= _Emission * _Emission;
			    float fresnel = ((-_Emission+ (_Emission*_InnerStr)) - (dot(i.N, i.viewDir) *(_Emission +  (_Emission* _OutterStr) )));
			    //fresnel = saturate(1-fresnel); //Clamps value between 0 and 1.
			    return (_Emission + fresnel)*(_Color*3) + 0.001;
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
                float3 normal : NORMAL;
				float3 tangent : TANGENT;

            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 screenSpace : TEXCOORD3;
                float3 viewDir : TEXCOORD4;
                float3 T : TEXCOORD5;
                float3 B : TEXCOORD6;
                float3 N : TEXCOORD7;
            };

            half _Glossiness;
            half _Metallic;
            fixed4 _Color;
		    float _Emission;
		    float _InnerStr;
		    float _OutterStr;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;

                o.screenSpace = ComputeScreenPos(o.vertex);
                o.viewDir = WorldSpaceViewDir(v.vertex);

                o.normal = UnityObjectToWorldNormal(v.normal);


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

            fixed4 frag(v2f i) : SV_Target
            {

			    //Apply effect of fresnal.
			    _Emission *= _Emission * _Emission;
			    float fresnel = ((-_Emission+ (_Emission*_InnerStr)) - (dot(i.N, i.viewDir) *(_Emission +  (_Emission* _OutterStr) )));
			    //fresnel = saturate(1-fresnel); //Clamps value between 0 and 1.
			    return (_Emission + fresnel)*(_Color*3) + 0.001;
            }

            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

        Pass
        {
            Name "DepthShadowsRaytracingShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../RayTraceHelpersUnigma.hlsl"

            Texture2D<float4> _MainTex;
            SamplerState sampler_MainTex;
            
            [shader("closesthit")]
            void MyHitShader(inout Payload payload : SV_RayPayload,
                AttributeData attributes : SV_IntersectionAttributes)
            {
                float2 uvs = GetUVs(attributes);
                float3 normals = GetNormals(attributes);
                float3 worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz);
                payload.normal = float4(worldNormal, 1);

                float3 position = WorldRayOrigin() + WorldRayDirection() * (RayTCurrent() - 0.00001);
                float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);

                payload.distance = RayTCurrent();
                payload.color = float4(1, 1, 0.0f, InstanceID());

                payload.uv = 0;
                payload.pixel = 0;
            }

            ENDHLSL
        }

        Pass
        {
            Name "GlobalIlluminationRaytracingShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../RayTraceHelpersUnigma.hlsl"
            #include "UnityCG.cginc"
            
            [shader("closesthit")]
            void MyHitShader(inout Payload payload : SV_RayPayload,
                AttributeData attributes : SV_IntersectionAttributes)
            {
                float2 uvs = GetUVs(attributes);
                float3 normals = GetNormals(attributes);
                float3 tangent = GetTangent(attributes);
                float3 bitangent = normalize(cross(normals, tangent));

                //Create tagents from normal maps...
                float3 worldNormal = mul(ObjectToWorld3x4(), normals);



                float3 position = WorldRayOrigin() + WorldRayDirection() * (RayTCurrent() - 0.00001);
                
                payload.distance = RayTCurrent();
                payload.direction = reflect(payload.direction, normals);

                payload.normal = float4(worldNormal, 1);

                payload.direction = 0;

                //payload.direction = diffuse;

                payload.color.xyz *= 0;
                payload.color.w += 0;
            }

            ENDHLSL
        }

        
        Pass
        {
            Tags{ "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex VSMain
            #pragma fragment PSMain

            float4 VSMain(float4 vertex:POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            float4 PSMain(float4 vertex:SV_POSITION) : SV_TARGET
            {
                return 0;
            }

            ENDCG
        }
    }
}
