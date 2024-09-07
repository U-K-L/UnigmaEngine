Shader "Unigma/UnigmaSprite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;

                /*
                float4x4 modelMatrix = unity_ObjectToWorld;
                float4x4 viewMatrix = UNITY_MATRIX_V;
                float4x4 projectionMatrix = UNITY_MATRIX_P;

                //Get the origin in view space and find the translation needed from world space.
                float4 origin = float4(0,0,0,1);
                float4 worldOrigin = mul(modelMatrix, origin);
                float4 viewOrigin = mul(viewMatrix, worldOrigin);

                float4 viewDiff = viewOrigin - worldOrigin;



                float3 right = float3(1,0,0);//normalize(viewMatrix._m00_m01_m02);
                float3 up = float3(0,1,0);//normalize(viewMatrix._m10_m11_m12);
                float3 forward = float3(0,0,1);//normalize(viewMatrix._m20_m21_m22);

                
                float4x4 rotationMatrix = float4x4(right, 0,
    	            up, 0,
    	            forward, 0,
    	            0, 0, 0, 1);

                //the inverse of a rotation matrix happens to always be the transpose
                float4x4 rotationMatrixInverse = transpose(rotationMatrix);
                */
                /*
                viewDiff = mul(rotationMatrixInverse, viewDiff);
                //Create the wanted view matrix.
                viewMatrix = float4x4(1,0,0, viewDiff.x,
                                      0,1,0, viewDiff.y,
                                      0,0,1, viewDiff.z,
                                      0,0,0, 1);
                                      */

                                      /*
                //Translate vertex by the view diff.
                float4 pos = v.vertex;
                //pos = mul(rotationMatrixInverse, pos);
                pos = mul(modelMatrix, pos);

                pos = mul(viewMatrix, pos);

                //pos = pos + viewDiff;

                //Project onto camera plane
                pos = mul(projectionMatrix, pos);
                */

                /*
                float4x4 m = unity_ObjectToWorld;
                float4x4 view = UNITY_MATRIX_V;
                float4x4 p = UNITY_MATRIX_P;

      
                //view._m00_m01_m02 = float3(1, 0, 0);
                //view._m10_m11_m12 = float3(0, 1, 0);
                //view._m20_m21_m22 = float3(0, 0, 1);

                //break out the axis
                //float3 right = normalize(m._m00_m01_m02);
                //float3 up = normalize(m._m10_m11_m12);
                //float3 forward = normalize(m._m20_m21_m22);
                //float3 right = float3(1,0,0);
                float3 up = float3(0,1,0);
                //float3 forward = float3(0,0,1);
                float3 right = normalize(view._m00_m01_m02);
                //float3 up = normalize(view._m10_m11_m12);
                float3 forward = normalize(view._m20_m21_m22);
                float4x4 rotationMatrix = float4x4(right, 0,
    	            up, 0,
    	            forward, 0,
    	            0, 0, 0, 1);

                //the inverse of a rotation matrix happens to always be the transpose
                float4x4 rotationMatrixInverse = transpose(rotationMatrix);

                //apply the rotationMatrixInverse, model, view and projection matrix
                float4 pos = v.vertex;
                //pos = mul(rotationMatrixInverse, pos);
                pos = mul(m, pos);
                pos = mul(view, pos);
                pos = mul(p, pos);


                pos = mul(UNITY_MATRIX_P, 
              mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0))
              + float4(v.vertex.x, v.vertex.y, 0.0, 0.0)
              * float4(1, 1, 1.0, 1.0));
              */

                float4 pos = UnityObjectToClipPos(v.vertex);
                o.vertex = pos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
