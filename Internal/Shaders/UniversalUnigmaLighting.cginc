// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


#define UnigmaLightingFunction
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"
#include "Lighting.cginc"
#include "UnityCG.cginc"

float4 _Tint, _SpecularTint, _MainTex_ST, _Scale, _DetailTex_ST, _FresnelColor, _ShakingSpeed, _ShakingAmount;
float4 _AmbientColor;
float4 _RimColor;
float _RimAmount, _FresnelExponent;
float _RimThreshold;
sampler2D _MainTex, _HeightMap, _NormalMap, _DetailTex, _TopText, _TopNormal, _Noise, _GlassBackground, _DisplacementTex;
float _Smoothness, _Metallic, _Anisotropic, _Ior, _LightSteps, _ShadowStrength, _Brightness, _SpecularIntensity, _BumpScale, _BlendSmooth, _NoiseScale, _Spread, _EdgeWidth, _Intensity;

float _Glossiness;
float4 _SpecularColor, _HeightMap_TexelSize;

float _NormalDistModel;
float _GeoShadowModel;
float _FresnelModel;
float _UnityLightingContribution, _NdotLThreshold, _ShakingOn;

struct VertexData {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
	float4 tangent : TANGENT;
};


struct Interpolators {
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 localPosition : TEXCOORD1;
	float3 normal : TEXCOORD2;
	float3 localNormal : TEXCOORD3;
	float3 worldPos : TEXCOORD4;
	float4 tangent : TEXCOORD5;
	float3 bitangent : TEXCOORD6;
	float3 viewDir : TEXCOORD7;
	float3 binormal : TEXCOORD8;
	float2 uv_BumpMap : TEXCOORD9;
	float4 screenPosition : TEXCOORD13;	
	//float3 TtoW0 : TEXCOORD13;
	//float3 TtoW1 : TEXCOORD14;
	//float3 TtoW2 : TEXCOORD15;
	UNITY_FOG_COORDS(10) //initialize fog.
		LIGHTING_COORDS(11, 12) //initalize light and shadow.
	//SHADOW_COORDS(13)
#if defined(VERTEXLIGHT_ON)
		float3 vertexLightColor : TEXCOORD16;
#endif
};

float random(float2 uv)
{
	return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
}

float wglnoise_mod(float x, float y)
{
	return x - y * floor(x / y);
}

float2 wglnoise_mod(float2 x, float2 y)
{
	return x - y * floor(x / y);
}

float3 wglnoise_mod(float3 x, float3 y)
{
	return x - y * floor(x / y);
}

float4 wglnoise_mod(float4 x, float4 y)
{
	return x - y * floor(x / y);
}

float2 wglnoise_fade(float2 t)
{
	return t * t * t * (t * (t * 6 - 15) + 10);
}

float3 wglnoise_fade(float3 t)
{
	return t * t * t * (t * (t * 6 - 15) + 10);
}

float wglnoise_mod289(float x)
{
	return x - floor(x / 289) * 289;
}

float2 wglnoise_mod289(float2 x)
{
	return x - floor(x / 289) * 289;
}

float3 wglnoise_mod289(float3 x)
{
	return x - floor(x / 289) * 289;
}

float4 wglnoise_mod289(float4 x)
{
	return x - floor(x / 289) * 289;
}

float3 wglnoise_permute(float3 x)
{
	return wglnoise_mod289((x * 34 + 1) * x);
}

float4 wglnoise_permute(float4 x)
{
	return wglnoise_mod289((x * 34 + 1) * x);
}

float4 SimplexNoiseGrad(float3 v)
{
	// First corner
	float3 i = floor(v + dot(v, 1.0 / 3));
	float3 x0 = v - i + dot(i, 1.0 / 6);

	// Other corners
	float3 g = x0.yzx <= x0.xyz;
	float3 l = 1 - g;
	float3 i1 = min(g.xyz, l.zxy);
	float3 i2 = max(g.xyz, l.zxy);

	float3 x1 = x0 - i1 + 1.0 / 6;
	float3 x2 = x0 - i2 + 1.0 / 3;
	float3 x3 = x0 - 0.5;

	// Permutations
	i = wglnoise_mod289(i); // Avoid truncation effects in permutation
	float4 p = wglnoise_permute(i.z + float4(0, i1.z, i2.z, 1));
	p = wglnoise_permute(p + i.y + float4(0, i1.y, i2.y, 1));
	p = wglnoise_permute(p + i.x + float4(0, i1.x, i2.x, 1));

	// Gradients: 7x7 points over a square, mapped onto an octahedron.
	// The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
	float4 gx = lerp(-1, 1, frac(floor(p / 7) / 7));
	float4 gy = lerp(-1, 1, frac(floor(p % 7) / 7));
	float4 gz = 1 - abs(gx) - abs(gy);

	bool4 zn = gz < -0.01;
	gx += zn * (gx < -0.01 ? 1 : -1);
	gy += zn * (gy < -0.01 ? 1 : -1);

	float3 g0 = normalize(float3(gx.x, gy.x, gz.x));
	float3 g1 = normalize(float3(gx.y, gy.y, gz.y));
	float3 g2 = normalize(float3(gx.z, gy.z, gz.z));
	float3 g3 = normalize(float3(gx.w, gy.w, gz.w));

	// Compute noise and gradient at P
	float4 m = float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3));
	float4 px = float4(dot(g0, x0), dot(g1, x1), dot(g2, x2), dot(g3, x3));

	m = max(0.5 - m, 0);
	float4 m3 = m * m * m;
	float4 m4 = m * m3;

	float4 temp = -8 * m3 * px;
	float3 grad = m4.x * g0 + temp.x * x0 +
		m4.y * g1 + temp.y * x1 +
		m4.z * g2 + temp.z * x2 +
		m4.w * g3 + temp.w * x3;

	return 107 * float4(grad, dot(m4, px));
}

float SimplexNoise(float3 v)
{
	return SimplexNoiseGrad(v).w;
}

float4 ToFloat4(float3 v)
{
    return float4(v, 1);
}

//Iquilezles, raises value slightly, m = threshold (anything above m stays unchanged).
// n = value when 0
// x = value input.
float almostIdentity(float x, float m, float n) {
	if (x > m) return x;
	const float a = 2.0 * n - m;
	const float b = 2.0 * m - 3.0 * n;
	const float t = x / m;
	return (a * t + b) * t * t + n;
}

float3 CreateBinormal(float3 normal, float3 tangent, float binormalSign) {
	return cross(normal, tangent.xyz) * (binormalSign * unity_WorldTransformParams.w);
}


float3 WorldNormalVector(Interpolators data, float3 normal)
{
    return 0; //fixed3(dot(data.TtoW0, normal), dot(data.TtoW1, normal), dot(data.TtoW2, normal));
}

