// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unigma/UnigmaToonStylized"
{
    Properties
    {
        _MainTex ("Augmented RGB Normal Map", 2D) = "black" {}
        _NormalMap("Normal Map", 2D) = "black" {}
	    _MainColor("Midtone", Color) = (1,1,1,1)
		_Shadow("Shadow", Color) = (1,1,1,1)
        _ShadowColors("Shadow Casted Colors", Color) = (1,1,1,1)
		_Highlight("Highlight", Color) = (1,1,1,1)
		_Thresholds("Light thresholds", Vector) = (0.2, 0.4, 0.6, 0.8)
        _Smoothness("Smoothness", Range(0,1)) = 0
        _LightAbsorbtion("Light Absorbtion", Range(0,1)) = 0
        _Emmittance("Light Emittance", Range(0,100)) = 0
        [HDR]
        _SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
        _Glossiness("Glossiness", Float) = 32
        [HDR]
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimAmount("Rim Amount", Range(0, 1)) = 0.716
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.1
		_UseRim("Use RIM", Float) = 0
        [KeywordEnum(CelShaded, ToonShaded, DistShaded)] _ColorDistModel("Color BRDF", Float) = 0
		_RimControl("Rim Control", Range(-1,1)) = 0
        [IntRange] _StencilRef("Stencil Ref Value", Range(0,255)) = 0
         _ReceiveShadow("Receive Shadow", Range(0,1)) = 1

        //For components.
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineInnerColor("Inner Outline Color", Color) = (0,0,0,1)
        _ThicknessTexture("Thickness of the line texture", 2D) = "black" {}
        _ThicknessTexture_ST("Thickness Vector", Vector) = (1, 1, 1, 1)
        
    }
    SubShader
    {
        Cull Off
        Tags { "RenderType" = "Transparent"
        "LightMode" = "ForwardBase" }
        LOD 100

        Stencil
        {
            Ref[_StencilRef]
            Comp Equal
        }
        
        ///----------------------SPECULAR PASS ------------------------------
        //
        //---------
        Pass
        {
            Name "SpecularRoughnessPass"
            CGPROGRAM
            #pragma vertex ToonSpecularVert
            #pragma fragment ToonSpecularFrag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            #include "UnigmaToonSpecular.cginc"
            ENDCG
        }

        ///----------------------ALBEDO PASS ------------------------------
        //
        //---------

        Pass
        {
            Name "AlbedoPass"
            CGPROGRAM
            #pragma vertex ToonAlbedoVert
            #pragma fragment ToonAlbedoFrag
            // make fog work
            #pragma multi_compile_fog 
            #pragma multi_compile _COLORDISTMODEL_CELSHADED _COLORDISTMODEL_TOONSHADED _COLORDISTMODEL_DISTSHADED

            #include "UnityCG.cginc"
            #include "../ShaderHelpers.hlsl"
            #include "UnigmaToonAlbedo.cginc"

            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

        
        ///----------------------DEPTH SHADOW PASS ------------------------------
        //
        //---------

        Pass
        {
            Name "DepthShadowsRaytracingShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../RayTraceHelpersUnigma.hlsl"
            #include "UnigmaToonShadow.cginc"


            ENDHLSL
        }

        ///----------------------GLOBAL ILLUMINATION PASS ------------------------------
        //
        //---------

        Pass
        {
            Name "GlobalIlluminationRaytracingShaderPass"

            HLSLPROGRAM
            #pragma raytracing MyRaytraceShaderPass
            #include "HLSLSupport.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "../RayTraceHelpersUnigma.hlsl"
            #include "UnityCG.cginc"
            #include "UnigmaToonGlobalIllum.cginc"

            ENDHLSL
        }

        Pass
        {
            Tags{ "LightMode" = "ShadowCaster" }
            Name "ShadowCast"
            CGPROGRAM
            #pragma vertex VSMain
            #pragma fragment PSMain

            float4 VSMain(float4 vertex:POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            float4 PSMain(float4 vertex:SV_POSITION) : SV_TARGET
            {
                return 1;
            }

            ENDCG
        }
    }
}
