Shader "Unigma/Smear"
{
	Properties
	{
		_Color("Color", Color) = (0.5, 0.65, 1, 1)
		_MainTex("Texture", 2D) = "white" {}
		_SpriteTex("Texture", 2D) = "white" {}
		[HDR]
		_AmbientColor("Ambient Color", Color) = (0.4, 0.4, 0.4, 1)
		_SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
		_Glossiness("Glosiness", Float) = 7.8
		_RimLightColor("Rim Light Color", Color) = (1,1,1,1)
		_RimOutlineColor("Outline Color", Color) = (1,1,1,1)
		_OutlineAmount("Outline Thickness", Range(0, 2)) = 0.875
		_RimAmount("Rim Thickness", Range(0, 1)) = 0.6
		_RimThreshold("Rim Threshold", Range(0, 1)) = 0.1

		//Smear Effect:
		_Position("Position", Vector) = (0, 0, 0, 0)
		_PrevPosition("Prev Position", Vector) = (0, 0, 0, 0)

		_NoiseScale("Noise Scale", Float) = 15
		_NoiseHeight("Noise Height", Float) = 1.3
		_SpriteTexOn("IsASprite", Range(0, 1)) = 0

		_SetFramesGPU("Set frames true false", Range(0,1)) = 1
		_Flip("Flips The Sprite", Range(0,1)) = 0

		//Remove a background of a single color.
		_BackGroundRemovalColor("Background Removal", Color) = (0,0,0,0)
	}
		SubShader
		{
			// No culling or depth
			Cull Off //ZWrite On ZTest Always

			Tags
			{
				"Queue" = "Transparent-1"
				//"RenderType" = "Transparent"
				"LightMode" = "ForwardBase" //Forward base lighting data. Sets the light mode to forwards.
				"PassFlags" = "Deferred" //Only take main direction.
				"Glowable" = "True"
			}

			//Grab pass needed for rendering behind the transparent sprite such as when half way submerged in water.
			GrabPass { "_BackgroundForSprites" }

			Pass
			{


				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase

				#include "UnityCG.cginc"
				#include "Lighting.cginc"
				#include "AutoLight.cginc"

				struct appdata
				{
					float3 normal : NORMAL;
					float4 vertex : POSITION;
					float4 uv : TEXCOORD0;
				};

				struct v2f
				{
					float3 viewDir : TEXCOORD1; //Finds viewing direction.
					float3 worldNormal : NORMAL;
					float2 uv : TEXCOORD0;
					float4 pos : SV_POSITION;
					float4 screenPosition : TEXCOORD3;
					SHADOW_COORDS(2)
				};

				sampler2D _MainTex, _SpriteTex, _BackgroundForSprites;
				float4 _MainTex_ST;
				float4 _Color, _BackGroundRemovalColor;
				float4 _AmbientColor;
				float _Glossiness;
				float4 _SpecularColor;
				float4 _RimLightColor;
				float4 _RimOutlineColor;
				float _OutlineAmount;
				float _RimAmount;
				float _RimThreshold, _SetFramesGPU, _Flip;
				

				fixed4 _PrevPosition;
				fixed4 _Position;

				half _NoiseScale;
				half _NoiseHeight;
				float _SpriteTexOn;

				float hash(float n)
				{
					return frac(sin(n)*43758.5453);
				}

				float noise(float3 x)
				{
					// The noise function returns a value in the range -1.0f -> 1.0f

					float3 p = floor(x);
					float3 f = frac(x);

					f = f * f*(3.0 - 2.0*f);
					float n = p.x + p.y*57.0 + 113.0*p.z;

					return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
						lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
						lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
							lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
				}

				appdata smearVert(appdata v) {
					//Create Smear Effect.
					fixed4 worldPos = mul(unity_ObjectToWorld, v.vertex);
					fixed3 worldOffset = _Position.xyz - _PrevPosition.xyz; // -5
					fixed3 localOffset = worldPos.xyz - _Position.xyz; // -5

					//Ensures offset is behind the momentum of change.
					float dirDot = dot(normalize(worldOffset), normalize(localOffset));
					fixed3 unitVec = fixed3(1, 1, 1) * _NoiseHeight;
					worldOffset = clamp(worldOffset, unitVec * -1, unitVec);
					worldOffset *= -clamp(dirDot, -1, 0) * lerp(1, 0, step(length(worldOffset), 0));

					//Sets the changing offsets.
					fixed3 smearOffset = -worldOffset.xyz * lerp(1, noise(worldPos * _NoiseScale), step(0, _NoiseScale));
					if(_SetFramesGPU > 0.1f)
						worldPos.xyz += smearOffset * 2;

					v.vertex = mul(unity_WorldToObject, worldPos);

					return v;
				}

				v2f vert(appdata v)
				{
					v = smearVert(v);
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.worldNormal = UnityObjectToWorldNormal(v.normal);
					o.viewDir = WorldSpaceViewDir(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					o.screenPosition = ComputeScreenPos(o.pos);

					TRANSFER_SHADOW(o)
					return o;
				}



				fixed4 frag(v2f i) : SV_Target
				{
					
					float3 viewDir = normalize(i.viewDir); //Unit vector (direction) of viewing angle.
					float3 betweenVec = normalize(_WorldSpaceLightPos0 + viewDir); //Get angle between light and view, then normalize.
					float3 normal = normalize(i.worldNormal); //Get normal from frag struct.
					float bdotn = dot(normal, betweenVec);
					float Ldotn = dot(_WorldSpaceLightPos0, normal); //Dot product light with normal.
					float shadow = SHADOW_ATTENUATION(i);

					float2 screenPosUV = i.screenPosition.xy / i.screenPosition.w;
					//screenPosUV.y += cos(_Time.y);
					//screenPosUV.x += sin(_Time.y);
					float4 backgroundForSprites = tex2D(_BackgroundForSprites, screenPosUV);

					float lightIntensity = smoothstep(0,0.01,Ldotn*shadow); //Colors one side and slowly transitions.
					float specularIntensity = pow(bdotn*lightIntensity, _Glossiness*_Glossiness);
					float specularSmoothed = smoothstep(0.005, 0.01, specularIntensity);
					float4 specular = specularSmoothed * _SpecularColor;
					float4 light = lightIntensity * _LightColor0;

					float4 rimLight = 1 - dot(normal, viewDir);
					float4 rimBloom = _RimAmount * 0.4456*dot(normal, viewDir);

					float4 OutLine = 10 * _RimOutlineColor*smoothstep(_OutlineAmount - 0.001, _OutlineAmount + 0.3, rimLight);
					float4 BlackOutLine = -10 * _RimOutlineColor*smoothstep(_OutlineAmount - 0.01, _OutlineAmount + 0.2, rimLight);
					OutLine = float4(cross(cross(OutLine, _Color), OutLine), 1);

					float rimIntensity = rimLight * pow(Ldotn, _RimThreshold);
					rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);


					float rim = rimIntensity * _RimLightColor;
					fixed4 text = tex2D(_MainTex, i.uv);
					float UVX = step(0.1, _Flip)*(_Flip - i.uv.x)  + step(_Flip, 0.1) * (i.uv.x);
					float4 spriteTex = tex2D(_SpriteTex, float2(UVX, i.uv.y));
					fixed4 result = text * _Color* (light + _AmbientColor + specular + rim + rimBloom + BlackOutLine + OutLine);
					result.a = 1;
					//Removes Background.
					/*
					if (length(_BackGroundRemovalColor) > 0)
						if (length(_BackGroundRemovalColor - text) < 0.0001)
							discard;
							*/
					if (spriteTex.w < 0.25)
						discard;
					//Determines if texture overwrites.
					if(_SpriteTexOn > 0.1)
						return spriteTex;
					return result;
				}

			ENDCG
		}

		//Shadowe casting pass.
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
				
				//v.vertex.z = 0.0;
				
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
				float4 vert = v.vertex * quadScale;//mul(UNITY_MATRIX_P,  mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0)) + float4(v.vertex.x, v.vertex.y, 0.0, 0.0)  * quadScale*5);
				i.pos =  UnityObjectToClipPos(float4(mul(vert.xyz, quadOrientationMatrixLight), 1));//float4(worldPos, 1);
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
				//clip(rayHit);
				
				float alphaCutout = tex2D(_MainTex, i.uv).a;
				clip(alphaCutout - 1);
				return 0;
			}
			
            ENDCG
        }

	}
}