float4 RandomPosition(float4 vertex)
{
	float trauma = sin(_Time.z);
	float3 randXYZ = SimplexNoise(vertex + _Time.xyz* _ShakingSpeed);
	float2 rand = random(_Time.xy);
	float2 rand2 = random(_Time.zw);
	float4 position = float4(randXYZ.x, randXYZ.y, randXYZ.z, 0) * _ShakingAmount * trauma * step(-0.25, trauma);

	return position;
}

Interpolators ComputeVertexLightColor(Interpolators i)
{
	#if defined(VERTEXLIGHT_ON)
		i.vertexLightColor = Shade4PointLights(
			unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
			unity_LightColor[0].rgb, unity_LightColor[1].rgb,
			unity_LightColor[2].rgb, unity_LightColor[3].rgb,
			unity_4LightAtten0, i.worldPos, i.normal
		);
	#endif
    return i;
}
#define TANGENT_SPACE_ROTATION \
float3 binormal = cross( v.normal, v.tangent.xyz ) * v.tangent.w; \
float3x3 rotation = float3x3( v.tangent.xyz, binormal, v.normal )

Interpolators VertexFunction(VertexData v)
{
    Interpolators i;
    i.localPosition = v.vertex.xyz;
    i.pos = UnityObjectToClipPos(v.vertex + RandomPosition(v.vertex + mul(unity_ObjectToWorld, v.vertex)) * _ShakingOn);
    i.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
    i.localNormal = v.normal;
    i.normal = UnityObjectToWorldNormal(v.normal);
    i.worldPos = mul(unity_ObjectToWorld, v.vertex);
    i.tangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
    i.bitangent = normalize(cross(i.normal, i.tangent) * v.tangent.w);
    i.viewDir = WorldSpaceViewDir(v.vertex);
    i.binormal = CreateBinormal(i.normal, i.tangent, v.tangent.w);
	i.screenPosition = ComputeScreenPos(i.pos);

	
	TANGENT_SPACE_ROTATION;
    //i.TtoW0 = mul(rotation, ((float3x3) unity_ObjectToWorld)[0].xyz) * 1.0 * 1.0;
    //i.TtoW1 = mul(rotation, ((float3x3) unity_ObjectToWorld)[1].xyz) * 1.0 * 1.0;
    //i.TtoW2 = mul(rotation, ((float3x3) unity_ObjectToWorld)[2].xyz) * 1.0 * 1.0;
    TRANSFER_SHADOW(i);
    ComputeVertexLightColor(i);
	
    return i;
}


float4 triPlanar(Interpolators i) {
    float3 worldNormalVec = normalize(i.normal); //WorldNormalVector(i, float3(0, 1, 0));
    float3 blendNormal = saturate(pow(worldNormalVec * _BlendSmooth, 4));

	// TriPlanar for XY, XZ, YZ
	//Top
	/*
    float4 xt = tex2D(_TopText, i.worldPos.zy * _Scale.x);
    float4 yt = tex2D(_TopText, i.worldPos.zx * _Scale.x);
    float4 zt = tex2D(_TopText, i.worldPos.xy * _Scale.x);
	*/
	float4 xt = tex2D(_TopText, i.uv * _Scale.x);
	float4 yt = tex2D(_TopText, i.uv * _Scale.x);
	float4 zt = tex2D(_TopText, i.uv * _Scale.x);
	//Side
    float4 xs = tex2D(_MainTex, i.worldPos.zy * _Scale.y);
    float4 ys = tex2D(_MainTex, i.worldPos.zx * _Scale.y);
    float4 zs = tex2D(_MainTex, i.worldPos.xy * _Scale.y);
	//Noise
    float4 xn = tex2D(_Noise, i.worldPos.zy * _NoiseScale);
    float4 yn = tex2D(_Noise, i.worldPos.zx * _NoiseScale);
    float4 zn = tex2D(_Noise, i.worldPos.xy * _NoiseScale);
	
	//Lerp the results of the world normals with the two textures.
    //Top
    float4 topTexture = zt;
    topTexture = lerp(topTexture, xt, blendNormal.x);
    topTexture = lerp(topTexture, yt, blendNormal.y);
    //Side
    float4 sideTexture = zs;
    sideTexture = lerp(sideTexture, xs, blendNormal.x);
    sideTexture = lerp(sideTexture, ys, blendNormal.y);
    //Noise
    float4 noisetexture = zn;
    noisetexture = lerp(noisetexture, xn, blendNormal.x);
    noisetexture = lerp(noisetexture, yn, blendNormal.y);
	
	
    //Determine how if on side or on top.
    float normDotNoise = dot(i.normal + (noisetexture.y + (noisetexture * 0.5)), worldNormalVec.y);
    //Checks if higher then top.
    float4 topTextureResult = step(_Spread + _EdgeWidth, normDotNoise) * topTexture;
    //Side
    float4 sideTextureResult = step(normDotNoise, _Spread) * sideTexture;

    float4 result = (topTextureResult * _Brightness) + sideTextureResult;
	
    return result;
}

float4 triPlanarCelShaded(Interpolators i)
{
	float3 worldNormalVec = normalize(i.normal); //WorldNormalVector(i, float3(0, 1, 0));
	float3 blendNormal = saturate(pow(worldNormalVec * _BlendSmooth, 4));

	// TriPlanar for XY, XZ, YZ
	//Top
	float4 xt = tex2D(_TopText, i.worldPos.zy * _Scale.x);
	float4 yt = tex2D(_TopText, i.worldPos.zx * _Scale.x);
	float4 zt = tex2D(_TopText, i.worldPos.xy * _Scale.x);
	//Side
	float4 xs = tex2D(_MainTex, i.worldPos.zy * _Scale.y);
	float4 ys = tex2D(_MainTex, i.worldPos.zx * _Scale.y);
	float4 zs = tex2D(_MainTex, i.worldPos.xy * _Scale.y);
	//Noise
	float4 xn = tex2D(_Noise, i.worldPos.zy * _NoiseScale);
	float4 yn = tex2D(_Noise, i.worldPos.zx * _NoiseScale);
	float4 zn = tex2D(_Noise, i.worldPos.xy * _NoiseScale);

	//Lerp the results of the world normals with the two textures.
	//Top
	float4 topTexture = zt;
	topTexture = lerp(topTexture, xt, blendNormal.x);
	topTexture = lerp(topTexture, yt, blendNormal.y);
	//Side
	float4 sideTexture = zs;
	sideTexture = lerp(sideTexture, xs, blendNormal.x);
	sideTexture = lerp(sideTexture, ys, blendNormal.y);
	//Noise
	float4 noisetexture = zn;
	noisetexture = lerp(noisetexture, xn, blendNormal.x);
	noisetexture = lerp(noisetexture, yn, blendNormal.y);


	//Determine how if on side or on top.
	float normDotNoise = dot(i.normal + (noisetexture.y + (noisetexture * 0.5)), worldNormalVec.y);
	//Checks if higher then top.
	float4 topTextureResult = step(_Spread + _EdgeWidth, normDotNoise) * topTexture;
	//Side
	float4 sideTextureResult = step(normDotNoise, _Spread) * sideTexture;

	float4 result = (topTextureResult * _Brightness) + sideTextureResult;

	return result;
}

