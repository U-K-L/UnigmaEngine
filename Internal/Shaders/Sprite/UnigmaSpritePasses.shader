Shader "Unigma/UnigmaSpritePasses"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [IntRange] _StencilRef("Stencil Ref Value", Range(0,255)) = 0
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
                return col;
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
            #include "../RayTraceHelpersUnigma.hlsl"

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
