Shader "DianDian/Ocean/Coastalwater1"
{
    Properties
    {
        _MaxWaterDepth("海水深度", Range(0.1,10)) = 3
        _ShorelineFoamMinDepth("近岸虚化", Range(0.01, 5.0)) = 0.27
        _Saturation("饱和度",Range(1.0,5.0)) = 1 

        [Header(Distort)]
        [Space(10)]
        [NoScaleOffset]_OceanSurface("水波法线", 2D) = "black" {}
        //_BumpScale("水波法线强度", Float) = 1
        _NormalDisInt("水底法线扭曲",Range(0,1)) = 0
        _NorDirOff("法线扭曲修正",vector) = (0,0,0,0)
        _WaveTex("海浪贴图",2D) = "white" {} 
        _WaveNoise("波形扭曲噪声", 2D) = "white" {}
        _HeightScale("波形扭曲强度", Float) = 0.1
        //_TideHeight("海浪高度", Float) = 1
        //_TideFrequency("海浪频率", Float) = 1
        
        _WaveInt("海浪亮度",Range(0,5)) = 0 
        _WaveDisInt("海浪扭曲强度",Range(0,1)) = 0
        
        [Header(NormalSpec)]
        _Smoothness("光滑度",Range(0,1)) = 0.95
        _Specular("高光强度",Range(0,1)) = 1
        _ReflectionExposure("反射曝光", Float) = 1
        _ReflectionRotation("反射旋转", Float) = 0
        _FresnelBias("反射范围", Range(-1, 1))=0
        
        [Header(StarSpec)]
        [Space(10)]
        _WaterLight("闪片高光",2D) = "white" {}     
        _DistorNiose("闪片高光Niose",2D) = "white" {}
        _FoamNoise("闪片mask", 2D) = "white" {}
        _SpecDisInt("闪片扭曲强度",Range(0,1)) = 1
        _SpecInt("闪片亮度(5)",Range(0,10)) = 5 

        //_WaveMove("水波移动 x,y: 移动方向 w:移动速度", Vector) = (1, 1, 0, 0.1)     
        //_FoamTexture("白沫贴图", 2D) = "white"
        //_FoamEdge("近岸白沫宽度", Float) = 0
        //_FoamSpeed("白沫位移速度", Float) = 1
        //_FoamUVScale("白沫UV重复", Float) = 1
        
        [Header(Caustics)]
        [Space(10)]
        [NoScaleOffset]_CausticTexture("焦散贴图", 2D) = "black" {}
        //_CausticDistortion("焦散扰动", Range(0, 50)) = 0.25
        //_CausticDepthFade("深度渐变", Range(0, 1)) = 0.1
        _CausticIntensity("焦散强度", Range(0, 10)) = 0.75
        //_CausticColor("颜色", Color) = (1, 1, 1)
        _CausticSize("焦散重复01(XY) 焦散重复02(ZW)",vector) = (1,1,1,1)
        _CausticSpeedDir("焦散方向(XY) 速度(Z)",vector) = (1,1,1,1)  
        
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 300

        GrabPass{}

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite Off
            
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile _ DD_SHADER_EDITOR
            #pragma fragmentoption ARB_precision_hint_fastest
            #define DD_HIGH

            #include "CoastalWaterLightModel1.cginc"

            ENDCG
        }
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
	    Blend SrcAlpha One

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            ZWrite Off
            
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            //#pragma multi_compile_fwdbase
            //#pragma multi_compile_fog
            #pragma multi_compile _ DD_SHADER_EDITOR
            #define DD_MIDDLE
            //#pragma fragmentoption ARB_precision_hint_fastest

            #include "CoastalWaterLightModel1.cginc"

            ENDCG
        }
    }
}
