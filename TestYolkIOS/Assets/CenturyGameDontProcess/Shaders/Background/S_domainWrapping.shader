Shader "Unlit/S_domainWrapping"
{
    Properties
    {
        [HDR]_MainColor("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
		_WarpUV("扭曲图", 2D) = "white" {}
		_Speed("速度",Range(0.0, 1.0)) = 0.06
		_Intensity("扭曲强度",Range(0.0, 1.0)) = 0.04
    }
    SubShader
    {
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
        LOD 100

        Pass
        {
			ZWrite off
			Blend SrcAlpha OneMinusSrcAlpha
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
			sampler2D _WarpUV;
			float4 _WarpUV_ST;

            half4 _MainColor;
			float _Speed;
			float _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				float2 uv = i.uv;
				float speed = _Speed;
				uv += _Intensity *tex2D(_WarpUV, _WarpUV_ST.xy*(uv + float2(speed *_Time.y, 0*speed *_Time.y)));
				//uv += 0.5*tex2D(_WarpUV, _WarpUV_ST.xy*(uv+float2(speed*_Time.y, 0*speed *_Time.y)));
				fixed4 col = tex2D(_MainTex, uv) * _MainColor;
				//return 1;
				return col;
			}
            ENDCG
        }
    }
}
