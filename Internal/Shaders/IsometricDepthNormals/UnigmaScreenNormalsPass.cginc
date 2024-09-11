#define UnigmaScreenNormalsPass

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float3 tangent : TANGENT;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float depthGen : TEXCOORD1;
    float3 normal : TEXCOORD2;
    float3 T : TEXCOORD3;
    float3 B : TEXCOORD4;
    float3 N : TEXCOORD5;
    float3 worldNormal : TEXCOORD6;
};

sampler2D _MainTex, _UnigmaNormal, _NormalMap;
float4 _MainTex_ST;
float _Fade, _NormalAmount, _DepthAmount;
int _StencilRef;

v2f ScreenNormalsVert(appdata v)
{
    v2f o;
    float4 vertexProgjPos = mul(UNITY_MATRIX_MV, v.vertex);
    o.depthGen = saturate((-vertexProgjPos.z - _ProjectionParams.y) / (_Fade + 0.001));
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.normal = v.normal; //UnityObjectToWorldNormal(v.normal);
    o.worldNormal = UnityObjectToWorldNormal(v.normal);

    float3 worldNormal = mul((float3x3) unity_ObjectToWorld, v.normal);
    float3 worldTangent = mul((float3x3) unity_ObjectToWorld, v.tangent);

    float3 binormal = cross(v.normal, v.tangent.xyz); // *input.tangent.w;
    float3 worldBinormal = mul((float3x3) unity_ObjectToWorld, binormal);

                // and, set them
    o.N = normalize(worldNormal);
    o.T = normalize(worldTangent);
    o.B = normalize(worldBinormal);
    return o;
}

            //Make entire shader a shader pass in the future.
fixed4 ScreenNormalsFrag(v2f i) : SV_Target
{
    //Get texture data
    return float4(i.worldNormal, 1.0);
    float3 tangentNormal = tex2D(_NormalMap, i.uv).xyz;
    float3 rgbNormalMap = tangentNormal.xzy * 2 - 1;
    rgbNormalMap = UnityObjectToWorldNormal(rgbNormalMap);
    return float4(rgbNormalMap, 1);
    return float4(i.worldNormal, 1.0);
    tangentNormal = normalize(tangentNormal * 2 - 1);
    float3x3 TBN = float3x3(normalize(i.T), normalize(i.B), normalize(i.N));
    TBN = transpose(TBN);
    float3 worldNormal = mul(TBN, tangentNormal);
    if (tex2D(_NormalMap, i.uv).w <= 0)
        return float4(i.worldNormal, 1.0);

    return float4(worldNormal, 1);
}