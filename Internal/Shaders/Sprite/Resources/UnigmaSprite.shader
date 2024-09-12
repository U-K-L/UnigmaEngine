Shader "Unigma/UnigmaSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NormalMap("Texture", 2D) = "black" {}
        [IntRange] _StencilRef("Stencil Ref Value", Range(0,255)) = 0
        _PostProcessStrength ("Post Process Strength", Range(0,1)) = 0
        _ObjectID("Object ID", Int) = 0
    }
    SubShader
    {
        Cull Off
        Tags { "Queue" = "Transparent+500" "LightMode" = "ForwardBase" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha


         ///----------------------SPECULAR PASS ------------------------------
        //
        //---------

        Pass
        {
            Name "SpecularRoughnessPass"
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                float4 pos = UnityObjectToClipPos(v.vertex);
                o.vertex = pos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return 0;
            }

            ENDCG
        }


        ///----------------------ALBEDO PASS ------------------------------
        //
        //---------

        Pass
        {
            Name "AlbedoPass"
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                float4 pos = UnityObjectToClipPos(v.vertex);
                o.vertex = pos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                if(col.a < 0.01)
                    discard;
                return float4(col.xyz, 1);
            }
            ENDCG
        }


        ///----------------------DEPTH SHADOW PASS ------------------------------
        //
        //---------

        Pass
        {
            Name "DepthShadowsRaytracingShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../../RayTraceHelpersUnigma.hlsl"
            #include "UnityCG.cginc"

            Texture2D<float4> _MainTex, _UnigmaNormal, _NormalMap;
            SamplerState sampler_MainTex, sampler_UnigmaNormal, sampler_NormalMap;
            float4 _NormalMap_ST;
            [shader("closesthit")]
            void MyHitShader(inout Payload payload : SV_RayPayload, AttributeData attributes : SV_IntersectionAttributes)
            {
                float2 uvs = GetUVs(attributes);
                float3 normals = GetNormals(attributes);
                //float3 worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz);



                //Get Texture.
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

                float3 tangentNormal = _NormalMap.SampleLevel(sampler_NormalMap, uvs * _NormalMap_ST.xy, 0).xyz;
                tangentNormal = normalize(tangentNormal * 2 - 1);
                float3x3 TBN = float3x3(normalize(T), normalize(B), normalize(N));
                TBN = transpose(TBN);
                float3 worldNormalTBN = mul(TBN, tangentNormal);

                float3x3 tangentMatrix = transpose(float3x3(tangent, bitangent, normals));

                //Get from Texture2D
                //normals = _UnigmaNormal.SampleLevel(sampler_UnigmaNormal, payload.uv, 0).xyz;
                float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);

                //Combine tangent normals with vertex normals
                worldNormal = worldNormalTBN * _NormalMap_ST.z + normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz) * _NormalMap_ST.w;

                float3 rgbNormalMap = tex.xzy * 2 - 1;

                //Wacky tests to set it to certain types of normal maps.
                if (_NormalMap.SampleLevel(sampler_NormalMap, uvs, 0).w <= 0 || tex.w <= 0.01) //If there isn't a normal map texture.
                    worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz); //Set to regular world normal based on vertices.
                
                //worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz);
                float3 specular = reflect(WorldRayDirection().xyz, worldNormal.xyz);




                payload.distance = RayTCurrent();


                if(tex.w <= 0.01)
					payload.color = float4(0, 0, 0, -2);
                else
                    payload.color = float4(1, 1, 0, InstanceID());
                    payload.direction = tex.xyz;
                    payload.uv = 0;
                    payload.pixel = 0;
                    payload.normal = float4(specular, 0);

            }


            ENDHLSL
        }

        
        ///----------------------SPRITE AFTER POST PROCESSING PASS------------------------------
        //
        //---------

        Pass
        {
            Name "PostProcessedAlbedo"

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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                float4 pos = UnityObjectToClipPos(v.vertex);
                o.vertex = pos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                if(col.a < 0.01)
                    discard;
                return col;
            }
            ENDCG
        }
        ///----------------------GLOBAL ILLUMINATION PASS ------------------------------
        //
        //---------
        Pass
        {
            Name "GlobalIlluminationRaytracingShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../../RayTraceHelpersUnigma.hlsl"
            #include "UnityCG.cginc"


            Texture2D<float4> _MainTex, _UnigmaNormal, _NormalMap;
            float4 _NormalMap_ST;
            SamplerState sampler_MainTex, sampler_UnigmaNormal, sampler_NormalMap;
            float4 _MainColor;
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

                float4 tangentNormal = _NormalMap.SampleLevel(sampler_NormalMap, uvs * _NormalMap_ST.xy, 0).xyzw;
                tangentNormal.xyz = normalize(tangentNormal * 2 - 1);
                float3x3 TBN = float3x3(normalize(T), normalize(B), normalize(N));
                TBN = transpose(TBN);
                float3 worldNormalTBN = mul(TBN, tangentNormal.xyz);

                float3x3 tangentMatrix = transpose(float3x3(tangent, bitangent, normals));
                float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);
                //Combine tangent normals with vertex normals
                worldNormal = worldNormalTBN * _NormalMap_ST.z + normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz) * _NormalMap_ST.w;

                //Wacky tests to set it to certain types of normal maps.
                if (tangentNormal.w <= 0 || tex.w <= 0.01) //If there isn't a normal map texture.
                    worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz); //Set to regular world normal based on vertices.

                //worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz);
                float3 specular = reflect(WorldRayDirection().xyz, worldNormal.xyz);
                
                payload.distance = RayTCurrent();
                

                
                if (tex.w <= 0.01)
                {
                    payload.uv = float2(0, -9999);
                }
                else
                {
                    payload.normal = float4(worldNormal, 1);
                    payload.direction = specular;
                    payload.color = tex;
                    payload.color.w = _Emmittance;
                }

            }


            ENDHLSL
        }
    }
}
