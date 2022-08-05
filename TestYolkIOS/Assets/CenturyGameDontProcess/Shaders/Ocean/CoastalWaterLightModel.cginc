#ifndef COASTAL_WATER_LIGHT_MODEL_INCLUDED
	#define COASTAL_WATER_LIGHT_MODEL_INCLUDED

	#define _NORMALMAP
	#define INPUT_NEED_SCREEN_UV
	#define INPUT_NEED_DEPTH
	#define INPUT_NEED_WORLD_POS
	//#define KEEP_ALPHA

	#include "../Framework/DDShaderLightingCommon.cginc"
	#include "../Predefine/DDPDSurfaceData.cginc"
	#include "UnityCG.cginc"
	
	#define PI 3.141592653
	#define PI2 6.2831852

	half _Number,_High,_HighMask;
	sampler2D _LutTex;
	sampler2D _WaveNoise;
	float4 _WaveNoise_ST;
	float4 _WaveNoise_TexelSize;
	half _Speed;
	//float4 _WaveA,_WaveB,_WaveC;
	//float _GerstnerWaveSpeed;
	half _FoamRange,_FoamRangeMask;
	half _ShorelineFoamMinDepth;
	sampler2D _OceanSurface;
	half _BumpScale;
	half _Smoothness;
	half _Specular;
	half4 _NormalTiling;
	half _ReflectionExposure;
	half _ReflectionRotation;
	half _HeightScale;
	half4 _WaveMove;
	half _TideHeight;
	half _TideFrequency;
	sampler2D _CoastWaterLUT;
	half _FresnelBias;

	sampler2D _CausticTexture;
	float4 _CausticTexture_ST;
	half _CausticDistortion;
	half _CausticDepthFade;
	half _CausticIntensity;
	half3 _CausticColor;

	float3 GerstnerWave(float4 wave, float3 p , float Speed)
	{
		float steepness = wave.z;
		float wavelength = wave.w;
		float Wi = 2 * 3.14 / wavelength;
		float Xi = sqrt(9.8 / Wi);
		float2 Di = normalize(wave.xy);
		float f = Wi * (dot(Di, p.xz) - Xi * _Time.y * Speed);

		float QA = steepness / Wi;
		//float Ai = X;
		//float Qi = 1/(Ai * Wi);
		//如果Qi是0，则是一般的正弦波；当Qi = 1/(wi*Ai)时，波峰最尖锐
		//Qi*Ai化简后则等于1/Wi;
		//steepness则控制Qi*Ai，0到1的变化

		return float3
		(
		Di.x * (QA * cos(f)),
		QA * sin(f),
		Di.y * (QA * cos(f))
		);
	}

	void vertex(inout a2v v)
	{
		float2 waveUV = v.texcoord.xy * _WaveNoise_ST.xy + _Time.y * _WaveMove.xy * _WaveMove.w;
		float waveHeight = tex2Dlod(_WaveNoise, float4(waveUV.xy, 0, 0)).r;
		waveHeight *= _HeightScale;

		v.vertex.y += waveHeight;
		float tideTime = frac(_Time.y * _TideFrequency);
		float tideHeight = tex2Dlod(_CoastWaterLUT, float4(tideTime, 0, 0, 0)).a;
		v.vertex.y += _TideHeight * tideHeight;

		//float mask = tex2Dlod(_WaveNoise,float4((v.texcoord - _Time.y*0.1) * _WaveNoise_ST.xy,0,0));

		//float uvx = frac(v.texcoord.x *_Number- _Time.y*_Speed);		
		//float x = tex2Dlod(_LutTex,float4(uvx,1,0,0))*PI2;
		//
		//float A = (1-abs(frac(v.texcoord.x + _HighMask)*2-1)) * _High ;
		//v.vertex.x += mask * v.texcoord.x*3; 
		//v.vertex.y = (A*cos(x)+A)/2;

		// float3 posOS = v.vertex.xyz;
		// float3 p = posOS;
		// v.vertex.xyz += GerstnerWave(_WaveA,posOS,_GerstnerWaveSpeed);
		// v.vertex.xyz += GerstnerWave(_WaveB,posOS,_GerstnerWaveSpeed);
		// v.vertex.xyz += GerstnerWave(_WaveC,posOS,_GerstnerWaveSpeed);
		
	}

	#define FUNC_VERTEX vertex

	struct WaterSurfaceData
	{
		half3 Albedo;
		half3 Normal;
		half Occlusion;
		half Smoothness;
		half3 Emission;
		float2 grabScreenUV;
		float2 screenUV;
		float depth;
		float height;
	};

	inline void RestOutput(out WaterSurfaceData IN)
	{
		IN.Albedo = 0;
		IN.Normal = half3(0, 0, 1);
		IN.Smoothness = 0;
		IN.Occlusion = 1;
		IN.Emission = 0;
		IN.grabScreenUV = 0;
		IN.screenUV = 0;
		IN.depth = 0;
		IN.height = 0;
	}

	void surf(Input IN, inout WaterSurfaceData o)
	{
		o.Albedo = 0;
		o.Smoothness = _Smoothness;
		o.grabScreenUV = IN.grabScreenUV;
		o.screenUV = IN.screenUV;
		o.depth = IN.depth;
		o.height = smoothstep(_FoamRange,1,frac(IN.uv.x * _Number- _Time.y*_Speed)) * smoothstep(_FoamRangeMask,1,IN.uv.x);

		float2 waveUV = IN.uv.xy * _WaveNoise_ST.xy + _Time.y * _WaveMove.xy * _WaveMove.w;
		float waveHeight = tex2Dlod(_WaveNoise, float4(waveUV.xy, 0, 0)).r;
		waveHeight *= _HeightScale;
		o.height = waveHeight;
		o.Normal = tex2D(_OceanSurface, waveUV);
		o.Normal.xy *= _HeightScale * _BumpScale;
		o.Normal = normalize(o.Normal);
	}

	#define SurfaceData_T WaterSurfaceData
	#define FUNC_SURF surf

	struct WaterShadingData
	{
		half3 diffColor;
		half smoothness;
		float2 grabScreenUV;
		float depth;
		float absDepth;
		float height;
		float3 grabWorldPos;
		//half alpha;
	};

	#define ShadingData_T WaterShadingData

	half _MaxWaterDepth;

	#ifndef DD_SHADER_EDITOR
		UNITY_DECLARE_DEPTH_TEXTURE(_DepthTexDOF);

		#define OCEAN_DEPTH_TEXTURE _DepthTexDOF
	#else
		UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

		#define OCEAN_DEPTH_TEXTURE _CameraDepthTexture
	#endif

	float3 ReconstructViewPos2(float2 uv, float eyeDepth)
	{
		float3x3 proj = (float3x3)unity_CameraProjection;
		float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
		float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);
		return float3((uv * 2.0 - 1.0 - p13_31) / p11_22 * (eyeDepth), eyeDepth);
	}

	float3 ReconstructWorldPos(float2 uv, float depth01)
	{
		float3 viewPos = ReconstructViewPos2(uv, depth01 * _ProjectionParams.z);
		float4 worldPos = mul(unity_CameraToWorld, float4(viewPos, 1));
		return worldPos.xyz;
	}

	inline void ShadingPrepare_Water(WaterSurfaceData IN, out WaterShadingData shadingData)
	{
		shadingData.diffColor = IN.Albedo;
		shadingData.smoothness = IN.Smoothness;
		shadingData.grabScreenUV = IN.grabScreenUV;

		float surfaceDepth = Linear01Depth(IN.depth);

		float sceneDepth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(OCEAN_DEPTH_TEXTURE, IN.screenUV));
		float ld = saturate((sceneDepth - surfaceDepth) * _ProjectionParams.z / _MaxWaterDepth);

		shadingData.grabWorldPos = ReconstructWorldPos(IN.screenUV, sceneDepth);

		shadingData.depth = max(0.01, ld);
		shadingData.absDepth = (sceneDepth - surfaceDepth) * _ProjectionParams.z;

		shadingData.height = IN.height;
		//shadingData.uv = IN.uv;

		//shadingData.alpha = saturate(((sceneDepth - surfaceDepth) * _ProjectionParams.z) / _ShorelineFoamMinDepth);
	}

	#define FUNC_SHADING_PREPARE ShadingPrepare_Water

	#define USE_UNITYGI
	#include "../Predefine/DDPDShaderGI.cginc"

	sampler2D _GrabTexture;

	half _AbsorptionR;
	half _AbsorptionG;
	half _AbsorptionB;
	half _AbsorptionScale;

	half3 _SubSurfaceColor;
	half _SubSurfaceBase;
	half _SubSurfaceSun;
	half _SubSurfaceSunFallOff;
	

	sampler2D _FoamTexture;
	sampler2D _FoamNoise;
	half4 _FoamNoise_ST;
	half _FoamSpeed;
	half _FoamUVScale;
	half _FoamEdge;

	inline float _Pow5(float x)
	{
		return x * x * x * x * x;
	}

	float CalculateFresnelReflectionCoefficient(float cosTheta)
	{
		float waterF0 = 0.02F;
		float t = _Pow5(max(0., 1.0 - cosTheta));
		const float R_theta = waterF0 + (1.0 - waterF0) * t;
		return R_theta;
	}

	half3 Caustics(float3 grabWorldPos, half3 worldNormal, half absDepth)
	{
		float depthFadeScale = _CausticDepthFade * _CausticDepthFade;
		float depthFade = exp(-max(0.0, absDepth) * depthFadeScale);

		float2 uv = grabWorldPos.xz * _CausticTexture_ST.xy + worldNormal.xz * _CausticDistortion;

		half3 caustic = tex2D(_CausticTexture, uv);
		return caustic * _CausticIntensity * depthFade;
	}

	half3 Refraction(float2 uv, half depth)
	{
		half3 refraction = tex2D(_GrabTexture, uv).rgb;
		half3 absorption = tex2D(_CoastWaterLUT, half2(depth, 0));

		return refraction * absorption;
	}

	inline half3 DD_DecodeHDR2(half4 data, half4 decodeInstructions)
	{
		// Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
		half alpha = decodeInstructions.w * (data.a - 1.0) + 1.0;

		// If Linear mode is not supported we can skip exponent part
		//#if defined(UNITY_COLORSPACE_GAMMA)
		//    return (decodeInstructions.x * alpha) * data.rgb;
		//#else
	#if defined(UNITY_USE_NATIVE_HDR)
		return decodeInstructions.x * data.rgb; // Multiplier for future HDRI relative to absolute conversion.
	#else
		return (decodeInstructions.x * pow(alpha, decodeInstructions.y)) * data.rgb;
	#endif
		//#endif
	}

	half3 DD_GlossyEnvironment2(UNITY_ARGS_TEXCUBE(tex), half4 hdr, half perceptualRoughness, half3 reflUVW)
	{
		// TODO: CAUTION: remap from Morten may work only with offline convolution, see impact with runtime convolution!
		// For now disabled
		perceptualRoughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);

		half mip = perceptualRoughnessToMipmapLevel(perceptualRoughness);
		half3 R = reflUVW;
		half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, R, mip);
	#if defined(UNITY_COLORSPACE_GAMMA)
		rgbm.rgb = GammaToLinearSpace(rgbm.rgb);
	#endif
		return DD_DecodeHDR2(rgbm, hdr);
	}


	half3 Reflection(half Smoothness, half3 worldViewDir, half3 Normal)
	{
		half3 reflUVW = reflect(-worldViewDir, Normal);

		half rcos = cos(_ReflectionRotation);
		half rsin = sin(_ReflectionRotation);

		reflUVW = half3(
			reflUVW.x * rcos - reflUVW.z * rsin,
			reflUVW.y,
			reflUVW.x * rsin + reflUVW.z * rcos);

		half3 env0 = DD_GlossyEnvironment2(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, 1 - Smoothness, reflUVW);
		return env0;
	}

	half3 DD_Skin_LightingFunc_Indirect(in WaterShadingData shadingData, in LightingInput lightingInput, in DDIndirect indirect)
	{
		half edgeFade = saturate(shadingData.absDepth / _ShorelineFoamMinDepth);
		float fresnel = CalculateFresnelReflectionCoefficient(dot(lightingInput.viewDir, lightingInput.worldNormal));
		fresnel += _FresnelBias;
		fresnel *= edgeFade;
		half3 refraction = Refraction(shadingData.grabScreenUV, shadingData.depth);
		refraction += Caustics(shadingData.grabWorldPos, lightingInput.worldNormal, shadingData.absDepth) * edgeFade;
		half3 reflection = Reflection(shadingData.smoothness, lightingInput.viewDir, lightingInput.worldNormal) * _ReflectionExposure;
		return lerp(refraction, reflection, saturate(fresnel));
	}

	#include "../Predefine/DDPDBRDF.cginc"

	half3 DD_Skin_LightingFunc_Direct(in WaterShadingData shadingData, in LightingInput lightingInput, in DDLight light)
	{
		half smoothness = shadingData.smoothness;
		half perceptualRoughness = 1.0h - smoothness;
		half roughness = perceptualRoughness * perceptualRoughness;

		half nl = dot(lightingInput.worldNormal, light.dir);
		half3 specularTerm = SpecularTerm_PBR_BRDF2(light.color, _Specular, nl, lightingInput.worldNormal, light.dir, lightingInput.viewDir, smoothness, roughness);

		half foamIntensity = saturate(shadingData.absDepth / _ShorelineFoamMinDepth) * saturate(1 - shadingData.absDepth + _FoamEdge);

		half2 foamUV1 = lightingInput.worldPos.xz * _FoamUVScale + _FoamSpeed * _Time.y;
		half2 foamUV2 = lightingInput.worldPos.xz * _FoamUVScale - _FoamSpeed * _Time.y;
		half3 foam1 = tex2D(_FoamTexture, foamUV1);
		half3 foam2 = tex2D(_FoamTexture, foamUV2);
		half foam3 = foam1 * shadingData.height;
		half3 noise = tex2D(_FoamNoise, foamUV1 * _FoamNoise_ST.xy + _FoamNoise_ST.zw).rgb;

		half3 foam = foamIntensity * (foam1 + foam2) * 0.5h * noise;

		return specularTerm + foam;
		return foamIntensity * (foam1 + foam2) * 0.5h * noise;
		return foam3 * (noise * saturate(shadingData.absDepth / _ShorelineFoamMinDepth));
		//return foam * tex2D(_FoamTexture, foamUV);
		return saturate(shadingData.absDepth / _ShorelineFoamMinDepth) * saturate(1 - shadingData.absDepth);
		return 0;
		//return shadingData.depth;
		//return light.atten;
		//return shadingData.depth;
		half3 lightColor = light.color * light.atten;

		//float v = abs(viewDir.y);
		//half sssAtten = /*dot(worldNormal, light.dir) +*/ dot(light.dir, -viewDir);
		//half towardsSun = pow(saturate(sssAtten), _SubSurfaceSunFallOff);

		//half3 absorption = half3(_AbsorptionR, _AbsorptionG, _AbsorptionB);
		//absorption = exp(-absorption * (shadingData.depth / 2) * _MaxWaterDepth * _AbsorptionScale);
		////return absorption;
		//half3 subsurface = (_SubSurfaceBase + _SubSurfaceSun * towardsSun) * _SubSurfaceColor.rgb * lightColor;
		//subsurface *= (1.0 - v * v) * 0.48;
		//float fresnel = CalculateFresnelReflectionCoefficient(dot(viewDir, worldNormal));
		//half nl = max(0, dot(light.dir, half3(0, 1, 0)));
		////return absorption * (1 - fresnel) * nl;
		////return absorption;
		////return 1 - fresnel;
		//return subsurface * (1 - fresnel);

		half LA = (dot(lightingInput.worldNormal, -light.dir) + dot(lightingInput.viewDir, -light.dir));
		half3 L = -light.dir;
		half ee = pow(saturate(dot(lightingInput.viewDir, L)), _SubSurfaceSunFallOff) * _SubSurfaceSun;
		half3 lt = LA * ee;
		return lt;
		//return (1 - shadingData.depth);
		return lt;

		//LA = ;
		return LA;






		//return shadingData.diffColor;
		return 0;
	}

	#ifdef UNITY_PASS_FORWARDBASE
		#define FUNC_LIGHTING_INDIRECT DD_Skin_LightingFunc_Indirect
		#define FUNC_LIGHTING_DIRECT DD_Skin_LightingFunc_Direct
		#include "../Framework/DDShaderLighting.cginc"
	#elif defined(UNITY_PASS_FORWARDADD)
		#define FUNC_LIGHTING_DIRECT DD_Skin_LightingFunc_Direct
		#include "../Framework/DDShaderLightingAdd.cginc"
	#elif defined(UNITY_PASS_SHADOWCASTER)
		#include "../Framework/DDShaderShadow.cginc"
	#endif

#endif