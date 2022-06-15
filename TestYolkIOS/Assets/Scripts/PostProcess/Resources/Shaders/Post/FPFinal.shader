// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FP/PostProcess/FPFinal"
{
	Properties
	{
		_MainTex("MainTex",2D) = "white"{}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ USE_LINEAR
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

			sampler2D _MainTex;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			fixed4 frag(v2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);
#if USE_LINEAR
				col.rgb = LinearToGammaSpace(col.rgb);
#endif
				return col;
			}
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ USE_LINEAR
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

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			fixed4 frag(v2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);
#if USE_LINEAR
				col.rgb = GammaToLinearSpace(col.rgb);
#endif
				return col;
			}
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ USE_LINEAR
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

			sampler2D _MainTex;
			sampler2D _ColorTex;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			fixed4 frag(v2f i) : SV_Target
			{
				half4 ui = tex2D(_MainTex, i.uv);
				half4 col =  tex2D(_ColorTex, i.uv);
				return lerp(ui, col, ui.a);
			}
				ENDCG
			}
	}
}
