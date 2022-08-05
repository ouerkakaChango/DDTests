Shader "DianDian/Ocean/Beach"
{
    Properties
    {
        _Color("Color", Color) = (0.5,0.5,0.5,1)
        _MainTex("MainTex", 2D) = "white" {}
        [Normal][NoScaleOffset]_BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("BumpScale", float) = 1.0
        [NoScaleOffset]_MetallicGlossMap("Mask(R:Metallic)(G:Smoothness)(B:Occlusion)(A:Emission)", 2D) = "white" {}
        [Gamma]_Metallic("Metallic Scale", Range(0.0, 1.0)) = 0.0
        _Glossiness("Smoothness Scale", Range(0.0, 1.0)) = 0.5
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _EmissionColor("Emission", Color) = (0,0,0,1)

		[Space][Space][Space][Space]
		[Header(Ocean)]
		_WetColor("变湿颜色", Color) = (1, 1, 1, 1)
		_WetColorTex("变湿贴图", 2D) = "white" {}
		[Normal][NoScaleOffset]_WetBumpMap("Wet Normal Map", 2D) = "bump" {}
		_WetBumpScale("Wet Bump Scale", float) = 1.0

		[Space]
		_SeaHeight("海平面高度", Float) = 0
		_TideHeight("海浪高度", Float) = 1
		_TideFrequency("海浪频率", Float) = 1
		_SeaHightestTime("海水最高点时间", Range(0, 1)) = 0.5

		[HideInInspector]_WaveNoise("波形噪声", 2D) = "white" {}
		[HideInInspector]_HeightScale("水波高度", Float) = 0.1
		[HideInInspector]_WaveMove("水波移动 x,y: 移动方向 w:移动速度", Vector) = (1, 1, 0, 0.1)

		[Space]
		_DrySpeed("变干速度", Float) = 0
		_WetDryEdge("干湿过渡区", Float) = 0.1
    }

	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
		}

		LOD 300

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile _ _USE_COLOR_SHADOW

			#define _NORMALMAP

			#define USE_PBR_BRDF2

			#include "BeachSurface.cginc"

			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ForwardAdd" }

			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#pragma multi_compile_fwdadd
			#pragma multi_compile_fog
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile _ _USE_COLOR_SHADOW

			#define _NORMALMAP

			#define USE_PBR_BRDF2

			#define LIGHT_ATTEN_SEPERATE

			#include "BeachSurface.cginc"

			ENDCG
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma vertex vert_shadow
			#pragma fragment frag_shadow
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest

			#include "BeachSurface.cginc"

			ENDCG
		}

		Pass
		{
			Name "META"
			Tags { "LightMode" = "Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature EDITOR_VISUALIZATION

			#define USE_PBR_BRDF2

			#include "BeachSurface.cginc"

			ENDCG
		}
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
		}

		LOD 200

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile _ _USE_COLOR_SHADOW

			#define _NORMALMAP

			#define USE_PBR_BRDF3

			#include "BeachSurface.cginc"

			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ForwardAdd" }

			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#pragma multi_compile_fwdadd
			#pragma multi_compile_fog
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile _ _USE_COLOR_SHADOW

			#define _NORMALMAP

			#define USE_PBR_BRDF3

			#define LIGHT_ATTEN_SEPERATE

			#include "BeachSurface.cginc"

			ENDCG
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma vertex vert_shadow
			#pragma fragment frag_shadow
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest

			#include "BeachSurface.cginc"

			ENDCG
		}

		Pass
		{
			Name "META"
			Tags { "LightMode" = "Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature EDITOR_VISUALIZATION

			#define USE_PBR_BRDF2

			#include "BeachSurface.cginc"

			ENDCG
		}
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
		}

		LOD 100

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile _ _USE_COLOR_SHADOW

			#define _NORMALMAP

			#define USE_LAMBERT

			#include "BeachSurface.cginc"

			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ForwardAdd" }

			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#pragma multi_compile_fwdadd
			#pragma multi_compile_fog
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile _ _USE_COLOR_SHADOW

			#define _NORMALMAP

			#define USE_LAMBERT

			#define LIGHT_ATTEN_SEPERATE

			#include "BeachSurface.cginc"

			ENDCG
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma vertex vert_shadow
			#pragma fragment frag_shadow
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest

			#include "BeachSurface.cginc"

			ENDCG
		}

		Pass
		{
			Name "META"
			Tags { "LightMode" = "Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature EDITOR_VISUALIZATION

			#define USE_PBR_BRDF2

			#include "BeachSurface.cginc"

			ENDCG
		}
	}
}
