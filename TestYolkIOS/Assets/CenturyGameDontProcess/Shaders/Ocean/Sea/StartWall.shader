Shader "DianDian/Ocean/StartWall"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DistorTex ("DistorTex", 2D) = "white" {}
        _DistorInt("DistorInt",Range(0,1)) = 0
        _DistorSpeed("DistorSpeed",Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
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
            sampler2D _DistorTex;
            float4 _DistorTex_ST;
            float _DistorInt,_DistorSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 distor = tex2D(_DistorTex, i.uv + float2(0,_Time.y*_DistorSpeed));
                fixed4 col = tex2D(_MainTex, i.uv + distor.x*_DistorInt);

                return col;
            }
            ENDCG
        }
    }
}