float4 triPlanarSIDE(Interpolators i)
{
    float3 worldNormalVec = normalize(i.normal); //WorldNormalVector(i, float3(0, 1, 0));
    float3 blendNormal = saturate(pow(worldNormalVec * _BlendSmooth, 4));

	// TriPlanar for XY, XZ, YZ
	//Top
    float4 xt = tex2D(_TopText, i.worldPos.zy * _Scale.x);
    float4 yt = tex2D(_TopText, i.worldPos.zx * _Scale.x);
    float4 zt = tex2D(_TopText, i.worldPos.xy * _Scale.x);
	//Side
    float4 xs = tex2D(_MainTex, i.worldPos.zy * _Scale.y);
    float4 ys = tex2D(_MainTex, i.worldPos.zx * _Scale.y);
    float4 zs = tex2D(_MainTex, i.worldPos.xy * _Scale.y);
	//Noise
    float4 xn = tex2D(_Noise, i.worldPos.zy * _NoiseScale);
    float4 yn = tex2D(_Noise, i.worldPos.zx * _NoiseScale);
    float4 zn = tex2D(_Noise, i.worldPos.xy * _NoiseScale);
	
	//Lerp the results of the world normals with the two textures.
    //Top
    float4 topTexture = zt;
    topTexture = lerp(topTexture, xt, blendNormal.x);
    topTexture = lerp(topTexture, yt, blendNormal.y);
    //Side
    float4 sideTexture = zs;
    sideTexture = lerp(sideTexture, xs, blendNormal.x);
    sideTexture = lerp(sideTexture, ys, blendNormal.y);
    //Noise
    float4 noisetexture = zn;
    noisetexture = lerp(noisetexture, xn, blendNormal.x);
    noisetexture = lerp(noisetexture, yn, blendNormal.y);
	
	
    //Determine how if on side or on top.
    float normDotNoise = dot(i.normal + (noisetexture.y + (noisetexture * 0.5)), worldNormalVec.y);
    //Checks if higher then top.
    float4 topTextureResult = step(_Spread + _EdgeWidth, normDotNoise) * topTexture;
    //Side
    float4 sideTextureResult = step(normDotNoise, _Spread) * sideTexture;

    float4 result = sideTextureResult + topTextureResult;
	
    return result;
}



UnityIndirect CreateIndirectLight(Interpolators i)
{
    UnityIndirect indirectLight;
    indirectLight.diffuse = 0;
    indirectLight.specular = 0;

#if defined(VERTEXLIGHT_ON)
		indirectLight.diffuse = i.vertexLightColor;
#endif

	#if defined(FORWARD_BASE_PASS)
		indirectLight.diffuse += max(0, ShadeSH9(float4(i.normal, 1))) * 0.85;
	#endif
	
    return indirectLight;
}

float3 GetHeightMap(Interpolators i)
{
    float du = float2(_HeightMap_TexelSize.x * 0.5, 0);
    float u1 = tex2D(_HeightMap, i.uv - du);
    float u2 = tex2D(_HeightMap, i.uv + du);
    float3 tangent = float3(1, u2 - u1, 0); //tangent
	
    float dv = float2(0, _HeightMap_TexelSize.y * 0.5);
    float v1 = tex2D(_HeightMap, i.uv - dv);
    float v2 = tex2D(_HeightMap, i.uv + dv);
    float3 bitangent = float3(0, v2 - v1, 1); //bitangent
	
	//Cross product produces the normal.
    float3 uvNormal = cross(tangent, bitangent);
    float3 normals = normalize(uvNormal);
	
    return normals;

}

float3 GetNormalMap(Interpolators i)
{
    float3 normals = float3(0,0,0); 
    float3 tangentSpaceNormal = UnpackScaleNormal(tex2D(_NormalMap, i.uv), _BumpScale);
    float3 binormal = cross(i.normal, i.tangent.xyz) * (i.tangent.w * unity_WorldTransformParams.w);
	
    normals = normalize(
		tangentSpaceNormal.x * i.tangent +
		tangentSpaceNormal.y * binormal +
		tangentSpaceNormal.z * i.normal
	);
	
    return normals;

}

//A diffuse shading method 2, slightly different from the other version.
float4 DiffuseShading(Interpolators i)
{
    float3 normals = GetNormalMap(i); //normalize(i.normal);
    float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
	
	

	
    float3 lightPos = _WorldSpaceLightPos0.xyz - i.worldPos;
    float3 lightDir = normalize(lightPos);
	
    float3 halfVector = normalize(lightDir + viewDir);
    float reflectance = pow(DotClamped(halfVector, normals), _Smoothness * 10);
	

	
	//Variable name, shadows, position.
    UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
    float3 lightColor = _LightColor0.rgb * attenuation;
	

	
    float NdotL = DotClamped(normals, lightDir);
	
	float4 debugNormals = float4(normals * 0.5 + 0.5, 1);

    float3 albedo = tex2D(_MainTex, i.uv).rgb * _Tint.rgb;
    float3 specularTint;
    float oneMinusReflectivity;
    albedo = DiffuseAndSpecularFromMetallic(
					albedo, _Metallic, specularTint, oneMinusReflectivity
				);
	
    float3 specular = specularTint * lightColor * pow(DotClamped(halfVector, normals),
	_Smoothness * 10);
	
    float4 diffuse = float4(lightColor * NdotL * albedo, 1);
    float3 shColor = ShadeSH9(float4(i.normal, 1));
    float4 SkyBoxEnvironment = ToFloat4(shColor);
	
    UnityLight light;
	
    light.color = lightColor;
#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
		light.dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
#else
    light.dir = _WorldSpaceLightPos0.xyz;
	#endif
    light.ndotl = NdotL;
    UnityIndirect indirectLight;
    indirectLight.diffuse = 0;
    indirectLight.specular = 0;
	

	
    float4 PBR = UNITY_BRDF_PBS(albedo, specularTint, oneMinusReflectivity, _Smoothness, normals, viewDir, light, CreateIndirectLight(i));

    return PBR *0.25 + SkyBoxEnvironment*0.85;
}


float4 DiffuseLightingFunction(Interpolators i)
{

	float4 value = 0;
    float3 normals = i.normal;

	float3 lightDir = _WorldSpaceLightPos0.xyz;
	float3 lightColor = _LightColor0;
	float3 albedo = tex2D(_MainTex, i.uv).rgb * _Tint.rgb;
	//Energy conservation.
	float oneMinusReflectivity;
	albedo = EnergyConservationBetweenDiffuseAndSpecular(albedo, _SpecularTint.rgb, oneMinusReflectivity);
	float metalicCol;
	//albedo *= DiffuseAndSpecularFromMetallic(albedo, _Metallic, _SpecularTint.rgb, metalicCol);

	float3 diffuseColor = albedo * lightColor * DotClamped(lightDir, normals);
	value = float4(diffuseColor, 1);

	return value;
}

