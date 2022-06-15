Shader "FP/PostProcess/ScreenWaterWave"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	uniform sampler2D _MainTex;
	uniform float _distanceFactor;
	uniform float _timeFactor;
	uniform float _totalFactor;
	uniform float _waveWidth;
	uniform float _curWaveDis;
	uniform float _centerPosX;
	uniform float _centerPosY;
	sampler2D _waterWaveMask;
	float _waterWaveRang;

	fixed4 frag(v2f_img i) : SV_Target
	{
		float2 dv = float2(_centerPosX, _centerPosY) - i.uv;
		dv *= float2(_ScreenParams.x / _ScreenParams.y, 1);
		float dis = length(dv);

		float sinFactor = sin(dis * _distanceFactor + _Time.y * _timeFactor) * _totalFactor * 0.01;

		float stepVal1 = step(dis, _waterWaveRang);
		sinFactor = sinFactor * stepVal1;

		float discardFactor = clamp(_waveWidth - abs(_curWaveDis - dis), 0, 1);

		float2 dv1 = normalize(dv);
		float2 offset = dv1 * sinFactor * discardFactor;
		float2 uv = offset + i.uv;

		float4 _Color = float4(1, 1, 1, 1);
		// if (dis < _waterWaveRang)
		//{
			// float2 maskUV = float2(_centerPosX, _centerPosY) - i.uv;
			// _Color = tex2D(_waterWaveMask, maskUV / _waterWaveRang  * 0.4 + float2(0.5, 0.5));
		//} 

		return tex2D(_MainTex, uv) * _Color;
	}

	ENDCG

	SubShader
	{
		// 0 : vert_img
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
			Fog { Mode off }

			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			ENDCG
		}
	}
	Fallback off
}
