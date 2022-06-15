Shader "Hidden/PostProcess/FPUnderWater" 
{
    Properties 
	{
        _WaterColor ("WaterColor", Color) = (0.463,1,0.933,1)
		_DepthColor ("DepthColor", Color) = (0.0,0.2,0.5,1.0)
		_FogSmooth	("FogSmooth", Range(0.0,0.1)) = 0.01
        _MainTex	("MainTex", 2D) = "white" {}
		_CausticsTex("CausticsTex", 2D) = "white" {}
		_CauStrength("Caustics Strength", float) = 100
		_NoiseTex	("NoiseTex", 2D) = "white" {}
		_CullNoise	("CullNoise", Range(0, 1)) = 0.9
        _WetHeight	("WerHeight", Range(0.05, 1)) = 0.097
		_Disturtion ("Disturtion", Range(0, 0.1)) = 0.03
        _WaveSpeed1 ("WaveSpeed1", Float) = 0.7
        _VaveSpeed2 ("VaveSpeed2", Float) = -0.8
    }
    SubShader 
	{
        Pass 
		{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform sampler2D _MainTex; 
			uniform sampler2D _CausticsTex;
			uniform fixed _CauStrength;
            uniform half _CullNoise;
            uniform fixed4 _WaterColor;
			uniform fixed4 _DepthColor;
			uniform fixed _FogSmooth;
            uniform half _Disturtion;
            uniform sampler2D _NoiseTex; 
            uniform half _WaterHeight;
            uniform half _WetHeight;
            uniform half _WaveSpeed1;
            uniform half _VaveSpeed2;
			uniform half _WaterPosY;
			uniform sampler2D _DepthTex;

			uniform fixed4x4 _InverseMVP;

            struct VertexInput 
			{
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            struct VertexOutput 
			{
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };

            VertexOutput vert (VertexInput v)
			{
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex );
                return o;
            }

			float3 CamToWorld(in float2 uv, in float depth)
			{
				float4 pos = float4(uv.x, uv.y, depth, 1.0);
					pos.xyz = pos.xyz * 2.0 - 1.0;
				pos = mul(_InverseMVP, pos);
				return pos.xyz / pos.w;
			}


            float4 frag(VertexOutput i) : COLOR
			{
				float waterHeight = _WaterHeight/_ScreenParams.y-0.01h;

                half2 waveUV1 = half2(((_WaveSpeed1*_Time.r)+i.uv0.r),i.uv0.g);
                half4 _NoiseTex1 = tex2D(_NoiseTex,waveUV1);
                half2 waveUV2 = half2(((_Time.r*_VaveSpeed2)+i.uv0.r),i.uv0.g);
                half4 _NoiseTex2 = tex2D(_NoiseTex,waveUV2);
                half noiseAppend = saturate((_NoiseTex1.r+_NoiseTex2.b)*0.5);

				//淡化水上水下过渡交界
                half waterJunction = smoothstep( (_WetHeight+waterHeight), waterHeight, i.uv0.g );

				//黑白Mask，分离水上水下效果
                half waterMask = saturate(((saturate((noiseAppend+waterJunction))-_CullNoise)*10.0));
    
				//扭曲屏幕（水上扭曲，水下不扭曲）
				half2 disturUV = (i.uv0+((noiseAppend-0.5)*_Disturtion*waterMask));
                half4 mainCol = tex2D(_MainTex,disturUV);

				//全屏颜色输出
                half3 emissive = lerp(mainCol.rgb,(mainCol.rgb*waterMask*_WaterColor.rgb),(waterMask*waterJunction));

				//深度
				half depth = tex2D(_DepthTex, disturUV).r;

				//世界坐标
				half3 pos = CamToWorld(fixed2(i.uv0.x, i.uv0.y), 1-depth);

				//较散
				fixed3 caustics = tex2D(_CausticsTex,fixed2(pos.x,pos.z)+noiseAppend).rgb*depth*_CauStrength*emissive;

				//根据Y值算雾
				fixed3 waterfog = smoothstep(0,1.0,saturate((_WaterPosY - pos.y)*_FogSmooth));

				//输出
				fixed3 underEffect = saturate(1-waterfog*2)*waterfog*3*caustics+waterfog*_DepthColor;

                fixed3 finalColor = emissive + underEffect;

                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
}