float4 SpecularLightingFunction(Interpolators i)
{
	float3 lightDir = _WorldSpaceLightPos0.xyz;
	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
	float3 reflectDir = reflect(-lightDir, i.normal);
	float3 halfVector = normalize(lightDir + viewDir);
	float4 specularLight = float4(_LightColor0.xyz * _SpecularTint.xyz, 1) * pow(DotClamped(halfVector, reflectDir), _Smoothness * 100);
	return specularLight;
}


//Get Global illumination from Unity
// https://www.jordanstevenstechart.com/physically-based-rendering
UnityGI GetUnityGI(float3 lightColor, float3 lightDirection, float3 normalDirection, float3 viewDirection,
	float3 viewReflectDirection, float attenuation, float roughness, float3 worldPos) {
	//Unity light Setup ::
	UnityLight light;
	light.color = lightColor;
	light.dir = lightDirection;
	light.ndotl = max(0.0h, dot(normalDirection, lightDirection));
	UnityGIInput d;
	d.light = light;
	d.worldPos = worldPos;
	d.worldViewDir = viewDirection;
	d.atten = attenuation;
	d.ambient = 0.0h;
#if UNITY_SPECCUBE_BOX_PROJECTION
	d.boxMax[0] = unity_SpecCube0_BoxMax;
	d.boxMin[0] = unity_SpecCube0_BoxMin;
	d.probePosition[0] = unity_SpecCube0_ProbePosition;
	d.probeHDR[0] = unity_SpecCube0_HDR;
	d.boxMax[1] = unity_SpecCube1_BoxMax;
	d.boxMin[1] = unity_SpecCube1_BoxMin;
	d.probePosition[1] = unity_SpecCube1_ProbePosition;
#endif
	d.probeHDR[1] = unity_SpecCube1_HDR;
	Unity_GlossyEnvironmentData ugls_en_data;
	ugls_en_data.roughness = roughness;
	ugls_en_data.reflUVW = viewReflectDirection;
	UnityGI gi = UnityGlobalIllumination(d, 1.0h, normalDirection, ugls_en_data);
	return gi;
}

//Different lighting functions...
//-------------------------------
//Blinn-Phong NDF.
float BlinnPhongNormalDistribution(float NdotH, float specularpower, float speculargloss) {
	float Distribution = pow(NdotH, speculargloss) * specularpower;
	Distribution *= (2 + specularpower) / (2 * 3.1415926535);
	return Distribution;
}
// ------------------------------
// Phong NDF
float PhongNormalDistribution(float RdotV, float specularpower, float speculargloss) {
	float Distribution = pow(RdotV, speculargloss) * specularpower;
	Distribution *= (2 + specularpower) / (2 * 3.1415926535);
	return Distribution;
}
// ------------------------------
// Beckman NDF
float BeckmannNormalDistribution(float roughness, float NdotH)
{
	float roughnessSqr = roughness * roughness;
	float NdotHSqr = NdotH * NdotH;
	return max(0.000001, (1.0 / (3.1415926535 * roughnessSqr * NdotHSqr * NdotHSqr))
		* exp((NdotHSqr - 1) / (roughnessSqr * NdotHSqr)));
}

// ------------------------------
// Guassian NDF			
float GaussianNormalDistribution(float roughness, float NdotH)
{
	float roughnessSqr = roughness * roughness;
	float thetaH = acos(NdotH);
	return exp(-thetaH * thetaH / roughnessSqr);
}

// ------------------------------
// GGX NDF	
float GGXNormalDistribution(float roughness, float NdotH)
{
	float roughnessSqr = roughness * roughness;
	float NdotHSqr = NdotH * NdotH;
	float TanNdotHSqr = (1 - NdotHSqr) / NdotHSqr;
	return (1.0 / 3.1415926535) * sqrt(roughness / (NdotHSqr * (roughnessSqr + TanNdotHSqr)));
}

// ------------------------------
// Trowbridge Reitz NDF
float TrowbridgeReitzNormalDistribution(float NdotH, float roughness) {
	float roughnessSqr = roughness * roughness;
	float Distribution = NdotH * NdotH * (roughnessSqr - 1.0) + 1.0;
	return roughnessSqr / (3.1415926535 * Distribution * Distribution);
}

// ------------------------------
// Trowbridge Reitz Anisotropic NDF
float TrowbridgeReitzAnisotropicNormalDistribution(float NdotH, float roughnessX, float roughnessY, float HdotX, float HdotY) {
	float roughnessXSqr = roughnessX * roughnessX;
	float roughnessYSqr = roughnessY * roughnessY;
	float Distribution = NdotH * NdotH * (roughnessXSqr * HdotY * HdotY + roughnessYSqr * HdotX * HdotX - 1.0) + 1.0;
	return (roughnessXSqr * roughnessYSqr) / (3.1415926535 * Distribution * Distribution);
}


// ------------------------------
// Ward Anisotropic NDF
float WardAnisotropicNormalDistribution(float anisotropic, float NdotL,
	float NdotV, float NdotH, float HdotX, float HdotY) {
	float aspect = sqrt(1.0h - anisotropic * 0.9h);
	float X = max(.001, sqrt(1.0 - _Smoothness) / aspect) * 5;
	float Y = max(.001, sqrt(1.0 - _Smoothness) * aspect) * 5;
	float exponent = -(sqrt(HdotX / X) + sqrt(HdotY / Y)) / sqrt(NdotH);
	float Distribution = 1.0 / (4.0 * 3.14159265 * X * Y * sqrt(NdotL * NdotV));
	Distribution *= exp(exponent);
	return Distribution;
}

// ------------------------------
// Cook-Torrance NDF

//Not used in final build.
float4 UnityBRDF(Interpolators i)
{
	float3 albedo = tex2D(_MainTex, i.uv).rgb * _Tint.rgb;
	float3 specularTint;
	float oneMinusReflectivity;
	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
	float3 lightColor = _LightColor0;
	float3 lightDir = _WorldSpaceLightPos0.xyz;
	albedo = DiffuseAndSpecularFromMetallic(
		albedo, _Metallic, specularTint, oneMinusReflectivity
	);
	UnityLight light;
	light.color = lightColor;
	light.dir = lightDir;
	light.ndotl = DotClamped(i.normal, lightDir);
	UnityIndirect indirectLight;
	indirectLight.diffuse = 0;
	indirectLight.specular = 0;

	return UNITY_BRDF_PBS(
		albedo, specularTint,
		oneMinusReflectivity, _Smoothness,
		i.normal, viewDir, light, indirectLight
	);
}

//Geometric Shadowing Algorithms
// ------------------------------

float GeometricShadow = 1;

float ImplicitGeometricShadowingFunction(float NdotL, float NdotV) {
	float Gs = (NdotL * NdotV);
	return Gs;
}

float AshikhminShirleyGSF(float NdotL, float NdotV, float LdotH) {
	float Gs = NdotL * NdotV / (LdotH * max(NdotL, NdotV));
	return  (Gs);
}


