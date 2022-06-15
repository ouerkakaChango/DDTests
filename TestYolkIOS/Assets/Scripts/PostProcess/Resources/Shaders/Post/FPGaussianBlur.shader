Shader "Hidden/PostProcess/FPGaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SampleScale("Sample Scale", float) = 1
    }
    
    CGINCLUDE

    #include "UnityCG.cginc"
    
    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };
    
    sampler2D _MainTex;
    half4 _MainTex_TexelSize;
    
    v2f vert (appdata_img v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }
    
    float _SampleScale;
    fixed4 frag_downsample (v2f i) : SV_Target
    {
        half4 col1 = tex2D(_MainTex, i.uv + _SampleScale * half2(0.75, 0.25) * _MainTex_TexelSize.xy);
        half4 col2 = tex2D(_MainTex, i.uv + _SampleScale * half2(-0.25, 0.75) * _MainTex_TexelSize.xy);
        half4 col3 = tex2D(_MainTex, i.uv + _SampleScale * half2(-0.75, -0.25) * _MainTex_TexelSize.xy);
        half4 col4 = tex2D(_MainTex, i.uv + _SampleScale * half2(0.25, -0.75) * _MainTex_TexelSize.xy);
        return (col1 + col2 + col3 + col4) / 4;
    }

    fixed4 frag_upsample(v2f i) : SV_Target
    {
        half4 col1 = tex2D(_MainTex, i.uv + _SampleScale * half2(0.75, 0.25) * _MainTex_TexelSize.xy);
        half4 col2 = tex2D(_MainTex, i.uv + _SampleScale * half2(-0.25, 0.75) * _MainTex_TexelSize.xy);
        half4 col3 = tex2D(_MainTex, i.uv + _SampleScale * half2(-0.75, -0.25) * _MainTex_TexelSize.xy);
        half4 col4 = tex2D(_MainTex, i.uv + _SampleScale * half2(0.25, -0.75) * _MainTex_TexelSize.xy);
        return (col1 + col2 + col3 + col4) / 4;
    }

    fixed4 frag_single(v2f i) : SV_Target
    {
        half4 col1 = tex2D(_MainTex, i.uv + _SampleScale * half2(0.75, 0.25) * _MainTex_TexelSize.xy);
        half4 col2 = tex2D(_MainTex, i.uv + _SampleScale * half2(-0.25, 0.75) * _MainTex_TexelSize.xy);
        half4 col3 = tex2D(_MainTex, i.uv + _SampleScale * half2(-0.75, -0.25) * _MainTex_TexelSize.xy);
        half4 col4 = tex2D(_MainTex, i.uv + _SampleScale * half2(0.25, -0.75) * _MainTex_TexelSize.xy);
        return (col1 + col2 + col3 + col4) / 4;
    }

    ENDCG

    SubShader
    {
        Pass
        {
            ZTest Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_downsample

            ENDCG
        }

        Pass
        {
            ZTest Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_upsample

            ENDCG
        }

        Pass
        {
            ZTest Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_single

            ENDCG
        }
    }
}
