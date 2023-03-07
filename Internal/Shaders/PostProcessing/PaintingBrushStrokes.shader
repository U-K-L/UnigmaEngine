Shader "UnigmaPP/PaintingBrushStrokes"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Normal]_DisplacementTex("Displacement Map", 2D) = "white"{}
        _Intensity ("Intensity of displacement", Range(0, 2)) = 1
        _Scale ("Scale of texture", Range(0,2)) = 1
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

            uniform sampler2D _StrokesMap; //The render texture of THIS Camera/command buffer.
            sampler2D_half _CameraMotionVectorsTexture;
            uniform sampler2D _NormalMap;
            float3 _FarCorner;
            float4x4 _CamToWorld;
            float _Scale;

            struct vertexInput
            {
                float4 vertex : POSITION;
                float3 texCoord : TEXCOORD0;
            };

            struct vertexOutput
            {
                float4 pos : SV_POSITION;
                float3 texCoord : TEXCOORD0;
                float linearDepth : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
            };

            vertexOutput vert(vertexInput input)
            {
                vertexOutput output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.texCoord = input.texCoord;

                output.screenPos = ComputeScreenPos(output.pos);
                output.linearDepth = -(UnityObjectToViewPos(input.vertex).z * _ProjectionParams.w);

                return output;
            }

            sampler2D _MainTex;
            sampler2D _DisplacementTex;
            float _Intensity;
            sampler2D_float _CameraDepthTexture;


            fixed4 frag(vertexOutput input) : SV_Target
            {
                float2 uv = input.screenPos.xy / input.screenPos.w;
                float camDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv); //Main camera depth texture.
                //camDepth = Linear01Depth(camDepth);

                float ThisCamDepth = input.linearDepth;
                float diff = saturate(ThisCamDepth - camDepth); //input.linearDepth is our depth.



                float2 scaledUV = uv * _Scale;
                float3 diplacementNormals = UnpackNormal(tex2D(_DisplacementTex, scaledUV) );
                float2 distortion = uv + ((_Intensity*0.01) * diplacementNormals.rg);
                fixed4 image = tex2D(_StrokesMap, uv);
                //float gradient = image.b;
                //float RedChange = fwidth(gradient)*100000;
                //if(RedChange < 0.00000001 && image.b > 0.999)
                //    return tex2D(_MainTex, distortion);
                if(image.g > 0.001)
                    return tex2D(_MainTex, distortion);
                
                fixed4 col = tex2D(_MainTex, uv);
                fixed4 distoredCol = tex2D(_MainTex, distortion);
                float4 finalColor = lerp(col, distoredCol, image.g);
                return col;
            }
            ENDCG
        }
    }
}
