Shader "Unlit/projectorshadow"
{
    Properties 
    {
        _ShadowTex ("Cookie", 2D) = "black"{}
        _ShadowColor("shadowColor",Color) = (1,1,1,1)
    }
    Subshader 
    {
        Tags {"Queue"="Transparent"}

        Pass 
        {
            ZWrite Off
            ColorMask RGB
            Blend DstColor Zero
            Offset -1, -1
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            
            struct v2f 
            {
                float4 uvShadow : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 pos : SV_POSITION;
            };
            
            float4x4 unity_Projector;
            float4 _ShadowColor;
            sampler2D _ShadowTex;
            
            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos (vertex);
                o.uvShadow = mul (unity_Projector, vertex);
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            
            
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = UNITY_PROJ_COORD(i.uvShadow);

                float4 texS = 1;
                
                if(uv.x>=0 && uv.x<1 && uv.y>=0 && uv.y<1)
                texS = tex2D(_ShadowTex, uv);
                
                float4 ratio =  texS.r + _ShadowColor * (1-texS.r);
                
                UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(1,1,1,1));
                return ratio;
            }
            ENDCG
        }
    }
}
