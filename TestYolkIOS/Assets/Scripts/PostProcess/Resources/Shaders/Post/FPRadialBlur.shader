Shader "Hidden/PostProcess/FPRadialBlur" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	    _BlurStrength("BlurStrength", Range(0.01, 1)) = 0.12
		_SampleStrength("SampleStrength", Range(1, 10)) = 3.0
		
	}

	SubShader 
	{
		Pass 
		{
			ZTest Always 
			Cull Off 
			ZWrite Off 
			Fog { Mode Off }
		
			CGPROGRAM
            #pragma vertex vert_img  
			#pragma fragment frag
			#include "UnityCG.cginc"

		    half _BlurStrength;
			half _SampleStrength;
			sampler2D _MainTex;

			float4 frag (v2f_img i) : COLOR
			{
                #define GRABPIXEL(dir, offset) tex2D(_MainTex, i.uv + dir * offset * _BlurStrength)

				half2 dir = 0.5 - i.uv;
				half dist = length(dir);
				dir /= dist;
				fixed4 color = GRABPIXEL(dir, 0);
				fixed4 sum = color;
				sum += GRABPIXEL(dir, -0.26);
				sum += GRABPIXEL(dir, -0.15);
				sum += GRABPIXEL(dir, -0.08);
				sum += GRABPIXEL(dir, -0.03);
				sum += GRABPIXEL(dir, -0.01);
				sum += GRABPIXEL(dir, 0.01);
				sum += GRABPIXEL(dir, 0.03);
				sum += GRABPIXEL(dir, 0.08);
				sum += GRABPIXEL(dir, 0.15);
				sum += GRABPIXEL(dir, 0.26);
				sum /= 11.0;
				float t = saturate(dist * _SampleStrength);
		    	return lerp(color, sum, t);
			}
			
			ENDCG
    	}
	}
} 
