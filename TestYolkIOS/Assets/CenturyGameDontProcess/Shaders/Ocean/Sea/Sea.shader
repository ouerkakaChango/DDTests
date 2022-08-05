Shader "DianDian/Ocean/Sea"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaOffset("AlphaOffset",Range(0,1)) = 0.5
        _NormalTex("NormalTex", 2D) = "white" {}
        _NormakScale("NormakScale",Range(0,5)) = 1
        _ReflectionTex("ReflectionTex",2D) = "white" {}
        _DistorInt("DistorInt",Range(0,1)) = 0.2
        _SpecularColor("Specular Color",Color) = (1,1,1,1)
        _SpecularDir("SpecularDir",vector) = (0,0,0,0)
        _SpeculatrIntensity("Speculatr Intensity",Range(0,5)) = 0.5
        _SpeculatrSmoothness("Speculatr Smoothness",Range(0,50)) = 1.5
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType"="Transparent" }

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
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 posCS : SV_POSITION;
                float4 TtoW0 : TEXCOORD1;
                float4 TtoW1 : TEXCOORD2;
                float4 TtoW2 : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _NormalTex; float4 _NormalTex_ST;
            half _NormakScale;

            float4 _SpecularColor,_SpecularDir;
            float _SpeculatrIntensity,_SpeculatrSmoothness;

            sampler2D _ReflectionTex;
            half _DistorInt,_AlphaOffset;

            v2f vert (appdata v)
            {
                v2f o;
                float3 posWS = mul(unity_ObjectToWorld,v.vertex);              

                float3 tangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 normal = UnityObjectToWorldNormal(v.normal);
                float3 biTangent = cross(tangent,normal) * v.tangent.w;
                o.TtoW0 = fixed4(tangent.x,biTangent.x,normal.x,posWS.x);
                o.TtoW1 = fixed4(tangent.y,biTangent.y,normal.y,posWS.y);
                o.TtoW2 = fixed4(tangent.z,biTangent.z,normal.z,posWS.z);

                o.posCS = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.posCS);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldPos = float3(i.TtoW0.w,i.TtoW1.w,i.TtoW2.w);
                half4 normalTex1 = tex2D(_NormalTex, i.uv.xy * _NormalTex_ST.xy + float2(0,frac(_Time.y*0.1)));
                half4 normalTex2 = tex2D(_NormalTex, i.uv.xy * _NormalTex_ST.xy);
                half4 normal3 = normalTex1 * normalTex2;
                normal3.xy *= _NormakScale;
                normal3.z = sqrt(1 - saturate(dot(normal3.xy,normal3.xy)));
                float3 UcknormalSpec = UnpackNormal(normal3).xyz;
                float3 N = normalize(half3(dot(i.TtoW0.xyz,UcknormalSpec),dot(i.TtoW1.xyz,UcknormalSpec),dot(i.TtoW2.xyz,UcknormalSpec)));

                half3 L = normalize(UnityWorldSpaceLightDir(worldPos.xyz));
                half3 V = normalize(worldPos.xyz - _WorldSpaceCameraPos.xyz + _SpecularDir.xyz);
                half3 H = normalize(L+V);
                half4 specular = _SpecularColor * _SpeculatrIntensity * pow(saturate(dot(N,H)),_SpeculatrSmoothness);

                half4 reflect = tex2D(_ReflectionTex, i.screenPos.xy / i.screenPos.w + float2(0,N.y)*_DistorInt);
                //return dot(N,L);
                half Alpha = smoothstep(_AlphaOffset,1,(1-i.uv.y)+_AlphaOffset);
                return float4((reflect +specular).rgb,Alpha);
                
            }
            ENDCG
        }
    }
}