float AshikhminPremozeGeometricShadowingFunction(float NdotL, float NdotV) {
	float Gs = NdotL * NdotV / (NdotL + NdotV - NdotL * NdotV);
	return  (Gs);
}

float AshikhminShirleyGeometricShadowingFunction(float NdotL, float NdotV, float LdotH) {
	float Gs = NdotL * NdotV / (LdotH * max(NdotL, NdotV));
	return  (Gs);
}

float NeumannGeometricShadowingFunction(float NdotL, float NdotV) {
	float Gs = (NdotL * NdotV) / max(NdotL, NdotV);
	return  (Gs);
}

float KelemenGeometricShadowingFunction(float NdotL, float NdotV,
	float LdotV, float VdotH) {
	float Gs = (NdotL * NdotV) / (VdotH * VdotH);
	return   (Gs);
}

float ModifiedKelemenGeometricShadowingFunction(float NdotV, float NdotL,
	float roughness)
{
	float c = 0.797884560802865;    // c = sqrt(2 / Pi)
	float k = roughness * roughness * c;
	float gH = NdotV * k + (1 - k);
	return (gH * gH * NdotL);
}


float CookTorrenceGeometricShadowingFunction(float NdotL, float NdotV,
	float VdotH, float NdotH) {
	float Gs = min(1.0, min(2 * NdotH * NdotV / VdotH,
		2 * NdotH * NdotL / VdotH));
	return  (Gs);
}

float WardGeometricShadowingFunction(float NdotL, float NdotV,
	float VdotH, float NdotH) {
	float Gs = pow(NdotL * NdotV, 0.5);
	return  (Gs);
}

float KurtGeometricShadowingFunction(float NdotL, float NdotV,
	float VdotH, float roughness) {
	float Gs = NdotL * NdotV / (VdotH * pow(NdotL * NdotV, roughness));
	return  (Gs);
}


float WalterEtAlGeometricShadowingFunction(float NdotL, float NdotV, float alpha) {
	float alphaSqr = alpha * alpha;
	float NdotLSqr = NdotL * NdotL;
	float NdotVSqr = NdotV * NdotV;

	float SmithL = 2 / (1 + sqrt(1 + alphaSqr * (1 - NdotLSqr) / (NdotLSqr)));
	float SmithV = 2 / (1 + sqrt(1 + alphaSqr * (1 - NdotVSqr) / (NdotVSqr)));


	float Gs = (SmithL * SmithV);
	return Gs;
}


float BeckmanGeometricShadowingFunction(float NdotL, float NdotV, float roughness) {
	float roughnessSqr = roughness * roughness;
	float NdotLSqr = NdotL * NdotL;
	float NdotVSqr = NdotV * NdotV;


	float calulationL = (NdotL) / (roughnessSqr * sqrt(1 - NdotLSqr));
	float calulationV = (NdotV) / (roughnessSqr * sqrt(1 - NdotVSqr));


	float SmithL = calulationL < 1.6 ? (((3.535 * calulationL)
		+ (2.181 * calulationL * calulationL)) / (1 + (2.276 * calulationL) +
			(2.577 * calulationL * calulationL))) : 1.0;
	float SmithV = calulationV < 1.6 ? (((3.535 * calulationV)
		+ (2.181 * calulationV * calulationV)) / (1 + (2.276 * calulationV) +
			(2.577 * calulationV * calulationV))) : 1.0;


	float Gs = (SmithL * SmithV);
	return Gs;
}


float GGXGeometricShadowingFunction(float NdotL, float NdotV, float roughness) {
	float roughnessSqr = roughness * roughness;
	float NdotLSqr = NdotL * NdotL;
	float NdotVSqr = NdotV * NdotV;


	float SmithL = (2 * NdotL) / (NdotL + sqrt(roughnessSqr +
		(1 - roughnessSqr) * NdotLSqr));
	float SmithV = (2 * NdotV) / (NdotV + sqrt(roughnessSqr +
		(1 - roughnessSqr) * NdotVSqr));


	float Gs = (SmithL * SmithV);
	return Gs;
}


float SchlickGeometricShadowingFunction(float NdotL, float NdotV, float roughness)
{
	float roughnessSqr = roughness * roughness;
	float SmithL = (NdotL) / (NdotL * (1 - roughnessSqr) + roughnessSqr);
	float SmithV = (NdotV) / (NdotV * (1 - roughnessSqr) + roughnessSqr);
	return (SmithL * SmithV);
}

float SchlickBeckmanGeometricShadowingFunction(float NdotL, float NdotV,
	float roughness) {
	float roughnessSqr = roughness * roughness;
	float k = roughnessSqr * 0.797884560802865;
	float SmithL = (NdotL) / (NdotL * (1 - k) + k);
	float SmithV = (NdotV) / (NdotV * (1 - k) + k);
	float Gs = (SmithL * SmithV);
	return Gs;
}

float SchlickGGXGeometricShadowingFunction(float NdotL, float NdotV, float roughness) {
	float k = roughness / 2;
	float SmithL = (NdotL) / (NdotL * (1 - k) + k);
	float SmithV = (NdotV) / (NdotV * (1 - k) + k);
	float Gs = (SmithL * SmithV);
	return Gs;
}

//Fresnel Function, To be Changed:

float MixFunction(float i, float j, float x) {
	return  j * x + i * (1.0 - x);
}


float SchlickFresnel(float i) {
	float x = clamp(1.0 - i, 0.0, 1.0);
	float x2 = x * x;
	return x2 * x2 * x;
}

float3 FresnelLerp(float3 x, float3 y, float d)
{
	float t = SchlickFresnel(d);
	return lerp(x, y, t);
}

float F0(float NdotL, float NdotV, float LdotH, float roughness) {
	float FresnelLight = SchlickFresnel(NdotL);
	float FresnelView = SchlickFresnel(NdotV);
	float FresnelDiffuse90 = 0.5 + 2.0 * LdotH * LdotH * roughness;
	return  MixFunction(1, FresnelDiffuse90, FresnelLight) * MixFunction(1, FresnelDiffuse90, FresnelView);
}

float3 SchlickFresnelFunction(float3 SpecularColor, float LdotH) {
	return SpecularColor + (1 - SpecularColor) * SchlickFresnel(LdotH);
}

float SchlickIORFresnelFunction(float ior, float LdotH) {
	float f0 = pow(ior - 1, 2) / pow(ior + 1, 2);
	return f0 + (1 - f0) * SchlickFresnel(LdotH);
}

float SphericalGaussianFresnelFunction(float LdotH, float SpecularColor)
{
	float power = ((-5.55473 * LdotH) - 6.98316) * LdotH;
	return SpecularColor + (1 - SpecularColor) * pow(2, power);
}



