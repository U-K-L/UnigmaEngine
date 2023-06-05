Shader "Unlit/PixelGrassShader"
{
    Properties
    {
		_Color("Color", Color) = (1,1,1,1)
        _Highlight("Color", Color) = (1,1,1,1)
        _Shadow("Color", Color) = (1,1,1,1)
        _MidShadowColor("Mid Shadow", Color) = (1,1,1,1)
        _RoadColor("Road Color", Color) = (1,1,1,1)
        _RoadShadowColor("Shadow Road Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
	    _WorldTex("World Texture", 2D) = "white" {}
		_Scale("Vector", Vector) = (1,1,1,1)
        _Brightness("Brightness", Vector) = (1,1,1,1)
        _LookUpTable("Look up", 2D) = "white" {}
        _ShadowsCuttOff("Shadow max threshold", Range(0,1)) = 0.05
        
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" 
        "LightMode" = "ForwardBase" }
        LOD 100
        Cull Off
        //Add transparency
		Blend SrcAlpha OneMinusSrcAlpha
       Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fwdadd_fullshadows
            #pragma target 5.0

            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #include "UnityCG.cginc"
        
			int _NumVerts;
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD2;
                float3 meshNormal : TEXCOORD3;
                float4 color : COLOR;
                float3 viewDir : TEXCOORD4;
                unityShadowCoord4 _ShadowCoord : TEXCOORD5;
            };

            struct OutputVertex
            {
                float3 position;
                float3 normal;
                float3 vertexColor;
                float2 uv;
            };


            struct OutputTriangle
            {
                float2 uv;
                float3 normal;
                OutputVertex vertices[3];
            };


            struct VertexOutput {
                float3 position : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD2;
				float4 positionCS : SV_POSITION;
            };

            StructuredBuffer <OutputTriangle> _outputTriangles;
			StructuredBuffer <OutputVertex> _outputVertices;

            sampler2D _MainTex, _WorldTex, _LookUpTable;
            float4 _MainTex_ST;
			float4 _Color, _Scale, _Brightness, _Shadow, _Highlight, _RoadColor, _RoadShadowColor, _MidShadowColor;
            float _ShadowsCuttOff;

            /*
            VertexOutput vert (uint vertexID: SV_VertexID)
            {
                VertexOutput o = (VertexOutput)0;

                OutputTriangle tri = _outputTriangles[vertexID / 3];
                OutputVertex v = tri.vertices[vertexID % 3];

                o.position = v.position;
                o.normal = tri.normal;
                o.uv = v.uv;
                return o;
            }
            */

            float3 LookUpColor(float3 color)
            {
                float2 s = float2(1.0 / 17.0, 1.0 / 17.0); //divided by texture size.
                float3 lookUp = float3(1, 1, 1);
                float3 finalColor = color;
                float minDist = 1000000;

                for (int i = 0; i < 17; i++)
                    for (int j = 0; j < 17; j++)
                    {
                        float3 lookUp = tex2D(_LookUpTable, float2(s.x * i, s.y * j)).rgb;

                        float dist = distance(lookUp, color);

                        if (minDist > dist)
                        {
                            finalColor = lookUp;
                            minDist = dist;
                        }

                    }


                return finalColor;
            }
            
            v2f vert(uint vertexID: SV_VertexID, appdata v)
            {
                v2f o;
                OutputTriangle tri = _outputTriangles[vertexID / 3];
                OutputVertex va = tri.vertices[vertexID % 3];
                //OutputVertex va = _outputVertices[vertexID];
                o.pos = UnityObjectToClipPos(float4(va.position, 1));
				o.worldPos = mul(unity_ObjectToWorld, float4(va.position, 1)).xyz;
                o.normal = UnityObjectToWorldNormal(tri.normal);
				o.meshNormal = va.normal;
                o.color = float4(va.vertexColor, 1);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                o.uv = TRANSFORM_TEX(va.uv, _MainTex);
                o._ShadowCoord = ComputeScreenPos(o.pos);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
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
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = DotClamped(normals, halfDir);

                float Shadow = SHADOW_ATTENUATION(i);
                float specularHighlight = pow(NdotH, 1.2);

                float specularCut = step(0.85, specularHighlight);
                float hardCut = step(_ShadowsCuttOff, NdotL);
                float MidCut = step(_ShadowsCuttOff + 0.09, NdotL);
                float shadow = SHADOW_ATTENUATION(i);

                float4 HighlightOrColor = lerp(_Highlight, 0, 1 - specularCut);
                float3 ShadowOrColor = lerp(_Shadow.rgb, _Color.rgb, hardCut * shadow);
                float3 MidShadowsOrColor = lerp(_MidShadowColor.rgb, _Color.rgb, MidCut * shadow);
                brightenedWorldTex.xyz = lerp(MidShadowsOrColor, brightenedWorldTex.xyz, 0.75);
                brightenedWorldTex.xyz = lerp(ShadowOrColor, brightenedWorldTex.xyz, 0.55);
                brightenedWorldTex.xyz = lerp(brightenedWorldTex.xyz, HighlightOrColor, saturate(HighlightOrColor.r * 100 * Shadow * NdotL));

                float3 roadColorShaded = lerp(_RoadShadowColor.rgb, _RoadColor.rgb, hardCut);
                brightenedWorldTex.xyz = lerp(roadColorShaded, brightenedWorldTex.xyz, step(0.5, 1 - i.color.r));

                //Partial derivative.
				float4 ddxy = float4(abs(ddx(i.worldPos)) + abs(ddy(i.worldPos)), 1)*100;

                float4 finalColorOutput = float4(brightenedWorldTex.xyz, 1);
                //if (col.a - 0.5f + i.color.r < 0)
                    //clip(length(i.color) - 1.1);
                clip(col.a - 0.5f);
                return finalColorOutput;
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
