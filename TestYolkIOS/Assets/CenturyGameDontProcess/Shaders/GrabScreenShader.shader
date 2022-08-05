Shader "GrabScreenShader"
{
	Properties
	{
		
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent+3" }
		LOD 300

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend SrcAlpha OneMinusSrcAlpha
		AlphaToMask Off
		Cull Back
		ColorMask 0
		ZWrite Off
		ZTest LEqual
		Offset 0 , 0

		GrabPass{ "_GrabScreen0" }

		Pass
		{
			Name "Unlit"
			Tags { "LightMode" = "ForwardBase" }
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
				float4 vertex : SV_POSITION;
			};


			v2f vert(appdata v)
			{
				v2f o = (v2f)0;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return fixed4(0,0,0,0);
			}
			ENDCG
		}
	}
	
	SubShader
	{	
		Tags { "RenderType"="Transparent" "Queue"="Transparent+3" }
		LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend SrcAlpha OneMinusSrcAlpha
		AlphaToMask Off
		Cull Back
		ColorMask 0
		ZWrite Off
		ZTest LEqual
		Offset 0 , 0
		
		//GrabPass{ "_GrabScreen0" }

		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }
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
				float4 vertex : SV_POSITION;
			};

			
			v2f vert ( appdata v )
			{
				v2f o = (v2f)0;
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				return fixed4(0,0,0,0);
			}
			ENDCG
		}
	}
	
}