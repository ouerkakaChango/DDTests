Shader "Unlit/UVMaskV"
{
    Properties
    {
        [HideInInspector]
        _MainTex("MainTex", 2D) = "white" {}
        BaseColor("BaseColor", Color) = (1,1,1,1)    
        StartAlpha("StartAlpha", Range(0,1)) = 0.2
        EndAlpha("EndAlpha", Range(0,1)) = 0
        Invert("Invert", Int) = 1
        //UseStartEnd("UseStartEnd",Int) = 0
        Power("Power", Float) = 3.84
    }
    SubShader
    {
        Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float4 BaseColor;
            float StartAlpha;
            float EndAlpha;
            int Invert;
            //int UseStartEnd;
            float Power;

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float k = Invert ?  1 - uv.y : uv.y ;
                k = pow(k, Power);
                //if (UseStartEnd)
                {
                    k = lerp(StartAlpha, EndAlpha, k);
                }
                k = pow(k, 2.2f);
                return float4(BaseColor.rgb, k);
            }
            ENDCG
        }
    }
}
