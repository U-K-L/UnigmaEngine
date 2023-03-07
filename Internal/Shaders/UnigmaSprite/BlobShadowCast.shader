Shader "Unlit/BlobShadowCast"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		//Shadowe casting pass.
		
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				clip(-1);
				return 0;
            }
            ENDCG
        }
		Pass
        {
		
			Name "ShadowCasterXZ"
            Tags {"LightMode"="ShadowCaster"}
			ZWrite On ZTest LEqual
            CGPROGRAM
			#pragma vertex UnigmaVertexShadowCaster
			#pragma fragment UnigmaFragmentShadowCaster
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"
			
			sampler2D _MainTex;

			//From bgolus and inigo. Classical sphere intersection test.
			float sphIntersect( float3 ro, float3 rd, float4 sph )
			{
				float3 oc = ro - sph.xyz;
				float b = dot( oc, rd );
				float c = dot( oc, oc ) - sph.w*sph.w;
				float h = b*b - c;
				if( h<0.0 ) return -1.0;
				h = sqrt( h );
				return -b - h;
			}
			
			struct VertexData
			{
				//V2F_SHADOW_CASTER;
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};
			
			struct Interpolators
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 rayDir : TEXCOORD1;
				float3 rayOrig : TEXCOORD2;
			};

			Interpolators UnigmaVertexShadowCaster(VertexData v)
			{
				Interpolators i;
				
                // check if the current projection is orthographic or not from the current projection matrix
                bool isOrtho = UNITY_MATRIX_P._m33 == 1.0;

                // viewer position, equivalent to _WorldSpaceCAmeraPos.xyz, but for the current view
                float3 worldSpaceViewerPos = UNITY_MATRIX_I_V._m03_m13_m23;

                // view forward, also the light direction.
                float3 worldSpaceViewForward = -UNITY_MATRIX_I_V._m02_m12_m22;

                // pivot position
                float3 worldSpacePivotPos = unity_ObjectToWorld._m03_m13_m23;

                // offset between pivot and camera
                float3 worldSpacePivotToView = worldSpaceViewerPos - worldSpacePivotPos;
				
				v.vertex.z = 0.0;
				
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

				//Calculate Light facing rotation matrix.
				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - worldSpacePivotPos);
				float3 upLight = lightDir;
				float3 forwardLight = isOrtho ? -worldSpaceViewerPos : normalize(worldSpacePivotToView);
				float3 rightLight = normalize(cross(forward, up));
				upLight = cross(right, forward);
				float3x3 quadOrientationMatrixLight = float3x3(rightLight, upLight, forwardLight);
				
                // use the max scale to figure out how big the quad needs to be to cover the entire sphere
                // we're using a hardcoded object space radius of 0.5 in the fragment shader
                float maxRadius = maxScale * 0.5;

                // find the radius of a cone that contains the sphere with the point at the camera and the base at the pivot of the sphere
                // this means the quad is always scaled to perfectly cover only the area the sphere is visible within
                float quadScale = maxScale;
				
				// calculate world space position for the camera facing quad
                float3 worldPos = mul(v.vertex.xyz * quadScale, quadOrientationMatrix) + worldSpacePivotPos;
				

				
                float3 worldSpaceRayOrigin = worldSpaceViewerPos;
                float3 worldSpaceRayDir = worldPos - worldSpaceRayOrigin;
				
                if (isOrtho)
                {
                    worldSpaceRayDir = worldSpaceViewForward * -dot(worldSpacePivotToView, worldSpaceViewForward);
                    worldSpaceRayOrigin = worldPos - worldSpaceRayDir;
                }

				
				i.rayDir = mul(unity_WorldToObject, float4(worldSpaceRayDir, 0.0));
				i.rayOrig = mul(unity_WorldToObject, float4(worldSpaceRayOrigin, 1.0));
				float4 vert = (v.vertex + float4(0,0,-1.5,1))* quadScale;//mul(UNITY_MATRIX_P,  mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0)) + float4(v.vertex.x, v.vertex.y, 0.0, 0.0)  * quadScale*5);
				i.pos =  UnityObjectToClipPos(float4(mul(vert.xyz, quadOrientationMatrix), 1));//float4(worldPos, 1);
				i.uv = v.uv;
				//TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return i;
			}

			fixed4 UnigmaFragmentShadowCaster(Interpolators i, out float outDepth : SV_Depth) : SV_Target
			{
				float3 rayOrigin = i.rayOrig;
				float3 rayDirection = normalize(i.rayDir);
				
				float rayHit = sphIntersect(rayOrigin, rayDirection, float4(0,0,0,1));
				float3 hitSpace = rayOrigin + rayHit * rayDirection;
				float4 clipPos = UnityClipSpaceShadowCasterPos(hitSpace, hitSpace);
				clipPos = UnityApplyLinearShadowBias(clipPos);

				outDepth = clipPos.z / clipPos.w;
				clip(rayHit);
				return 0;
			}
			
            ENDCG
        }
    }
}
