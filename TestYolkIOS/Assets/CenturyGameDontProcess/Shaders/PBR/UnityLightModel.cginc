#ifndef UNITY_LIGHT_MODEL_INCLUDED
#define UNITY_LIGHT_MODEL_INCLUDED

#ifdef USE_UNITY_LIGHT_MODEL
	struct ClothShadingData
{
	half3 diffColor;
	half3 specColor;
	half oneMinusReflectivity;
	half smoothness;
	half alpha;
#if _USE_FRESNEL_ON
	half fresnelmask;
#endif
};

	#define ShadingData_T ClothShadingData

	#include "../Predefine/DDPDUtils.cginc"
inline void ClothPrepare_DiffuseAndSpecularFromMetallic(SurfaceData_T IN, out ShadingData_T shadingData)
{
	shadingData.diffColor = DD_DiffuseAndSpecularFromMetallic(IN.Albedo, IN.Metallic, shadingData.specColor, shadingData.oneMinusReflectivity);
	shadingData.smoothness = IN.Smoothness;
	shadingData.diffColor = DD_PreMultiplyAlpha(shadingData.diffColor, IN.Alpha, shadingData.oneMinusReflectivity, shadingData.alpha);
#if _USE_FRESNEL_ON
	shadingData.fresnelmask = IN.Fresnelmask;
#endif
}

inline void ClothPrepare_DiffuseAndNoSpecular(SurfaceData_T IN, out ShadingData_T shadingData)
{
	shadingData.diffColor = IN.Albedo;
	shadingData.specColor = 0;
	shadingData.oneMinusReflectivity = 1;
	shadingData.smoothness = 0;
	shadingData.diffColor = DD_PreMultiplyAlpha(shadingData.diffColor, IN.Alpha, shadingData.oneMinusReflectivity, shadingData.alpha);
#if _USE_FRESNEL_ON
	shadingData.fresnelmask = IN.Fresnelmask;
#endif
}

	#include "../Predefine/DDPDShadingPrepareFunc.cginc"
	#if defined(USE_PBR_BRDF2) || defined(USE_PBR_BRDF3)
	#	define FUNC_SHADING_PREPARE ClothPrepare_DiffuseAndSpecularFromMetallic
	#else
	#	define FUNC_SHADING_PREPARE ClothPrepare_DiffuseAndNoSpecular
	#endif

	#include "../Predefine/DDPDBRDF.cginc"
	#define DIFFUSE_TERM DiffuseTerm_Lambert

	#if defined(USE_PBR_BRDF2)
	#	define INDIRECT_SPECULAR_TERM IndirectSpecularTerm_BRDF2
	#	define SPECULAR_TERM SpecularTerm_PBR_BRDF2
	#elif defined(USE_PBR_BRDF3)
	#	define INDIRECT_SPECULAR_TERM IndirectSpecularTerm_BRDF3
	#	define SPECULAR_TERM SpecularTerm_PBR_BRDF3
	#else
	#	define INDIRECT_SPECULAR_TERM NoIndirectSpecularTerm
	#	define SPECULAR_TERM NoSpecularTerm
	#endif

	#ifdef USE_LAMBERT
	#define NO_NEED_INDIRECT_SPECULAR
	#endif

	#define USE_UNITYGI
	#include "../Predefine/DDPDShaderGI.cginc"
	#include "../Predefine/DDPDLightingFunc.cginc"

	half3 SpecularTerm_BRDF2_Anisotropic_Cloth1(half3 lightColor, half3 specColor, half nl, half3 worldNormal, half3 lightDir, half3 viewDir, half smoothness, half roughness, half3 worldTangent)
{
	half3 tangent = normalize(worldTangent + worldNormal);
	float3 halfDir = Unity_SafeNormalize(float3(lightDir)+viewDir);
	half th = dot(tangent, halfDir);
	half sinTH = sqrt(1 - th * th);
	float nh = saturate(dot(worldNormal, halfDir));
	float lh = saturate(dot(lightDir, halfDir));

	half a2 = roughness * roughness;

	half d = sinTH * sinTH * (a2 - 1.f) + 1.00001f;
	half specularTerm = 1;
	specularTerm = a2 / (max(0.1h, lh * lh) * (roughness + 0.5h) * (d * d) * 4.0h);
#if defined (SHADER_API_MOBILE)
	specularTerm = specularTerm - 1e-4f;
#endif
#if defined (SHADER_API_MOBILE)
	specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif

	return specularTerm * specColor * max(nl, 0) * lightColor;
}

	half3 DD_Common_LightingFunc_Direct_Cloth(in ShadingData_T shadingData, in LightingInput lightingInput, in DDLight light)
{
	half3 L = light.dir;
	half3 lightColor = light.color * GetShadowAtten(light.atten);
#ifdef LIGHT_ATTEN_SEPERATE
	lightColor *= light.lightAtten;
#endif

	half3 diffuse = shadingData.diffColor;
	half3 specular = shadingData.specColor;
	half smoothness = shadingData.smoothness;

	half perceptualRoughness = 1.0h - smoothness;
	half roughness = perceptualRoughness * perceptualRoughness;

	half nl = dot(lightingInput.worldNormal, L);
	half3 diffuseTerm = DIFFUSE_TERM(lightColor, diffuse, nl);
	half3 specularTerm = SPECULAR_TERM(lightColor, specular, nl, lightingInput.worldNormal, L, lightingInput.viewDir, smoothness, roughness);

	half3 freColor = 0;

#ifdef _USE_FRESNEL_ON
	half3 V = normalize(UnityWorldSpaceViewDir(lightingInput.worldPos));
	half3 N = lightingInput.worldNormal;
	half fre = pow(1-saturate(dot(V,N)),_FresnelPow);
	freColor = fre*_FresnelColor * shadingData.fresnelmask;
#endif

	half3 final = 0;
	final += diffuseTerm  + freColor + specularTerm;

#ifdef _USE_ANISOTROPIC_ON
	half3 specTerm1 = SpecularTerm_BRDF2_Anisotropic_Cloth1(lightColor, specular, nl, lightingInput.worldNormal, L, lightingInput.viewDir, smoothness, roughness, lightingInput.worldTangent);
	final = diffuseTerm  + freColor + specTerm1;
#endif
	return final;
}


#if defined(UNITY_PASS_FORWARDBASE)
	#include "../Predefine/DDPDLightingFunc.cginc"
	#define FUNC_LIGHTING_INDIRECT DD_Common_LightingFunc_Indirect
	#define FUNC_LIGHTING_DIRECT DD_Common_LightingFunc_Direct_Cloth
	#include "../Framework/DDShaderLighting.cginc"
#elif defined(UNITY_PASS_FORWARDADD)
	#include "../Predefine/DDPDLightingFunc.cginc"
	#define FUNC_LIGHTING_DIRECT DD_Common_LightingFunc_Direct_Cloth
	#include "../Framework/DDShaderLightingAdd.cginc"
#elif defined(UNITY_PASS_SHADOWCASTER)
	#include "../Framework/DDShaderShadow.cginc"
#elif  defined(UNITY_PASS_META)
	#include "../Framework/DDShaderMeta.cginc"
#endif
#endif
#endif