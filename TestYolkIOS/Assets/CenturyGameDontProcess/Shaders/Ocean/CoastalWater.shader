Shader "DianDian/Ocean/Coastalwater"
{
    Properties
    {
        _TideHeight("海浪高度", Float) = 1
        _TideFrequency("海浪频率", Float) = 1

        _WaveNoise("水波噪声", 2D) = "white" {}
        _HeightScale("水波高度", Float) = 0.1

        [NoScaleOffset]_OceanSurface("水波法线", 2D) = "black" {}
        _BumpScale("水波法线强度", Float) = 1

        _WaveMove("水波移动 x,y: 移动方向 w:移动速度", Vector) = (1, 1, 0, 0.1)

        _MaxWaterDepth("海水深度", Range(0.1,10)) = 3
        _ShorelineFoamMinDepth("近岸虚化", Range(0.01, 5.0)) = 0.27

        _Smoothness("光滑度",Range(0,1)) = 0.95
        _Specular("高光强度",Range(0,1)) = 1

        _ReflectionExposure("反射曝光", Float) = 1
        _ReflectionRotation("反射旋转", Float) = 0
        _FresnelBias("反射范围", Range(-1, 1))=0

        _FoamTexture("白沫贴图", 2D) = "white"
        _FoamEdge("近岸白沫宽度", Float) = 0
        _FoamSpeed("白沫位移速度", Float) = 1
        _FoamUVScale("白沫UV重复", Float) = 1
        _FoamNoise("白沫Noise", 2D) = "white" {}

        _CausticTexture("焦散贴图", 2D) = "black" {}
        _CausticDistortion("焦散扰动", Range(0, 50)) = 0.25
        _CausticDepthFade("深度渐变", Range(0, 1)) = 0.1
        _CausticIntensity("强度", Range(0, 2)) = 0.75
        _CausticColor("颜色", Color) = (1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

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

            #include "CoastalWaterLightModel.cginc"

            ENDCG
        }
    }
}
