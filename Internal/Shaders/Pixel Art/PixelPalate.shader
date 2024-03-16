Shader "Hidden/PixelPalate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LookUpTable ("Look up", 2D) = "white" {}
		_HSVWeights("HSV Weights", Vector) = (1.0, 1.0, 1.0, 1.0)
	
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
            float4 _HSVWeights;

            float3x3 rgb2yuv = float3x3(0.2126, 0.7152, 0.0722,
                -0.09991, -0.33609, 0.43600,
                0.615, -0.5586, -0.05639);
            float3 rgb2xyz(float3 c) {
                float3 tmp;
                tmp.x = (c.r > 0.04045) ? pow((c.r + 0.055) / 1.055, 2.4) : c.r / 12.92;
                tmp.y = (c.g > 0.04045) ? pow((c.g + 0.055) / 1.055, 2.4) : c.g / 12.92,
                    tmp.z = (c.b > 0.04045) ? pow((c.b + 0.055) / 1.055, 2.4) : c.b / 12.92;
                const float3x3 mat = float3x3(
                    0.4124, 0.3576, 0.1805,
                    0.2126, 0.7152, 0.0722,
                    0.0193, 0.1192, 0.9505
                    );
                return 100.0 * mul(tmp, mat);
            }
                float3 xyz2lab(float3 c) {
                    float3 n = c / float3(95.047, 100, 108.883);
                    float3 v;
                    v.x = (n.x > 0.008856) ? pow(n.x, 1.0 / 3.0) : (7.787 * n.x) + (16.0 / 116.0);
                    v.y = (n.y > 0.008856) ? pow(n.y, 1.0 / 3.0) : (7.787 * n.y) + (16.0 / 116.0);
                    v.z = (n.z > 0.008856) ? pow(n.z, 1.0 / 3.0) : (7.787 * n.z) + (16.0 / 116.0);
                    return float3((116.0 * v.y) - 16.0, 500.0 * (v.x - v.y), 200.0 * (v.y - v.z));
                }

                float3 rgb2lab(float3 c) {
                    float3 lab = xyz2lab(rgb2xyz(c));
                    return float3(lab.x / 100.0, 0.5 + 0.5 * (lab.y / 127.0), 0.5 + 0.5 * (lab.z / 127.0));
                }

                float3 lab2xyz(float3 c) {
                    float fy = (c.x + 16.0) / 116.0;
                    float fx = c.y / 500.0 + fy;
                    float fz = fy - c.z / 200.0;
                    return float3(
                        95.047 * ((fx > 0.206897) ? fx * fx * fx : (fx - 16.0 / 116.0) / 7.787),
                        100.000 * ((fy > 0.206897) ? fy * fy * fy : (fy - 16.0 / 116.0) / 7.787),
                        108.883 * ((fz > 0.206897) ? fz * fz * fz : (fz - 16.0 / 116.0) / 7.787)
                        );
                }

                float3 xyz2rgb(float3 c) {
                    const float3x3 mat = float3x3(
                        3.2406, -1.5372, -0.4986,
                        -0.9689, 1.8758, 0.0415,
                        0.0557, -0.2040, 1.0570
                        );
                    float3 v = mul(c / 100.0, mat);
                    float3 r;
                    r.x = (v.r > 0.0031308) ? ((1.055 * pow(v.r, (1.0 / 2.4))) - 0.055) : 12.92 * v.r;
                    r.y = (v.g > 0.0031308) ? ((1.055 * pow(v.g, (1.0 / 2.4))) - 0.055) : 12.92 * v.g;
                    r.z = (v.b > 0.0031308) ? ((1.055 * pow(v.b, (1.0 / 2.4))) - 0.055) : 12.92 * v.b;
                    return r;
                }

                float3 lab2rgb(float3 c) {
                    return xyz2rgb(lab2xyz(float3(100.0 * c.x, 2.0 * 127.0 * (c.y - 0.5), 2.0 * 127.0 * (c.z - 0.5))));
                }


                float3 RGBtoHSV(float3 rgb)
                {
                    // Hue: red = 0/6, yellow = 1/6, green = 2/6,
                    //      cyan = 3/6, blue = 4/6, magenta = 5/6
                    float3 hsv;
                    float cmax = max(rgb.r, max(rgb.g, rgb.b));
                    float cmin = min(rgb.r, min(rgb.g, rgb.b));

                    hsv.z = cmax; // value

                    float chroma = cmax - cmin;
                    //if(chroma != 0.0)
                    {
                        hsv.y = chroma / cmax; // saturation

                        //if(cmax == rgb.r)
                        if (rgb.r > rgb.g && rgb.r > rgb.b)
                        {
                            hsv.x = (0.0 + (rgb.g - rgb.b) / chroma) / 6.0; // hue
                        }
                        //else if(cmax == rgb.m_Green)
                        else if (rgb.g > rgb.b)
                        {
                            hsv.x = (2.0 + (rgb.b - rgb.r) / chroma) / 6.0; // hue
                        }
                        else
                        {
                            hsv.x = (4.0 + (rgb.r - rgb.g) / chroma) / 6.0; // hue
                        }

                        // Make sure hue is in range [0..1]
                        hsv.x = frac(hsv.x);
                    }
                    //else
                    //{
                    //    hsv.x = 0.0; // rnd();
                    //}
                    return hsv;
                }


            float3 LookUpColor(float3 color)
            {
                float2 s = float2(1.0 / 17.0, 1.0 / 17.0); //divided by texture size.
                float3 lookUp = float3(1, 1, 1);
                float3 finalColor = color;
				float minDist = 1000000;

                float3 hsvColor = RGBtoHSV(color)* _HSVWeights;
                float3 labColor = rgb2lab(color) * _HSVWeights;
                
				for(int i = 0; i < 17; i++)
                    for (int j = 0; j < 17; j++)
                    {
                        float3 lookUp = tex2D(_LookUpTable, float2(s.x * i, s.y * j)).rgb;
                        float3 lookUpHsv= RGBtoHSV(lookUp)* _HSVWeights;
                        float3 lookUpLab = rgb2lab(lookUp) * _HSVWeights;
                        
						float3 colA = float3(hsvColor.x, hsvColor.y * hsvColor.z, labColor.x);
						float3 colB = float3(lookUpHsv.x, lookUpHsv.y * lookUpHsv.z, lookUpLab.x);
                        float dist = distance(lookUpHsv, hsvColor);
						
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
			    float3 YuVCol = mul(rgb2yuv, col.rgb);
                float4 closesColor = col;
                closesColor.rgb = LookUpColor(col.rgb);
				
                return closesColor;
            }
            ENDCG
        }
    }
}
