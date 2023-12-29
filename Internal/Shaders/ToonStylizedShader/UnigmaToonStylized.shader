Shader "Unigma/UnigmaToonStylized"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
	    _Midtone("Midtone", Color) = (1,1,1,1)
		_Shadow("Shadow", Color) = (1,1,1,1)
		_Highlight("Highlight", Color) = (1,1,1,1)
		_Thresholds("Light thresholds", Vector) = (0.2, 0.4, 0.6, 0.8)
        
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent"
        "LightMode" = "ForwardBase" }
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
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _Midtone;
			float4 _Shadow;
			float4 _Highlight;
			float4 _Thresholds;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //Three colors shadow, midtone, highlight.
                //Each of this colors are on different normals of a percieved box ...
                //The normals are NOT interpolated and it is a flat shading.
                float4 normals = float4(i.normal, 1);
                float3 lightDirAbsolute = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightDir = normalize(lightDirAbsolute);
                
				float NdotL = dot(i.normal, lightDir);
                
				float4 midTones = _Midtone * step(_Thresholds.x, NdotL);
				float4 shadows = _Shadow * step(NdotL, _Thresholds.y);
				float4 highlights = _Highlight * step(_Thresholds.z, NdotL);
                
				float4 finalColor = max(midTones, shadows);
				finalColor = max(finalColor, highlights);

                return finalColor;

                float4 xzCol = _Shadow*step(_Thresholds.x, abs(normals).r);
                float4 zxCol = _Midtone*step(_Thresholds.z, abs(normals).b);
                float4 zyCol = _Highlight* step(_Thresholds.z, abs(normals).g);

                return zyCol+ xzCol + zxCol;
                
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
                //float3 worldNormal = mul((float4x4)unity_ObjectToWorld, float4(normals, 0)).xyz;


                float3 position = WorldRayOrigin() + WorldRayDirection() * (RayTCurrent() - 0.00001);
                float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);

                payload.distance = RayTCurrent();
                if(InstanceID() == payload.color.w)
                    //Incode self-shadows as y
                    payload.color = float4(0,1,0, InstanceID());
                else
                    //Encode cast shadows as x.
                    payload.color = float4(1,0,0, InstanceID());
                //payload.color = 1;
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

            Texture2D<float4> _MainTex;
            SamplerState sampler_MainTex;
            float4 _Midtone;
            float4 _Shadow;
            float4 _Highlight;
            float4 _Thresholds;

            [shader("closesthit")]
            void MyHitShader(inout Payload payload : SV_RayPayload,
                AttributeData attributes : SV_IntersectionAttributes)
            {
                float2 uvs = GetUVs(attributes);
                float3 normals = GetNormals(attributes);
                float3 tangent = GetTangent(attributes);
                float3 bitangent = normalize(cross(normals, tangent));

                float3x3 tangentMatrix = transpose(float3x3(tangent, bitangent, normals));

                
                float3 worldNormal = mul(ObjectToWorld3x4(), float4(normals, 0)).xyz;


                float3 position = WorldRayOrigin() + WorldRayDirection() * (RayTCurrent() - 0.00001);
                float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);

                //Project position into tangent, basically uv space.

                float3 localPosition = mul(WorldToObject3x4(), float4(position, 1.0)).xyz;
                float3 tangentSpace = mul(localPosition, tangentMatrix);

                float2 tangentUVs = float2(tangentSpace.x + 0.5, -tangentSpace.y + 0.5);

                float distSquared = min(1, 1 / (RayTCurrent() * RayTCurrent()) ) ;
                payload.distance = RayTCurrent();
                payload.direction = reflect(payload.direction, normals);

                //Calculate object.

                float3 lightDirAbsolute = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightDir = normalize(lightDirAbsolute);

                float NdotL = dot(normals, lightDir);

                float4 midTones = _Midtone * step(_Thresholds.x, NdotL);
                float4 shadows = _Shadow * step(NdotL, _Thresholds.y);
                float4 highlights = _Highlight * step(_Thresholds.z, NdotL);

                float4 finalColor = max(midTones, shadows);
                finalColor = max(finalColor, highlights);


                float4 xzCol = _Shadow * step(_Thresholds.x, abs(normals).r);
                float4 zxCol = _Midtone * step(_Thresholds.z, abs(normals).b);
                float4 zyCol = _Highlight * step(_Thresholds.z, abs(normals).g);

                float4 objectColor = zyCol + xzCol + zxCol;

                payload.direction = worldNormal;
                payload.color = finalColor;
                //payload.color = objectColor* distSquared;//_Midtone* distSquared;//float4(normals, 1);
                //payload.color = float4(float3(uvs.x, uvs.y, 1) *0.5 + 0.5, 1);
                //payload.color = float4(uvs.x, uvs.y, 1, 1);
                //payload.color = float4(tangentUVs.x, tangentUVs.y, 1, 1);
            }

            ENDHLSL
        }
    }
}
