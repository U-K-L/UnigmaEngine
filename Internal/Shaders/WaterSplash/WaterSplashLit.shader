// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Unigma/WaterSplashLit"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white"{}
        _Color("Color", Color) = (1,1,1,1)
        _ColorRamp ("Color Ramp", Color) = (1,1,1,1)
        [Normal]_Noise("Wave Noise", 2D) = "white" {}
        [Normal]_EmissionNoise("Specular Noise", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        _CellSize("Cell Size", Range(0,200)) = 2
        _FCellSize("Cell Size", Range(0,200)) = 7
        _VSmoothness("Voronoi Smoothness", Range(0,100)) = 1
        _Angle("Angle", Range(0,360)) = 0
        _Scale("Scale", Vector) = (1,1,1,1)
        _Speed("Speed", Range(0, 10)) = 1
        _RingSpeed("Ring Speed", Range(0, 10)) = 1
        _CircleOffsets("Circle Offsets", Vector) = (0,0,0,0)
        _Rings("Ring Amount", Range(0,20)) = 4
        _CutOff("Rings Thinness", Range(0,1)) = 0.99 
        _DistortionPower("Distortion amount", Range(0,10)) = 1
        _DistortionSpeed("Distortion Speed", Range(0,100)) = 1

    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-1" }
        LOD 200
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard alpha fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0


        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        float pi = 3.141592653589793238462;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _ColorRamp;
        float _CellSize, _FCellSize;
        float _VSmoothness;
        float _Angle;
        float4 _Scale;
        float _Speed;
        float _RingSpeed;
        fixed4 _CircleOffsets;
        float _Rings;
        float _CutOff;
        float _DistortionPower;
        float _DistortionSpeed;
        sampler2D _Noise;
        sampler2D _EmissionNoise;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float rand2dTo1d(float2 value, float2 dotDir = float2(12.9898, 78.233)) {
            float2 smallValue = sin(value);
            float random = dot(smallValue, dotDir);
            random = frac(sin(random) * 143758.5453);
            return random;
        }

        float2 rand2dTo2d(float2 value) {
            return float2(
                rand2dTo1d(value, float2(12.989, 78.233)),
                rand2dTo1d(value, float2(39.346, 11.135))
                );
        }

        float2 voronoiNoise(float2 value) {

            //From ronja Tutorials. Gets the distance of each point to generate voronoi Noise.
            float2 baseCell = floor(value);
            float minDistToCell = 10;
            float2 closestCell;
            [unroll]
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    float2 cell = baseCell + float2(x, y);
                    float2 cellPosition = cell + rand2dTo2d(cell);
                    float2 toCell = cellPosition - value;
                    float distToCell = length(toCell);
                    if (distToCell < minDistToCell) {
                        minDistToCell = distToCell;
                        closestCell = cell;
                    }

                }
            }
            float random = rand2dTo1d(closestCell);
            return float2(minDistToCell, random);
        }

        float2 unity_voronoi_noise_randomVector(float2 UV, float offset)
        {
            float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
            UV = frac(sin(mul(UV, m)) * 46839.32);
            return float2(sin(UV.y * +offset) * 0.5 + 0.5, cos(UV.x * offset) * 0.5 + 0.5);
        }

        void Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
        {
            float2 g = floor(UV * CellDensity);
            float2 f = frac(UV * CellDensity);
            float t = 8.0;
            float3 res = float3(8.0, 0.0, 0.0);

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    //Find the nearest point to color the grid.
                    float2 lattice = float2(x, y); //The point.
                    float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset); //Randomly generated point.
                    float d = distance(lattice + offset, f); //The distance between them.
                    if (d < res.x)
                    {
                        res = float3(d, offset.x, offset.y);
                        Out = res.x;
                        Cells = res.y;
                    }
                }
            }
        }

        //Smooth Version
        void F1Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells, float smoothness)
        {
            float2 g = floor(UV * CellDensity);
            float2 f = frac(UV * CellDensity);
            float t = 8.0;
            float3 res = float3(8.0, 0.0, 0.0);
            float resX = 0.0;

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    //Find the nearest point to color the grid.
                    float2 lattice = float2(x, y); //The point.
                    float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset); //Randomly generated point.
                    float d = distance(lattice + offset, f); //The distance between them.

                    resX += 1.0 / pow(d, _VSmoothness);
                    if (d < res.x)
                    {
                        res = float3(d, offset.x, offset.y);
                        Out = pow(1.0 / resX, 1.0 / 16.0);
                        Cells = res.y;
                    }
                }
            }
        }

        //Returns the sawtooth function.
        float sawTooth(float speed, float t) {
            float cot = 1 / tan((speed*t * pi) / 2);
            float s = -(1 / pi) * atan(cot) + 0.5;
            return s;
        }

        float2 animateUVs(float2 uv, float speed) {
            
            //uv *= -abs(sin(_Time.r * speed));
            //uv *= sawTooth(speed, _Time.r);
            float progress = frac((2+((3+_Time.r)%7)) *speed*0.0001);
            return uv*(progress*10000);
        }

        //Creates a twirling effect for texture.
        float2 Twirl(float2 UV, float2 Center, float Strength, float2 Offsets, float speed)
        {
            Center += (Strength * 0.5);
            float2 delta = (UV * Strength) - Center;
            float angle = (_Time.y * speed) + Strength * length(delta);
            float x = cos(angle) * delta.x - sin(angle) * delta.y;
            float y = sin(angle) * delta.x + cos(angle) * delta.y;
            return float2(x + Center.x + Offsets.x, y + Center.y + Offsets.y);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float Cells = 100;
            float SmoothOut = -1;
            float Out = -1;
            float4 text = tex2D(_MainTex, IN.uv_MainTex);
            float4 output = _Color;
            float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;

            float2 animatedUV = animateUVs(_Scale.xy, _Speed);
            IN.uv_MainTex.y += animatedUV.y;

            F1Unity_Voronoi_float(IN.uv_MainTex + _Scale.xy, 0, _FCellSize, SmoothOut, Cells, _VSmoothness);
            Unity_Voronoi_float(IN.uv_MainTex + _Scale.xy, _Angle, _CellSize, Out, Cells);

            //float2 value = IN.worldPos.xz / _CellSize;
            //float noise = voronoiNoise(value).x;
            float colorRamp = Out-SmoothOut;
            if (colorRamp > _ColorRamp.r) {
                output = 1.5;
            }
            float radius = _CircleOffsets.y;
            float diff = abs((1 - abs(distance(localPos.xz* radius, _CircleOffsets.xz* radius))));

            //Twirl the UV Map.
            float2 twirl = Twirl(IN.uv_MainTex, float2(0, 0), _DistortionPower, float2(0, 0), _DistortionSpeed);
            float2 distortion = UnpackNormal(tex2D(_Noise, twirl)).xy;

            float xdist = _CircleOffsets.x - IN.uv_MainTex.x;// + distortion.x;
            float ydist = _CircleOffsets.y - IN.uv_MainTex.y;// + distortion.y;
            float distanceToCenter = (xdist * xdist + ydist * ydist) * (_Rings/10);
            float time = _Time.r * _Speed;
            float waves = max(0, sin(distanceToCenter + time*_RingSpeed));


            waves *= step(_CutOff, waves); //Gets the cuttoff waves how thin it is.
            
            output.xyz += waves*5;
            o.Albedo = output;


            o.Alpha = (output.a+1) * diff; //output.a* (max(0,saturate(distanceToCenter)));//output.a;


            //Emissions:
            twirl = Twirl(IN.uv_MainTex, float2(0, 0), _DistortionPower, float2(0, 0), _DistortionSpeed);
            distortion = UnpackNormal(tex2D(_EmissionNoise, twirl)).xy;

            xdist = _CircleOffsets.x+ distortion.x;
            ydist = _CircleOffsets.y - IN.uv_MainTex.y + distortion.y;
            distanceToCenter = distortion.x*(xdist * xdist + ydist * ydist) * (_Rings / 10);
            //Controls rings
            waves = max(0, sin(distanceToCenter*0.0125));

            waves *= 4*step(0.999, waves); //Gets the cuttoff waves how thin it is.

            o.Emission = waves*text.r;
            o.Alpha = text.r*1.15;
        }
        ENDCG
    }
    FallBack "Standard"
}
