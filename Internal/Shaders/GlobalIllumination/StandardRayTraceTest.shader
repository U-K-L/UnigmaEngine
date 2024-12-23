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
                #include "../FluidHelpers.hlsl"

                struct AABB
                {
                    float3 min;
                    float3 max;
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

            float sdSphere( float3 sp, float3 rp, float s )
            {
              float3 p = sp - rp;
              return length(p)-s;
            }

            float2 eliIntersect( in float3 ro, in float3 rd, in float3 ra, float3 pos )
            {
                float3 oc = ro - pos;
                float3 ocn = oc/ra;
                float3 rdn = rd/ra;
                float a = dot( rdn, rdn );
                float b = dot( ocn, rdn );
                float c = dot( ocn, ocn );
                float h = b*b - a*(c-1.0);
                if( h<0.0 ) return float2(-1.0, -1.0); //no intersection
                h = sqrt(h);
                return float2(-b-h,-b+h)/a;
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

                float maxScalingFactor = 1.5f;

                float3x3 resultMatrix;

                //Check determinant.
                float3x3 identityMatrix = {
                    float3(1,0,0),
                    float3(0,1,0),
                    float3(0,0,1)
                };

                resultMatrix = identityMatrix;

                float3x3 Gmatrix = {
                    _Particles[PrimitiveIndex()].anisotropicTRS[0].xyz * identityMatrix[0],
                    _Particles[PrimitiveIndex()].anisotropicTRS[1].xyz * identityMatrix[1],
                    _Particles[PrimitiveIndex()].anisotropicTRS[2].xyz * identityMatrix[2]
                };



                float qualityCheck = 0.0000001f;
                float distanceCheck = 0.00001f;
                float densityCheck = 0.025f;

                float density = _Particles[PrimitiveIndex()].density / 35.0f;

                float det = determinant(Gmatrix);

                //Check distance from identity, if close set to identity Matrix.
                float3x3 identitySub = Gmatrix - identityMatrix;
                float identityDist = euclideanNorm(identitySub);

                float3x3 ginv = inverse(Gmatrix);
                float3x3 minv = {
                    min(maxScalingFactor, abs(ginv[0])),
                    min(maxScalingFactor, abs(ginv[1])),
                    min(maxScalingFactor, abs(ginv[2])),
                };

                minv[0].x = max(1, minv[0].x);
                minv[1].y = max(1, minv[1].y);
                minv[2].z = max(1, minv[2].z);

                if( (det > qualityCheck) && (identityDist > distanceCheck) && density > densityCheck)
                    resultMatrix = minv;

                float3 velocityP = _Particles[PrimitiveIndex()].velocity.xyz;

                float3 velocityStretch = abs(velocityP);
                float verticalRatio = velocityStretch.y / (velocityStretch.x + velocityStretch.z);
                velocityStretch = min(float3(min(2, velocityStretch.x / verticalRatio), min(2, velocityStretch.y * verticalRatio), min(2, velocityStretch.z / verticalRatio)), 0.1);

                if(abs(velocityP.y) < 15 || density > densityCheck)
                    velocityStretch = float3(1,1,1);
                    

                float3 pos = _Particles[PrimitiveIndex()].position;
				float4 sphere = float4(pos, _SizeOfParticle);
                float4 rad = float4(1.0f, 1.0f, 1.0f, 0.0f) * sphere.w; //(float3(0.1, 0.1, 0.1) + abs(normalize(_Particles[PrimitiveIndex()].velocity))) * float3(1.0f, 1.0f, 1.0f) * sphere.w;

 

                rad.xyz = mul(rad.xyz, minv).xyz * velocityStretch;

                //float t1 = ellipIntersect(ro, rd, sphere, rad);
                float2 t12 = eliIntersect(ro, rd, rad.xyz, pos);
                //t12.x = sphIntersect(ro, rd, sphere);

                //t1 =  sdSphere(sphere.xyz, ro, sphere.w);
                //if (t1 < 0)
                //{
                    attr.distance = t12.x;
					attr.position = ro + rd * t12.x;
                    ReportHit(t12.x, 0, attr);
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
                payload.color = float4(attributes.position, PrimitiveIndex());// * _Particles[PrimitiveIndex()].type;//float4(normals, 1);
                payload.uv = float2(0.05, 1);
                
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
