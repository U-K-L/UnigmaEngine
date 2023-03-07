Shader "Hidden/PixelPalate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LookUpTable ("Look up", 2D) = "white" {}
	
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex, _LookUpTable;

            float3 LookUpColor(float3 color)
            {
                float2 s = float2(1.0 / 17.0, 1.0 / 17.0); //divided by texture size.
                float3 lookUp = float3(1, 1, 1);
                float3 finalColor = color;
				float minDist = 1000000;
				
				for(int i = 0; i < 17; i++)
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

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
			    col.rgb = LookUpColor(col.rgb);
				
                return col;
            }
            ENDCG
        }
    }
}
