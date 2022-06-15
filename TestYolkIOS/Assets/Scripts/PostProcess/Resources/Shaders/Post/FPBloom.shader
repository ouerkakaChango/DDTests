Shader "Hidden/PostProcess/FPBloom" 
{
	Properties 
	{
		_MainTex ("Main (RGB)", 2D) = "white" {}
		_Threshold("Threshold", float) = 0.0
		_Curve("Curve", Vector) = (0.0,0.0,0.0,0.0)
		_SampleScale("SampleScale", float) = 0.0
		_Intensity("Intensity", float) = 0.0
		_FxScale("FxScale", float) = 0.0
	}
	SubShader 
	{
		// 0 : Prefilter
		Pass 
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
			#include "FPBloom.cginc"
			#pragma vertex vert
			#pragma fragment frag_prefilter
			#pragma target 3.0
			ENDCG
    	}
		// 1 : First level downsampler
		Pass 
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
			#include "FPBloom.cginc"
			#pragma vertex vert
			#pragma fragment frag_downsample
			#pragma target 3.0
			ENDCG
    	}
		// 2: Upsampler
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #include "FPBloom.cginc"
            #pragma vertex vert_multitex
            #pragma fragment frag_upsample
            #pragma target 3.0
            ENDCG
        }
		// 3: Combiner
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #include "FPBloom.cginc"
            #pragma vertex vert_multitex
            #pragma fragment frag_upsample_final
            #pragma target 3.0
            ENDCG
        }
		// 4: Fx Bloom
		Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
			#include "FPBloom.cginc"
			#pragma vertex vert_multitex
			#pragma fragment frag_getfx
			#pragma target 3.0
			ENDCG
		}
	}
} 
