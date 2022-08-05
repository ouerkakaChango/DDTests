#ifndef UNITY_PBR_SURFACE_INCLUDED
#define UNITY_PBR_SURFACE_INCLUDED

#define TEX1_UVST _MainTex_ST
#define TEX2_UVST _WetColorTex_ST
#define INPUT_NEED_WORLD_POS

#include "../Framework/DDShaderLightingCommon.cginc"
#include "../Predefine/DDPDSurfaceData.cginc"
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

half _TideHeight;
half _TideFrequency;
sampler2D _CoastWaterLUT;
half4 _WetColor;
sampler2D _WetColorTex;
sampler2D _WetBumpMap;
half _WetBumpScale;
float4 _WetColorTex_ST;
half _SeaHeight;
half _SeaHightestTime;
half _DrySpeed;
half _WetDryEdge;

void surf(Input IN, inout CommonSurfaceData o)
{
	half4 col = tex2D(_MainTex, IN.uv);
#ifdef UNITY_COLORSPACE_GAMMA
	col.rgb = GammaToLinearSpace(col.rgb);
#endif
	half3 albedo = col.rgb * _Color.rgb;

	o.Albedo = albedo;
	o.Alpha = col.a * _Color.a;
	half3 normal = ScaleNormal(tex2D(_BumpMap, IN.uv), _BumpScale);

	o.Normal = normal;

	half4 mask = tex2D(_MetallicGlossMap, IN.uv);
	o.Metallic = mask.r * _Metallic;
	o.Smoothness = mask.g * _Glossiness;
	o.Occlusion = LerpOneTo(mask.b, _OcclusionStrength);
	o.Emission = mask.a * _EmissionColor.rgb * o.Albedo;


	half4 wetCol = tex2D(_WetColorTex, IN.uv2);
	half3 wetAlbedo = wetCol.rgb * _WetColor.rgb;
	half3 wetNormal = ScaleNormal(tex2D(_WetBumpMap, IN.uv2), _WetBumpScale);
	//o.Albedo = wetAlbedo;

	float tideTime = frac(_Time.y * _TideFrequency);
	float tideHeight = tex2Dlod(_CoastWaterLUT, float4(tideTime, 0, 0, 0)).a;
	half h = _SeaHeight + _TideHeight * tideHeight;

	half ot = tideTime - _SeaHightestTime;
	half gTime = ot >= 0 ? ot : 1 + ot;

	half dry = saturate(gTime * _DrySpeed);

	half wet = h > IN.worldPos.y;
	wet = (h + _WetDryEdge - IN.worldPos.y) / _WetDryEdge;
	wet = saturate(wet);

	half wet2 = 0;
	half baseDry = ((_WetDryEdge + _SeaHeight + _TideHeight) - IN.worldPos.y) / _WetDryEdge;
	baseDry = saturate(baseDry);
	wet2 = baseDry - dry;

	wet = max(wet, wet2);

	o.Albedo = lerp(albedo, wetAlbedo, wet);
	o.Normal = lerp(normal, wetNormal, wet);
}

#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
#define KEEP_ALPHA
#endif

#define SurfaceData_T CommonSurfaceData
#define FUNC_SURF surf
#define USE_UNITY_LIGHT_MODEL

#include "../PBR/UnityLightModel.cginc"

#endif