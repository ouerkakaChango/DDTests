Shader "DianDian/Shadow/FloorShadow"
{
    Properties
    {
        _ShadowColor("ShadowColor",color) = (1,1,1,1)
        _ShadowInt("ShadowInt",Range(0,1)) = 0.5
        _ShadowMask("Shadow Mask(0,1)",Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType" = "AlphaTest" }

        Pass
        {
            Tags{"LightMode" = "ForwardBase"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag  
            #pragma multi_compile_fwdbase
            
            #include "UnityCG.cginc"
            #include "PCFShadow.cginc"

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float4 _ShadowCoord : TEXCOORD2;
            };

            float4 _ShadowColor;
            float _ShadowInt;
            float4 _ShadowMask;

            v2f vert (a2v v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld,v.vertex);
                o._ShadowCoord = mul( unity_WorldToShadow[0], mul( unity_ObjectToWorld, v.vertex ) );
                //UNITY_TRANSFER_LIGHTING(o, v.texcoord1);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float shadow = SampleShadowPCF5x5_9Tap(i._ShadowCoord.xyz);
                
                float2 dir = i.worldPos.xz - float2(_ShadowMask.x,_ShadowMask.y);
                float R = length(dir);
                float stopR = 1-smoothstep(_ShadowMask.z,_ShadowMask.w,R);
                
                float Alpha = smoothstep(0,1, (1-(shadow+_ShadowInt+stopR)));

                //return Alpha;
                return float4(_ShadowColor.rgb,Alpha);
            }
            ENDCG
        }
    }
    
}
