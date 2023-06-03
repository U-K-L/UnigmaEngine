Shader "Unigma/ShadedRoundedSphere"
{
Properties
	{
		_Tint("Tint", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		[NoScaleOffset] _HeightMap("Heights", 2D) = "gray" {}
		_NormalMap("Normals", 2D) = "bump" {}
		_BumpScale("Bumpiness", Float) = 1
		_DetailTex("Details", 2D) = "white" {}
		_TopText("Top Texture", 2D) = "white" {}
		_TopNormal("Top Normal", 2D) = "white" {}
		_Smoothness("Smoothness", Range(0, 1)) = 0.5
		_SpecularTint("Specular", Color) = (0.0, 0.0, 0.0)
		[Gamma] _Metallic("Metallic", Range(0, 1)) = 0
		_Anisotropic("Anisotropic",  Range(-20,1)) = 0
		_Ior("Ior",  Range(1,4)) = 1.5
		_LightSteps("LightSteps",  Range(1,400)) = 1
		_ShadowStrength("ShadowStrength",  Range(0,1)) = 1
		[HDR]
		_Brightness("Brightness",  Range(0,20)) = 1
		[HDR]
		_AmbientColor("Ambient Color", Color) = (0.4,0.4,0.4,1)
		[HDR]
		_SpecularIntensity("SpecularBrightness",  Range(0,20)) = 1
		_Glossiness("Glossiness", Float) = 32
		[HDR]
		_RimColor("Rim Color", Color) = (1,1,1,1)
		_RimAmount("Rim Amount", Range(0, 1)) = 0.716
		_RimThreshold("Rim Threshold", Range(0, 1)) = 0.1
		_BlendSmooth("Blending smoothness", Range(0,4)) = 1.47
		_Scale("Scale of and Normals T,S,TN,SN", Vector) = (1,1,1,1)
		_NoiseScale("Noise Scale", Range(-2,2)) = -0.08
		_Noise("Noise", 2D) = "white" {}
		_Spread("Spread of texture", Range(-2,5)) = 1.88
		_EdgeWidth("EdgeWidth", Range(0,0.5)) = 1
			
		_UnityLightingContribution("Unity Reflection Contribution", Range(0,1)) = 1
		[KeywordEnum(BlinnPhong,Phong,Beckmann,Gaussian,GGX,TrowbridgeReitz,TrowbridgeReitzAnisotropic, Ward)] _NormalDistModel("Normal Distribution Model;", Float) = 0
		[KeywordEnum(AshikhminShirley,AshikhminPremoze,Duer,Neumann,Kelemen,ModifiedKelemen,Cook,Ward,Kurt)]_GeoShadowModel("Geometric Shadow Model;", Float) = 0
		[KeywordEnum(None,Walter,Beckman,GGX,Schlick,SchlickBeckman,SchlickGGX, Implicit)]_SmithGeoShadowModel("Smith Geometric Shadow Model; None if above is Used;", Float) = 0
		[KeywordEnum(Schlick,SchlickIOR, SphericalGaussian)]_FresnelModel("Normal Distribution Model;", Float) = 0
		[Toggle] _ENABLE_NDF("Normal Distribution Enabled?", Float) = 0
		[Toggle] _ENABLE_G("Geometric Shadow Enabled?", Float) = 0
		[Toggle] _ENABLE_F("Fresnel Enabled?", Float) = 0
		[Toggle] _ENABLE_D("Diffuse Enabled?", Float) = 0

		_NdotLThreshold("NdotL", Range(0,1)) = 1
	}

	SubShader
	{
		Pass
		{
			Tags{
				"LightMode" = "ForwardBase"
			}

			Cull Off
			CGPROGRAM
			
			#define FORWARD_BASE_PASS
				#if !defined(UnigmaLightingFunction)
					#define UnigmaLightingFunction
					#include "../UniversalUnigmaLighting.cginc"
				#endif
			
				#pragma multi_compile _ VERTEXLIGHT_ON
				//#pragma multi_compile_fwdbase_fullshadows      
				#pragma target 3.0
				#pragma vertex VertexFunction
				#pragma fragment UnigmaPBR
				#pragma multi_compile _NORMALDISTMODEL_BLINNPHONG _NORMALDISTMODEL_PHONG _NORMALDISTMODEL_BECKMANN _NORMALDISTMODEL_GAUSSIAN _NORMALDISTMODEL_GGX _NORMALDISTMODEL_TROWBRIDGEREITZ _NORMALDISTMODEL_TROWBRIDGEREITZANISOTROPIC _NORMALDISTMODEL_WARD
				#pragma multi_compile _GEOSHADOWMODEL_ASHIKHMINSHIRLEY _GEOSHADOWMODEL_ASHIKHMINPREMOZE _GEOSHADOWMODEL_DUER_GEOSHADOWMODEL_NEUMANN _GEOSHADOWMODEL_KELEMAN _GEOSHADOWMODEL_MODIFIEDKELEMEN _GEOSHADOWMODEL_COOK _GEOSHADOWMODEL_WARD _GEOSHADOWMODEL_KURT 
				#pragma multi_compile _SMITHGEOSHADOWMODEL_NONE _SMITHGEOSHADOWMODEL_WALTER _SMITHGEOSHADOWMODEL_BECKMAN _SMITHGEOSHADOWMODEL_GGX _SMITHGEOSHADOWMODEL_SCHLICK _SMITHGEOSHADOWMODEL_SCHLICKBECKMAN _SMITHGEOSHADOWMODEL_SCHLICKGGX _SMITHGEOSHADOWMODEL_IMPLICIT
				#pragma multi_compile _FRESNELMODEL_SCHLICK _FRESNELMODEL_SCHLICKIOR _FRESNELMODEL_SPHERICALGAUSSIAN
				#pragma multi_compile  _ENABLE_NDF_OFF _ENABLE_NDF_ON
				#pragma multi_compile  _ENABLE_G_OFF _ENABLE_G_ON
				#pragma multi_compile  _ENABLE_F_OFF _ENABLE_F_ON
				#pragma multi_compile  _ENABLE_D_OFF _ENABLE_D_ON
			ENDCG
		}

		// Add below the existing Pass.
		Pass
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			CGPROGRAM
			#pragma vertex VertexFunction
			#pragma fragment frag
			#pragma target 4.6
			#pragma multi_compile_shadowcaster

			float4 frag(Interpolators i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}

			ENDCG
		}
		/*
		//Multiple lights.
		Pass 
		{
			Tags {
				"LightMode" = "ForwardAdd"
			}
			//Blends this light with the previous one.
			Blend One One
			Zwrite Off
			CGPROGRAM

				#pragma target 3.0  
				#pragma multi_compile_fwdadd_fullshadows
				#pragma vertex VertexFunction
				#pragma fragment UnigmaPBR

				#include "../UniversalUnigmaLighting.cginc"

			ENDCG
		}
		*/
	}
}
