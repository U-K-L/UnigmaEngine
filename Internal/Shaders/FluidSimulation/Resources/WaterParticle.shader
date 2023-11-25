Shader "Unlit/WaterParticle"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma editor_sync_compilation
#pragma target 4.5

            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "UnityCG.cginc"

            struct Particle
            {
                float4 force;
                float3 position;
                float3 lastPosition;
                float3 predictedPosition;
                float3 positionDelta;
                float3 debugVector;
                float3 velocity;
                float3 normal;
                float3 curl;
                float density;
                float lambda;
                float mass;
                int parent;
            };
    
            StructuredBuffer<Particle> _Particles;
                
            struct appdata
            {
                float4 vertex : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                uint instanceID : SV_InstanceID;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;

                UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.
                
                float3 position = _Particles[v.instanceID].position;
                
                unity_ObjectToWorld = 0.0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
                unity_ObjectToWorld._m00_m11_m22 = 0.220875;

                float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
                //o.vertex = UnityObjectToClipPos(v.vertex);
                //o.vertex = mul(unity_ObjectToWorld, v.vertex);
                o.instanceID = v.instanceID;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.

                //float3 position = abs(_Particles[i.instanceID].position);
                //return float4(position, 1);//float4(i.instanceID/10, i.instanceID, position.z, 1);
                float velocity = length(_Particles[i.instanceID].velocity) + length(_Particles[i.instanceID].curl) * 0.055;
                return float4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}
