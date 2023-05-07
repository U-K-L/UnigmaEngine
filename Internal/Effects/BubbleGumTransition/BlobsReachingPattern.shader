Shader "Hidden/BlobsReachingPattern"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MaskColor("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _TransitionTexture("Transition Texture", 2D) = "white" {}
        _BackgroundTexture("Background Texture", 2D) = "white" {}
        _Transition("Transition Slider", Range(0,1)) = 0
        _SurfaceNoiseScroll("Surface Noise Scroll Amount", Vector) = (0.03, 0.03, 0, 0)
        _RefracStr("Refractance Strength, amp, speed.", Vector) = (12,2.0,0.002,1)
        _Intensity("Intensity of displacement", Range(0, 2)) = 1
        [Normal]_DisplacementTex("Displacement Map", 2D) = "white"{}

		_Count("Count", Range(0, 100)) = 1
		_Spacing("Spacing", Range(0, 1)) = 0.5
        _Radius("Radius", Range(0,1)) = 1
		_Speed("Speed", Range(0,10)) = 0.5
    }
    SubShader
    {
        // No culling or depth
		Cull Off ZWrite On ZTest LEqual
        //Add transparency
		Blend SrcAlpha OneMinusSrcAlpha

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
                float4 screenPosition : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPosition = ComputeScreenPos(o.vertex);
                return o;
            }

            sampler2D _MainTex, _TransitionTexture, _DisplacementTex, _BackgroundTexture;
            float _Transition, _Intensity, _Spacing, _Speed;
            float4 _SurfaceNoiseScroll, _RefracStr, _Color;
            float4 _MainTex_TexelSize, _MaskColor;
            
            float _Radius;
            const int _Count;

            float sdStar5(float2 p, float r, float rf)
            {
                const float2 k1 = float2(0.809016994375, -0.587785252292);
                const float2 k2 = float2(-k1.x, k1.y);
                p.x = abs(p.x);
                p -= 2.0 * max(dot(k1, p), 0.0) * k1;
                p -= 2.0 * max(dot(k2, p), 0.0) * k2;
                p.x = abs(p.x);
                p.y -= r;
                float2 ba = rf * float2(-k1.y, k1.x) - float2(0, 1);
                float h = clamp(dot(p, ba) / dot(ba, ba), 0.0, r);
                return length(p - ba * h) * sign(p.y * ba.x - p.x * ba.y);
            }
            
            float sdCircle(float2 p, float r)
            {
                return length(p) - r;
            }
            
            float sdMoon(float2 p, float d, float ra, float rb)
            {
                p.y = abs(p.y);
                float a = (ra * ra - rb * rb + d * d) / (2.0 * d);
                float b = sqrt(max(ra * ra - a * a, 0.0));
                if (d * (p.x * b - p.y * a) > d * d * max(b - p.y, 0.0))
                    return length(p - float2(a, b));
                return max((length(p) - ra),
                    -(length(p - float2(d, 0)) - rb));
            }
            float2 dot2(float2 p)
            {
                return dot(p, p);
            }
            float sdHeart(float2 p, float r)
            {
                p.x = abs(p.x);

                if (p.y + p.x > 1.0)
                    return sqrt(dot2(p - float2(0.25, 0.75))) - sqrt(2.0) / 4.0;
                return sqrt(min(dot2(p - float2(0.00, 1.00)),
                    dot2(p - 0.5 * max(p.x + p.y, 0.0)))) * sign(p.x - p.y);
            }

            float4 vec4(float3 vec)
            {
				return float4(vec.x, vec.y, vec.z, 1);
            }
            

            float4 vec4(float vec)
            {
                return float4(vec, vec, vec, 1);
            }

            float LineCluster(float p, float spacing, float count)
            {
				float result = step(frac(p * count), spacing);
                
                return result;
            }

            
            float PointGrid(float2 p)
            {
                float2 vecSteps = step(frac(p * 10), float2(ddx(p.x), ddy(p.y))*10 );
				float result = vecSteps.x * vecSteps.y;
				return result;
            }

            float2 ClosetCell(float2 p)
            {
                float minDistToCell = 100;
                float2 finalCell = p;
				for (int i = 0; i < _Count; i++)
				{
                    for (int j = 0; j < _Count; j++)
                    {
                        float2 currentPoint = float2((1.0 / _Count) * i, (1.0 / _Count) * j);
                        float dist = distance(currentPoint, p);
                        if (dist < minDistToCell)
                        {
                            minDistToCell = dist;
                            finalCell = currentPoint;
                        }
                    }

				}

                return finalCell;
            }

            float2 ClosetLineCell(float2 p)
            {
                float minDistToCell = 100;
                float2 finalCell = p;
                const int count = 10;
                for (int i = 0; i < count; i++)
                {
                    float2 currentPoint = float2( 1-((1.0 / count) * i), (1.0 / count) * i);
                    float dist = distance(currentPoint, p);
                    if (dist < minDistToCell)
                    {
                        minDistToCell = dist;
                        finalCell = currentPoint;
                    }

                }

                return finalCell;
            }
            
            fixed4 frag(v2f i) : SV_Target{

                //Get a circle given the current coordinates of the uv.
                // 
                //Make a circle at its center. This circle is a gradient from -1 to 1.
                //float2 center = i.uv - float2(0.5, 0.5);
                //float circle = sdCircle(center, 0.015);

                //Takes the circle and cuts it out for a clear shape, smoothing the edges.
                //float smoothResult = smoothstep(0, 0.002, circle);

                //Next we make a clustering function. This function will create many different circles in a pattern.
                //float horiCluster = LineCluster(i.uv.y, 0.5, 10);

                //Take the cluser and create a grid of points.
                //float grid = PointGrid(i.uv);

                //The grid has been set. If this point is part of the grid create a circle, else destroy it.
                //animated UVs
                float2 uv = i.uv;//i.screenPosition.xy / i.screenPosition.w;
				uv += float2(_Speed *_Time.y, _Speed * _Time.y);
                uv = fmod(uv, 0.1);//float2((uv.x % 1.001), (uv.y % 1.001));
                
                float2 positionalPoint = ClosetCell(uv);
                float2 circlePos = uv - positionalPoint;
                
                float dist = distance(i.uv, 0.1*float2(cos(0.5*_Time.y),sin(0.5*_Time.y)) + float2(0.5, 0.5));

                float x = dist;// the input value
                float result = sin(2.5 * (1.0 - (1.0 / sqrt(1.0 + pow(x, 3.0))))) + 3 
                    / (cos(10*sin(0.5*_Time.y) + 260.0 * (1.0 - (1.0 / sqrt(1.0 + pow(x, 1.0))))) + 1.0);
                
                float circleDistort = 4*smoothstep(0, 1, distance(i.uv, ClosetLineCell(i.uv) ));
                float radiusIntensity = 25.0 * result * circleDistort;
                
				float circleResult = sdCircle(circlePos, _Radius* radiusIntensity);
				float circleGrid = smoothstep(0, 0.0025, circleResult);


                //Use different SDFs.
                float di = 1.2 * cos(_Time.y + 3.9);
                float sdfResult = sdStar5(circlePos, _Radius * radiusIntensity, 2.0);//sdHeart(circlePos, _Radius * radiusIntensity);
                float sdfGrid = smoothstep(0, 0.0025, sdfResult);

                float finalMask = lerp(sdfGrid, circleGrid, 0.0000000045 * radiusIntensity);
                finalMask = max(step(1, 1 - finalMask), 0);
                
                //Get animated UVs again.
                float3 flowDirection = _SurfaceNoiseScroll.xyz * _SurfaceNoiseScroll.w;
                float3 noiseUV = float3(i.uv.x + _Time.y * flowDirection.x, i.uv.y + _Time.y 
                    * flowDirection.y, i.uv.y + _Time.y * flowDirection.z);



                float2 textureCoordinate = i.uv;

                float XUV = textureCoordinate.x * _RefracStr.x + _Time.y * _RefracStr.y;
                float YUV = textureCoordinate.y * _RefracStr.x + _Time.y * _RefracStr.y;
                float2 screenPosUV = textureCoordinate;
                screenPosUV.y += cos(XUV + YUV) * _RefracStr.z * cos(YUV);
                screenPosUV.x += sin(XUV - YUV) * _RefracStr.z * sin(YUV);

                //Create paintery effect for that under the water.
                float2 uv2 = i.screenPosition.xy / i.screenPosition.w;
                float3 diplacementNormals = UnpackNormal(tex2D(_DisplacementTex, screenPosUV));
                float2 distortion = uv2 + ((_Intensity * 0.01) * diplacementNormals.rg);

                float2 backgroundUVs = (screenPosUV + noiseUV) * 0.75;
                //Now add color
                //Get the main texture.
                fixed4 tex = tex2D(_BackgroundTexture, backgroundUVs);
                tex.b *= 1.5;
                float4 ColorMask = vec4(finalMask);
                ColorMask *= _MaskColor;//1.75*_MaskColor * float4(i.uv, i.uv.x, 1);
                
                float4 finalCol = lerp(tex, ColorMask, max(ColorMask.r, max(ColorMask.b, ColorMask.g) ) );
                finalCol.rgb *= 0.97;
                

                float4 col = 0;
                float2 posUV = textureCoordinate;
                float cutoff = tex2D(_TransitionTexture, ((screenPosUV - 0.5) * (_Transition * 50) + 0.5)).r;//sdHeart(posUV, _Transition);
                
                col = lerp(_Color, finalCol, step(cutoff, 0));
                col = lerp(finalCol, col, step(_Transition, 0.999));
                
                return col;
            }
                
            ENDCG
        }
    }
}
