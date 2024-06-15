Shader "Custom/StandardRayTraceTest"
{

    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
        SubShader
        {
            Pass
            {
                Name "MyRaytraceShaderPass"

                HLSLPROGRAM
                #pragma raytracing MyRaytraceShaderPass
                #include "HLSLSupport.cginc"
                #include "UnityRaytracingMeshUtils.cginc"
                #include "../RayTraceHelpersUnigma.hlsl"

                struct AABB
                {
                    float3 min;
                    float3 max;
                };

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
        
            StructuredBuffer<AABB> g_AABBs;
            Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
            StructuredBuffer<Particle> _Particles;
			float _SizeOfParticle;

            float RaySphereIntersect(float3 orig, float3 dir, float radius)
            {
                float a = dot(dir, dir);
                float b = 2 * dot(orig, dir);
                float c = dot(orig, orig) - radius * radius;
                float delta2 = b * b - 4 * a * c;
                float t = -1.0f;

                if (delta2 >= 0)
                {
                    float t0 = (-b + sqrt(delta2)) / (2 * a);
                    float t1 = (-b - sqrt(delta2)) / (2 * a);

                    // Get the smallest root larger than 0 (t is in object space);
                    t = max(t0, t1);

                    if (t0 >= 0)
                        t = min(t, t0);

                    if (t1 >= 0)
                        t = min(t, t1);

                    float3 localPos = orig + t * dir;

                    float3 worldPos = mul(ObjectToWorld(), float4(localPos, 1));

                    t = length(worldPos - WorldRayOrigin());
                }

                return t;
            }
            
            [shader("intersection")]
            void IntersectionMain()
            {
                AABB aabb = g_AABBs[PrimitiveIndex()];
                float3 aabbPos = (aabb.min + aabb.max) * 0.5f;
                float3 aabbSize = aabb.max;
                
                float3 ro = WorldRayOrigin();
                float3 rd = WorldRayDirection();
                AttributeData attr;
                attr.barycentrics = float2(0, 0);
                attr.distance = -1;


                float3 pos = _Particles[PrimitiveIndex()].position;
				float4 sphere = float4(pos, _SizeOfParticle);
                float t1 = sphIntersect(ro, rd, sphere);
                //if (t1 > 0)
                //{
                    attr.distance = t1;
					attr.position = ro + rd * t1;
                    ReportHit(t1, 0, attr);
                //}
            }

            bool RayBoxIntersectionTest(in float3 rayWorldOrigin, in float3 rayWorldDirection, in float3 boxPosWorld, in float3 boxHalfSize,
                out float outHitT, out float3 outNormal, out float2 outUVs, out int outFaceIndex)
            {
                // convert from world to box space
                float3 rd = rayWorldDirection;
                float3 ro = rayWorldOrigin - boxPosWorld;

                // ray-box intersection in box space
                float3 m = 1.0 / rd;
                float3 s = float3(
                    (rd.x < 0.0) ? 1.0 : -1.0,
                    (rd.y < 0.0) ? 1.0 : -1.0,
                    (rd.z < 0.0) ? 1.0 : -1.0);

                float3 t1 = m * (-ro + s * boxHalfSize);
                float3 t2 = m * (-ro - s * boxHalfSize);

                float tN = max(max(t1.x, t1.y), t1.z);
                float tF = min(min(t2.x, t2.y), t2.z);

                if (tN > tF || tF < 0.0)
                    return false;

                // compute normal (in world space), face and UV
                if (t1.x > t1.y && t1.x > t1.z)
                {
                    outNormal = float3(s.x, 0, 0);
                    outUVs = float2(0.5, 0.5) + (ro.yz + rd.yz * t1.x) / (boxHalfSize.yz * 2);
                    outFaceIndex = (1 + int(s.x)) / 2;
                }
                else if (t1.y > t1.z)
                {
                    outNormal = float3(0, s.y, 0);
                    outUVs = float2(0.5, 0.5) + (ro.zx + rd.zx * t1.y) / (boxHalfSize.zx * 2);
                    outFaceIndex = (5 + int(s.y)) / 2;
                }
                else
                {
                    outNormal = float3(0, 0, s.z);
                    outUVs = float2(0.5, 0.5) + (ro.xy + rd.xy * t1.z) / (boxHalfSize.xy * 2);
                    outFaceIndex = (9 + int(s.z)) / 2;
                }

                outHitT = tN;

                return true;
            }
            /*
            [shader("intersection")]
            void BoxIntersectionMain()
            {
                AABB aabb = g_AABBs[PrimitiveIndex()];

                float3 aabbPos = (aabb.min + aabb.max) * 0.5f;
                float3 aabbSize = aabb.max;

                float outHitT = 0;
                float3 outNormal = float3(1, 0, 0);
                float2 outUVs = float2(0, 0);
                int outFaceIndex = 0;

                bool isHit = RayBoxIntersectionTest(WorldRayOrigin(), WorldRayDirection(), aabbPos, aabbSize * 0.5, outHitT, outNormal, outUVs, outFaceIndex);
                AttributeData attr;
                attr.normalOS = outNormal;
                ReportHit(1, 0, attr);
                if (isHit)
                {


                    
                }
            }
            */
            
            [shader("closesthit")]
            void MyHitShader(inout Payload payload : SV_RayPayload,
                AttributeData attributes : SV_IntersectionAttributes)
            {
                float2 uvs = GetUVs(attributes);
                float3 normals = GetNormals(attributes);
                //float3 worldNormal = mul((float4x4)unity_ObjectToWorld, float4(normals, 0)).xyz;

				float4 tex = _MainTex.SampleLevel(sampler_MainTex, uvs, 0);

				//payload.hits = interlockedAdd(payload.hits, 1);
				payload.distance = attributes.distance;
                payload.color = float4(attributes.position, PrimitiveIndex());//float4(normals, 1);
                
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
