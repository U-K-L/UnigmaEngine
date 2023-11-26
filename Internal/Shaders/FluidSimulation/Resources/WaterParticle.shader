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
            #include "UnityShaderVariables.cginc"

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
				float3 worldPos : TEXCOORD0;
                uint instanceID : SV_InstanceID;
				float3 rayDir : TEXCOORD1;
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



                float3 worldSpacePivot = unity_ObjectToWorld._m03_m13_m23;
                // offset between pivot and camera
                float3 worldSpacePivotToCamera = _WorldSpaceCameraPos.xyz - worldSpacePivot;
                // camera up vector
                // used as a somewhat arbitrary starting up orientation
                float3 up = unity_MatrixInvV._m01_m11_m21;
                // forward vector is the normalized offset
                // this it the direction from the pivot to the camera
                float3 forward = normalize(worldSpacePivotToCamera);
                // cross product gets a vector perpendicular to the input vectors
                float3 right = normalize(cross(forward, up));
                // another cross product ensures the up is perpendicular to both
                up = cross(right, forward);
                // construct the rotation matrix
                float3x3 rotMat = float3x3(right, up, forward);
                // the above rotate matrix is transposed, meaning the components are
                // in the wrong order, but we can work with that by swapping the
                // order of the matrix and vector in the mul()
                float3 worldPos2 = mul(v.vertex.xyz, rotMat) + worldSpacePivot;
                // ray direction
                float3 worldRayDir = worldPos2 - _WorldSpaceCameraPos.xyz;
                o.rayDir = mul(unity_WorldToObject, float4(worldRayDir, 0.0));
                // clip space position output
                o.vertex = UnityWorldToClipPos(worldPos2);


                float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;
                //o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
                //o.vertex = UnityObjectToClipPos(v.vertex);
                //o.vertex = mul(unity_ObjectToWorld, v.vertex);
                o.instanceID = v.instanceID;
                o.worldPos = worldPos;

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
                UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
                float3 positionWS = abs(_Particles[i.instanceID].position);
                float3 cameraPosition = _WorldSpaceCameraPos;           // Unity provided position of the camera/eye.
                float distanceToCamera = length(positionWS - cameraPosition);
                float linearDepth = (distanceToCamera - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y);
                
				float depth = LinearDepthToRawDepth(linearDepth);

                float4 clipPos = UnityWorldToClipPos(float4(i.worldPos, 1));
                float depthN = (clipPos.z * 1.0) / (clipPos.w * 1.0);
                depthN = 1.0 - depthN;
                //
                //return float4(position, 1);//float4(i.instanceID/10, i.instanceID, position.z, 1);
                float velocity = length(_Particles[i.instanceID].velocity) + length(_Particles[i.instanceID].curl) * 0.055;
                float surface = 1.0 - (_Particles[i.instanceID].density / 180.0);
                float density = 0;
                float4 velocitySurfaceDensityDepth = float4(velocity, surface, density, depthN);
                GRT0 = velocitySurfaceDensityDepth;
                //GRT1 = float4(0, 1,0,1);
                GRT2 = float4(i.worldPos, depthN);//float4(_Particles[i.instanceID].velocity, length(_Particles[i.instanceID].velocity) + length(_Particles[i.instanceID].curl) * 0.055);
                GRT3 = float4(_Particles[i.instanceID].normal, 1);
                GRTDepth = depth;
            }
            ENDCG
        }

        //Pass 2.

        Pass
        {
            Blend One One
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

            float LinearDepthToRawDepth(float linearDepth)
            {
                return (1.0f - (linearDepth * _ZBufferParams.y)) / (linearDepth * _ZBufferParams.x);
            }

            //MRT output
            void frag(v2f i,
                out half4 GRT0:SV_Target0,
                out half4 GRT1 : SV_Target1,
                out half4 GRT2 : SV_Target2,
                out float GRTDepth : SV_Depth)
            {
                UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
                float3 positionWS = abs(_Particles[i.instanceID].position);
                float3 cameraPosition = _WorldSpaceCameraPos;           // Unity provided position of the camera/eye.
                float distanceToCamera = length(positionWS - cameraPosition);
                float linearDepth = (distanceToCamera - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y);

                float depth = LinearDepthToRawDepth(linearDepth);
                //
                //return float4(position, 1);//float4(i.instanceID/10, i.instanceID, position.z, 1);
                float velocity = length(_Particles[i.instanceID].velocity) + length(_Particles[i.instanceID].curl) * 0.055;
                float surface = _Particles[i.instanceID].density / 28.0;
                float density = 0.025;
                float4 velocitySurfaceDensityDepth = float4(GRT0.x, GRT0.y, density, GRT0.w);
                GRT0 = velocitySurfaceDensityDepth;
				GRT1 = density*4;
            }
            ENDCG
        }
        
    }
}
