Shader "Hidden/PostProcess/FPSSSS" 
{
    Properties 
	{
		//_StencilTex("Texture", 2D) = "white" {}
		_Stencil("Stencil", float) = 5
		_SSSScale("SSSScale", float) = 2
    }
    
    CGINCLUDE
	#include "UnityCG.cginc" 
	#define DistanceToProjectionWindow 5.671281819617709             //1.0 / tan(0.5 * radians(20));
	#define DPTimes300 1701.384545885313                             //DistanceToProjectionWindow * 300
	#define SamplerSteps 7
	uniform sampler2D _DepthTex;
	float4 _DepthTex_TexelSize;

	uniform sampler2D _StencilTex;
	//uniform float4 _StencilTex_ST;
	uniform float _SSSScale;
	uniform float4 _Kernel[SamplerSteps];

	struct VertexInput 
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};
	struct VertexOutput 
	{
		float4 pos : SV_POSITION;
		float2 colorUV : TEXCOORD0;
		float2 depthUV : TEXCOORD1;
	};
	VertexOutput vert(VertexInput v) 
	{
		VertexOutput o;
		o.pos = v.vertex;
		o.colorUV = v.uv;
		o.depthUV = v.uv;
#if !UNITY_UV_STARTS_AT_TOP
		o.colorUV.y = 1.0 - o.colorUV.y;
#endif

		return o;
	}

	float4 SSS(float4 SceneColor, float2 colorUV, float2 depthUV, float2 SSSIntencity)
	{
		float SceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_DepthTex, depthUV));
		float BlurLength = DistanceToProjectionWindow / SceneDepth;
		float2 UVOffset = SSSIntencity * BlurLength;
		float4 BlurSceneColor = SceneColor;
		BlurSceneColor.rgb *= _Kernel[0].rgb;

		[loop]
		for (int i = 1; i < SamplerSteps; i++)
		{
			float4 SSSSceneColor = tex2D(_StencilTex, colorUV + _Kernel[i].a * UVOffset);
			//float SSSDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_DepthTex, depthUV + _Kernel[i].a * UVOffset)).r;
			float SSSDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_DepthTex, depthUV + _Kernel[i].a * UVOffset));
			float SSSScale = saturate(DPTimes300 * SSSIntencity * abs(SceneDepth - SSSDepth));
			SSSSceneColor.rgb = lerp(SSSSceneColor.rgb, SceneColor.rgb, SSSScale);
			BlurSceneColor.rgb += _Kernel[i].rgb * SSSSceneColor.rgb;
		}
		return BlurSceneColor;
	}
    ENDCG

    SubShader 
	{
        ZTest Always
        ZWrite Off 
        Cull Off
        Stencil
	    {
            Ref[_Stencil]
            comp equal
            pass keep
        }
        Pass 
	    {
            Name "XBlur"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0

            float4 frag(VertexOutput i) : SV_TARGET 
	        {
                float4 SceneColor = tex2D(_StencilTex, i.colorUV);
                float SSSIntencity = (_SSSScale * _DepthTex_TexelSize.x);
                float3 XBlur = SSS(SceneColor, i.colorUV, i.depthUV, float2(SSSIntencity, 0) ).rgb;
                return float4(XBlur, SceneColor.a);
            }
            ENDCG
        } 
		Pass 
		{
            Name "YBlur"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0

            float4 frag(VertexOutput i) : COLOR 
			{
                float4 SceneColor = tex2D(_StencilTex, i.colorUV);
                float SSSIntencity = (_SSSScale * _DepthTex_TexelSize.y);
                float3 YBlur = SSS(SceneColor, i.colorUV, i.depthUV, float2(0, SSSIntencity)).rgb;
                return float4(YBlur, SceneColor.a);
            }
            ENDCG
        }
    }
}
