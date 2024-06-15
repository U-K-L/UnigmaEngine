// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

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
            #include "../../ShaderHelpers.hlsl"

            struct Particle
            {
                float4 force;
                float3 position;
                float3 lastPosition;
                float3 predictedPosition;
                float3 positionDelta;
                float3 velocity;
                float3 normal;
                float3 curl;
                float density;
                float lambda;
                float spring;
            };
    
            StructuredBuffer<Particle> _Particles;
            sampler2D _UnigmaDepthMap;
                
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
				float3 rayOrigin : TEXCOORD2;
				float2 uv : TEXCOORD3;
                float4 depth : TEXCOORD4;
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
                unity_ObjectToWorld._m00_m11_m22 = 1.220875;

                /*
                // check if the current projection is orthographic or not from the current projection matrix
                bool isOrtho = UNITY_MATRIX_P._m33 == 1.0;

                // viewer position, equivalent to _WorldSpaceCAmeraPos.xyz, but for the current view
                float3 worldSpaceViewerPos = UNITY_MATRIX_I_V._m03_m13_m23;

                // view forward
                float3 worldSpaceViewForward = -UNITY_MATRIX_I_V._m02_m12_m22;

                // pivot position
                float3 worldSpacePivotPos = unity_ObjectToWorld._m03_m13_m23;

                // offset between pivot and camera
                float3 worldSpacePivotToView = worldSpaceViewerPos - worldSpacePivotPos;

                // get the max object scale
                float3 scale = float3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22)
                    );
                float maxScale = max(abs(scale.x), max(abs(scale.y), abs(scale.z)));

                // calculate a camera facing rotation matrix
                float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
                float3 forward = isOrtho ? -worldSpaceViewForward : normalize(worldSpacePivotToView);
                float3 right = normalize(cross(forward, up));
                up = cross(right, forward);
                float3x3 quadOrientationMatrix = float3x3(right, up, forward);

                // use the max scale to figure out how big the quad needs to be to cover the entire sphere
                // we're using a hardcoded object space radius of 0.5 in the fragment shader
                float maxRadius = maxScale * 2.5;

                // find the radius of a cone that contains the sphere with the point at the camera and the base at the pivot of the sphere
                // this means the quad is always scaled to perfectly cover only the area the sphere is visible within
                float quadScale = maxScale;
                if (!isOrtho)
                {
                    // get the sine of the right triangle with the hyp of the sphere pivot distance and the opp of the sphere radius
                    float sinAngle = maxRadius / length(worldSpacePivotToView);
                    // convert to cosine
                    float cosAngle = sqrt(1.0 - sinAngle * sinAngle);
                    // convert to tangent
                    float tanAngle = sinAngle / cosAngle;

                    // basically this, but should be faster
                    //tanAngle = tan(asin(sinAngle));

                    // get the opp of the right triangle with the 90 degree at the sphere pivot * 2
                    quadScale = tanAngle * length(worldSpacePivotToView) * 2.0;
                }

                // flatten mesh, in case it's a cube or sloped quad mesh
                v.vertex.z = 0.0;


                // offset towards the camera for use with conservative depth
#if defined(USE_CONSERVATIVE_DEPTH)
                worldPos += worldSpaceRayDir / dot(normalize(worldSpacePivotToView), worldSpaceRayDir) * maxRadius;
#endif

*/
// check if the current projection is orthographic or not from the current projection matrix
                bool isOrtho = UNITY_MATRIX_P._m33 == 1.0;

                // viewer position, equivalent to _WorldSpaceCAmeraPos.xyz, but for the current view
                float3 worldSpaceViewerPos = UNITY_MATRIX_I_V._m03_m13_m23;

                // view forward
                float3 worldSpaceViewForward = -UNITY_MATRIX_I_V._m02_m12_m22;

                // pivot position
                float3 worldSpacePivotPos = unity_ObjectToWorld._m03_m13_m23;

                // offset between pivot and camera
                float3 worldSpacePivotToView = worldSpaceViewerPos - worldSpacePivotPos;

                // calculate world space view ray direction and origin for perspective or orthographic
                float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;

                float3 worldSpaceRayOrigin = worldSpaceViewerPos;
                float3 worldSpaceRayDir = worldPos - worldSpaceRayOrigin;
                if (isOrtho)
                {
                    worldSpaceRayDir = worldSpaceViewForward * -dot(worldSpacePivotToView, worldSpaceViewForward);
                    worldSpaceRayOrigin = worldPos - worldSpaceRayDir;
                }

                // output object space ray direction and origin
                o.rayDir = mul(unity_WorldToObject, float4(worldSpaceRayDir, 0.0));
                o.rayOrigin = mul(unity_WorldToObject, float4(worldSpaceRayOrigin, 1.0));
                float4 vert = UnityObjectToClipPos(v.vertex);
                float4 uvs = ComputeScreenPos(vert);
                uvs.xy /= uvs.w;
                o.uv = uvs.xy;

                o.depth = UnityObjectToClipPos(v.vertex);
                o.depth.z /= o.depth.w;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1));

                //o.vertex = UnityObjectToClipPos(worldPos);
                //o.vertex = mul(unity_ObjectToWorld, v.vertex);
                o.instanceID = v.instanceID;
                o.worldPos = worldPos;

                return o;
            }

            float LinearDepthToRawDepth(float linearDepth)
            {
                return (1.0f - (linearDepth * _ZBufferParams.y)) / (linearDepth * _ZBufferParams.x);
            }
            float epsilon = 0.00001;
			//MRT output
            void frag(v2f i,
                out half4 GRT0:SV_Target0,
                out half4 GRT1 : SV_Target1,
                out half4 GRT2 : SV_Target2,
                out half4 GRT3 : SV_Target3,
                out float GRTDepth : SV_Depth)
            {
                UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.

                float velocity = length(_Particles[i.instanceID].velocity) + length(_Particles[i.instanceID].curl) * 0.055;
                float surface = 1.0 - (_Particles[i.instanceID].density / 180.0);
                float density = 0;
                fixed4 unigmaDepth = tex2D(_UnigmaDepthMap, i.uv);
                float Rdepth = lerp(i.depth.z, 0, step(i.depth.z, unigmaDepth.z));

                float4 velocitySurfaceDensityDepth = float4(velocity, surface, density, Rdepth);
                GRT0 = velocitySurfaceDensityDepth;
                GRT2 = float4(_Particles[i.instanceID].position, Rdepth);//float4(positionWS, depthN);//float4(_Particles[i.instanceID].velocity, length(_Particles[i.instanceID].velocity) + length(_Particles[i.instanceID].curl) * 0.055);
                GRT3 = float4(_Particles[i.instanceID].normal, 1);

                //GRTDepth = 1;
                
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
                float spring;
                float mass;
                int parent;
            };

            StructuredBuffer<Particle> _Particles;
            sampler2D _DistancesMap;
            
            struct appdata
            {
                float4 vertex : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                uint instanceID : SV_InstanceID;
				float2 uv : TEXCOORD0;
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

                float4 vert = UnityObjectToClipPos(v.vertex);
                float4 uvs = ComputeScreenPos(vert);
                uvs.xy /= uvs.w;
                o.uv = uvs.xy;
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

                float4 clipPos = UnityWorldToClipPos(float4(positionWS, 1));
                float4 distances = tex2D(_DistancesMap, i.uv);//
                float depthN = (clipPos.z * 1.0) / (clipPos.w * 1.0);
                depthN = 1.0 - depthN;

                float4 velocitySurfaceDensityDepth = float4(GRT0.x, GRT0.y, density, GRT0.w);
                GRT0 = velocitySurfaceDensityDepth;
                GRT1 = density * 4;
            }
            ENDCG
        }

    }
}
