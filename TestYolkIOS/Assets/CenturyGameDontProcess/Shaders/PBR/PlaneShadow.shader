Shader "DianDian/PlaneShadow"
{
	Properties
	{
		_ShadowColor("Shadow Color",Color) = (0,0,0,0)
		_Shadow("shadow Offset",vector) = (0,0,0,0)
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}

		LOD 300

		pass
		{
			Name "PlaneShadow"

			Blend SrcAlpha OneMinusSrcAlpha

			Stencil
			{
				Ref 100
				Comp NotEqual
				Pass Replace
			}
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			float4 _Shadow;
			float4 _ShadowColor;

			v2f vert (appdata v)
			{
				v2f o;
				float4 worldPos = mul(unity_ObjectToWorld , v.vertex);
				float worldPosY = worldPos.y;
				worldPos.y = _Shadow.y;
				worldPos.xz += _Shadow.xz * (worldPosY - _Shadow.y);
				o.pos = mul(UNITY_MATRIX_VP,worldPos);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return _ShadowColor;
			}
			ENDCG

		}

	}			
	//CustomEditor "DDRenderPipeline.PBRShaderGUI"
}