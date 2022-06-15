Shader "Hidden/PostProcess/FPFXAA3"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_ContrastThreshold("ContrastThreshold", float) = 0.0312
		_RelativeThreshold("RelativeThreshold", float) = 0.063
		_Sharpness("Sharpness", float) = 1.3
		_PointScale("PointScale", float) = 1.0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);
				col.a = LinearRgbToLuminance(col.rgb);
				return col;
			}
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ LUMINANCE_GREEN
			#pragma multi_compile __ ISMOBILE
			#include "UnityCG.cginc"

		#if defined(ISMOBILE)
			#define EDGE_STEP_COUNT 2.0h
			#define EDGE_STEPS 1.5h,3.5h
			#define EDGE_GUESS 8.5h

			//#define EDGE_STEP_COUNT 4.0h
			//#define EDGE_STEPS 1.0h, 1.5h, 2.0h, 4.0h
			//#define EDGE_GUESS 10.0h
		#else
			//#define EDGE_STEP_COUNT 4.0h
			//#define EDGE_STEPS 1.0h, 1.5h, 2.0h, 4.0h
			//#define EDGE_GUESS 10.0h
			#define EDGE_STEP_COUNT 10.0h
			#define EDGE_STEPS 1.0h, 1.5h, 2.0h, 2.0h, 2.0h, 2.0h, 2.0h, 2.0h, 2.0h, 4.0h
			#define EDGE_GUESS 8.0h
		#endif
