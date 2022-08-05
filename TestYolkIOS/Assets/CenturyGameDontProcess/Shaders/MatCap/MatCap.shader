// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "DianDian/Matcap"
{
    Properties 
    {
        _MainTex("Main Tex",2D) = "Black"{}
        _Color ("Main Color", Color)=(0.5,0.5,0.5,1)
        
        _MatCap("MatCap (RGB)",2D)="Black"{}
    }
    SUbShader
    {
        Tags {"RenderType"="Queue"}

        Pass 
        {
            Name "BASE"
            Tags {"LightMode"="Always"}
            
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                float3 TtoV0 : TEXCOODR1;
                float3 TtoV1 : TEXCOORD2;
            };
            

            float4 _Color;
            sampler2D _MatCap;
            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv.xy = v.texcoord;

                o.uv.z = dot(normalize(UNITY_MATRIX_IT_MV[0].xyz),normalize(v.normal));
                o.uv.w = dot(normalize(UNITY_MATRIX_IT_MV[1].xyz),normalize(v.normal));
                o.uv.zw = o.uv.zw * 0.5 + 0.5;
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float4 eyecol = tex2D(_MainTex,i.uv.xy);
                float4 matcapLookup = tex2D(_MatCap,i.uv.zw);

                return matcapLookup * _Color  + eyecol;
            }
            ENDCG
        }
    }
}
