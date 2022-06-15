Shader "Hidden/PostProcess/FPDepthOfField"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}

		CGINCLUDE
#include "UnityCG.cginc"  

		struct v2f_blur
	{
		float4 pos : SV_POSITION;
		half2 uv  : TEXCOORD0;
		half4 uv01 : TEXCOORD1;
		half4 uv23 : TEXCOORD2;
		half4 uv45 : TEXCOORD3;
		half4 uv67 : TEXCOORD4;
	};

	struct v2f_dof
	{
		float4 pos : SV_POSITION;
		half2 uv  : TEXCOORD0;
		half2 uv1 : TEXCOORD1;
	};

	sampler2D _MainTex;
	half4 _MainTex_TexelSize;
	sampler2D _BlurTex;
	sampler2D_float _DepthTexDOF;
	half4 _offsets;
	half4 _parameter;

	v2f_blur vert_blur(appdata_img v)
	{
		v2f_blur o;
		_offsets *= _MainTex_TexelSize.xyxy;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		o.uv01 = v.texcoord.xyxy + _offsets.xyxy * half4(1.0h, 1.0h, -1.0h, -1.0h);
		o.uv23 = v.texcoord.xyxy + _offsets.xyxy * half4(1.0h, 1.0h, -1.0h, -1.0h) * 2.0h;
		o.uv45 = v.texcoord.xyxy + _offsets.xyxy * half4(1.0h, 1.0h, -1.0h, -1.0h) * 3.0h;
		o.uv67 = v.texcoord.xyxy + _offsets.xyxy * half4(1.0h, 1.0h, -1.0h, -1.0h) * 4.0h;
		return o;
	}


	half4 dof_pixel(half scale, half2 uv)
	{
		return tex2D(_MainTex, uv) * scale;
	}

	half4 frag_blur(v2f_blur i) : SV_Target
	{
		half4 old = tex2D(_MainTex, i.uv);
		if (old.a == 0) return old;
		half4 color = old * 0.14h;
		color += dof_pixel(0.15h, i.uv01.xy);
		color += dof_pixel(0.15h, i.uv01.zw);
		color += dof_pixel(0.12h, i.uv23.xy);
		color += dof_pixel(0.12h, i.uv23.zw);
		color += dof_pixel(0.09h, i.uv45.xy);
		color += dof_pixel(0.09h, i.uv45.zw);
		color += dof_pixel(0.07h, i.uv67.xy);
		color += dof_pixel(0.07h, i.uv67.zw);

		//color = color.a <= 0.0h ? 0.0h : half4(color.rgb / color.a, 1.0h);
		color = half4(color.rgb / color.a, 1.0h) * step(0.0h, color.a);
		return color;
	}

	v2f_dof vert_dof(appdata_img v)
	{
		v2f_dof o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv.xy = v.texcoord.xy;
		o.uv1.xy = o.uv.xy;
#if UNITY_UV_STARTS_AT_TOP  
		o.uv.y = _MainTex_TexelSize.y < 0.0h ? 1.0h - o.uv.y : o.uv.y;
#endif    
		return o;
	}

	half4 frag_dof(v2f_dof i) : SV_Target
	{
		half4 ori = tex2D(_MainTex, i.uv1);
	    half4 blur = tex2D(_BlurTex, i.uv);
		float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_DepthTexDOF, i.uv));
		float abs = saturate((depth - _parameter.x) * _parameter.z) + saturate((_parameter.y - depth) * _parameter.z);
		fixed4 final = lerp(ori, blur * _parameter.w, abs);
		return final;
	}

	half4 frag_copy(v2f_dof i) : SV_Target
	{
		float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_DepthTexDOF, i.uv1));
		half4 color = half4(tex2D(_MainTex, i.uv1).rgb, 1.0h);
		half abs = step(0.0h, depth - _parameter.x) + step(0.0h, _parameter.y - depth);
		color = color * abs;
		return color;
	}
		ENDCG

		SubShader
	{
		Pass
		{
			ZTest Off
			ZWrite Off
			Fog{ Mode Off }

			CGPROGRAM
			#pragma vertex vert_blur  
			#pragma fragment frag_blur  
			ENDCG
		}
			Pass
		{
			ZTest Off
			ZWrite Off
			Fog{ Mode Off }
			ColorMask RGBA
			CGPROGRAM
			#pragma vertex vert_dof  
			#pragma fragment frag_dof  
			ENDCG
		}

			Pass
		{
			ZTest Off
			ZWrite Off
			Fog{ Mode Off }
			ColorMask RGBA
			CGPROGRAM
			#pragma vertex vert_dof  
			#pragma fragment frag_copy  
			ENDCG
		}
	}
}