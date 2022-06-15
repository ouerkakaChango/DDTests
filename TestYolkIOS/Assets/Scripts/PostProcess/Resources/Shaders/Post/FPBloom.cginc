#include "UnityCG.cginc"

#define USE_RGBM defined(SHADER_API_MOBILE)

uniform sampler2D _MainTex;
uniform sampler _BaseTex;
uniform float2 _MainTex_TexelSize;
uniform float2 _BaseTex_TexelSize;
uniform half4 _MainTex_ST;
uniform half4 _BaseTex_ST;

//uniform float _PrefilterOffs;
//uniform half _Threshold;
uniform half4 _Curve;
uniform float _SampleScale;
uniform half _Intensity;
uniform half _FxScale;
half4 _BloomColor;

half Brightness(half3 c)
{
	return max(max(c.r,c.g), c.b);
}

half3 Median(half3 a, half3 b, half3 c)
{
	return a + b + c - min(min(a,b), c) - max(max(a,b),c);
}
half4 SafeHDR(half4 c) { return clamp(c, 0, 1000); }

half4 Prefilter(half4 color)
{
	half br = Brightness(color);

	half rq = clamp(br - _Curve.x, 0, _Curve.y);
	rq = _Curve.z * rq * rq;

	half up = max(rq, br - _Curve.w);
	half down = max(br, 0.0001);
	half percent = up / down;
	color *= percent;

	return color;
}

half4 DownsampleFilter(float2 uv)
{
	float4 d = _MainTex_TexelSize.xyxy * float4(-1,-1,1,1);

	half4 s;

	s = tex2D(_MainTex, uv + d.xy);
	s += tex2D(_MainTex, uv + d.zy);
	s += tex2D(_MainTex, uv + d.xw);
	s += tex2D(_MainTex, uv + d.zw);

	return s * (1.0 / 4);
}

half4 UpsampleFilter(float2 uv)
{
    float4 d = _MainTex_TexelSize.xyxy * float4(-1, -1, +1, +1) * (_SampleScale * 0.5);

    half4 s;
    s  = tex2D(_MainTex, uv + d.xy);
    s += tex2D(_MainTex, uv + d.zy);
    s += tex2D(_MainTex, uv + d.xw);
    s += tex2D(_MainTex, uv + d.zw);

    return s * (1.0 / 4);
}

v2f_img vert(appdata_img v)
{
	v2f_img o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
	if (_MainTex_TexelSize.y < 0.0)
	{
		o.uv.y = 1.0 - o.uv.y;
	}
	return o;
}

struct v2f_multitex
{
	float4 pos : SV_POSITION;
	float2 uvMain : TEXCOORD0;
	float2 uvBase : TEXCOORD1;
};

v2f_multitex vert_multitex(appdata_img v)
{
	v2f_multitex o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uvMain = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
	o.uvBase = UnityStereoScreenSpaceUVAdjust(v.texcoord, _BaseTex_ST);

	#if UNITY_UV_STARTS_AT_TOP
	//if (_BaseTex_TexelSize.y < 0.0)
	//{
	//	o.uvBase.y = 1.0 - v.texcoord.y;
	//	o.uvMain.y = 1.0 - v.texcoord.y;
	//}
	#endif

	return o;
}

half4 frag_prefilter(v2f_img i) : SV_Target
{
	half4 s = DownsampleFilter(i.uv);
	return Prefilter(SafeHDR(s));
}

half4 frag_getfx(v2f_img i) : SV_Target
{
	half4 base = tex2D(_BaseTex, i.uv);
	half4 main = tex2D(_MainTex, i.uv);
	main.rgb = max(saturate(main - base) * _FxScale, saturate(main.rgb - _Curve.w));
	return main;
}

half4 frag_downsample(v2f_img i) : SV_Target
{
	return DownsampleFilter(i.uv);
}

half4 frag_upsample(v2f_multitex i) : SV_Target
{
	half4 base = tex2D(_BaseTex, i.uvBase);
	half4 blur = UpsampleFilter(i.uvMain);

	return base + blur;
}

half4 frag_upsample_final(v2f_multitex i) : SV_Target
{
	half4 base = tex2D(_BaseTex, i.uvBase);
#if UNITY_COLORSPACE_GAMMA
	base.rgb = GammaToLinearSpace(base.rgb);
#endif
	half3 blur = UpsampleFilter(i.uvMain);
	half3 cout = base.rgb + blur * _Intensity * _BloomColor;
#if UNITY_COLORSPACE_GAMMA
	cout = LinearToGammaSpace(cout);
#endif
	return SafeHDR(half4(cout,1.0));
}
