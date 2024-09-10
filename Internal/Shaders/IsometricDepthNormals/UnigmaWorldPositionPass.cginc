#define UnigmaWorldPositionPass

struct appdata
{
    float4 vertex : POSITION;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float3 worldPos : TEXCOORD3;
};


v2f worldPosVert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    return o;
}

fixed4 worldPosFrag(v2f i) : SV_Target
{
    float4 finalColor = float4(i.worldPos, 1.0);
    return finalColor;
}