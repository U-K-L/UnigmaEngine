Shader "Unigma/ToonGrass"
{
	Properties
	{
		[Header(Shading)]
		_MainTex("Source", 2D) = "white" {}
		_TopColor("Top Color", Color) = (1,1,1,1)
		_BottomColor("Bottom Color", Color) = (1,1,1,1)
		_TranslucentGain("Translucent Gain", Range(0,1)) = 0.5
		_BendRotationRandom("Bends the grass", Range(0,1)) = 0.2
		_BladeWidth("Blade Width", Float) = 0.05
		_BladeWidthRandom("Blade Width Random", Float) = 0.02
		_BladeHeight("Blade Height", Float) = 0.5
		_BladeHeightRandom("Blade Height Random", Float) = 0.3
		_BladeForward("Blade Forward Amount", Float) = 0.38
		_BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2
		_TessellationUniform("Tessellation Uniform", Range(1, 64)) = 1
		_WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
		_WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
		_WindStrength("Wind Strength", Float) = 1

		[HDR]
		_AmbientColor("Ambient Color", Color) = (0.4, 0.4, 0.4, 1)
		_SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
		_Glossiness("Glosiness", Float) = 7.8
		_RimLightColor("Rim Light Color", Color) = (1,1,1,1)
		_RimOutlineColor("Outline Color", Color) = (1,1,1,1)
		_OutlineAmount("Outline Thickness", Range(0, 2)) = 0.875
		_RimAmount("Rim Thickness", Range(0, 1)) = 0.6
		_RimThreshold("Rim Threshold", Range(0, 1)) = 0.1

	}

		CGINCLUDE
#include "UnityCG.cginc"
#include "Autolight.cginc"
#include "Shaders/CustomTessellation.cginc"

#define BLADE_SEGMENTS 12

			float _BendRotationRandom;
		float _BladeHeight;
		float _BladeHeightRandom;
		float _BladeWidth;
		float _BladeWidthRandom;
		float _BladeForward;
		float _BladeCurve;
		sampler2D _WindDistortionMap;
		float4 _WindDistortionMap_ST;
		float2 _WindFrequency;
		float _WindStrength;
		// Simple noise function, sourced from http://answers.unity.com/answers/624136/view.html
		// Extended discussion on this function can be found at the following link:
		// https://forum.unity.com/threads/am-i-over-complicating-this-random-function.454887/#post-2949326
		// Returns a number in the 0...1 range.

		struct geometryOutput
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			unityShadowCoord4 _ShadowCoord : TEXCOORD1;
			float3 normal : NORMAL;
			float3 viewDir : TEXCOORD2; //Finds viewing direction.
		};

		geometryOutput VertexOutput(float3 pos, float2 uv, float3 normal)
		{
			geometryOutput o;
			o.pos = UnityObjectToClipPos(pos);
			o.uv = uv;
			o._ShadowCoord = ComputeScreenPos(o.pos);
			o.normal = UnityObjectToWorldNormal(normal);
			o.viewDir = WorldSpaceViewDir(o.pos);
#if UNITY_PASS_SHADOWCASTER
			// Applying the bias prevents artifacts from appearing on the surface.
			o.pos = UnityApplyLinearShadowBias(o.pos);
#endif
			return o;
		}

		float rand(float3 co)
		{
			return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
		}

		// Construct a rotation matrix that rotates around the provided axis, sourced from:
		// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
		float3x3 AngleAxis3x3(float angle, float3 axis)
		{
			float c, s;
			sincos(angle, s, c);

			float t = 1 - c;
			float x = axis.x;
			float y = axis.y;
			float z = axis.z;

			return float3x3(
				t * x * x + c, t * x * y - s * z, t * x * z + s * y,
				t * x * y + s * z, t * y * y + c, t * y * z - s * x,
				t * x * z - s * y, t * y * z + s * x, t * z * z + c
				);
		}

		geometryOutput GenerateGrassVertex(float3 vertexPosition, float width, float height, float forward, float2 uv, float3x3 transformMatrix)
		{
			float3 tangentPoint = float3(width, forward, height);
			float3 tangentNormal = normalize(float3(0, -1, forward));
			float3 localNormal = mul(transformMatrix, tangentNormal);
			float3 localPosition = vertexPosition + mul(transformMatrix, tangentPoint);
			return VertexOutput(localPosition, uv, localNormal);
		}

		[maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
		void geo(triangle vertexOutput IN[3], inout TriangleStream<geometryOutput> triStream) {
			float3 pos = IN[0].vertex;
			float3 vNormal = IN[0].normal;
			float4 vTangent = IN[0].tangent;
			float3 vBinormal = cross(vNormal, vTangent)*vTangent.w;

			float3x3 tangentToLocal = float3x3(
				vTangent.x, vBinormal.x, vNormal.x,
				vTangent.y, vBinormal.y, vNormal.y,
				vTangent.z, vBinormal.z, vNormal.z
				);
			float3x3 facingRotationMatrix = AngleAxis3x3(rand(pos)*UNITY_TWO_PI, float3(0, 0, 1));
			float3x3 bendRotationMatrix = AngleAxis3x3(rand(pos.zzx)*_BendRotationRandom*UNITY_PI*0.5, float3(-1, 0, 0));

			float2 uv = pos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
			float2 windSample = (tex2Dlod(_WindDistortionMap, float4(uv, 0, 0)).xy * 2 - 1)*_WindStrength;
			float3 wind = normalize(float3(windSample.x, windSample.y, 0)); //Gets the direction of the wind.
			float3x3 windRotation = AngleAxis3x3(UNITY_PI *windSample, wind);

			float3x3 transformationMatrix = mul(mul(mul(tangentToLocal, windRotation), facingRotationMatrix), bendRotationMatrix);
			float3x3 transformationMatrixFacing = mul(tangentToLocal, facingRotationMatrix); //Transformation matrix for base vertices in order to correct clipping.

			float height = (rand(pos.zyx) * 2 - 1)*_BladeHeightRandom + _BladeHeight;
			float width = (rand(pos.xzy) * 2 - 1)*_BladeWidthRandom + _BladeWidth;
			float forward = rand(pos.yyz)*_BladeForward;

			for (int i = 0; i < BLADE_SEGMENTS; i++) {
				float t = i / (float)BLADE_SEGMENTS;
				float segmentHeight = height * t;
				float segmentWidth = width * (1 - t);
				float segmentForward = pow(t, _BladeCurve)*forward;
				float3x3 transformMatrix = i == 0 ? transformationMatrixFacing : transformationMatrix;
				triStream.Append(GenerateGrassVertex(pos, segmentWidth, segmentHeight, segmentForward, float2(0, t), transformMatrix));
				triStream.Append(GenerateGrassVertex(pos, -segmentWidth, segmentHeight, segmentForward, float2(1, t), transformMatrix));
			}
			triStream.Append(GenerateGrassVertex(pos, 0, height, forward, float2(0.5, 1), transformationMatrix));
		}
		ENDCG

			SubShader
		{
			Cull Off

			Pass
			{
				Tags
				{
					"RenderType" = "Opaque"
					"LightMode" = "ForwardBase"
				}

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma geometry geo
				#pragma target 4.6
				#pragma hull hull
				#pragma domain domain

				#include "Lighting.cginc"

				float4 _TopColor;
				float4 _BottomColor;
				float _TranslucentGain;
				sampler2D _MainTex;

				float4 _MainTex_ST;
				float4 _Color;
				float4 _AmbientColor;
				float _Glossiness;
				float4 _SpecularColor;
				float4 _RimLightColor;
				float4 _RimOutlineColor;
				float _OutlineAmount;
				float _RimAmount;
				float _RimThreshold;
				float4 frag(geometryOutput i, fixed facing : VFACE) : SV_Target
				{
				
				float3 viewDir = normalize(i.viewDir); //Unit vector (direction) of viewing angle.
				float3 betweenVec = normalize(_WorldSpaceLightPos0 + viewDir); //Get angle between light and view, then normalize.
				float3 normal = normalize(i.normal); //Get normal from frag struct.
				float bdotn = dot(normal, betweenVec);
				float Ldotn = dot(_WorldSpaceLightPos0, normal); //Dot product light with normal.
				float shadow = SHADOW_ATTENUATION(i);

				float lightIntensity = smoothstep(0,0.005,Ldotn*shadow); //Colors one side and slowly transitions.
				float specularIntensity = pow(bdotn*lightIntensity, _Glossiness*_Glossiness);
				float specularSmoothed = smoothstep(0.005, 0.01, specularIntensity);
				float4 specular = specularSmoothed * _SpecularColor;
				float4 light = lightIntensity * _LightColor0;

				float4 rimLight = 1 - dot(normal, viewDir);
				float4 rimBloom = _RimAmount *0.4456*dot(normal, viewDir);

				float4 OutLine = _RimOutlineColor*smoothstep(_OutlineAmount - 0.001, _OutlineAmount + 0.3, rimLight);
				float4 BlackOutLine = -10 *_RimOutlineColor*smoothstep(_OutlineAmount - 0.01, _OutlineAmount + 0.2, rimLight);
				OutLine = float4(cross(cross(OutLine, _Color), OutLine), 1);

				float rimIntensity = rimLight * pow(Ldotn, _RimThreshold);
				rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);
	

				float rim = rimIntensity * _RimLightColor;
				fixed4 image = tex2D(_MainTex, i.uv);
				float4 color = image * lerp(_BottomColor, _TopColor, i.uv.y);
				return color* (light+_AmbientColor+specular+rim+ rimBloom+BlackOutLine+OutLine);
				}
				ENDCG
			}

			//Shadows pass.
			Pass
			{
				Tags
				{
					"LightMode" = "ShadowCaster"
				}

				CGPROGRAM
				#pragma vertex vert
				#pragma geometry geo
				#pragma fragment frag
				#pragma hull hull
				#pragma domain domain
				#pragma target 4.6
				#pragma multi_compile_fwdbase
				#pragma multi_compile_shadowcaster

				float4 frag(geometryOutput i) : SV_Target
				{
					SHADOW_CASTER_FRAGMENT(i)
				}

				ENDCG
			}

		}
}