Shader "Unlit/PixelGrassShader"
{
    Properties
    {
		_Color("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
	    _WorldTex("World Texture", 2D) = "white" {}
		_Scale("Vector", Vector) = (1,1,1,1)
        _Brightness("Brightness", Vector) = (1,1,1,1)
        _LookUpTable("Look up", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
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
            #pragma multi_compile_fog
            #pragma target 5.0

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
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD2;
            };

            struct OutputVertex
            {
                float3 position;
                float3 normal;
                float2 uv;
            };

            struct OutputTriangle
            {
                float3 normal;
                float2 uv;
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
			float4 _Color, _Scale, _Brightness;

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
                o.vertex = UnityObjectToClipPos(float4(va.position, 1));
				o.worldPos = mul(unity_ObjectToWorld, float4(va.position, 1)).xyz;
                //o.normal = tri.normal;
                o.uv = TRANSFORM_TEX(va.uv, _MainTex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                //Let's scroll through a texture the uses world space, a world space texture mapping.
                float4 yt = tex2D(_WorldTex, i.worldPos.zx * _Scale.x);
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 worldText = tex2D(_WorldTex, yt);
                fixed4 brightenedWorldTex = worldText * (_Color + _Brightness);

                //Clip out according to clip texture.
                clip(col.a - 0.5f);

                //Simplify color according to look up texture.
                

                return brightenedWorldTex;
            }
            ENDCG
        }
    }
}
