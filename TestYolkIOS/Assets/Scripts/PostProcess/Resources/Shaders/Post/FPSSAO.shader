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
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragSSAO
			#pragma enable_d3d11_debug_symbols

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
				float4 frustumDir : TEXCOORD1;
				float3 viewRay : TEXCOORD2;
				float3 worldRay : TEXCOORD3;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D _DepthTexDOF;
			float4x4 _FrustumDir;
			float4 _Params;
			float _AORadius;
			float4 _SampleKernelArray[32];
			int _SampleCount;
			float4x4 _InverseProjectionMatrix;
			float4x4 _InverseViewMatrix;
			float _DepthBiasValue;
			float4 _NoiseArray[16];

			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			sampler2D _CameraDepthNormalsTexture;

			static const float3 uvOffsets[8] =
			{
				float3(0, 1, 0.125f),
				float3(1, 1, 0.125f),
				float3(1, 0, 0.125f),
				float3(1, -1, 0.125f),

				float3(0, -1, 0.125f),
				float3(-1, -1, 0.125f),
				float3(-1, 0, 0.125f),
				float3(-1, 1, 0.125f)
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = float4(v.uv, v.uv);
				#if UNITY_UV_STARTS_AT_TOP
					if (_MainTex_TexelSize.y < 0)
						o.uv.w = 1 - o.uv.w;
				#endif

				int ix = (int)o.uv.z;
				int iy = (int)o.uv.w;
				o.frustumDir = _FrustumDir[ix + 2 * iy];

				float4 clipPos = float4(v.uv * 2 - 1.0, 1.0, 1.0);
				float4 viewRay = mul(unity_CameraInvProjection, clipPos);
				o.viewRay = viewRay.xyz / viewRay.w;

				float4 worldRay = mul(_InverseViewMatrix, viewRay);
				//o.worldRay = worldRay;

				return o;
			}

			float3 getUvOffset(int index)
			{
				if (index < 0)
				{
					index += 8;
				}
				if (index > 7)
				{
					index -= 8;
				}
				return uvOffsets[index];
			}

			float detMatrix(float3 v1, float3 v2, float3 v3, float3 v4)
			{
				float4x4 tmpMatrix = float4x4(
					1, 1, 1, 1,
					v1.x, v2.x, v3.x, v4.x, 
					v1.y, v2.y, v3.y, v4.y,
					v1.z, v2.z, v3.z, v4.z
					);
				float result = determinant(tmpMatrix);
				return result > _Params.x ? 1 : 0;
			}

			float CheckPerspective(float x)
			{
				return lerp(x, 1.0, unity_OrthoParams.w);
			}

			// Reconstruct view-space position from UV and depth.
			// p11_22 = (unity_CameraProjection._11, unity_CameraProjection._22)
			// p13_31 = (unity_CameraProjection._13, unity_CameraProjection._23)
			float3 ReconstructViewPos(float2 uv, float depth, float2 p11_22, float2 p13_31)
			{
				return float3((uv * 2.0 - 1.0 - p13_31) / p11_22 * CheckPerspective(depth), depth);
			}

			float GradientNoise(float2 uv)
			{
				uv = floor(uv * _ScreenParams.xy);
				float f = dot(float2(0.06711056, 0.00583715), uv);
				return frac(52.9829189 * frac(f));
			}

			float UVRandom(float u, float v)
			{
				float f = dot(float2(12.9898, 78.233), float2(u, v));
				return frac(43758.5453 * sin(f));
			}

			float2 CosSin(float theta)
			{
				float sn, cs;
				sincos(theta, sn, cs);
				return float2(cs, sn);
			}

#define FIX_SAMPLING_PATTERN
#define TWO_PI          6.28318530718

			float3 PickSamplePoint(float2 uv, float index)
			{
				// Uniformaly distributed points on a unit sphere
				// http://mathworld.wolfram.com/SpherePointPicking.html
#if defined(FIX_SAMPLING_PATTERN)
				float gn = GradientNoise(uv * 1);
				// FIXEME: This was added to avoid a NVIDIA driver issue.
				//                                   vvvvvvvvvvvv
				float u = frac(UVRandom(0.0, index + uv.x * 1e-10) + gn) * 2.0 - 1.0;
				float theta = (UVRandom(1.0, index + uv.x * 1e-10) + gn) * TWO_PI;
#else
				float u = UVRandom(uv.x + _Time.x, uv.y + index) * 2.0 - 1.0;
				float theta = UVRandom(-uv.x - _Time.x, uv.y + index) * TWO_PI;
#endif
				float3 v = float3(CosSin(theta) * sqrt(1.0 - u * u), u);
				// Make them distributed between [0, _Radius]
				float l = sqrt((index + 1.0) / _SampleCount) * _AORadius;
				return v * l;
			}

			float4 fragSSAO(v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);

				float3x3 proj = (float3x3)unity_CameraProjection;
				float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
				float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);

				float linear01Depth;
				float3 viewNormal;
				float4 cdn = tex2D(_CameraDepthNormalsTexture, i.uv);
				DecodeDepthNormal(cdn, linear01Depth, viewNormal);
				viewNormal = normalize(viewNormal);

				float depth = UNITY_SAMPLE_DEPTH(tex2D(_DepthTexDOF, i.uv));
				float depthEye = LinearEyeDepth(depth);

				float3 viewPos = ReconstructViewPos(i.uv, depthEye, p11_22, p13_31);
				viewPos.z = -viewPos.z;

				int sampleCount = _SampleCount;

				float2 uv = i.uv.xy;
				float oc = 0.0;
				for (int s = 0; s < sampleCount; s++)
				{
				#if defined(SHADER_API_D3D11)
					// This 'floor(1.0001 * s)' operation is needed to avoid a NVidia shader issue. This issue
					// is only observed on DX11.
					float3 randomVec = PickSamplePoint(uv, floor(1.0001 * s));
				#else
					float3 randomVec = PickSamplePoint(uv, s);
				#endif

					//如果随机点的位置与法线反向，那么将随机方向取反，使之保证在法线半球

					randomVec = faceforward(randomVec, -viewNormal, randomVec);

					float3 randomPos = viewPos + randomVec;
					float viewDepthI = randomPos.z;

					float4 rclipPos = mul(unity_CameraProjection, float4(randomPos, 1));
					float4 rscreenPos = ComputeScreenPos(rclipPos);

					float2 ruv = rscreenPos.xy / rscreenPos.w;
					ruv.y = 1 - ruv.y;

					float depthI = UNITY_SAMPLE_DEPTH(tex2D(_DepthTexDOF, ruv));
					float depthIEye = LinearEyeDepth(depthI);

					float range = _AORadius > (abs(depthEye - depthIEye));
					float ao = -depthIEye + _DepthBiasValue > viewDepthI;
					if (depthIEye >= _ProjectionParams.z)
						ao = 0;
					oc += ao * range;
				}

				oc /= sampleCount;
				oc = max(0.0, 1 - oc * _Params.y);

				if (depth <= 0)
					oc = 1;

				return col * oc;
			}

			ENDCG
        }
    }
}
