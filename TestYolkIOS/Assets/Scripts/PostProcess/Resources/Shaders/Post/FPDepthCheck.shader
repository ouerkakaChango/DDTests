Shader "Hidden/PostProcess/FPPostCheck"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
       
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
			#pragma multi_compile DEPTHBUFFER_ON DEPTHNORMAL_ON
 
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenuv : TEXCOORD1;
            };
           
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenuv = ComputeScreenPos(o.pos);
                return o;
            }

			#if defined(DEPTHBUFFER_ON)
				sampler2D _DepthTex;
			#elif defined(DEPTHNORMAL_ON)
				sampler2D _CameraDepthTexture;
			#endif

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.screenuv.xy / i.screenuv.w;
				#if defined(DEPTHBUFFER_ON)
					float depth = 1 - Linear01Depth(SAMPLE_DEPTH_TEXTURE(_DepthTex, uv));
				#elif defined(DEPTHNORMAL_ON)
					float depth = 1 - Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
				#endif
                return fixed4(depth, depth, depth, 1);
            }
            ENDCG
        }
    }
}