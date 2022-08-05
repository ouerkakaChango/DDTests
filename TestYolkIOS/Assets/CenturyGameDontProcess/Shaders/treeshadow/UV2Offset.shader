Shader "Unlit/UV2Offset"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Rotate("旋转幅度",Range(0,0.5)) = 0  
        _Offset("位移位置",Range(0,0.5)) = 1
        _Speed("速度",float) = 1
        _Rotate01("旋转幅度02",Range(0,0.5)) = 0
        _Offset01("位移位置02",Range(0,0.5)) = 1
        _Speed01("速度02",float) = 1
        _Color("Color",color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend DstColor Zero
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _Speed,_Rotate,_Offset;
            half _Rotate01,_Speed01,_Offset01;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv1 = v.uv1;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float RotateRange = sin(_Time.y * _Speed)*_Rotate;
                float2 uv = i.uv - float2(0.5, 0);  
                //旋转矩阵公式  
                uv = float2(uv.x * cos(RotateRange ) - uv.y * sin(RotateRange ), 
                uv.x * sin(RotateRange ) + uv.y * cos(RotateRange ));  
                //恢复纹理位置  
                uv += float2(0.5,0); 

                fixed4 col = tex2D(_MainTex, uv + float2(_Offset,0));


                float RotateRange01 = sin(_Time.y * _Speed01) * _Rotate01;
                float2 uv1 = i.uv1 - float2(0.5, 0);  
                //旋转矩阵公式  
                uv1 = float2(uv1.x * cos(RotateRange01 ) - uv1.y * sin(RotateRange01 ), 
                uv1.x * sin(RotateRange01 ) + uv1.y * cos(RotateRange01 ));  
                //恢复纹理位置  
                uv1 += float2(0.5,0);     
                //float OffsetRange01 = sin(_Time.y * _Speed01) * _Offset01 ;
                fixed4 col1 = tex2D(_MainTex, uv1 + float2(_Offset01,0));


                return saturate(col * col1 + _Color);
            }
            ENDCG
        }
    }
}
