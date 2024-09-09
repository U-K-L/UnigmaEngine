// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unigma/UnigmaToonStylizedPasses"
{
    Properties
    {
        _MainTex ("Augmented RGB Normal Map", 2D) = "black" {}
        _NormalMap("Normal Map", 2D) = "black" {}
	    _MainColor("Midtone", Color) = (1,1,1,1)
		_Shadow("Shadow", Color) = (1,1,1,1)
        _ShadowColors("Shadow Casted Colors", Color) = (1,1,1,1)
		_Highlight("Highlight", Color) = (1,1,1,1)
		_Thresholds("Light thresholds", Vector) = (0.2, 0.4, 0.6, 0.8)
        _Smoothness("Smoothness", Range(0,1)) = 0
        _LightAbsorbtion("Light Absorbtion", Range(0,1)) = 0
        _Emmittance("Light Emittance", Range(0,100)) = 0
        [HDR]
        _SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
        _Glossiness("Glossiness", Float) = 32
        [HDR]
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimAmount("Rim Amount", Range(0, 1)) = 0.716
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.1
		_UseRim("Use RIM", Float) = 0
        [KeywordEnum(CelShaded, ToonShaded, DistShaded)] _ColorDistModel("Color BRDF", Float) = 0
		_RimControl("Rim Control", Range(-1,1)) = 0
        [IntRange] _StencilRef("Stencil Ref Value", Range(0,255)) = 0
         _ReceiveShadow("Receive Shadow", Range(0,1)) = 1

        //For components.
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineInnerColor("Inner Outline Color", Color) = (0,0,0,1)
        _ThicknessTexture("Thickness of the line texture", 2D) = "black" {}
        _ThicknessTexture_ST("Thickness Vector", Vector) = (1, 1, 1, 1)

        //GBuffer settings.
        _Fade("Fade camera", Range(0,100000)) = 1
		_NormalAmount("Normal amount", Range(0,50)) = 1
		_DepthAmount("Depth amount", Range(0,50)) = 1
		_ObjectID("Object ID", Int) = 0
        
    }
    SubShader
    {
        Cull Off
        LOD 100

        Stencil
        {
            Ref[_StencilRef]
            Comp Equal
        }
        
        ///----------------------SPECULAR PASS ------------------------------
        //
        //---------
        Pass
        {
            Name "SpecularRoughnessPass"
            CGPROGRAM
            #pragma vertex ToonSpecularVert
            #pragma fragment ToonSpecularFrag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            #include "UnigmaToonSpecular.cginc"
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"



        //PASSES FOR OUTLINES.


        Pass
        {
            Name "ScreenNormalsPass"
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

            sampler2D _MainTex, _UnigmaNormal, _NormalMap;
            float4 _MainTex_ST;
			float _Fade, _NormalAmount, _DepthAmount;
            int _StencilRef;

            v2f vert (appdata v)
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
                float3 tangentNormal = tex2D(_NormalMap, i.uv).xyz;
                float3 rgbNormalMap = tangentNormal.xzy * 2 - 1;
                rgbNormalMap = UnityObjectToWorldNormal(rgbNormalMap);
                return float4(rgbNormalMap, 1);
                return float4(i.worldNormal, 1.0);
                tangentNormal = normalize(tangentNormal * 2 - 1);
                float3x3 TBN = float3x3(normalize(i.T), normalize(i.B), normalize(i.N));
                TBN = transpose(TBN);
                float3 worldNormal = mul(TBN, tangentNormal);
                if (tex2D(_NormalMap, i.uv).w <= 0)
                    return float4(i.worldNormal, 1.0);

				return float4(worldNormal, 1);
            }
            ENDCG
        }



                  
	    //Pass 2.
        Pass
        {
            Name "ScreenWorldPostionsPass"
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD3;
            };


            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
				float4 finalColor = float4(i.worldPos, 1.0);
                return finalColor;
            }
            ENDCG
        }


        //Pass 3. Object IDs
        Pass
        {
            Name "IDsPass"
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
			int _ObjectID;
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
                return float4(rand(_ObjectID), rand(_ObjectID + 1), rand(_ObjectID + 2), 1);
            }
            ENDCG
        }

        Pass
        {
            Name "OutlineThicknessPass"
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

            float4 _OutlineColor, _ThicknessTexture_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _ThicknessTexture);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            sampler2D _ThicknessTexture;

            fixed4 frag(v2f i) : SV_Target
            {
				float4 texcol = tex2D(_ThicknessTexture, i.uv);
                float thickness = 1;//dot(texcol, texcol) / 3.0;
                float4 finalOutput = float4(_OutlineColor.xyz, thickness);
				return finalOutput* thickness;
            }
            ENDCG
        }

        Pass
        {
            Name "OutlineColorsPass"
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

            float4 _OutlineInnerColor, _OutlineColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineInnerColor;
            }
            ENDCG
        }


        //For editor visualization place at bottom because Unity too dumb to specify which passes to run -_-
        ///----------------------ALBEDO PASS ------------------------------
        //
        //---------

        Pass
        {
            Name "AlbedoPass"
            CGPROGRAM
            #pragma vertex ToonAlbedoVert
            #pragma fragment ToonAlbedoFrag
            // make fog work
            #pragma multi_compile_fog 
            #pragma multi_compile _COLORDISTMODEL_CELSHADED _COLORDISTMODEL_TOONSHADED _COLORDISTMODEL_DISTSHADED

            #include "UnityCG.cginc"
            #include "../ShaderHelpers.hlsl"
            #include "UnigmaToonAlbedo.cginc"

            ENDCG
        }
    }
}
