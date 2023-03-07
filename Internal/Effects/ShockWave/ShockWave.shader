Shader "Hidden/ShockWave"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		[Header(Wave)]
		_WaveDistance("Distance from player", float) = 10
		_WaveTrail("Length of trail", Range(0,10)) = 1
		_WaveColor("Color", Color) = (1,1,1,1)
		[Header(Distortion)]
		_Noise("Texture", 2D) = "white" {}
		_Strength("Distortion Strength", Range(0,10)) = 1

		_CenterX("CenterX", Range(-1,2)) = 0.5
		_CenterY("CenterY", Range(-1,2)) = 0.5
		_Radius("Radius", Range(-1,1)) = 0.2
		_Amplitude("Amplitude", Range(-10,10)) = 0.05

		_Center("Object Position", Vector) = (1,1,1,1)
		_MaxSize("Maximum Circle Size", Range(0,10000)) = 1
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
			#include "HLSLSupport.cginc"

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
				o.screenPosition = ComputeScreenPos(o.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			sampler2D _CameraDepthNormalsTexture;
			sampler2D _CameraDepthTexture;
			sampler2D _Noise;

			//Wave properties.
			float _WaveDistance;
			float _WaveTrail;
			float _Strength;
			float4 _WaveColor;

			float _CenterX;
			float _CenterY;
			float _Radius;
			float _Amplitude;
			float _MaxSize;
			float4 _Center;
			float4x4 unity_ViewToWorldMatrix;
			float4x4 unity_InverseProjectionMatrix;

			float distancePoint(float3 pos, float3 source) {

				float3 v = pos - source;
				float distance = sqrt(dot(v, v));
				distance = distance / length(pos+source);
				distance = saturate(distance);
				return distance;
			}

			float3 GetWorldFromViewPosition(v2f i) {

				float2 screenPos = i.screenPosition.xy / i.screenPosition.w;
				float z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenPos).r;
				float4 result = mul(unity_InverseProjectionMatrix, float4(2 * screenPos - 1.0, z, 1.0));
				float3 viewPos = result.xyz / result.w;

				//Get world space.
				float3 worldPos = mul(unity_ViewToWorldMatrix, float4(viewPos, 1.0));
				return worldPos;
			}

            fixed4 frag (v2f i) : SV_Target
            {

				//Calculate depth.
				
				float depth = tex2D(_CameraDepthTexture, i.uv).r;


				depth = Linear01Depth(depth);
				depth = depth * (_ProjectionParams.z*25);
				depth *= 0.025; //Scales the depth so that lighter line weights closer.
				//Calculate depth and normals.
				float4 depthnormal = tex2D(_CameraDepthNormalsTexture, i.uv);
				float normalDepth;
				float3 normal;

				//decode depthnormal
				DecodeDepthNormal(depthnormal, normalDepth, normal);

				//Calculate Wave.
				float waveFront = step(depth, _WaveDistance);
				float waveTrail = smoothstep(_WaveDistance - _WaveTrail, _WaveDistance, depth);
				//Multiply the two which go opposite direction. Each 0 will cancel out causing a line.
				float wave = waveFront * waveTrail;
				//fixed4 col = lerp(source, _WaveColor, wave);
				

				// get worldSpace position of pixel.
				float3 worldPos = GetWorldFromViewPosition(i);
				//Create circle in worldspace.
				float circleDist = distance(_Center, worldPos);
				// clamp radius so we don't get weird artifacts
				float effectRadius = clamp(_WaveDistance, 0, _MaxSize);

				float blend = circleDist <= effectRadius ? 0 : 1;
				float blendBack = smoothstep(circleDist-3, effectRadius, 1);
				blendBack = (1 - blendBack);
				wave = blend*blendBack;

				//Distort
				float2 diff = float2(i.uv.x - _CenterX, i.uv.y - _CenterY);
				float dist = sqrt(diff.x*diff.x + diff.y*diff.y);


				float2 uv_displaced = float2(i.uv.x, i.uv.y);
				float wavesize = 1;
				_Radius *= _WaveDistance;
				float angle = (dist - _Radius) * 2 * 3.141592654 / wavesize;
				float cossin = (1 - cos(angle))*0.5;
				uv_displaced.x -= cossin * diff.x*_Amplitude;
				uv_displaced.y -= cossin * diff.y*_Amplitude;
				fixed4 distortCol = tex2D(_MainTex, uv_displaced); //Get the orginal rendered color
				


				//Get main texture.
				fixed4 source = tex2D(_MainTex, i.uv);


				//Send off to final render.

				fixed4 col = lerp(source, distortCol*_WaveColor, wave);
				//return distPoint*col;





                return col;
            }
            ENDCG
        }
    }
}