/*
9 pix filter
NW    N    NE
W     M    E
SW    S    SE
*/
			static const float edgeSteps[EDGE_STEP_COUNT] = { EDGE_STEPS };
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			float _ContrastThreshold, _RelativeThreshold, _PointScale, _Sharpness;
			sampler2D _MainTex;
			float2 _MainTex_TexelSize;
			float4 Sample(float2 uv)
			{
				return tex2Dlod(_MainTex, float4(uv, 0, 0));
			}

			float SampleLuminance(float2 uv)
			{
				#if defined(LUMINANCE_GREEN)
					return Sample(uv).g;
				#else
					return Sample(uv).a;
				#endif
			}

			float SampleLuminance(float2 uv, float uOffset, float vOffset)
			{
				uv += _MainTex_TexelSize * float2(uOffset, vOffset) * _PointScale;
				return SampleLuminance(uv);
			}
			float3 SampleLuminance(float2 uv, float uOffset, float vOffset, out float luma)
			{
				uv += _MainTex_TexelSize * float2(uOffset, vOffset) * _PointScale;
				float4 col = Sample(uv);
				#if defined(LUMINANCE_GREEN)
					luma = col.g;
				#else
					luma = col.a;
				#endif
				return col.rgb;
			}
			struct LuminanceData
			{
				float m, n, e, s, w, highest, lowest, contrast, ne, nw, se, sw;
				/* add sharpness*/
				float3 blur, col;
			};
			LuminanceData SampleLuminanceNeighborhood(float2 uv)
			{
				LuminanceData l;
				l.col = SampleLuminance(uv, 0, 0, l.m);

				float3 col = SampleLuminance(uv, 0,  1, l.n);
				col += SampleLuminance(uv, 1,  0, l.e);
				col += SampleLuminance(uv, 0, -1, l.s);
				col += SampleLuminance(uv,-1,  0, l.w);
				l.highest = max(max(max(max(l.n, l.e), l.s), l.w), l.m);
				l.lowest = min(min(min(min(l.n, l.e), l.s), l.w), l.m);
				l.contrast = l.highest - l.lowest;
				col += SampleLuminance(uv,  1,  1, l.ne);
				col += SampleLuminance(uv, -1,  1, l.nw);
				col += SampleLuminance(uv,  1, -1, l.se);
				col += SampleLuminance(uv, -1, -1, l.sw);

				l.blur = (col + l.col) * .1111111f;
				return l;
			}
			/*
			LuminanceData SampleLuminanceNeighborhood (float2 uv)
			{
				LuminanceData l;
				l.m = SampleLuminance(uv);
				l.n = SampleLuminance(uv, 0,  1);
				l.e = SampleLuminance(uv, 1,  0);
				l.s = SampleLuminance(uv, 0, -1);
				l.w = SampleLuminance(uv,-1,  0);
				l.highest = max(max(max(max(l.n, l.e), l.s), l.w), l.m);
				l.lowest = min(min(min(min(l.n, l.e), l.s), l.w), l.m);
				l.contrast = l.highest - l.lowest;
				l.ne = SampleLuminance(uv,  1,  1);
				l.nw = SampleLuminance(uv, -1,  1);
				l.se = SampleLuminance(uv,  1, -1);
				l.sw = SampleLuminance(uv, -1, -1);
				return l;
			}
			*/
			bool ShouldSkipPixel(LuminanceData l)
			{
				float threshold = max(_ContrastThreshold, _RelativeThreshold * l.highest);
				return l.contrast < threshold;
			}
			/*
			float DeterminePixelBlendFactor (LuminanceData l)
			{
				float filter = 2.0h * (l.n + l.e + l.s + l.w);
				filter += l.ne + l.nw + l.se + l.sw;
				filter *= 1.0h / 12.0h;
				filter = abs(filter - l.m);
				filter = saturate(filter / l.contrast);
				float blendFactor = smoothstep(0, 1, filter);
				return blendFactor * blendFactor *  _SubpixelBlending;
			}
			*/
			struct EdgeData
			{
				bool isHorizontal;
				float pixelStep, oppositeLuminance, gradient;
			};
			EdgeData DetermineEdge(LuminanceData l)
			{
				EdgeData e;
				float horizontal =
					abs(l.n + l.s - 2 * l.m) * 2 +
					abs(l.ne + l.se - 2 * l.e) +
					abs(l.nw + l.sw - 2 * l.w);
				float vertical =
					abs(l.e + l.w - 2 * l.m) * 2 +
					abs(l.ne + l.nw - 2 * l.n) +
					abs(l.se + l.sw - 2 * l.s);
				e.isHorizontal = horizontal >= vertical;
				e.pixelStep = e.isHorizontal ? _MainTex_TexelSize.y : _MainTex_TexelSize.x;
				float pLuminance = e.isHorizontal ? l.n : l.e;
				float nLuminance = e.isHorizontal ? l.s : l.w;
				float pGradient = abs(pLuminance - l.m);
				float nGradient = abs(nLuminance - l.m);
				if (pGradient < nGradient)
				{
					e.pixelStep = -e.pixelStep;
					e.oppositeLuminance = nLuminance;
					e.gradient = nGradient;
				}
				else
				{
					e.oppositeLuminance = pLuminance;
					e.gradient = pGradient;
				}
				return e;
			}
			float DetermineEdgeBlendFactor(LuminanceData l, EdgeData e, float2 uv)
			{
				float2 uvEdge = uv;
				float2 edgeStep;
				if (e.isHorizontal)
				{
					uvEdge.y += e.pixelStep * 0.5h;
					edgeStep = float2(_MainTex_TexelSize.x, 0.0h);
				}
				else
				{
					uvEdge.x += e.pixelStep * 0.5h;
					edgeStep = float2(0.0h, _MainTex_TexelSize.y);
				}
				float edgeLuminance = (l.m + e.oppositeLuminance) * 0.5h;
				float gradientThreshold = e.gradient * 0.25h;

				float2 puv = uvEdge + edgeStep * edgeSteps[0];
				float pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
				bool pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
#if ISMOBILE
				if (!pAtEnd)
				{
					puv += edgeStep * edgeSteps[0];
					pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
					pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
				}
				if (!pAtEnd)
				{
					puv += edgeStep * edgeSteps[1];
					pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
					pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
				}
#else
				UNITY_UNROLL
				for (int i = 0; i < EDGE_STEP_COUNT && !pAtEnd; i++)
				{
					puv += edgeStep * edgeSteps[i];
					pLuminanceDelta = SampleLuminance(puv) - edgeLuminance;
					pAtEnd = abs(pLuminanceDelta) >= gradientThreshold;
				}
#endif

				if (!pAtEnd)
				{
					puv += edgeStep * EDGE_GUESS;
				}

				float2 nuv = uvEdge - edgeStep;
				float nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
				bool nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
#if ISMOBILE
				if (!nAtEnd)
				{
					nuv -= edgeStep * edgeSteps[0];
					nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
					nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
				}
				if (!nAtEnd)
				{
					nuv -= edgeStep * edgeSteps[1];
					nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
					nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
				}
#else
				UNITY_UNROLL
				for (int i = 0; i < EDGE_STEP_COUNT && !nAtEnd; i++)
				{
					nuv -= edgeStep * edgeSteps[i];
					nLuminanceDelta = SampleLuminance(nuv) - edgeLuminance;
					nAtEnd = abs(nLuminanceDelta) >= gradientThreshold;
				}
#endif
				if (!nAtEnd)
				{
					nuv -= edgeStep * EDGE_GUESS;
				}
				float pDistance, nDistance;
				if (e.isHorizontal)
				{
					pDistance = puv.x - uv.x;
					nDistance = uv.x - nuv.x;
				}
				else
				{
					pDistance = puv.y - uv.y;
					nDistance = uv.y - nuv.y;
				}

				float shortestDistance;
				bool deltaSign;
				if (pDistance <= nDistance)
				{
					shortestDistance = pDistance;
					deltaSign = pLuminanceDelta >= 0;
				}
				else
				{
					shortestDistance = nDistance;
					deltaSign = nLuminanceDelta >= 0;
				}
				if (deltaSign == (l.m - edgeLuminance >= 0))
				{
					return 0;
				}
				return 0.5h - shortestDistance / (pDistance + nDistance);
			}

			float4 ApplyFXAA(float2 uv)
			{
				LuminanceData l = SampleLuminanceNeighborhood(uv);
				if (ShouldSkipPixel(l))
				{
				   return Sample(uv);
				}
				//float pixelBlend = DeterminePixelBlendFactor(l);
				float pixelBlend = 0.0h;
				EdgeData e = DetermineEdge(l);
				float edgeBlend = DetermineEdgeBlendFactor(l, e, uv);
				float finalBlend = max(pixelBlend, edgeBlend);
				if (e.isHorizontal)
				{
					uv.y += e.pixelStep * finalBlend;
				}
				else
				{
					uv.x += e.pixelStep * finalBlend;
				}
				half3 col = lerp(l.blur, Sample(uv).rgb, _Sharpness);
				//float3 col = Sample(uv).rgb;
				//float3 abs = clamp(l.blur - col, -0.5h, 0.5h);
				//col = col + abs * (_Sharpness - 1.0h);
				return half4(col, l.m);
			}
			fixed4 frag(v2f i) : SV_Target
			{
				return ApplyFXAA(i.uv);
			}
			ENDCG
		}
	}
}
