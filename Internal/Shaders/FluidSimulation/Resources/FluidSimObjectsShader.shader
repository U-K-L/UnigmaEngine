Shader "Unlit/FluidSimObjectsShader"
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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float LinearDepthToRawDepth(float linearDepth)
            {
                return (1.0f - (linearDepth * _ZBufferParams.y)) / (linearDepth * _ZBufferParams.x);
            }
            
            //MRT output
            void frag(v2f i,
                out half4 GRT0:SV_Target0,
                out half4 GRT1 : SV_Target1,
                out half4 GRT2 : SV_Target2,
                out half4 GRT3 : SV_Target3,
                out float GRTDepth : SV_Depth)
            {

                float3 positionWS = i.worldPos;
                float3 cameraPosition = _WorldSpaceCameraPos;           // Unity provided position of the camera/eye.
                float distanceToCamera = length(positionWS - cameraPosition);
                float linearDepth = (distanceToCamera - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y);

                //float t1 = sphIntersect(i.rayOrigin, normalize(i.rayDir), float4(positionWS, 0.5));
                //float3 worldRayPos = i.rayOrigin + i.rayDir * t1;
                float depth = LinearDepthToRawDepth(linearDepth);
                //GRTDepth = 1;
                GRT0 = float4(i.worldPos, 0);
                GRT2 = float4(i.worldPos, 0);

            }
            ENDCG
        }
    }
}
