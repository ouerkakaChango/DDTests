Shader "Hidden/PostProcess/FPHBAO"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
			Name "AO Pass"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragHBAO

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D_float _DepthTexDOF;

			sampler2D_float _CameraDepthTexture;
			sampler2D _CameraDepthNormalsTexture;

			v2f vert(appdata v, out float4 outpos : SV_POSITION)
			{
				v2f o;
				o.uv = float4(v.uv, v.uv);
				#if UNITY_UV_STARTS_AT_TOP
					if (_MainTex_TexelSize.y < 0)
						o.uv.w = 1 - o.uv.w;
				#endif

				outpos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			float _Intensity;
			float _Radius;
			float _MaxDistance;
			float _MaxPixelRadius;
			int _RayMarchingStepCount;
			int _RayMarchingDirectionCount;
			float _AngleBiasValue;
			float _AOmultiplier;
			float _DistanceFalloff;
			float _NegInvRadius2;
			sampler2D _NoiseTex;
			float4 _NoiseTex_TexelSize;
			float4 _UVToView;
			float4 _TargetScale;

			inline float Falloff2(float distance, float radius)
			{
				float a = distance / radius;
				return clamp(1.0 - a * a, 0.0, 1.0);
			}

			inline float2 RotateDirections(float2 dir, float2 rot) {
				return float2(dir.x * rot.x - dir.y * rot.y,
					dir.x * rot.y + dir.y * rot.x);
			}

			inline float2 GetRayMarchingDir(float angle, float2 rand)
			{
				float sinValue, cosValue;
				sincos(angle, sinValue, cosValue);
				return RotateDirections(float2(cosValue, sinValue), rand);
			}

			float3 ReconstructViewPos2(float2 uv, float eyeDepth)
			{
				float3x3 proj = (float3x3)unity_CameraProjection;
				float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
				float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);
				return float3((uv * 2.0 - 1.0 - p13_31) / p11_22 * (eyeDepth), eyeDepth);
			}

			float3 ReconstructViewPos2(float2 uv)
			{
				float depth = UNITY_SAMPLE_DEPTH(tex2D(_DepthTexDOF, uv));
				depth = LinearEyeDepth(depth);
				return ReconstructViewPos2(uv, depth);
			}

			inline float3 FetchViewPos(float2 uv, float eyeDepth) {
				return float3((uv * _UVToView.xy + _UVToView.zw) * eyeDepth, eyeDepth);
			}

			inline float3 FetchViewPos(float2 uv) {
				float depth = UNITY_SAMPLE_DEPTH(tex2D(_DepthTexDOF, uv * _TargetScale.xy));
				depth = LinearEyeDepth(depth);
				return FetchViewPos(uv, depth);
			}

			inline float Falloff(float distanceSquare) {
				// 1 scalar mad instruction
				return distanceSquare * _NegInvRadius2 + 1.0;
			}

			inline float ComputeAO(float3 P, float3 N, float3 S) {
				float3 V = S - P;
				float VdotV = dot(V, V);
				float NdotV = dot(N, V) * rsqrt(VdotV);

				// Use saturate(x) instead of max(x,0.f) because that is faster on Kepler
				return saturate(NdotV - _AngleBiasValue) * saturate(Falloff(VdotV));
			}

			float3 GetNormal(float2 uv)
			{
				float4 cdn = tex2D(_CameraDepthNormalsTexture, uv);
				float3 viewNormal = DecodeViewNormalStereo(cdn);
				viewNormal.z = -viewNormal.z;
				return viewNormal;
			}

			inline float3 MinDiff(float3 P, float3 Pr, float3 Pl) {
				float3 V1 = Pr - P;
				float3 V2 = P - Pl;
				return (dot(V1, V1) < dot(V2, V2)) ? V1 : V2;
			}

			float3 ReconstructNormal(float2 uv, float3 viewPos, float2 InvScreenParams)
			{
				float3 Pr, Pl, Pt, Pb;
				Pr = FetchViewPos(uv + float2(InvScreenParams.x, 0));
				Pl = FetchViewPos(uv + float2(-InvScreenParams.x, 0));
				Pt = FetchViewPos(uv + float2(0, InvScreenParams.y));
				Pb = FetchViewPos(uv + float2(0, -InvScreenParams.y));
				float3 N = normalize(cross(MinDiff(viewPos, Pr, Pl), MinDiff(viewPos, Pt, Pb)));
				return N;
			}

			float4 fragHBAO(v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
			{
				float2 InvScreenParams = _ScreenParams.zw - 1.0;

				float depth = UNITY_SAMPLE_DEPTH(tex2D(_DepthTexDOF, i.uv));
				float eyeDepth = LinearEyeDepth(depth);

				float3 viewPos = FetchViewPos(i.uv);

				float3 viewNormal = GetNormal(i.uv);
				viewNormal = ReconstructNormal(i.uv, viewPos, InvScreenParams);

				float3 rand = tex2D(_NoiseTex, screenPos.xy * _NoiseTex_TexelSize.xy);
				float rayAngleStepSize = 2.0 * UNITY_PI / _RayMarchingDirectionCount;
				float stepSize = min(_Radius / viewPos.z, _MaxPixelRadius) / (_RayMarchingStepCount + 1.0);

				float oc = 0.0;
				for (int j = 0; j < _RayMarchingDirectionCount; j++)
				{
					float angle = rayAngleStepSize * float(j);
					float2 rayMarchingDir = GetRayMarchingDir(angle, rand.xy);
					rayMarchingDir = RotateDirections(float2(cos(angle), sin(angle)), rand.xy);
					float2 direction = RotateDirections(float2(cos(angle), sin(angle)), rand.xy);

					float rayPixels = (rand.z * stepSize + 1.0);

					for (int k = 0; k < _RayMarchingStepCount; k++)
					{
						float2 snappedUV = round(rayPixels * direction) * InvScreenParams + i.uv.zw;
						float3 sviewPos = FetchViewPos(snappedUV);

						rayPixels += stepSize;

						float contrib = ComputeAO(viewPos, viewNormal, sviewPos);
						oc += contrib;
					}
				}

				oc *= (_AOmultiplier / (_RayMarchingDirectionCount * _RayMarchingStepCount)) * _Intensity;
				float fallOffStart = _MaxDistance - _DistanceFalloff;
				oc = lerp(saturate(1.0 - oc), 1.0, saturate((viewPos.z - fallOffStart) / (_MaxDistance - fallOffStart)));

				if (depth <= 0)
					oc = 1;

				return oc;
			}
			ENDCG
        }

		Pass
		{
			Name "Composite Pass"

			BLEND ZERO SRCALPHA

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _AOTex;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = float4(v.uv, v.uv);

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 ao = tex2D(_AOTex, i.uv);
				return ao;
			}
			ENDCG
		}

		Pass
		{
			Name "Blur_Down"
			Blend One Zero
			ZWrite Off
			ZTest Always
			ColorMask RGBA

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv: TEXCOORD1;
				float4 uv01: TEXCOORD2;
				float4 uv23: TEXCOORD3;
			};

			sampler2D _MainTex;
			half4 _MainTex_TexelSize;
			half _Offset;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv01.xy = v.uv - _MainTex_TexelSize * float2(1 + _Offset, 1 + _Offset);//top right
				o.uv01.zw = v.uv + _MainTex_TexelSize * float2(1 + _Offset, 1 + _Offset);//bottom left
				o.uv23.xy = v.uv - float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * float2(1 + _Offset, 1 + _Offset);//top left
				o.uv23.zw = v.uv + float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * float2(1 + _Offset, 1 + _Offset);//bottom right

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				half4 sum = tex2D(_MainTex, i.uv) * 4;
				sum += tex2D(_MainTex, i.uv01.xy);
				sum += tex2D(_MainTex, i.uv01.zw);
				sum += tex2D(_MainTex, i.uv23.xy);
				sum += tex2D(_MainTex, i.uv23.zw);
				return sum * 0.125;
			}
			ENDCG
		}

		Pass
		{
			Name "Blur_Up"
			Blend One Zero
			ZWrite Off
			ZTest Always
			ColorMask RGBA

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 uv01: TEXCOORD1;
				float4 uv23: TEXCOORD2;
				float4 uv45: TEXCOORD3;
				float4 uv67: TEXCOORD4;
			};

			sampler2D _MainTex;
			half4 _MainTex_TexelSize;
			half2 _Blur_Dir;
			half _Offset;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.uv = v.uv;

				_MainTex_TexelSize *= 0.5;
				_Offset = float2(1 + _Offset, 1 + _Offset);

				o.uv01.xy = v.uv + float2(-_MainTex_TexelSize.x * 2, 0) * _Offset;
				o.uv01.zw = v.uv + float2(-_MainTex_TexelSize.x, _MainTex_TexelSize.y) * _Offset;
				o.uv23.xy = v.uv + float2(0, _MainTex_TexelSize.y * 2) * _Offset;
				o.uv23.zw = v.uv + _MainTex_TexelSize * _Offset;
				o.uv45.xy = v.uv + float2(_MainTex_TexelSize.x * 2, 0) * _Offset;
				o.uv45.zw = v.uv + float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _Offset;
				o.uv67.xy = v.uv + float2(0, -_MainTex_TexelSize.y * 2) * _Offset;
				o.uv67.zw = v.uv - _MainTex_TexelSize * _Offset;

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				half4 sum = 0;
				sum += tex2D(_MainTex, i.uv01.xy);
				sum += tex2D(_MainTex, i.uv01.zw) * 2;
				sum += tex2D(_MainTex, i.uv23.xy);
				sum += tex2D(_MainTex, i.uv23.zw) * 2;
				sum += tex2D(_MainTex, i.uv45.xy);
				sum += tex2D(_MainTex, i.uv45.zw) * 2;
				sum += tex2D(_MainTex, i.uv67.xy);
				sum += tex2D(_MainTex, i.uv67.zw) * 2;

				return sum * 0.0833;
			}
			ENDCG
		}

		Pass
		{
			Name "Blur_X"
			Blend One Zero
			ZWrite Off
			ZTest Always
			ColorMask RGBA

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv: TEXCOORD1;
				float4 uv01: TEXCOORD2;
			};

			sampler2D _MainTex;
			half4 _MainTex_TexelSize;
			half2 _Offset_X;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv01.xy = v.uv - _MainTex_TexelSize * _Offset_X;
				o.uv01.zw = v.uv + _MainTex_TexelSize * _Offset_X;

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 sum = tex2D(_MainTex, i.uv) * 0.384397;
				sum += tex2D(_MainTex, i.uv01.xy) * 0.307801;
				sum += tex2D(_MainTex, i.uv01.zw) * 0.307801;

				return sum;
			}
			ENDCG
		}

		Pass
		{
			Name "Blur_Y"
			Blend One Zero
			ZWrite Off
			ZTest Always
			ColorMask RGBA

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv: TEXCOORD1;
				float4 uv01: TEXCOORD2;
			};

			sampler2D _MainTex;
			half4 _MainTex_TexelSize;
			half2 _Offset_Y;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv01.xy = v.uv - _MainTex_TexelSize * _Offset_Y;
				o.uv01.zw = v.uv + _MainTex_TexelSize * _Offset_Y;

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 sum = tex2D(_MainTex, i.uv) * 0.384397;
				sum += tex2D(_MainTex, i.uv01.xy) * 0.307801;
				sum += tex2D(_MainTex, i.uv01.zw) * 0.307801;

				return sum;
			}
			ENDCG
		}

	}
}
