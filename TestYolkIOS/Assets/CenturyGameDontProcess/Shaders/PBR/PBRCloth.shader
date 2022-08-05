Shader "DianDian/PBRCloth"
{
	Properties
	{
        _Color("Color", Color) = (0.5,0.5,0.5,1)
		_MainTex("MainTex", 2D) = "white" {}
		[Normal][NoScaleOffset]_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("BumpScale", float) = 1.0
		[NoScaleOffset]_MetallicGlossMap("Mask(R:Metallic)(G:Smoothness)(B:Occlusion)(A:Emission)", 2D) = "white" {}
		[Gamma]_Metallic("Metallic Scale", Range(0.0, 1.0)) = 0.0
		[Toggle]_Use_Anisotropic("Use Anisotropic", float) = 0
		_Glossiness("Smoothness Scale", Range(0.0, 1.0)) = 0.5
		_OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
		_EmissionColor("Emission", Color) = (0,0,0,1)

		[Toggle]_Use_Detail("Use Detail", float) = 0
		[NoScaleOffset]_DetailMask("Detail Mask", 2D) = "white" {}
		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        [Normal][NoScaleOffset]_DetailNormalMap("Normal Map", 2D) = "bump" {}
        _DetailNormalMapScale("Scale", Float) = 1.0

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle]_Use_Clipplane("Use Clipplane", float) = 0

		[Toggle]_Use_Fresnel("Use Fresnel", float) = 0
		_FresnelMask("Fresnel Mask",2D) = "white" {}
		_FresnelPow("Fresnel Pow",Range(0.01,10)) = 5
		_FresnelColor("Fresnel Color",Color) = (1,1,1,1)

        // Blending state
        [HideInInspector] _Mode ("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0

		[HideInInspector] _QueueOffset("Queue offset", Int) = 0.0
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
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
			Cull Back
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma shader_feature_local _USE_DETAIL_ON
			#pragma multi_compile _ _USE_COLOR_SHADOW
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local _USE_CLIPPLANE_ON
			#pragma shader_feature_local _USE_FRESNEL_ON
			#pragma shader_feature_local _USE_ANISOTROPIC_ON

			#define _NORMALMAP

			#define USE_PBR_BRDF2
			#if _USE_CLIPPLANE_ON
				#define INPUT_NEED_WORLD_POS
				#define CLIPPLANE
			#endif
			
			#if _USE_FRESNEL_ON
				#define _FRESNEL_ON
			#endif

			#if _USE_ANISOTROPIC_ON
				#define _ANISOTROPIC_ON
			#endif

			#include "UnityPBRSurface_Cloth.cginc"

			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ForwardAdd" }

            Blend [_SrcBlend] One
			ZWrite Off
			ZTest LEqual
			Cull Back

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#pragma multi_compile_fwdadd
			#pragma multi_compile_fog
			#pragma multi_compile_fwdadd_fullshadows
			#pragma shader_feature_local _USE_DETAIL_ON
			#pragma multi_compile _ _USE_COLOR_SHADOW
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local _USE_CLIPPLANE_ON

			#define _NORMALMAP

			#define USE_PBR_BRDF2
			#if _USE_CLIPPLANE_ON
				#define INPUT_NEED_WORLD_POS
				#define CLIPPLANE
			#endif

			#define LIGHT_ATTEN_SEPERATE

			#include "UnityPBRSurface_Cloth.cginc"

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
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local _USE_CLIPPLANE_ON

			#if _USE_CLIPPLANE_ON
				#define INPUT_NEED_WORLD_POS
				#define CLIPPLANE
			#endif

			#include "UnityPBRSurface_Cloth.cginc"

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

			#include "UnityPBRSurface_Cloth.cginc"

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
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
			Cull Back
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma shader_feature_local _USE_DETAIL_ON
			#pragma multi_compile _ _USE_COLOR_SHADOW
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local _USE_CLIPPLANE_ON

			#define _NORMALMAP

			#define USE_PBR_BRDF3
			#if _USE_CLIPPLANE_ON
				#define INPUT_NEED_WORLD_POS
				#define CLIPPLANE
			#endif

			#include "UnityPBRSurface_Cloth.cginc"

			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ForwardAdd" }

            Blend [_SrcBlend] One
			ZWrite Off
			ZTest LEqual
			Cull Back

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#pragma multi_compile_fwdadd
			#pragma multi_compile_fog
			#pragma multi_compile_fwdadd_fullshadows
			#pragma shader_feature_local _USE_DETAIL_ON
			#pragma multi_compile _ _USE_COLOR_SHADOW
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local _USE_CLIPPLANE_ON

			#define _NORMALMAP

			#define USE_PBR_BRDF3
			#if _USE_CLIPPLANE_ON
				#define INPUT_NEED_WORLD_POS
				#define CLIPPLANE
			#endif
			#define LIGHT_ATTEN_SEPERATE

			#include "UnityPBRSurface_Cloth.cginc"

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
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local _USE_CLIPPLANE_ON

			#if _USE_CLIPPLANE_ON
				#define INPUT_NEED_WORLD_POS
				#define CLIPPLANE
			#endif

			#include "UnityPBRSurface_Cloth.cginc"

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

			#include "UnityPBRSurface_Cloth.cginc"

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
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
			Cull Back
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma shader_feature_local _USE_DETAIL_ON
			#pragma multi_compile _ _USE_COLOR_SHADOW
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local _USE_CLIPPLANE_ON

			#define _NORMALMAP

			#define USE_LAMBERT
			#if _USE_CLIPPLANE_ON
				#define INPUT_NEED_WORLD_POS
				#define CLIPPLANE
			#endif
			#include "UnityPBRSurface_Cloth.cginc"

			ENDCG
		}

		Pass
		{
			Tags { "LightMode" = "ForwardAdd" }

            Blend [_SrcBlend] One
			ZWrite Off
			ZTest LEqual
			Cull Back

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vertAdd
			#pragma fragment fragAdd
			#pragma multi_compile_fwdadd
			#pragma multi_compile_fog
			#pragma multi_compile_fwdadd_fullshadows
			#pragma shader_feature_local _USE_DETAIL_ON
			#pragma multi_compile _ _USE_COLOR_SHADOW
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local _USE_CLIPPLANE_ON

			#define _NORMALMAP

			#define USE_LAMBERT
			#if _USE_CLIPPLANE_ON
				#define INPUT_NEED_WORLD_POS
				#define CLIPPLANE
			#endif
			#define LIGHT_ATTEN_SEPERATE

			#include "UnityPBRSurface_Cloth.cginc"

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
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local _USE_CLIPPLANE_ON

			#if _USE_CLIPPLANE_ON
				#define INPUT_NEED_WORLD_POS
				#define CLIPPLANE
			#endif

			#include "UnityPBRSurface_Cloth.cginc"

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

			#include "UnityPBRSurface_Cloth.cginc"

			ENDCG
		}
	}

	 CustomEditor "DDRenderPipeline.PBRShaderGUI_Cloth"
}