#define UnigmaToonSpecular


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
    float3 normal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    float4 screenSpace : TEXCOORD3;
    float3 viewDir : TEXCOORD4;
    float3 T : TEXCOORD5;
    float3 B : TEXCOORD6;
    float3 N : TEXCOORD7;
};

sampler2D _UnigmaGlobalIllumination;
sampler2D _MainTex;
float4 _MainTex_ST;
float4 _MainColor;
float4 _Shadow;
float4 _Highlight;
float4 _Thresholds;
float _Glossiness;
float4 _SpecularColor;
float4 _RimColor;
float _RimAmount;
float _RimThreshold;
float _UseRim;
float _RimControl;
float _Smoothness;

//Specular Pass.
v2f ToonSpecularVert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;

    o.screenSpace = ComputeScreenPos(o.vertex);
    o.viewDir = WorldSpaceViewDir(v.vertex);

    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.normal = UnityObjectToWorldNormal(v.normal);


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

fixed4 ToonSpecularFrag(v2f i) : SV_Target
{
                //Get main texture
    float4 col = tex2D(_MainTex, i.uv);
                //Three colors shadow, midtone, highlight.
                //Each of this colors are on different normals of a percieved box ...
                //The normals are NOT interpolated and it is a flat shading.
    float2 screenPos = i.screenSpace.xy / i.screenSpace.w;
                //screenPos = screenPos * 0.5 + 0.5;
    screenPos *= _ScreenParams.y / _ScreenParams.x;
    float4 globalIllum = tex2D(_UnigmaGlobalIllumination, screenPos);
    float4 normals = normalize(float4(i.normal, 1));
    float3 lightDirAbsolute = normalize(_WorldSpaceLightPos0.xyz);
    float3 lightDir = normalize(lightDirAbsolute);
    float lightIntensity = 1.0;

                //float3 normals = UnityObjectToWorldNormal(i.normal);

    float NdotL = dot(normals, lightDir);

    float4 midTones = _MainColor * step(_Thresholds.x, NdotL);
    float4 shadows = _Shadow * step(NdotL, _Thresholds.y);
    float4 highlights = _Highlight * step(_Thresholds.z, NdotL);

    float4 finalColor = max(midTones, shadows);
    finalColor = max(finalColor, highlights);

                //return _Midtone;
                //return col;
                //return globalIllum;



                
    float3 tangentNormal = tex2D(_MainTex, i.uv).xzy * 2 - 1;
    tangentNormal = normalize(tangentNormal * 2 - 1);
    float3x3 TBN = float3x3(normalize(i.T), normalize(i.B), normalize(i.N));
    TBN = transpose(TBN);
                //float3 normalMap = mul(TBN, tangentNormal);
    float3 RGBNormals = mul((float3x3) unity_ObjectToWorld, tangentNormal);

    float3 viewDir = normalize(i.viewDir);

    float3 halfVector = normalize(_WorldSpaceLightPos0 + viewDir);
    float NdotH = dot(i.normal, halfVector);

    float specularIntensity = pow(NdotH * lightIntensity, _Glossiness * _Glossiness);
    float specularIntensitySmooth = smoothstep(0.005, 0.05, specularIntensity);
    float4 specular = specularIntensitySmooth * _SpecularColor;
                
    float4 rimDot = _RimControl + 0.75 - dot(viewDir, i.normal) * _RimColor;
    float4 rimDotNormalMap = 0; //_RimControl - dot(viewDir, RGBNormals) * _RimColor;

    rimDot = max(rimDot, rimDotNormalMap);
    rimDot = max(rimDot, 0);

    float rimIntensity = rimDot * pow(NdotL, _RimThreshold);
    rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);

    if (_UseRim < 0.1)
        return 0;

    return float4((specular + rimIntensity + rimDot).xyz, _Smoothness);

}