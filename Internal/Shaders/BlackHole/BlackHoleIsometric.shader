Shader "Unigma/BlackHoleIsometric"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		[Normal]_Noise("Noise", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_DistortionPower("Distortion amount", Range(0,10)) = 1
		_DistortionSpeed("Speed", Range(0,100)) = 1
    }
    SubShader
    {
		Tags { "Queue" = "Transparent" }
        LOD 100

		GrabPass
		{
			"_GrabPassBlackHole"
		}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(3)
                float4 vertex : SV_POSITION;
				float4 grabPassUV : TEXCOORD1;
				float2 distortionUV : TEXCOORD2;
				float4 screenPosition : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			sampler2D _GrabPassBlackHole;
			sampler2D _Noise;
			float4 _Noise_ST;
			float4 _Color;
			float _DistortionPower;
			float _DistortionSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.grabPassUV = ComputeGrabScreenPos(o.vertex);
				o.distortionUV = TRANSFORM_TEX(v.uv, _Noise);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.screenPosition = ComputeScreenPos(o.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

			//Creates a twirling effect for texture.
			float2 Twirl(float2 UV, float2 Center, float Strength, float2 Offsets, float speed)
			{
				Center += (Strength * 0.5);
				float2 delta = (UV*Strength) - Center;
				float angle = (_Time.y*speed)+Strength * length(delta);
				float x = cos(angle) * delta.x - sin(angle) * delta.y;
				float y = sin(angle) * delta.x + cos(angle) * delta.y;
				return float2(x + Center.x + Offsets.x, y + Center.y + Offsets.y);
			}

			//Deprecated...Gets rotation of UV.
			float2 DirectionalFlowUV(float2 uv, float2 flowVector, float time) {
				float2 dir = normalize(flowVector.xy);
				uv = mul(float2x2(dir.y, dir.x, -dir.x, dir.y), uv);
				uv.y -= time;
				return uv;
			}

            fixed4 frag (v2f i) : SV_Target
            {
				//Twirl the UV Map.
				float2 twirl = Twirl(i.uv, float2(0, 0), _DistortionPower, float2(0,0), _DistortionSpeed);
				//Create noise texture and twirl the noise.
				float2 distortion = UnpackNormal(tex2D(_Noise, twirl)).xy;

				//Get the main texture so effect slowly fades out.
				fixed4 col = tex2D(_MainTex, i.uv); 
				distortion *= _DistortionPower*col;

				//Get the UV for the background (grabpass) and distort with the twirl.
				i.grabPassUV.xy += distortion * i.grabPassUV.z;
				
				float2 screenPosUV = i.screenPosition.xy/i.screenPosition.w;
				screenPosUV.xy += distortion*0.01;
				float4 grab = tex2D(_GrabPassBlackHole, screenPosUV)+_Color;

                return grab;
            }
            ENDCG
        }
    }
}
