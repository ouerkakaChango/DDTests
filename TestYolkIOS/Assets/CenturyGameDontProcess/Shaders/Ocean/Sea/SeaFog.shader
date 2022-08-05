Shader "DianDian/Ocean/SeaFog"
{
    Properties
    {
        _FogColor1("FogColor1",Color) = (1,1,1,1)
        _FogColor2("FogColor2",Color) = (1,1,1,1)
        _FogColorMask("FogColorMask",2D) = "white"{}
        _MainTex ("FogTex", 2D) = "white" {}
        _MainSpeed ("MainSpeed" , vector) = (0,0,0,0)
        _AlphaTex ("AlphaTex", 2D) = "white" {}
        _AlphaSpeed ("AlphaSpeed" , Range(0,1)) = 0
        _AlphaNoise ("AlphaNoise", 2D) = "white" {}
        _DirstorInt ("DirstorInt",Range(0,1)) = 0
        _Fade("Fade",Range(0,0.9)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType" = "Transparent" }

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
                float fade : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _AlphaTex;
            float4 _AlphaTex_ST;
            sampler2D _AlphaNoise;
            float4 _AlphaNoise_ST;
            float _DirstorInt,_AlphaSpeed;
            float4 _FogColor1,_FogColor2,_MainSpeed;
            sampler2D _FogColorMask;
            float4 _FogColorMask_ST;
            half _Fade;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 posView = mul(UNITY_MATRIX_MV,v.vertex).xyz;
                float dis = length(posView);
                o.fade = smoothstep(_Fade,1, dis*0.01);

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 fogColorMask = tex2D(_FogColorMask, i.uv * _FogColorMask_ST.xy);
                float4 alphanoise = tex2D(_AlphaNoise, i.uv*_AlphaNoise_ST - float2(0,frac(_Time.y*_AlphaSpeed*0.1)));
                float4 col = tex2D(_MainTex, i.uv*_MainTex_ST + alphanoise*_DirstorInt - frac(_Time.y*_MainSpeed.xy*0.1));
                col = pow(col,2);
                
                float4 alpha = 1 - tex2D(_AlphaTex, i.uv * _AlphaTex_ST + float2(0, alphanoise.x * _DirstorInt));

                float fogalpha = smoothstep(alpha-0.5,alpha+0.5,col);
                float3 fincol = lerp (_FogColor1.rgb, _FogColor2.rgb, fogColorMask.x);
                //return i.fade;
                return float4(fincol.rgb*2,fogalpha*i.fade);
            }
            ENDCG
        }
    }
}
