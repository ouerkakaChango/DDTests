Shader "Hidden/PostProcess/FPVignette"
{
    Properties
    {
        _MainTex("Main (RGB)", 2D) = "white" {}
    }
    CGINCLUDE

    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    sampler2D _MainTex;
    float4 _MainTex_ST;
    uniform float2 _MainTex_TexelSize;

    half3 _Vignette_Color;
    half2 _Vignette_Center;
    half4 _Vignette_Settings;
    half _Vignette_Opacity;
    sampler2D _Vignette_Mask;

    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        if (_MainTex_TexelSize.y < 0.0)
        {
            o.uv.y = 1.0 - o.uv.y;
        }
        return o;
    }

    half4 fragClassic(v2f i) : SV_Target
    {
        float2 uv = i.uv;
        half4 col = tex2D(_MainTex, uv);

        half2 d = abs(uv - _Vignette_Center) * _Vignette_Settings.x;
        d.x *= lerp(1.0, _ScreenParams.x / _ScreenParams.y, _Vignette_Settings.w);
        d = pow(saturate(d), _Vignette_Settings.z); // Roundness
        half vfactor = pow(saturate(1.0 - dot(d, d)), _Vignette_Settings.y);
        col.rgb *= lerp(_Vignette_Color, (1.0).xxx, vfactor);

        return col;
    }

    half4 fragMasked(v2f i) : SV_Target
    {
        float2 uv = i.uv;
        half4 col = tex2D(_MainTex, uv);

        half vfactor = tex2D(_Vignette_Mask, uv).a;
        half3 new_color = col.rgb * lerp(_Vignette_Color, (1.0).xxx, vfactor);
        col.rgb = lerp(col.rgb, new_color, _Vignette_Opacity);
        return col;
    }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "Classic"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragClassic

            ENDCG
        }

        Pass
        {
            Name "Masked"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragMasked

            ENDCG
        }
    }
}