float4 customBRDF(Interpolators i)
{






	float3 normal = normalize(i.normal);
	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
	float3 lightDir = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.worldPos.xyz, _WorldSpaceLightPos0.w));
	float3 halfVector = normalize(lightDir + viewDir);

	float3 viewReflection = normalize(reflect(-viewDir, normal));

	float3 lightReflect = reflect(-lightDir, normal);

	float NdotL = max(0.0, dot(normal, lightDir));
	float NdotH = max(0.0, dot(normal, halfVector));
	float NdotV = max(0.0, dot(normal, viewDir));
	float VdotH = max(0.0, dot(viewDir, halfVector));
	float LdotH = max(0.0, dot(lightDir, halfVector));
	float LdotV = max(0.0, dot(lightDir, viewDir));
	float RdotV = max(0.0, dot(lightReflect, viewDir));

	float attenuation = LIGHT_ATTENUATION(i);
	float3 attenColor = attenuation * _LightColor0.rgb;
	float roughness = 1 - (_Smoothness * _Smoothness);
	roughness = roughness * roughness;
	float3 diffuseColor = _Tint.rgb * (1 - _Metallic);
	float3 specCol = lerp(_SpecularTint.rgb, _Tint.rgb, _Metallic * 0.5);

	//Get Unity GI
	UnityGI gi = GetUnityGI(_LightColor0.rgb, lightDir,
		normal, viewDir, viewReflection, attenuation, 1 - _Smoothness, i.worldPos.xyz);

	float3 indirectDiffuse = gi.indirect.diffuse.rgb;
	float3 indirectSpecular = gi.indirect.specular.rgb;
	//--------------------------------------------------

	float3 SpecularDistribution = specCol;
	float GeometricShadow = 1;
	float3 FresnelFunction = specCol;

	//Normal Distribution Function/Specular Distribution-----------------------------------------------------	      

#ifdef _NORMALDISTMODEL_BLINNPHONG 
	SpecularDistribution *= BlinnPhongNormalDistribution(NdotH, _Smoothness, max(1, _Smoothness * 40));
#elif _NORMALDISTMODEL_PHONG
	SpecularDistribution *= PhongNormalDistribution(RdotV, _Smoothness, max(1, _Smoothness * 40));
#elif _NORMALDISTMODEL_BECKMANN
	SpecularDistribution *= BeckmannNormalDistribution(roughness, NdotH);
#elif _NORMALDISTMODEL_GAUSSIAN
	SpecularDistribution *= GaussianNormalDistribution(roughness, NdotH);
#elif _NORMALDISTMODEL_GGX
	SpecularDistribution *= GGXNormalDistribution(roughness, NdotH);
#elif _NORMALDISTMODEL_TROWBRIDGEREITZ
	SpecularDistribution *= TrowbridgeReitzNormalDistribution(NdotH, roughness);
#elif _NORMALDISTMODEL_TROWBRIDGEREITZANISOTROPIC
	SpecularDistribution *= TrowbridgeReitzAnisotropicNormalDistribution(NdotH, _Anisotropic, roughness, dot(halfVector, i.tangent), dot(halfVector, i.bitangent));
#elif _NORMALDISTMODEL_WARD
	SpecularDistribution *= WardAnisotropicNormalDistribution(_Anisotropic, NdotL, NdotV, NdotH, dot(halfVector, i.tangent), dot(halfVector, i.bitangent));
#else
	SpecularDistribution *= GGXNormalDistribution(roughness, NdotH);
#endif


	//Geometric Shadowing term----------------------------------------------------------------------------------
#ifdef _SMITHGEOSHADOWMODEL_NONE
#ifdef _GEOSHADOWMODEL_ASHIKHMINSHIRLEY
	GeometricShadow *= AshikhminShirleyGeometricShadowingFunction(NdotL, NdotV, LdotH);
#elif _GEOSHADOWMODEL_ASHIKHMINPREMOZE
	GeometricShadow *= AshikhminPremozeGeometricShadowingFunction(NdotL, NdotV);
#elif _GEOSHADOWMODEL_DUER
	GeometricShadow *= DuerGeometricShadowingFunction(lightDirection, viewDirection, normalDirection, NdotL, NdotV);
#elif _GEOSHADOWMODEL_NEUMANN
	GeometricShadow *= NeumannGeometricShadowingFunction(NdotL, NdotV);
#elif _GEOSHADOWMODEL_KELEMAN
	GeometricShadow *= KelemenGeometricShadowingFunction(NdotL, NdotV, LdotH, VdotH);
#elif _GEOSHADOWMODEL_MODIFIEDKELEMEN
	GeometricShadow *= ModifiedKelemenGeometricShadowingFunction(NdotV, NdotL, roughness);
#elif _GEOSHADOWMODEL_COOK
	GeometricShadow *= CookTorrenceGeometricShadowingFunction(NdotL, NdotV, VdotH, NdotH);
#elif _GEOSHADOWMODEL_WARD
	GeometricShadow *= WardGeometricShadowingFunction(NdotL, NdotV, VdotH, NdotH);
#elif _GEOSHADOWMODEL_KURT
	GeometricShadow *= KurtGeometricShadowingFunction(NdotL, NdotV, VdotH, roughness);
#else 			
	GeometricShadow *= ImplicitGeometricShadowingFunction(NdotL, NdotV);
#endif
	////SmithModelsBelow
	////Gs = F(NdotL) * F(NdotV);
#elif _SMITHGEOSHADOWMODEL_WALTER
	GeometricShadow *= WalterEtAlGeometricShadowingFunction(NdotL, NdotV, roughness);
#elif _SMITHGEOSHADOWMODEL_BECKMAN
	GeometricShadow *= BeckmanGeometricShadowingFunction(NdotL, NdotV, roughness);
#elif _SMITHGEOSHADOWMODEL_GGX
	GeometricShadow *= GGXGeometricShadowingFunction(NdotL, NdotV, roughness);
#elif _SMITHGEOSHADOWMODEL_SCHLICK
	GeometricShadow *= SchlickGeometricShadowingFunction(NdotL, NdotV, roughness);
#elif _SMITHGEOSHADOWMODEL_SCHLICKBECKMAN
	GeometricShadow *= SchlickBeckmanGeometricShadowingFunction(NdotL, NdotV, roughness);
#elif _SMITHGEOSHADOWMODEL_SCHLICKGGX
	GeometricShadow *= SchlickGGXGeometricShadowingFunction(NdotL, NdotV, roughness);
#elif _SMITHGEOSHADOWMODEL_IMPLICIT
	GeometricShadow *= ImplicitGeometricShadowingFunction(NdotL, NdotV);
#else
	GeometricShadow *= ImplicitGeometricShadowingFunction(NdotL, NdotV);
#endif
	//Fresnel Function-------------------------------------------------------------------------------------------------

#ifdef _FRESNELMODEL_SCHLICK
	FresnelFunction *= SchlickFresnelFunction(specCol, LdotH);
#elif _FRESNELMODEL_SCHLICKIOR
	FresnelFunction *= SchlickIORFresnelFunction(_Ior, LdotH);
#elif _FRESNELMODEL_SPHERICALGAUSSIAN
	FresnelFunction *= SphericalGaussianFresnelFunction(LdotH, specCol);
#else
	FresnelFunction *= SchlickIORFresnelFunction(_Ior, LdotH);
