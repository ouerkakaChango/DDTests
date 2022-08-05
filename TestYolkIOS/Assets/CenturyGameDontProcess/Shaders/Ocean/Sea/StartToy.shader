Shader "DianDian/Ocean/StartToy"
{
    Properties
    {     
        [Header(01)]
        _Mi1("密度1",float) = 10    
        _SpeedMask("MaskSpeed" ,Range(0,1)) = 0
        _StartRange("StartRange",Range(0,1)) = 1  
        _SpeedOffset("SpeedOffset" ,Range(0,1)) = 0
        [Header(02)]
        _Mi2("密度2",float) = 10
        _SpeedMask2("MaskSpeed2" ,Range(0,1)) = 0
        _StartRange2("StartRange2",Range(0,1)) = 1   
        _SpeedOffset2("SpeedOffset2" ,Range(0,1)) = 0
        [Header(03)]
        _Mi3("密度3",float) = 10
        _StartRange3("StartRange3",Range(0,1)) = 1   
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

            float random (float2 st) {
                return frac(sin(dot(st.xy, float2(565656.233,123123.2033))) * 323434.34344);
            }

            float2 random2( float2 p ) {
                return frac(sin(float2(dot(p,float2(234234.1,54544.7)), sin(dot(p,float2(33332.5,18563.3))))) *323434.34344);
            }

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

            float _SpeedOffset,_StartRange,_SpeedMask;
            float _SpeedMask2,_SpeedOffset2,_StartRange2;
            float _StartRange3;
            float _Mi1,_Mi2,_Mi3;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * _Mi1;
                float2 ipos = floor(uv);  
                float2 fpos = frac(uv);                      
                float2 CenterPos = random2(ipos);   

                float2 uv2 = i.uv * _Mi2;
                float2 ipos2 = floor(uv2);  
                float2 fpos2 = frac(uv2);  
                float2 CenterPos2 = random2(ipos2);        

                float2 uv3 = i.uv * _Mi3;
                float2 ipos3 = floor(uv3);  
                float2 fpos3 = frac(uv3);  
                float2 CenterPos3 = random2(ipos3);    

                float circleCenter = 1.5*abs(sin(_Time.y*_SpeedOffset*0.1 + 6.2831*CenterPos))-0.5;
                float circleCenter2 = 1.5*abs(sin(_Time.y*_SpeedOffset2*0.1 + 5.5633*CenterPos2))-0.5;
                float circleCenter3 = 1.5*abs(sin(6.2541*CenterPos3))-0.5;

                float circle = length(fpos - circleCenter);
                float circle2 = length(fpos2 - circleCenter2);
                float circle3 = length(fpos3 - circleCenter3);

                float Mask = 1.5*abs(sin(_Time.y*_SpeedMask + 6.2831*CenterPos.x))-0.5;      
                float Mask2 = 1.5*abs(sin(_Time.y*_SpeedMask2 + 5.5633*CenterPos2.x))-0.5;   
                float Mask3 = 1.5*abs(sin(6.2541*CenterPos3.x))-0.5;      

                float AAA = 1 - smoothstep(0, _StartRange*0.1 ,circle);
                float AAA2 = 1 - smoothstep(0, _StartRange2*0.1 ,circle2);
                float AAA3 = 1 - smoothstep(0, _StartRange3*0.1 ,circle3);

                float color = AAA*Mask + AAA2*Mask2 + AAA3*Mask3;

                return color;
            }
            ENDCG
        }
    }
}
