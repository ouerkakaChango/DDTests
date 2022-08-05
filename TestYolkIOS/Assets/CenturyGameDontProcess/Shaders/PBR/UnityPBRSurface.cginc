#ifndef UNITY_PBR_SURFACE_INCLUDED
#define UNITY_PBR_SURFACE_INCLUDED

#define CARE_FACING
#define BACK_FACE_REVERT
#define TEX1_UVST _MainTex_ST
#ifdef _USE_DETAIL_ON
#define TEX2_UVST _DetailAlbedoMap_ST
#endif

#include "../Framework/DDShaderLightingCommon.cginc"
#include "../Framework/DDShaderUtil.cginc"
#include "UnityCG.cginc"
#include "UnityStandardUtils.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
half4 _Color;

sampler2D _BumpMap;
half _BumpScale;
sampler2D _MetallicGlossMap;
half _Metallic;
half _Glossiness;
half _OcclusionStrength;
half4 _EmissionColor;

sampler2D _DetailMask;
sampler2D _DetailAlbedoMap;
float4 _DetailAlbedoMap_ST;
sampler2D _DetailNormalMap;
half _DetailNormalMapScale;
float4x4 _MatrixClipplane;

float4 _FresnelColor;
float _FresnelPow;
sampler2D _FresnelMask;

sampler2D _NoiseTex;
float4 _NoiseTex_ST;
float _Off,_OffInt;

struct ClothSurfaceData
{
	half3 Albedo;
	half3 Normal;
	half3 Emission;
	half Metallic;
	half Smoothness;
	half Occlusion;
	half Alpha;
#if _USE_FRESNEL_ON
	half Fresnelmask;
#endif
};

inline void RestOutput(out ClothSurfaceData IN)
{
	IN.Albedo = 0;
	IN.Normal = half3(0, 0, 1);
	IN.Emission = 0;
	IN.Metallic = 0;
	IN.Smoothness = 0;
	IN.Occlusion = 1;
	IN.Alpha = 1;
#if _USE_FRESNEL_ON
	IN.Fresnelmask = 1;
#endif
}

void surf(Input IN, inout ClothSurfaceData o)
{
#ifdef _USE_CLIPPLANE_ON
	float4 clippos = mul(_MatrixClipplane, float4(IN.worldPos, 1));  
	clip(clippos.y); 

#endif

	half4 col = tex2D(_MainTex, IN.uv);
#ifdef UNITY_COLORSPACE_GAMMA
	col.rgb = GammaToLinearSpace(col.rgb);
#endif
	half3 albedo = col.rgb * _Color.rgb;

#if _USE_DETAIL_ON
	half detailMask = tex2D(_DetailMask, IN.uv).a;
	half3 detailAlbedo = tex2D(_DetailAlbedoMap, IN.uv2).rgb;
	albedo *= LerpWhiteTo(detailAlbedo * unity_ColorSpaceDouble.rgb, detailMask);
#endif

	o.Albedo = albedo;
	float4 noise = tex2D(_NoiseTex, IN.uv * _NoiseTex_ST.xy);
	half alpha = col.a * _Color.a;
	half alphaEmissMask = 1;
#ifdef _USE_DISSOLVE_ON
	float3 ObjectPos = IN.worldPos - GetModelWorldPos().xyz;
	alpha = smoothstep(_Off,_Off+0.5, noise.r + ObjectPos.y*_OffInt)*alpha;
	alphaEmissMask = saturate( alpha);
#endif

	o.Alpha = alpha;
	half3 normal = ScaleNormal(tex2D(_BumpMap, IN.uv), _BumpScale);

#if defined(_USE_DETAIL_ON)
	half3 detailNormal = ScaleNormal(tex2D(_DetailNormalMap, IN.uv2), _DetailNormalMapScale);
	normal = lerp(normal, BlendNormals(normal, detailNormal), detailMask);
#endif
	o.Normal = normal;

	half4 mask = tex2D(_MetallicGlossMap, IN.uv);
	o.Metallic = mask.r * _Metallic;
	o.Smoothness = mask.g * _Glossiness;
	o.Occlusion = LerpOneTo(mask.b, _OcclusionStrength);
	o.Emission = mask.a * _EmissionColor.rgb * o.Albedo * alphaEmissMask;
#if _USE_FRESNEL_ON
	o.Fresnelmask = tex2D(_FresnelMask,IN.uv).r;
#endif
}

#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
#define KEEP_ALPHA
#endif

#define SurfaceData_T ClothSurfaceData
#define FUNC_SURF surf
#define USE_UNITY_LIGHT_MODEL

#include "UnityLightModel.cginc"

#endif