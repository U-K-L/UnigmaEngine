// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/IsometricDepthNormals"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
		_Fade("Fade camera", Range(0,100000)) = 1
		_NormalAmount("Normal amount", Range(0,50)) = 1
		_DepthAmount("Depth amount", Range(0,50)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
            
	    //Pass 2.
        Pass
        {
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
    }
}
