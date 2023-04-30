Shader "Hidden/StarPatternShader"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
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
            float _Transition, _Intensity, _Spacing;
            float4 _SurfaceNoiseScroll, _RefracStr, _Color;
            float4 _MainTex_TexelSize;
            
            float _Radius;
            const int _Count;

            float sdCircle(float2 p, float r)
            {
                return length(p) - r;
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
				uv += float2(0.1*_Time.y, 0.1 * _Time.y);
                uv = fmod(uv, 0.1);//float2((uv.x % 1.001), (uv.y % 1.001));
                
                float2 positionalPoint = ClosetCell(uv);
                float2 circlePos = uv - positionalPoint;
                
                float dist = distance(i.uv, ClosetLineCell( i.uv ));

                float x = dist;// the input value
                float result = sin(2.5 * (1.0 - (1.0 / sqrt(1.0 + pow(x, 3.0))))) + 0.01555 * (cos(760.0 * (1.0 - (1.0 / sqrt(1.0 + pow(x, 3.0))))) + 1.0);
                
                float circleDistort = 40*smoothstep(0, 1, distance(i.uv, float2(0.5, 0.5)));
                float radiusIntensity = 5.0 * result / circleDistort;
                
				float circleResult = sdCircle(circlePos, _Radius* radiusIntensity);
				float circleGrid = smoothstep(0, 0.0025, circleResult);
                
                return vec4(circleGrid);
            }
                
            ENDCG
        }
    }
}
