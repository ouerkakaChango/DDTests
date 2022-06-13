// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#define GRABPIXELX(weight, kernelx) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(half4(i.uvgrab.x + _GrabTexture_TexelSize.x * kernelx, i.uvgrab.y, i.uvgrab.z, i.uvgrab.w))) * weight
#define GRABPIXELY(weight, kernely) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(half4(i.uvgrab.x, i.uvgrab.y + _GrabTexture_TexelSize.y * kernely, i.uvgrab.z, i.uvgrab.w))) * weight

struct appdata_t {
	half4 vertex : POSITION;
	half2 texcoord: TEXCOORD0;
	half4 color : COLOR;
};

struct v2f {
	half4 vertex : POSITION;
	half4 uvgrab : TEXCOORD0;
	half2 bluruvmain : TEXCOORD2;
	half4 color : COLOR;
};

half4 _MainTex_ST;
half _Size;
sampler2D _GrabTexture;
half4 _GrabTexture_TexelSize;
half _KFactor;
half _MFactor;
half4 _BlurMaskTex_ST;
sampler2D _BlurMaskTex;

v2f vert(appdata_t v) {

	v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.uvgrab = ComputeGrabScreenPos(o.vertex);
	o.bluruvmain = TRANSFORM_TEX(v.texcoord, _BlurMaskTex);
	o.color = v.color;
	return o;

}

half4 fragX(v2f i) : SV_Target{

	half4 sum = half4(0, 0, 0, 0);
	half4 blurmask = tex2D(_BlurMaskTex, i.bluruvmain);
	half4 col = tex2Dproj(_GrabTexture, i.uvgrab);

	//sum += GRABPIXELX(0.05 * _KFactor, -4.0 * _MFactor);
	//sum += GRABPIXELX(0.09 * _KFactor, -3.0 * _MFactor);
	//sum += GRABPIXELX(0.12 * _KFactor, -2.0 * _MFactor);
	//sum += GRABPIXELX(0.15 * _KFactor, -1.0 * _MFactor);
	//sum += GRABPIXELX(0.18 * _KFactor,  0.0 * _MFactor);
	//sum += GRABPIXELX(0.15 * _KFactor, +1.0 * _MFactor);
	//sum += GRABPIXELX(0.12 * _KFactor, +2.0 * _MFactor);
	//sum += GRABPIXELX(0.09 * _KFactor, +3.0 * _MFactor);
	//sum += GRABPIXELX(0.05 * _KFactor, +4.0 * _MFactor);

	////5x5,sigma=1
	////0.05448869 0.2442014 0.40262 0.2442014 0.05448869
	//sum += GRABPIXELX(0.05448869 * _KFactor, -2.0 * _MFactor);
	//sum += GRABPIXELX(0.2442014 * _KFactor, -1.0 * _MFactor);
	//sum += GRABPIXELX(0.40262 * _KFactor, 0.0 * _MFactor);
	//sum += GRABPIXELX(0.2442014 * _KFactor, +1.0 * _MFactor);
	//sum += GRABPIXELX(0.05448869 * _KFactor, +2.0 * _MFactor);

	////5x5,sigma=2
	////0.1524691 0.2218413 0.2513791 0.2218413 0.1524691
	sum += GRABPIXELX(0.1524691 * _KFactor, -2.0 * _MFactor);
	sum += GRABPIXELX(0.2218413 * _KFactor, -1.0 * _MFactor);
	sum += GRABPIXELX(0.2513791 * _KFactor, 0.0 * _MFactor);
	sum += GRABPIXELX(0.2218413 * _KFactor, +1.0 * _MFactor);
	sum += GRABPIXELX(0.1524691 * _KFactor, +2.0 * _MFactor);
	
	//float k = saturate((_MFactor - 1) / 19);
	//float b = 1 / 5.0f;
	//sum += GRABPIXELX(lerp(0.05448869,b, k) * _KFactor, -2.0 * _MFactor);
	//sum += GRABPIXELX(lerp(0.2442014,b,k) * _KFactor, -1.0 * _MFactor);
	//sum += GRABPIXELX(lerp(0.40262, b, k) * _KFactor, 0.0 * _MFactor);
	//sum += GRABPIXELX(lerp(0.2442014, b, k) * _KFactor, +1.0 * _MFactor);
	//sum += GRABPIXELX(lerp(0.05448869, b, k) * _KFactor, +2.0 * _MFactor);

	return lerp(col, sum, blurmask.r * i.color.a);
}

