#define UnigmaOutlineColorsPass


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

float4 _OutlineInnerColor, _OutlineColor;

v2f OutlineColorsVert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    UNITY_TRANSFER_FOG(o, o.vertex);
    return o;
}

fixed4 OutlineColorsFrag(v2f i) : SV_Target
{
    return _OutlineInnerColor;
}