Shader "Unlit/UnigmaToonShaderV3"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Highlight("Highlight Color", Color) = (1,1,1,1)
        _Shadow("Shadow Color", Color) = (1,1,1,1)
        _MidShadowColor("Mid Shadow", Color) = (1,1,1,1)
        _RoadColor("Road Color", Color) = (1,1,1,1)
        _RoadShadowColor("Shadow Road Color", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _WorldTex("World Texture", 2D) = "white" {}
        _Scale("Vector", Vector) = (1,1,1,1)
        _Brightness("Brightness", Vector) = (1,1,1,1)

		_ShadowsCuttOff("Shadow max threshold", Range(0,1)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent"
        "LightMode" = "ForwardBase" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fwdadd_fullshadows

            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "UnityCG.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 pos : SV_POSITION;
				float4 color : COLOR;
                float3 worldPos : TEXCOORD2;
                float3 normal : TEXCOORD3;
                float3 viewDir : TEXCOORD4;
                unityShadowCoord4 _ShadowCoord : TEXCOORD1;
            };

            sampler2D _MainTex, _WorldTex, _LookUpTable;
            float4 _MainTex_ST;
            float4 _Color, _Scale, _Brightness, _Shadow, _Highlight, _RoadColor, _RoadShadowColor, _MidShadowColor;
            float _ShadowsCuttOff;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = UnityObjectToWorldNormal(v.normal);
				o.color = v.color;
				o.viewDir = WorldSpaceViewDir(v.vertex);
                o._ShadowCoord = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //Let's scroll through a texture the uses world space, a world space texture mapping.
                float4 yt = tex2D(_WorldTex, i.worldPos.zx * _Scale.x);
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 worldText = tex2D(_WorldTex, yt);
                fixed4 brightenedWorldTex = worldText * (_Color + _Brightness);


                float3 lightPos = _WorldSpaceLightPos0.xyz - i.worldPos;
                float3 lightDirAbsolute = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightDir = normalize(lightDirAbsolute);
				float3 viewDir = normalize(i.viewDir);
                float3 normals = i.normal;

                float NdotL = DotClamped(normals, lightDir);
				float3 halfDir = normalize(lightDir +viewDir);
				float NdotH = DotClamped(normals, halfDir);

                float Shadow = SHADOW_ATTENUATION(i);
				float specularHighlight = pow(NdotH, 1.2);
                
				float specularCut = step(0.85, specularHighlight);
                float hardCut = step(_ShadowsCuttOff, NdotL);
                float MidCut = step(_ShadowsCuttOff+0.09, NdotL);
                float shadow = SHADOW_ATTENUATION(i);

				float4 HighlightOrColor = lerp(_Highlight, 0, 1-specularCut);
                float3 ShadowOrColor = lerp(_Shadow.rgb, _Color.rgb, hardCut* shadow);
                float3 MidShadowsOrColor = lerp(_MidShadowColor.rgb, _Color.rgb, MidCut* shadow);
                brightenedWorldTex.xyz = lerp(MidShadowsOrColor, brightenedWorldTex.xyz, 0.75);
                brightenedWorldTex.xyz = lerp(ShadowOrColor, brightenedWorldTex.xyz, 0.55);
                brightenedWorldTex.xyz = lerp(brightenedWorldTex.xyz, HighlightOrColor, saturate(HighlightOrColor.r*100* Shadow* NdotL));
                
                float3 roadColorShaded = lerp(_RoadShadowColor.rgb, _RoadColor.rgb, hardCut);
                brightenedWorldTex.xyz = lerp(roadColorShaded, brightenedWorldTex.xyz, step(0.5, (1 - i.color.r) + i.color.b));


                //Partial derivative.
                float4 ddxy = float4(abs(ddx(i.worldPos)) + abs(ddy(i.worldPos)), 1) * 100;

                float4 finalColorOutput = float4(brightenedWorldTex.xyz, 1);
                return finalColorOutput;
            }
            ENDCG
        }
            UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
