#define UnigmaIDsPass

struct appdata
{
    float4 vertex : POSITION;
};

struct v2f
{
    float4 vertex : SV_POSITION;
};
int _ObjectID;

v2f IDsVert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    return o;
}

fixed4 IDsFrag(v2f i) : SV_Target
{
    return float4(rand(_ObjectID), rand(_ObjectID + 1), rand(_ObjectID + 2), 1);
}