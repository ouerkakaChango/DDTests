Shader "Hidden/PostProcess/FPColorGrading"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "" {}
		_Amount("Amount of Color Filter (0 - 1)", float) = 1.0
		_TintColor("Tint (RGB)", Color) = (1, 1, 1, 1)
		_Hue("Hue (0 - 360)", float) = 0
		_Saturation("Saturation (0 - 2)", float) = 1.0
		_Brightness("Brightness (0 - 3)", float) = 1.0
		_Contrast("Contrast (0 - 2)", float) = 1.0
		_Scale("Scale (0 - 3)", float) = 1.0
		_Offset("Offset (0 - 2)", float) = 1.0
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 uv  : TEXCOORD0;
	};

	uniform sampler2D _MainTex;
	uniform sampler3D _ClutTex;
	uniform float _Amount;
	uniform float4 _TintColor;
	uniform float _Hue;
	uniform float _Saturation;
	uniform float _Brightness;
	uniform float _Contrast;
	uniform float _Scale;
	uniform float _Offset;
	sampler2D _Lut2D;
	float3 _Lut2D_Params;

	inline half3 applyHue(half3 aColor)
	{
		half angle = radians(_Hue);
		half3 k = half3(0.57735, 0.57735, 0.57735);
		float cosAngle = cos(angle);

		return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
	}

	inline half3 applyHSBEffect(half3 c)
	{
		c.rgb = applyHue(c.rgb);
		c.rgb = ((c.rgb - 0.5f) * _Contrast) + 0.5f;
		c.rgb *= _Brightness;
		half3 intensity = dot(c.rgb, half3(0.299, 0.587, 0.114));
		c.rgb = lerp(intensity, c.rgb, _Saturation);

		return c;
	}

	half3 ApplyLut2D(sampler2D tex, float3 uvw, float3 scaleOffset)
	{
		// Strip format where `height = sqrt(width)`
		uvw.z *= scaleOffset.z;
		float shift = floor(uvw.z);
		uvw.xy = uvw.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
		uvw.x += shift * scaleOffset.y;
		uvw.xyz = lerp(
			tex2D(tex, uvw.xy).rgb,
			tex2D(tex, uvw.xy + float2(scaleOffset.y, 0.0)).rgb,
			uvw.z - shift
		);
		return uvw;
	}

	#define LUT_SPACE_ENCODE(x) LinearToLogC(x)

	struct ParamsLogC
	{
		float cut;
		float a, b, c, d, e, f;
	};

	static const ParamsLogC LogC =
	{
		0.011361, // cut
		5.555556, // a
		0.047996, // b
		0.244161, // c
		0.386036, // d
		5.301883, // e
		0.092819  // f
	};

	float3 LinearToLogC(float3 x)
	{
		return LogC.c * log10(LogC.a * x + LogC.b) + LogC.d;
	}


	v2f vert(appdata_img v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;

		return o;
	}

	half4 frag(v2f i) : SV_Target
	{
		half4 c = tex2D(_MainTex, i.uv);
		c.rgb = saturate(c.rgb);
		c.rgb = ApplyLut2D(_Lut2D, c.rgb, _Lut2D_Params);
		return c;
	}

	half4 fragLinear(v2f i) : SV_Target
	{
		half4 c = tex2D(_MainTex, i.uv);
		c.rgb = saturate(c.rgb);
		c.rgb = sqrt(c.rgb);
		c.rgb = ApplyLut2D(_Lut2D, c.rgb, _Lut2D_Params);
		c.rgb = c.rgb * c.rgb;
		return c;
	}

	half4 fragHDR(v2f i) : SV_Target
	{
		half4 c = tex2D(_MainTex, i.uv);
		float3 colorLutSpace = saturate(LUT_SPACE_ENCODE(c.rgb));
		c.rgb = ApplyLut2D(_Lut2D, colorLutSpace, _Lut2D_Params);
		return c;
	}

	ENDCG

	Subshader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragLinear
			#pragma target 3.0
			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragHDR
			#pragma target 3.0
			ENDCG
		}
	}
	Fallback off
}
