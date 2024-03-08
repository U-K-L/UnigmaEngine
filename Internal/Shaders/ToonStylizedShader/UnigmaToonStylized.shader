Shader "Unigma/UnigmaToonStylized"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NormalMap("Texture", 2D) = "black" {}
	    _Midtone("Midtone", Color) = (1,1,1,1)
		_Shadow("Shadow", Color) = (1,1,1,1)
		_Highlight("Highlight", Color) = (1,1,1,1)
		_Thresholds("Light thresholds", Vector) = (0.2, 0.4, 0.6, 0.8)
        _Smoothness("Smoothness", Range(0,1)) = 0
        _LightAbsorbtion("Light Absorbtion", Range(0,1)) = 0
        _Emmittance("Light Emittance", Range(0,100)) = 0
        
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
                float4 screenSpace : TEXCOORD3;
            };

            sampler2D _UnigmaGlobalIllumination;
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

                o.screenSpace = ComputeScreenPos(o.vertex);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //Get main texture
				float4 col = tex2D(_MainTex, i.uv);
                //Three colors shadow, midtone, highlight.
                //Each of this colors are on different normals of a percieved box ...
                //The normals are NOT interpolated and it is a flat shading.
                float2 screenPos = i.screenSpace.xy / i.screenSpace.w;
                //screenPos = screenPos * 0.5 + 0.5;
                screenPos *= _ScreenParams.y / _ScreenParams.x;
                float4 globalIllum = tex2D(_UnigmaGlobalIllumination, screenPos);
                float4 normals = normalize(float4(i.normal, 1));
                float3 lightDirAbsolute = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightDir = normalize(lightDirAbsolute);

                //float3 normals = UnityObjectToWorldNormal(i.normal);
                
				float NdotL = dot(normals, lightDir);
                
				float4 midTones = _Midtone * step(_Thresholds.x, NdotL);
				float4 shadows = _Shadow * step(NdotL, _Thresholds.y);
				float4 highlights = _Highlight * step(_Thresholds.z, NdotL);
                
				float4 finalColor = max(midTones, shadows);
				finalColor = max(finalColor, highlights);

                return _Midtone * col;
                return col;
                //return globalIllum;
                return finalColor;

                float4 xzCol = _Shadow*step(_Thresholds.x, abs(normals).r);
                float4 zxCol = _Midtone*step(_Thresholds.z, abs(normals).b);
                float4 zyCol = _Highlight* step(_Thresholds.z, abs(normals).g);

                return col;//zyCol+ xzCol + zxCol;
                
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
                payload.color = float4(1, 1, 0, InstanceID());
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

        Pass
        {
            Name "GlobalIlluminationRaytracingShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../RayTraceHelpersUnigma.hlsl"
            #include "UnityCG.cginc"

            Texture2D<float4> _MainTex, _UnigmaNormal, _NormalMap;
			SamplerState sampler_MainTex, sampler_UnigmaNormal, sampler_NormalMap;
            float4 _Midtone;
            float4 _Shadow;
            float4 _Highlight;
            float4 _Thresholds;
            float _Smoothness, _LightAbsorbtion, _Emmittance;

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
                float3 worldTangent = mul(ObjectToWorld3x4(), tangent);

                float3 binormal = cross(normals, tangent); // *input.tangent.w;
                float3 worldBinormal = mul(ObjectToWorld3x4(), binormal);

                float3 N = normalize(worldNormal);
                float3 T = normalize(worldTangent);
                float3 B = normalize(worldBinormal);

                float3 tangentNormal = _NormalMap.SampleLevel(sampler_NormalMap, uvs, 0).xyz;
                tangentNormal = normalize(tangentNormal * 2 - 1);
                float3x3 TBN = float3x3(normalize(T), normalize(B), normalize(N));
                TBN = transpose(TBN);
                float3 worldNormalTBN = mul(TBN, tangentNormal);

                float3x3 tangentMatrix = transpose(float3x3(tangent, bitangent, normals));
                
                //Get from Texture2D
				//normals = _UnigmaNormal.SampleLevel(sampler_UnigmaNormal, payload.uv, 0).xyz;
                
                worldNormal = worldNormalTBN;
                if (_NormalMap.SampleLevel(sampler_NormalMap, uvs, 0).w <= 0)
                    worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz);


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

                payload.normal = float4(worldNormal, 1);


                float2 rxy = randGaussian(float3(payload.pixel.xyy), rand(payload.pixel.x));
                float2 rxz = randGaussian(float3(payload.pixel.xy + 2452, payload.pixel.x), rand(payload.pixel.y));
                float3 ruv = float3(rxy, rxz.x);
                float3 diffuse = RandomPointOnHemisphere(payload.pixel, worldNormal,payload.pixel);
                float3 specular = reflect(WorldRayDirection(), worldNormal);

                float lightAbsorbed = _LightAbsorbtion < ruv.x;

                payload.direction = lerp(diffuse, specular, _Smoothness* lightAbsorbed);

                //payload.direction = diffuse;

                payload.color.xyz *=  _Midtone;
                payload.color.w += _Emmittance;
                //payload.color = objectColor* distSquared;//_Midtone* distSquared;//float4(normals, 1);
                //payload.color = float4(float3(uvs.x, uvs.y, 1) *0.5 + 0.5, 1);
                //payload.color = float4(uvs.x, uvs.y, 1, 1);
                //payload.color = float4(tangentUVs.x, tangentUVs.y, 1, 1);
            }

            ENDHLSL
        }
    }
}
