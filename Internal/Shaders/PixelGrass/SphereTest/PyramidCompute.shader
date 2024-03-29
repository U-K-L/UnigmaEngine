Shader "Unlit/PyramidCompute"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma target 5.0
        
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
            
            struct OutputVertex {
                float3 position;
                float2 uv;
            };
            
            struct OutputTriangle
            {
                float3 normal; //This normal in world space.
                OutputVertex vertices[3];
            };

            struct VertexOutput {
                float3 position : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            StructuredBuffer <OutputTriangle> _outputTriangles;
			StructuredBuffer <OutputVertex> _outputVertices;
            
            sampler2D _MainTex;
            float4 _MainTex_ST;

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
            v2f vert(uint vertexID: SV_VertexID, appdata v)
            {
                v2f o;
                OutputTriangle tri = _outputTriangles[vertexID / 3];
                OutputVertex va = tri.vertices[vertexID % 3];
                

                o.vertex = UnityObjectToClipPos(float4(va.position, 1));
                o.normal = tri.normal;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            fixed4 frag (VertexOutput i) : SV_Target
            {
				fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
