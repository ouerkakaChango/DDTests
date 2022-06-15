Shader "Hidden/PostProcess/FPTonemapping"
{
	Properties
	{
		_MainTex("MainTex",2D) = "white"{}
		_Lum("Lum", float) = 1.0
		_CentralFactor("CentralFactor", float) = 1.0
		_SideFactor("SideFactor", float) = 1.0
	}
    SubShader
    {
        Cull Off 
		ZWrite Off 
		ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile __ SHARPEN_ON
			#pragma multi_compile ACES HABLE
            #include "UnityCG.cginc"
			 
			uniform sampler2D _MainTex;
			uniform half _Lum;
			uniform half _CentralFactor;
			uniform half _SideFactor;
			uniform half4 _MainTex_TexelSize;

            struct appdata
            {
				half4 vertex : POSITION;
				half2 uv : TEXCOORD0;
            };

            struct v2f
            {
				half4 vertex : SV_POSITION;
				half2 uv : TEXCOORD0;
#if defined(SHARPEN_ON)
				half4 uv1  : TEXCOORD1;
#endif
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
#if defined(SHARPEN_ON)
				o.uv1 = half4(v.uv - _MainTex_TexelSize.xy * 0.5h, v.uv + _MainTex_TexelSize.xy * 0.5h);
#endif
                return o;
            }

			inline half3 hable_tonemap_core(half3 x)
			{
				//Shoulder strength
				const half hA = 0.15h;
				//Linear strength
				const half hB = 0.50h;
				//Linear angle
				const half hC = 0.10h;
				//Toe strength
				const half hD = 0.20h;
				//Toe numerator
				const half hE = 0.02h;
				//Toe denominator
				const half hF = 0.30h;

				return ((x*(hA*x + hC * hB) + hD * hE) / (x*(hA*x + hB) + hD * hF)) - hE / hF;
			}
			//# Hable vs ACES https://forums.odforce.net/topic/25019-hable-and-aces-tonemapping/
            //# Hable (Uncharted 2) Tonemapping
			//# Adapted from code by John Hable
			//# http://filmicgames.com/archives/75
			//# http://filmicworlds.com/blog/filmic-tonemapping-operators 
		    //# http://www.slideshare.net/ozlael/hable-john-uncharted2-hdr-lighting
			inline half3 HableToneMapping(half3 c)
			{
				//Linear white point
				//const half w = 11.2h;
				//return saturate(hable_tonemap_core(c) / hable_tonemap_core(w));
				//const half w = 1.2h;
				//half3 result = hable_tonemap_core(1.2h);
				//return result.r > 0.25296805f ? 1 : 0;
				return saturate(hable_tonemap_core(c) / half3(0.25296805h, 0.25296805h, 0.25296805h));
			}

			//# ACES Filmic Tone Mapping Curve
			//# Adapted from code by Krzysztof Narkowicz
			//# https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
			//# https://github.com/TheRealMJP/BakingLab/blob/master/BakingLab/ToneMapping.hlsl#L105
			//# aces - filmic - tone - mapping - curve /
			inline half3 ACESToneMapping(half3 c)
			{
				const float _A = 2.51f;
				const float _B = 0.03f;
				const float _C = 2.43f;
				const float _D = 0.59f;
				const float _E = 0.14f;
				return (c * (_A * c + _B)) / (c * (_C * c + _D) + _E);
			}

#if defined(SHARPEN_ON)
			inline half3 Sharpen(half3 c, half4 uv1)
			{
				c *= _CentralFactor;
				c -= tex2D(_MainTex, uv1.xy).rgb * _SideFactor;
				c -= tex2D(_MainTex, uv1.xw).rgb * _SideFactor;
				c -= tex2D(_MainTex, uv1.zy).rgb * _SideFactor;
				c -= tex2D(_MainTex, uv1.zw).rgb * _SideFactor;
				return c;
			}
#endif

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = tex2D(_MainTex, i.uv);
#if defined(SHARPEN_ON)
			    col.rgb = saturate(Sharpen(col, i.uv1));
#endif

#if ACES
				col.rgb = ACESToneMapping(col * _Lum);
#else
				col.rgb = HableToneMapping(col * _Lum);
#endif
                return col;
            }
            ENDCG
        }
    }
}
