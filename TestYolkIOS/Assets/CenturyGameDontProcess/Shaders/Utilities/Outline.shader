Shader "Hidden/Utilities/Outline"
{
    Properties
    {
		_OutlineWidth("OutlineWidth",Range(0,10)) = 0
		_OutlineColor("OutlineColor",Color) = (1,1,1,1)
	}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

		Pass
		{
			Name "Outline"
			Tags { "LightMode" = "Always" }
			Stencil
			{
				Ref 23          
				Comp NotEqual
				Pass keep
			}
			Cull Front

			CGPROGRAM
			#pragma vertex vert_outline
			#pragma fragment frag_outline
			#include "UnityCG.cginc"

			half _OutlineWidth;
			half4 _OutlineColor;

			struct a2v
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				float4 vertColor : COLOR;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
			};


			v2f vert_outline(a2v v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				float3 vec = v.vertColor.xyz * 2 - 1;
				float3 finvecol = float3(-vec.x,vec.y,vec.z);
				o.pos = UnityObjectToClipPos(float4(v.vertex.xyz + v.normal.xyz * _OutlineWidth * 0.01 ,1));

				return o;
			}

			half4 frag_outline(v2f i) : SV_TARGET
			{
				return _OutlineColor;
			}
			ENDCG
		}
    }
}
