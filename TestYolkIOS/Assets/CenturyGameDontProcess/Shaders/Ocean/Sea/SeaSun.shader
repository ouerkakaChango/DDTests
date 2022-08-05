Shader "DianDian/Ocean/SeaSun"
{
    Properties
    {
        _BeachTex ("BeachTex", 2D) = "white" {}
        _BeachColor("BeachColor",Color) = (1,1,1,1)
        _BeachColor2("BeachColor2",Color) = (1,1,1,1)
        _Depthcol("Depth",Range(0.2,1)) = 0
        _ReflectTex("reflectTex",2D) = "white" {}
        _ReflectColor("ReflectColor",Color) = (1,1,1,1)
        _Wat2Bea("Wat2Bea",Range(0,1)) = 0.5
        _AlphaOffset("AlphaOffset",Range(0,1)) = 0.5
        [Header(Normal)]
        [Speac(10)]
        _NormalTex("NormalTex", 2D) = "white" {}
        //_NormalScale("NormalScale",Range(4,5)) = 5
        [Header(Distor)]
        [Speac(10)]
        _DistorIntBeach("DistorIntBeach",Range(0,1)) = 0.2
        _DistorIntReflect("DistorIntReflect",Range(0,1)) = 0.2
        [Header(Specular)]
        [Speac(10)]
        _SpecularNormal("Specular Normal", 2D) = "white" {}
        _SpecularColor("Specular Color",Color) = (1,1,1,1)
        _SpecularDir("SpecularDir",vector) = (0,0,0,0)
        _SpeculatrIntensity("Speculatr Intensity",Range(0,10)) = 0.5
        _SpeculatrSmoothness("Speculatr Smoothness",Range(0.1,50)) = 1.5
        [Header(WaveFoam)]
        [Speac(10)]
        _WaveNormal("WaveNormal",2D) = "white"{}
        _WaveNormalInt("WaveNormalInt",Range(0,1)) = 1
        _WaveSpeed("WaveSpeed",Range(0,0.1)) = 0.03
        _FoamTex("FoamTex",2D) = "white"{}
        _FoamSpeed("FoamSpeed",Range(0,0.1)) = 0.03
        _FoamNoiseTex("FoamNoiseTex",2D) = "white"{}
        _FoamTex02("FoamTex02",2D) = "white"{}
        _FoamNoiseTex02("FoamNoiseTex02",2D) = "white"{}
        [Header(Caustic)]
        [Speac(10)]
        [NoScaleOffset]_CausticTex("CausticTex", 2D) = "white"{}
        _CausticInt("CausticInt",Range(0,5)) = 1
        _CausticSize("Tili01(XY) Tili02(ZW)",vector) = (1,1,1,1)
        _CausticSpeedDir("01DirSpeed(XY) 02DirSpeed(ZW)",vector) = (1,1,1,1)
        _CausticNoise("CausticNoise",2D) = "white"{}

    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 300

        Pass
        {
            Tags {"LightMode" = "ForwardBase"}

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
            };

            sampler2D _BeachTex;float4 _BeachTex_ST;
            sampler2D _ReflectTex;float4 _ReflectTex_ST;
            float _Wat2Bea,_Depthcol;
            float4 _BeachColor,_BeachColor2,_ReflectColor;
            half _DistorIntBeach,_AlphaOffset,_DistorIntReflect;

            sampler2D _NormalTex; float4 _NormalTex_ST;
            //half _NormalScale;

            sampler2D _SpecularNormal;float4 _SpecularNormal_ST;
            float4 _SpecularColor,_SpecularDir;
            float _SpeculatrIntensity,_SpeculatrSmoothness;

            sampler2D _WaveNormal;float4 _WaveNormal_ST;
            sampler2D _FoamTex;float4 _FoamTex_ST;
            sampler2D _FoamTex02;float4 _FoamTex02_ST;
            sampler2D _FoamNoiseTex;float4 _FoamNoiseTex_ST;
            sampler2D _FoamNoiseTex02;float4 _FoamNoiseTex02_ST;
            float _WaveNormalInt,_WaveSpeed,_FoamSpeed;

            sampler2D _CausticTex; float4 _CausticTex_ST;
            sampler2D _CausticNoise;float4 _CausticNoise_ST;
            half4 _CausticSpeedDir,_CausticSize;
            float _CausticInt;

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
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldPos = float3(i.TtoW0.w,i.TtoW1.w,i.TtoW2.w);
                half4 normalTex1 = tex2D(_NormalTex, i.uv.xy * _NormalTex_ST.xy + float2(0,frac(_Time.y*0.1)));
                half4 normalTex2 = tex2D(_NormalTex, i.uv.xy * _NormalTex_ST.xy);
                half4 normal3 = normalTex1 * normalTex2;
                normal3.xy *= 5;
                normal3.z = sqrt(1 - saturate(dot(normal3.xy,normal3.xy)));
                float3 UcknormalSpec = UnpackNormal(normal3).xyz;
                float3 N = normalize(half3(dot(i.TtoW0.xyz,UcknormalSpec),dot(i.TtoW1.xyz,UcknormalSpec),dot(i.TtoW2.xyz,UcknormalSpec)));

                half4 wavenormal = tex2D(_WaveNormal, i.uv.xy * _WaveNormal_ST.xy + float2(0,frac(_Time.y * _WaveSpeed)));
                float3 Ucknormalwave = UnpackNormal(wavenormal).xyz;
                float3 waveN = normalize(half3(dot(i.TtoW0.xyz,Ucknormalwave),dot(i.TtoW1.xyz,Ucknormalwave),dot(i.TtoW2.xyz,Ucknormalwave)));
                

                float lerpn = (N.y + waveN.y * _WaveNormalInt);
                half depth = smoothstep(0.2,_Depthcol,i.uv.y);
                half4 depthcol = lerp(_BeachColor,_BeachColor2,depth); 
                half4 reflect = tex2D(_ReflectTex, i.uv * _ReflectTex_ST + lerpn*_DistorIntReflect) * _ReflectColor;    
                half4 beachtex = tex2D(_BeachTex, i.uv * _BeachTex_ST + lerpn*_DistorIntBeach) * depthcol;

                half4 foam = tex2D(_FoamTex, i.uv.xy * _FoamTex_ST.xy + float2(0,frac(_Time.y * _FoamSpeed)));
                half4 foamnoise = tex2D(_FoamNoiseTex, i.uv.xy * _FoamNoiseTex_ST.xy + frac(_Time.y * 0.06));
                half foamcol = foamnoise.r*foam.r;
                foamcol = pow(foamcol,2)*5;
                half4 foam02 = tex2D(_FoamTex02, i.uv.xy * _FoamTex02_ST.xy + float2(0,frac(_Time.y * 0.02)));
                half4 foamnoise02 = tex2D(_FoamNoiseTex02, i.uv.xy * _FoamNoiseTex02_ST.xy + frac(_Time.y * 0.05));
                half foamcol2 = foamnoise02.r*foam02.r;
                //return foamcol2*5;

                half4 spNormal1 = tex2D(_SpecularNormal, i.uv.xy * _SpecularNormal_ST.xy + float2(0,frac(_Time.y*0.1)));
                half4 spNormal2 = tex2D(_SpecularNormal, i.uv.xy * _SpecularNormal_ST.xy);
                half4 spNormal = spNormal1 * spNormal2;
                float3 spnormalSpec = UnpackNormal(spNormal).xyz;
                float3 SPN = normalize(half3(dot(i.TtoW0.xyz,spnormalSpec),dot(i.TtoW1.xyz,spnormalSpec),dot(i.TtoW2.xyz,spnormalSpec)));
                half3 L = normalize(UnityWorldSpaceLightDir(worldPos.xyz)-sin(_SpecularDir.xyz*0.1));
                half3 V = normalize(UnityWorldSpaceViewDir(worldPos.xyz));
                half3 H = normalize(L+V);
                half4 specular = _SpecularColor * _SpeculatrIntensity * pow(saturate(dot(SPN,H)),_SpeculatrSmoothness);
                //return specular;

                half4 dirspeed = _CausticSpeedDir*0.01;
                half4 causticTex01 = tex2D(_CausticTex, i.uv * _CausticSize.xy + _Time.y * dirspeed.xy + float2(0,N.y)*0.05);
                half4 causticTex02 = tex2D(_CausticTex, i.uv * _CausticSize.zw + _Time.y * dirspeed.zw * float2(-1.07,1.43) + float2(0,N.y)*0.05);
                half4 caustic = min(pow(causticTex01,2), pow(causticTex02,2));
                caustic = caustic*_CausticInt*10;
                half4 causticnoise = tex2D(_CausticNoise,i.uv*_CausticNoise_ST + _Time.y*0.05);
                caustic *= causticnoise*pow((1-i.uv.y),2);
                //return caustic;

                half4 fincol = lerp(reflect,beachtex,_Wat2Bea) + caustic + specular + foamcol + foamcol2*5;
                half Alpha = smoothstep(_AlphaOffset,1,(1-i.uv.y)+_AlphaOffset);
                //return 0;
                return float4(fincol.rgb,Alpha);
                
            }
            ENDCG
        }
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 200
        
        Pass
        {
            Tags {"LightMode" = "ForwardBase"}

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
            };

            sampler2D _BeachTex;float4 _BeachTex_ST;
            sampler2D _ReflectTex;float4 _ReflectTex_ST;
            float _Wat2Bea,_Depthcol;
            float4 _BeachColor,_BeachColor2,_ReflectColor;
            half _DistorIntBeach,_AlphaOffset,_DistorIntReflect;

            sampler2D _NormalTex; float4 _NormalTex_ST;

            float4 _SpecularColor;

            sampler2D _WaveNormal;float4 _WaveNormal_ST;
            sampler2D _FoamTex02;float4 _FoamTex02_ST;
            sampler2D _FoamNoiseTex02;float4 _FoamNoiseTex02_ST;
            float _WaveNormalInt,_WaveSpeed,_FoamSpeed;

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
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldPos = float3(i.TtoW0.w,i.TtoW1.w,i.TtoW2.w);
                half4 normalTex1 = tex2D(_NormalTex, i.uv.xy * _NormalTex_ST.xy + float2(0,frac(_Time.y*0.1)));
                half4 normalTex2 = tex2D(_NormalTex, i.uv.xy * _NormalTex_ST.xy);
                half4 normal3 = normalTex1 * normalTex2;
                normal3.xy *= 5;
                normal3.z = sqrt(1 - saturate(dot(normal3.xy,normal3.xy)));
                float3 UcknormalSpec = UnpackNormal(normal3).xyz;
                float3 N = normalize(half3(dot(i.TtoW0.xyz,UcknormalSpec),dot(i.TtoW1.xyz,UcknormalSpec),dot(i.TtoW2.xyz,UcknormalSpec)));

                half4 wavenormal = tex2D(_WaveNormal, i.uv.xy * _WaveNormal_ST.xy + float2(0,frac(_Time.y * _WaveSpeed)));
                float3 Ucknormalwave = UnpackNormal(wavenormal).xyz;
                float3 waveN = normalize(half3(dot(i.TtoW0.xyz,Ucknormalwave),dot(i.TtoW1.xyz,Ucknormalwave),dot(i.TtoW2.xyz,Ucknormalwave)));           

                float lerpn = (N.y + waveN.y * _WaveNormalInt);
                half depth = smoothstep(0.2,_Depthcol,i.uv.y);
                half4 depthcol = lerp(_BeachColor,_BeachColor2,depth); 
                half4 reflect = tex2D(_ReflectTex, i.uv * _ReflectTex_ST + lerpn*_DistorIntReflect) * _ReflectColor;    
                half4 beachtex = tex2D(_BeachTex, i.uv * _BeachTex_ST + lerpn*_DistorIntBeach) * depthcol;

                half4 foam02 = tex2D(_FoamTex02, i.uv.xy * _FoamTex02_ST.xy + float2(0,frac(_Time.y * 0.02)));
                half4 foamnoise02 = tex2D(_FoamNoiseTex02, i.uv.xy * _FoamNoiseTex02_ST.xy + frac(_Time.y * 0.05));
                half foamcol2 = foamnoise02.r*foam02.r;
                //return foamcol2*5;

                half4 specular = smoothstep(0.49,0.55,i.uv.x)*_SpecularColor;
                //half4 specular = _SpecularColor * _SpeculatrIntensity * pow(saturate(dot(SPN,H)),_SpeculatrSmoothness);
                //return specular;
                //return 


                half4 fincol = lerp(reflect,beachtex,_Wat2Bea) + specular + foamcol2*5;
                half Alpha = smoothstep(_AlphaOffset,1,(1-i.uv.y)+_AlphaOffset);
                return float4(fincol.rgb,Alpha);
                
            }
            ENDCG
        }
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 100
        
        Pass
        {
            Tags {"LightMode" = "ForwardBase"}

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
            };

            sampler2D _BeachTex;float4 _BeachTex_ST;
            sampler2D _ReflectTex;float4 _ReflectTex_ST;
            float _Wat2Bea,_Depthcol;
            float4 _BeachColor,_BeachColor2,_ReflectColor;
            half _DistorIntBeach,_AlphaOffset,_DistorIntReflect;

            sampler2D _NormalTex; float4 _NormalTex_ST;
            
            float4 _SpecularColor;

            sampler2D _WaveNormal;float4 _WaveNormal_ST;
            sampler2D _FoamTex02;float4 _FoamTex02_ST;
            sampler2D _FoamNoiseTex02;float4 _FoamNoiseTex02_ST;
            float _WaveNormalInt,_WaveSpeed,_FoamSpeed;

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
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldPos = float3(i.TtoW0.w,i.TtoW1.w,i.TtoW2.w);
                half4 normalTex1 = tex2D(_NormalTex, i.uv.xy * _NormalTex_ST.xy + float2(0,frac(_Time.y*0.1)));
                half4 normalTex2 = tex2D(_NormalTex, i.uv.xy * _NormalTex_ST.xy);
                half4 normal3 = normalTex1 * normalTex2;
                normal3.xy *= 5;
                normal3.z = sqrt(1 - saturate(dot(normal3.xy,normal3.xy)));
                float3 UcknormalSpec = UnpackNormal(normal3).xyz;
                float3 N = normalize(half3(dot(i.TtoW0.xyz,UcknormalSpec),dot(i.TtoW1.xyz,UcknormalSpec),dot(i.TtoW2.xyz,UcknormalSpec)));

                half4 wavenormal = tex2D(_WaveNormal, i.uv.xy * _WaveNormal_ST.xy + float2(0,frac(_Time.y * _WaveSpeed)));
                float3 Ucknormalwave = UnpackNormal(wavenormal).xyz;
                float3 waveN = normalize(half3(dot(i.TtoW0.xyz,Ucknormalwave),dot(i.TtoW1.xyz,Ucknormalwave),dot(i.TtoW2.xyz,Ucknormalwave)));           

                float lerpn = (N.y + waveN.y * _WaveNormalInt);
                half depth = smoothstep(0.2,_Depthcol,i.uv.y);
                half4 depthcol = lerp(_BeachColor,_BeachColor2,depth); 
                half4 reflect = tex2D(_ReflectTex, i.uv * _ReflectTex_ST + lerpn*_DistorIntReflect) * _ReflectColor;    
                half4 beachtex = tex2D(_BeachTex, i.uv * _BeachTex_ST + lerpn*_DistorIntBeach) * depthcol;

                half4 specular = smoothstep(0.49,0.55,i.uv.x)*_SpecularColor;
                //half4 specular = _SpecularColor * _SpeculatrIntensity * pow(saturate(dot(SPN,H)),_SpeculatrSmoothness);
                //return specular;

                half4 fincol = lerp(reflect,beachtex,_Wat2Bea) + specular;
                half Alpha = smoothstep(_AlphaOffset,1,(1-i.uv.y)+_AlphaOffset);
                return float4(fincol.rgb,Alpha);
                
            }
            ENDCG
        }
    }
}
