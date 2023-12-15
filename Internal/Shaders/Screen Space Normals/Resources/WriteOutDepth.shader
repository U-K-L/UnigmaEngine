// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/WriteOutDepth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            };

            struct v2f
            {

                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float4 depth : TEXCOORD5;
                float4 screen: TEXCOORD3;
                float4 rawVert : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4x4 _Perspective_Matrix_VP;

            v2f vert (appdata v)
            {
                v2f o;
                // calculate world space view ray direction and origin for perspective or orthographic
                float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;
                o.rawVert.xyz = UnityObjectToViewPos(v.vertex.xyz);
                o.rawVert.z *= -1;
                o.worldPos = worldPos;
                //o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z * _ProjectionParams.w;
                //o.depth = UnityObjectToClipPos(v.vertex);
                //o.depth.z /= o.depth.w;

                if (unity_OrthoParams.w > 0)
                {
                    //isOrthographic.
                    //But....actually we do something neat here. We use a perspective camera and take its viewing projection into the orthographic Camera.
                    float4 clipPos = mul(_Perspective_Matrix_VP, float4(worldPos, 1));//UnityWorldToClipPos(position);
                    o.depth = clipPos; //clipPos.w is always 1 in this case. Ignore.
                    o.depth.z /= o.depth.w;
                    //We get back raw depth, so now interpolate with the near and far plane.
                    //depth = lerp(_ProjectionParams.y, _ProjectionParams.z*0.01, clipPos.z);
                }
                else
                {
                    //Perspective.
                    float4 clipPos = UnityWorldToClipPos(worldPos);// mul(_Perspective_Matrix_VP, float4(position,1));
                    o.depth = clipPos;
                    o.depth.z /= o.depth.w;
                }

                float4 cclipos = UnityWorldToClipPos(worldPos); //depth to use as a comparison alway same projection.
                o.depth.x = cclipos.z / cclipos.w;
                o.vertex = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)));//UnityObjectToClipPos(v.vertex);
                //o.vertex = UnityApplyLinearShadowBias(o.vertex);
                //o.vertex.z /= o.vertex.w;
                o.screen = ComputeScreenPos(o.vertex);
                o.screen.z /= o.screen.w;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float LinearDepthToRawDepth(float linearDepth)
            {
                return (1.0f - (linearDepth * _ZBufferParams.y)) / (linearDepth * _ZBufferParams.x);
            }


            fixed4 frag(v2f i) : SV_Target
            {
                float3 positionWS = i.worldPos;
                float3 cameraPosition = _WorldSpaceCameraPos;           // Unity provided position of the camera/eye.
                float distanceToCamera = length(positionWS - cameraPosition);
                float linearDepth = (distanceToCamera - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y);



                //float t1 = sphIntersect(i.rayOrigin, normalize(i.rayDir), float4(positionWS, 1));
                //float3 worldRayPos = i.rayOrigin + i.rayDir * t1;
                float depth = LinearDepthToRawDepth(linearDepth);

                float4 clipPos = UnityWorldToClipPos(float4(positionWS, 1));
                float wp = mul(UNITY_MATRIX_V, float4(positionWS, 1.0)).w;
                float dp = -mul(UNITY_MATRIX_MV, i.vertex).z * _ProjectionParams.w;
                //float zp = -TransformWorldToView(positionWS).z;
                //float depthN = (clipPos.z * 1.0) / (clipPos.w * 1.0);
                //float4 clipPos = mul(UNITY_MATRIX_VP, float4(i.worldPos, 1.0));
                fixed4 clipPosV = UnityWorldToClipPos(i.rawVert);
                fixed4 sccreen = ComputeScreenPos(i.rawVert);


                return i.depth;//fixed4(depthnn.z, depthnn.z, depthnn.w/10, 0);// -0.07059;//-0.04;//depthnn.z;
            }
            ENDCG
        }

                Pass
                {
                    Tags { "LightMode" = "ShadowCaster" "RenderType" = "Opaque" }
                    ColorMask 0
                    AlphaToMask On

                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment frag

                    #include "UnityCG.cginc"

                    sampler2D _MainTex;
                    fixed4 _TintColor;
                    float4 _MainTex_ST;

                    struct appdata_t {
                        float4 vertex : POSITION;
                        fixed4 color : COLOR;
                        float2 texcoord : TEXCOORD0;
                    };

                    struct v2f {
                        float4 vertex : SV_POSITION;
                        fixed4 color : COLOR;
                        float2 texcoord : TEXCOORD0;
                    };

                    v2f vert(appdata_t v)
                    {
                        v2f o;
                        o.vertex = UnityObjectToClipPos(v.vertex);
                        o.vertex.z /= o.vertex.w;
                        o.color = v.color;
                        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                        return o;
                    }

                    fixed4 frag(v2f i) : SV_Target
                    {
                        return 1;
                    }
                    ENDCG
                }

    }
}
