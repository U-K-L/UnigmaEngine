Shader "Unigma/PathShader"
{
	Properties
	{
		_Color("Color", Color) = (0.5, 0.65, 1, 1)
		_MainTex("Texture", 2D) = "white" {}
		[HDR]
		_AmbientColor("Ambient Color", Color) = (0.4, 0.4, 0.4, 1)
		_SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
		_Glossiness("Glosiness", Float) = 7.8
		_RimLightColor("Rim Light Color", Color) = (1,1,1,1)
		_RimOutlineColor("Outline Color", Color) = (1,1,1,1)
		_OutlineAmount("Outline Thickness", Range(-1, 2)) = 0.875
		_RimAmount("Rim Thickness", Range(0, 1)) = 0.6
		_RimThreshold("Rim Threshold", Range(0, 1)) = 0.1

	}
		SubShader
	{
		// No culling or depth
		//Cull Off ZWrite Off ZTest Always

		Tags
		{
			"Queue" = "Transparent-1"
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase" //Forward base lighting data. Sets the light mode to forwards.
			"PassFlags" = "Deferred" //Only take main direction.
			"Glowable" = "True"
		}
		Blend SrcAlpha OneMinusSrcAlpha

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
				float4 color : COLOR;
			};

			struct v2f
			{
				float3 viewDir : TEXCOORD1; //Finds viewing direction.
				float3 worldNormal : NORMAL;
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
				float4 color : COLOR;
				SHADOW_COORDS(2)
			};

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

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.viewDir = WorldSpaceViewDir(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				// Defined in Autolight.cginc. Assigns the above shadow coordinate
				// by transforming the vertex from world space to shadow-map space.
				TRANSFER_SHADOW(o)
				return o;
			}



			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 text = tex2D(_MainTex, i.uv);
				float speed = 10.0;
				float4 finalColor = float4(i.color.x +abs(0.55*cos(speed*_Time.x)), i.color.y+abs(0.05*cos(speed*_Time.x)), i.color.z +abs(0.25*sin(speed*_Time.x)), text.r);
				_Color = finalColor;
				float3 viewDir = normalize(i.viewDir); //Unit vector (direction) of viewing angle.
				float3 betweenVec = normalize(_WorldSpaceLightPos0 + viewDir); //Get angle between light and view, then normalize.
				float3 normal = normalize(i.worldNormal); //Get normal from frag struct.
				float bdotn = dot(normal, betweenVec);
				float Ldotn = dot(_WorldSpaceLightPos0, normal); //Dot product light with normal.
				float shadow = SHADOW_ATTENUATION(i);

				float lightIntensity = smoothstep(0,0.01,Ldotn*shadow); //Colors one side and slowly transitions.
				float specularIntensity = pow(bdotn*lightIntensity, _Glossiness*_Glossiness);
				float specularSmoothed = smoothstep(0.005, 0.01, specularIntensity);
				float4 specular = specularSmoothed * _SpecularColor;
				float4 light = lightIntensity * _LightColor0;

				float4 rimLight = 1 - dot(normal, viewDir);
				float4 rimBloom = _RimAmount *0.4456*dot(normal, viewDir);

				float4 OutLine = 10*_RimOutlineColor*smoothstep(_OutlineAmount - 0.001, _OutlineAmount + 0.3, rimLight);
				float4 BlackOutLine = -10 *_RimOutlineColor*smoothstep(_OutlineAmount - 0.01, _OutlineAmount + 0.2, rimLight);
				OutLine = float4(cross(cross(OutLine, _Color), OutLine), 1);

				float rimIntensity = rimLight * pow(Ldotn, _RimThreshold);
				rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);
	

				float rim = rimIntensity * _RimLightColor;

				fixed4 result = text * finalColor* (light + _AmbientColor + specular + rim + rimBloom + BlackOutLine + OutLine);
				result.a = 1;

				return finalColor;
			}

		ENDCG
		}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
