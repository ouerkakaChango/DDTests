Shader "FP/PostProcess/FPDistortion"
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "black" {}
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1.0
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0.0
		 _DistortionScale("Distortion Scale", Float) = 1.0
		[Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1.0
		[Enum(Off, 0, Front, 1, Back, 2)] _Cull("Cull", Float) = 2.0
	}
 
	Category 
	{	
		ZTest Off Cull Off ZWrite Off 
 
		SubShader 
		{
			Pass
			{
			    Tags { "RenderType" = "Overlay" "LightMode" = "ForwardBase" }
				Blend[_SrcBlend][_DstBlend]
			    //Blend One Zero
				ZWrite[_ZWrite]
			    Cull[_Cull]
				ZTest LEqual
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
 
				struct appdata_t
				{
					float4 vertex : POSITION;
					half2 texcoord: TEXCOORD0;
				};
 
				struct v2f
				{
					float4 vertex : POSITION;
					half2 uv : TEXCOORD0;
					float4 distortion : TEXCOORD1;
				};
 
				sampler2D _MainTex;
				sampler2D _GrabTexture;
				sampler2D _DepthTex;
				half4 _MainTex_ST;
				half2 _MainTex_TexelSize;
				half _DistortionScale;
				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
					o.distortion = ComputeGrabScreenPos(o.vertex);
					return o;
				}
 
				half4 frag( v2f i ) : COLOR
				{
					half4 col = tex2D( _MainTex, i.uv) * 2.0h - 1.0h;
					half4 dis = tex2Dproj(_GrabTexture, i.distortion + col * _DistortionScale);
					return dis;
				}
				ENDCG
			}
		}
	}
}