#endif


#ifdef _ENABLE_NDF_ON
	return float4(float3(1, 1, 1) * SpecularDistribution, 1);
#endif
#ifdef _ENABLE_G_ON 
	return float4(float3(1, 1, 1) * GeometricShadow, 1);
#endif
#ifdef _ENABLE_F_ON 
	return float4(float3(1, 1, 1) * FresnelFunction, 1);
#endif
#ifdef _ENABLE_D_ON 
	return float4(float3(1, 1, 1) * diffuseColor, 1);
#endif

	float3 specularity = (SpecularDistribution * FresnelFunction * GeometricShadow) / (4 * (NdotL * NdotV));

	float grazingTerm = saturate(roughness + _Metallic);
	float3 unityIndirectSpecularity = indirectSpecular * FresnelLerp(specCol, grazingTerm, NdotV) *
		max(0.15, _Metallic) * (1 - roughness * roughness * roughness);

	float3 lightingModel = (diffuseColor + specularity
		+ (unityIndirectSpecularity * _UnityLightingContribution));

	lightingModel *= NdotL;
	float4 finalDiffuse = float4(lightingModel * attenColor, 1);
	return finalDiffuse;
}

//Function for creating solid bands of light.
float4 LightBands(Interpolators i)
{
	float3 normals = i.normal;
	float3 lightDir = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.worldPos.xyz, _WorldSpaceLightPos0.w));
	float3 diffuseColor = _Tint.rgb * (1 - _Metallic);
	float NdotL = max(0.0, dot(normals, lightDir));

	float lightBandsMultiplier = _LightSteps / 256;
	float lightBandsAdditive = _LightSteps / 2;
	fixed bandedNdotL = (floor((NdotL * 256 + lightBandsAdditive) / _LightSteps))
		* lightBandsMultiplier;

	float3 lightingModel = bandedNdotL * diffuseColor;
	float attenuation = LIGHT_ATTENUATION(i);
	float3 attenColor = attenuation * _LightColor0.rgb;
	float4 finalDiffuse = float4(lightingModel * attenColor, 1);
	return finalDiffuse;
}



//Final functions to put things together:

float4 UnigmaPBRL(Interpolators i)
{
		/*
    float3 normals = i.normal;
				
    float4 mainTex = tex2D(_MainTex, i.uv);
    float3 viewDir = normalize(i.viewDir);

    float3 halfVector = normalize(_WorldSpaceLightPos0 + viewDir);
    float3 lightDir = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.worldPos.xyz, _WorldSpaceLightPos0.w));
    float NdotH = dot(i.normal, halfVector);
    float NdotL = max(0.0, dot(i.normal, lightDir));

    float specularIntensity = pow(NdotH * _SpecularIntensity, _Glossiness * _Glossiness);
    float specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
    float4 specular = specularIntensitySmooth * _SpecularColor;

    float4 lightingVal = DiffuseLightingFunction(i);
    float4 specularLight = SpecularLightingFunction(i);

    float4 rimDot = 1 - dot(viewDir, i.normal);
    float rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimDot);
    rimIntensity *= pow(NdotL, _RimThreshold);
    float4 rim = rimIntensity * _RimColor;


    float4 baseLightingBRDF = customBRDF(i); //UnityBRDF(i);//lightingVal+specularLight;
    float monochromeBands = lerp(customBRDF(i), LightBands(i), 0.35).x; //This function splits colors into black and white bands.
    monochromeBands = saturate(monochromeBands);
    monochromeBands += step(monochromeBands, 0.5) * _ShadowStrength;

    float4 coloredBands = float4(monochromeBands * float3(1, 1, 1), 1) * _Tint * _Brightness;

    specular = specular * (monochromeBands * 0.35);
    float4 FinalResult = coloredBands + specular + rim;

				//return LightBands(i);

				//float4 col = albedo;
				//float4 TopResult = step(triResult, 0) * float4(col.xyz, 1);
				//float4 TempResult = TopResult + triResult;


				//float4 result = topTextureResult + sideTextureResult;
				//return result;
				

				//return TempResult * FinalResult;

				//return float4(normals,1);
*/
    return DiffuseShading(i);
	
}

float4 ShadowVertexFunction(VertexData v) : SV_POSITION
{
    float4 position = UnityClipSpaceShadowCasterPos(v.vertex.xyz, v.normal);
    return UnityApplyLinearShadowBias(position);
}

half4 ShadowUnigmaPBR(Interpolators i) : SV_TARGET
{
    return 0;
}

float Clamp0To1(float x)
{
	float _min = -1;
	float _max = abs(1 - _min);

	return 1 - ((x - _min) / _max);
}


float4 UnigmaPBR(Interpolators i) : SV_TARGET
{
    float3 normals = i.normal;
				
    float4 mainTex = tex2D(_MainTex, i.uv);
    float3 viewDir = normalize(i.viewDir);

    float3 halfVector = normalize(_WorldSpaceLightPos0 + viewDir);
    float3 lightDir = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.worldPos.xyz, _WorldSpaceLightPos0.w));
    float NdotH = dot(i.normal, halfVector);
    float NdotL = max(0.0, dot(i.normal, lightDir));

    float specularIntensity = pow(NdotH * _SpecularIntensity, _Glossiness * _Glossiness);
    float specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
    float4 specular = specularIntensitySmooth * _SpecularColor;

    float4 lightingVal = DiffuseLightingFunction(i);
    float4 specularLight = SpecularLightingFunction(i);

    float4 rimDot = 1 - dot(viewDir, i.normal);
    float rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimDot);
    rimIntensity *= pow(NdotL, _RimThreshold);
    float4 rim = rimIntensity * _RimColor;


    float4 baseLightingBRDF = customBRDF(i); //UnityBRDF(i);//lightingVal+specularLight;
    float monochromeBands = lerp(customBRDF(i), LightBands(i), 0.35).x; //This function splits colors into black and white bands.
    monochromeBands = saturate(monochromeBands);
    monochromeBands += step(monochromeBands, 0.5) * _ShadowStrength;

    float4 coloredBands = float4(monochromeBands * float3(1, 1, 1), 1) * _Tint * _Brightness;

    specular = specular * (monochromeBands * 0.35);
    float4 FinalResult = coloredBands + specular + rim;
	

	//return LightBands(i);

	//float4 col = albedo;
	//float4 TopResult = step(triResult, 0) * float4(col.xyz, 1);
	//float4 TempResult = TopResult + triResult;


	//float4 result = topTextureResult + sideTextureResult;
	//return result;
    float4 colorSpread = lerp(FinalResult, _AmbientColor, step(i.localPosition.z, _Spread));
    float4 triResult = triPlanarSIDE(i);
    float4 result = lerp(FinalResult, colorSpread, i.localPosition.z);

    return result * triResult;

	//return float4(normals,1);
    //return DiffuseShading(i);
	
}

