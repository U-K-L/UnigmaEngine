#define UnigmaOutlineThicknessPass

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

float4 _OutlineColor, _ThicknessTexture_ST;

v2f OutlineThicknessVert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _ThicknessTexture);
    UNITY_TRANSFER_FOG(o, o.vertex);
    return o;
}

sampler2D _ThicknessTexture;

fixed4 OutlineThicknessFrag(v2f i) : SV_Target
{
    float4 texcol = tex2D(_ThicknessTexture, i.uv);
    float thickness = 1; //dot(texcol, texcol) / 3.0;
    float4 finalOutput = float4(_OutlineColor.xyz, thickness);
    return finalOutput * thickness;
}