half4 fragY(v2f i) : SV_Target{

	half4 sum = half4(0, 0, 0, 0);
	half4 blurmask = tex2D(_BlurMaskTex, i.bluruvmain);
	half4 col = tex2Dproj(_GrabTexture, i.uvgrab);

	//sum += GRABPIXELY(0.05 * _KFactor, -4.0* _MFactor);
	//sum += GRABPIXELY(0.09* _KFactor, -3.0* _MFactor);
	//sum += GRABPIXELY(0.12* _KFactor, -2.0* _MFactor);
	//sum += GRABPIXELY(0.15* _KFactor, -1.0* _MFactor);
	//sum += GRABPIXELY(0.18* _KFactor,  0.0* _MFactor);
	//sum += GRABPIXELY(0.15* _KFactor, +1.0* _MFactor);
	//sum += GRABPIXELY(0.12* _KFactor, +2.0* _MFactor);
	//sum += GRABPIXELY(0.09* _KFactor, +3.0* _MFactor);
	//sum += GRABPIXELY(0.05* _KFactor, +4.0* _MFactor);

	////5x5,sigma=1
	////0.05448869 0.2442014 0.40262 0.2442014 0.05448869
	//sum += GRABPIXELY(0.05448869 * _KFactor, -2.0 * _MFactor);
	//sum += GRABPIXELY(0.2442014 * _KFactor, -1.0 * _MFactor);
	//sum += GRABPIXELY(0.40262 * _KFactor, 0.0 * _MFactor);
	//sum += GRABPIXELY(0.2442014 * _KFactor, +1.0 * _MFactor);
	//sum += GRABPIXELY(0.05448869 * _KFactor, +2.0 * _MFactor);

	////5x5,sigma=2
	////0.1524691 0.2218413 0.2513791 0.2218413 0.1524691
	sum += GRABPIXELY(0.1524691 * _KFactor, -2.0 * _MFactor);
	sum += GRABPIXELY(0.2218413 * _KFactor, -1.0 * _MFactor);
	sum += GRABPIXELY(0.2513791 * _KFactor, 0.0 * _MFactor);
	sum += GRABPIXELY(0.2218413 * _KFactor, +1.0 * _MFactor);
	sum += GRABPIXELY(0.1524691 * _KFactor, +2.0 * _MFactor);

	//float k = saturate((_MFactor - 1) / 19);
	//float b = 1 / 5.0f;
	//sum += GRABPIXELY(lerp(0.05448869, b, k) * _KFactor, -2.0 * _MFactor);
	//sum += GRABPIXELY(lerp(0.2442014, b, k) * _KFactor, -1.0 * _MFactor);
	//sum += GRABPIXELY(lerp(0.40262, b, k) * _KFactor, 0.0 * _MFactor);
	//sum += GRABPIXELY(lerp(0.2442014, b, k) * _KFactor, +1.0 * _MFactor);
	//sum += GRABPIXELY(lerp(0.05448869, b, k) * _KFactor, +2.0 * _MFactor);

	return lerp(col, sum, blurmask.r * i.color.a);
}

#define GRABPIXELXY(weight, kernelx, kernely) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(half4(i.uvgrab.x+ _GrabTexture_TexelSize.x * kernelx, i.uvgrab.y + _GrabTexture_TexelSize.y * kernely, i.uvgrab.z, i.uvgrab.w))) * weight

half4 fragG3(v2f i) : SV_Target{

	half4 sum = half4(0, 0, 0, 0);
	half4 blurmask = tex2D(_BlurMaskTex, i.bluruvmain);
	half4 col = tex2Dproj(_GrabTexture, i.uvgrab);

	//3x3 sigma=2
	//0.1018681 0.1154316 0.1018681
	//0.1154316 0.1308012 0.1154316
	//0.1018681 0.1154316 0.1018681
	sum += GRABPIXELXY(0.1018681 * _KFactor, -1.0 * _MFactor,-_MFactor);
	sum += GRABPIXELXY(0.1154316 * _KFactor, 0.0 * _MFactor, -_MFactor);
	sum += GRABPIXELXY(0.1018681 * _KFactor, 1.0 * _MFactor, -_MFactor);

	sum += GRABPIXELXY(0.1154316 * _KFactor, -1.0 * _MFactor, 0);
	sum += GRABPIXELXY(0.1308012 * _KFactor, 0.0 * _MFactor, 0);
	sum += GRABPIXELXY(0.1154316 * _KFactor, 1.0 * _MFactor, 0);

	sum += GRABPIXELXY(0.1018681 * _KFactor, -1.0 * _MFactor, _MFactor);
	sum += GRABPIXELXY(0.1154316 * _KFactor, 0.0 * _MFactor, _MFactor);
	sum += GRABPIXELXY(0.1018681 * _KFactor, 1.0 * _MFactor, _MFactor);

	return lerp(col, sum, blurmask.r * i.color.a);
}