float4 UnigmaGlass(Interpolators i) : SV_TARGET
{
		//Diffuse
	float3 normals = GetNormalMap(i);
	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);




	float3 lightPos = _WorldSpaceLightPos0.xyz - i.worldPos;
	float3 lightDir = normalize(lightPos);

	float3 halfVector = normalize(lightDir + viewDir);
	float reflectance = pow(DotClamped(halfVector, normals), _Smoothness * 10);



	//Variable name, shadows, position.
	UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
	float3 lightColor = _LightColor0.rgb * attenuation;



	float NdotL = DotClamped(normals, lightDir);


	float3 albedo = _Tint.rgb;
	float3 specularTint;
	float oneMinusReflectivity;
	albedo = DiffuseAndSpecularFromMetallic(
					albedo, _Metallic, specularTint, oneMinusReflectivity
				);

	float3 specular = specularTint * lightColor * pow(DotClamped(halfVector, normals),
	_Smoothness * 10);

	float4 diffuse = float4(lightColor * NdotL * albedo, 1);
	float3 shColor = ShadeSH9(float4(i.normal, 1));
	float4 SkyBoxEnvironment = ToFloat4(shColor);

	UnityLight light;

	light.color = 1;
	#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
			light.dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
	#else
		light.dir = _WorldSpaceLightPos0.xyz;
	#endif
		light.ndotl = NdotL;
		UnityIndirect indirectLight;
		indirectLight.diffuse = 0;
		indirectLight.specular = 0;



	float4 PBR = UNITY_BRDF_PBS(albedo, specularTint, oneMinusReflectivity, _Smoothness, normals, viewDir, light, CreateIndirectLight(i));

	
	float3 viewDirGlass = normalize(i.viewDir);

	float2 screenPosUV = i.screenPosition.xy / i.screenPosition.w;
	
	float3 diplacementNormals = UnpackNormal(tex2D(_DisplacementTex, screenPosUV));
	float2 distortion = screenPosUV + ((_Intensity * 0.01) * diplacementNormals.rg);

	float4 underglass = tex2D(_GlassBackground, distortion);

	float fresnel = 1 - saturate(dot(i.normal, viewDirGlass));
	fresnel = pow(fresnel, _FresnelExponent);
	float3 fresnelColor = fresnel * _FresnelColor;
	
	float4 Glass = float4(underglass.rgb + fresnelColor.rgb, underglass.w);

	
	return Glass + (PBR*0.2) + SkyBoxEnvironment;

}


float4 UnigmaPBRTriplanar(Interpolators i) : SV_TARGET
{
    float4 triResult = triPlanar(i);
	
	//Combine triplanar results with diffuse shading.
	
	//Diffuse
	float3 normals = normalize(i.normal);//GetNormalMap(i);
    float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
	
	

	
    float3 lightPos = _WorldSpaceLightPos0.xyz - i.worldPos;
    float3 lightDir = normalize(lightPos);
	
    float3 halfVector = normalize(lightDir + viewDir);
    float reflectance = pow(DotClamped(halfVector, normals), _Smoothness * 10);
	

	
	//Variable name, shadows, position.
    UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
    float3 lightColor = _LightColor0.rgb * attenuation;
	

	
    float NdotL = DotClamped(normals, lightDir);
	

    float3 albedo = _Tint.rgb * triResult;
    float3 specularTint;
    float oneMinusReflectivity;
    albedo = DiffuseAndSpecularFromMetallic(
					albedo, _Metallic, specularTint, oneMinusReflectivity
				);
	
    float3 specular = specularTint * lightColor * pow(DotClamped(halfVector, normals),
	_Smoothness * 10);
	
    float4 diffuse = float4(lightColor * NdotL * albedo, 1);
    float3 shColor = ShadeSH9(float4(i.normal, 1));
    float4 SkyBoxEnvironment = ToFloat4(shColor);
	
    UnityLight light;
	
    light.color = lightColor;
#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
		light.dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
#else
    light.dir = _WorldSpaceLightPos0.xyz;
#endif
    light.ndotl = NdotL;
    UnityIndirect indirectLight;
    indirectLight.diffuse = 0;
    indirectLight.specular = 0;
	

	
    float4 PBR = UNITY_BRDF_PBS(albedo, specularTint, oneMinusReflectivity, _Smoothness, normals, viewDir, light, CreateIndirectLight(i));
	
	//create darker darks
    float lightThreshold1 = step(NdotL, _NdotLThreshold);
	float4 result = PBR + (SkyBoxEnvironment * 0.375 * (lightThreshold1));
	result.a = triResult.a;
    return result;
}


float4 UnigmaPBRCelShadedTriplanar(Interpolators i) : SV_TARGET
{
	float4 triResult = triPlanarCelShaded(i);

	//Combine triplanar results with diffuse shading.

	//Diffuse
	float3 normals = normalize(i.normal);//GetNormalMap(i);
	float3 localNormals = normalize(i.localNormal);
	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);




	float3 lightPos = _WorldSpaceLightPos0.xyz - i.worldPos;
	float3 lightDirAbsolute = normalize(_WorldSpaceLightPos0.xyz);
	float3 lightDir = normalize(lightPos);

	float3 halfVector = normalize(lightDir + viewDir);
	float reflectance = pow(DotClamped(halfVector, normals), _Smoothness * 10);



	//Variable name, shadows, position.
	UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
	float3 lightColor = _LightColor0.rgb * attenuation;



	float NdotL = DotClamped(normals, lightDirAbsolute);


	float3 albedo = _Tint.rgb * triResult;
	float3 specularTint;
	float oneMinusReflectivity;
	albedo = DiffuseAndSpecularFromMetallic(
					albedo, _Metallic, specularTint, oneMinusReflectivity
				);

	float3 specular = specularTint * lightColor * pow(DotClamped(halfVector, normals),
	_Smoothness * 10);

	float4 diffuse = float4(lightColor * (1 - NdotL) * albedo, 1);
	float3 shColor = ShadeSH9(float4(i.normal, 1));
	float4 SkyBoxEnvironment = ToFloat4(shColor);

	float ndotl = dot(normals, lightDirAbsolute);

	UnityLight light;

	light.color = lightColor;
#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
		light.dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
#else
	light.dir = _WorldSpaceLightPos0.xyz;
#endif
	light.ndotl = NdotL;
	UnityIndirect indirectLight;
	indirectLight.diffuse = 0;
	indirectLight.specular = 0;



	float4 PBR = UNITY_BRDF_PBS(albedo, specularTint, oneMinusReflectivity, _Smoothness, normals, viewDir, light, CreateIndirectLight(i));

	//create darker darks
	float lightThreshold1 = step(_NdotLThreshold, NdotL);
	float4 result = PBR + (SkyBoxEnvironment * 0.375 * (lightThreshold1));

	float4 finalResult = lerp(result, diffuse, NdotL);
	float r = random(localNormals).x;

	float diffuseCutt = 1 - Clamp0To1(ndotl);
	float4 diffuseTint = diffuseCutt > _ShadowStrength ? result : _AmbientColor;



	return diffuseTint;//float4(localNormals, 1);
}
