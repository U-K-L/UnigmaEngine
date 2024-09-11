#define UnigmaToonAlbedo

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float4 color : COLOR;

};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float3 normal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    float4 screenSpace : TEXCOORD3;
    float3 viewDir : TEXCOORD4;
    float4 color : COLOR0;
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
float _ColorDistModel;
float _Emmittance;


//Albedo Pass.
v2f ToonAlbedoVert (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)).xyz;

    o.screenSpace = ComputeScreenPos(o.vertex);
    o.viewDir = WorldSpaceViewDir(v.vertex);

    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.color = v.color;
    return o;
}

fixed4 ToonAlbedoFrag(v2f i) : SV_Target
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
                
    float4 finalColor = 1;
#ifdef _COLORDISTMODEL_CELSHADED
    float4 xzCol = _Shadow * step(_Thresholds.x, abs(normals).r);
    float4 zxCol = _MainColor * step(_Thresholds.z, abs(normals).b);
    float4 zyCol = _Highlight * step(_Thresholds.z, abs(normals).g);
    finalColor = zyCol + xzCol + zxCol;
#elif _COLORDISTMODEL_TOONSHADED
    float4 midTones = _MainColor * step(_Thresholds.x, NdotL);
    float4 shadows = _Shadow * step(NdotL, _Thresholds.y);
    float4 highlights = _Highlight * step(_Thresholds.z, NdotL);

    finalColor = max(midTones, shadows);
    finalColor = max(finalColor, highlights);

#elif _COLORDISTMODEL_DISTSHADED
	finalColor = _MainColor * _Emmittance*10;
    //float3 objectOrigin = mul(unity_ObjectToWorld, float4(0,0,0, 1)).xyz;
    //finalColor = pow(distance(objectOrigin, i.worldPos), 1);
#endif

    //return _Midtone;
    //return col;
    //return globalIllum;

                
    float3 viewDir = normalize(i.viewDir);

    float3 halfVector = normalize(_WorldSpaceLightPos0 + viewDir);
    float NdotH = dot(i.normal, halfVector);

    float specularIntensity = pow(NdotH * lightIntensity, _Glossiness * _Glossiness);
    float specularIntensitySmooth = smoothstep(0.005, 0.05, specularIntensity);
    float4 specular = specularIntensitySmooth * _SpecularColor;

    float4 rimDot = 1 - dot(viewDir, i.normal) * _RimColor;

    float rimIntensity = rimDot * pow(NdotL, _RimThreshold);
    rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);
                
    //if (_UseRim < 0.1)
    //    return finalColor;
    
    return finalColor;
                
}