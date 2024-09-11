Shader "Unigma/UnigmaSpritePasses"
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

                //PASSES FOR OUTLINES.


        Pass
        {
            Name "ScreenNormalsPass"
            CGPROGRAM
            #pragma vertex ScreenNormalsVert
            #pragma fragment ScreenNormalsFrag
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
                float4 vertex : SV_POSITION;
                float depthGen : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float3 T : TEXCOORD3;
                float3 B : TEXCOORD4;
                float3 N : TEXCOORD5;
                float3 worldNormal : TEXCOORD6;
            };

            sampler2D _MainTex, _UnigmaNormal, _NormalMap;
            float4 _MainTex_ST;
            float _Fade, _NormalAmount, _DepthAmount;
            int _StencilRef;

            v2f ScreenNormalsVert(appdata v)
            {
                v2f o;
                float4 vertexProgjPos = mul(UNITY_MATRIX_MV, v.vertex);
                o.depthGen = saturate((-vertexProgjPos.z - _ProjectionParams.y) / (_Fade + 0.001));
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal; //UnityObjectToWorldNormal(v.normal);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);

                float3 worldNormal = mul((float3x3) unity_ObjectToWorld, v.normal);
                float3 worldTangent = mul((float3x3) unity_ObjectToWorld, v.tangent);

                float3 binormal = cross(v.normal, v.tangent.xyz); // *input.tangent.w;
                float3 worldBinormal = mul((float3x3) unity_ObjectToWorld, binormal);

                            // and, set them
                o.N = normalize(worldNormal);
                o.T = normalize(worldTangent);
                o.B = normalize(worldBinormal);
                return o;
            }

                        //Make entire shader a shader pass in the future.
            fixed4 ScreenNormalsFrag(v2f i) : SV_Target
            {
                //Get texture data
                float3 tangentNormal = tex2D(_NormalMap, i.uv).xyz;
                float3 rgbNormalMap = tangentNormal.xzy * 2 - 1;
                rgbNormalMap = UnityObjectToWorldNormal(rgbNormalMap);
                tangentNormal = normalize(tangentNormal * 2 - 1);
                float3x3 TBN = float3x3(normalize(i.T), normalize(i.B), normalize(i.N));
                TBN = transpose(TBN);
                float3 worldNormal = mul(TBN, tangentNormal);

                float4 finalNormal = float4(worldNormal, 1.0f);

                if (tex2D(_NormalMap, i.uv).w <= 0)
                    finalNormal = float4(i.worldNormal, 1.0);

                //Cut out using alpha value.
                float4 col = tex2D(_MainTex, i.uv);
                if(col.a < 0.01)
                    discard;

                return finalNormal;
            }


            ENDCG
        }

                  
	    //Pass 2.
        Pass
        {
            Name "ScreenWorldPostionsPass"
            CGPROGRAM

            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                discard;
                return 0;
            }

            ENDCG
        }


        //Pass 3. Object IDs
        Pass
        {
            Name "IDsPass"
            CGPROGRAM
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "../../ShaderHelpers.hlsl"

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            
            sampler2D _MainTex;
            float4 _MainTex_ST;
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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            int _ObjectID;
            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                if(col.a < 0.01)
                    discard;
                return float4(rand(_ObjectID), rand(_ObjectID + 1), rand(_ObjectID + 2), 1);

            }

            ENDCG
        }

        Pass
        {
            Name "OutlineThicknessPass"
            CGPROGRAM
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

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
                fixed4 col = tex2D(_MainTex, i.uv);
                if(col.a < 0.01)
                    discard;
                return 0;

            }

            ENDCG
        }

        Pass
        {
            Name "OutlineColorsPass"
            CGPROGRAM
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

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
                fixed4 col = tex2D(_MainTex, i.uv);
                if(col.a < 0.01)
                    discard;
                return 0;

            }

            ENDCG
        }


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

            Texture2D<float4> _MainTex;
            SamplerState sampler_MainTex;
            [shader("closesthit")]
            void MyHitShader(inout Payload payload : SV_RayPayload, AttributeData attributes : SV_IntersectionAttributes)
            {
                float2 uvs = GetUVs(attributes);
                float3 normals = GetNormals(attributes);
                float3 worldNormal = normalize(mul(ObjectToWorld3x4(), float4(normals, 0)).xyz);


                float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);
                
                float3 specular = reflect(WorldRayDirection().xyz, worldNormal.xyz);

                payload.normal = float4(specular, 0);


                payload.distance = RayTCurrent();
                payload.color = float4(1, 1, 0, InstanceID());
                payload.direction = tex.xyz;
                payload.uv = 0;
                payload.pixel = 0;

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
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex, _UnigmaComposite;
            float4 _MainTex_ST;
            float _PostProcessStrength;

            v2f vert (appdata v)
            {
                v2f o;
                float4 pos = UnityObjectToClipPos(v.vertex);
                o.vertex = pos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.screenPos = ComputeScreenPos(pos);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 screenUV = (i.screenPos.xy / i.screenPos.w);
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 posProcPixel = tex2D(_UnigmaComposite, screenUV);
                if(col.a < 0.01)
                    discard;

                return float4(lerp(col.xyz, col.xyz*0.5+posProcPixel.xyz*0.5, _PostProcessStrength), 1);
            }
            ENDCG
        }
        ///----------------------GLOBAL ILLUMINATION PASS ------------------------------
        //
        //---------
        /*
        Pass
        {
            Name "GlobalIlluminationRaytracingShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../RayTraceHelpersUnigma.hlsl"
            #include "UnityCG.cginc"
            //#include "../ToonStylizedShader/UnigmaToonGlobalIllum.cginc"

            ENDHLSL
        }
        */

    }
}
