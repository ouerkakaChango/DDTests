Shader "Shaders/Post/FPColorGradingBaker"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        CGINCLUDE

        #include "UnityCG.cginc"
        #include "ACES.cginc"

        struct a2v
        {
            float3 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        float4 _Lut2D_Params;
        float4 _UserLut2D_Params;

        float3 _ColorBalance;
        float3 _ColorFilter;
        float3 _HueSatCon;
        float _Brightness; // LDR only

        float3 _ChannelMixerRed;
        float3 _ChannelMixerGreen;
        float3 _ChannelMixerBlue;

        float3 _Lift;
        float3 _InvGamma;
        float3 _Gain;

        float4 _CustomToneCurve;
        float4 _ToeSegmentA;
        float4 _ToeSegmentB;
        float4 _MidSegmentA;
        float4 _MidSegmentB;
        float4 _ShoSegmentA;
        float4 _ShoSegmentB;

        sampler2D _Curves;

        v2f vert(a2v v)
        {
            v2f o = (v2f)0;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.texcoord.xy;
            return o;
        }

        float3 GetLutStripValue(float2 uv, float4 params)
        {
            uv -= params.yz;
            float3 color;
            color.r = frac(uv.x * params.x);
            color.b = uv.x - color.r / params.x;
            color.g = uv.y;
            return color * params.w;
        }

        float3 Contrast(float3 c, float midpoint, float contrast)
        {
            return (c - midpoint) * contrast + midpoint;
        }

        static const float3x3 LIN_2_LMS_MAT = {
            3.90405e-1, 5.49941e-1, 8.92632e-3,
            7.08416e-2, 9.63172e-1, 1.35775e-3,
            2.31082e-2, 1.28021e-1, 9.36245e-1
        };

        static const float3x3 LMS_2_LIN_MAT = {
            2.85847e+0, -1.62879e+0, -2.48910e-2,
            -2.10182e-1,  1.15820e+0,  3.24281e-4,
            -4.18120e-2, -1.18169e-1,  1.06867e+0
        };


        float3 WhiteBalance(float3 c, float3 balance)
        {
            float3 lms = mul(LIN_2_LMS_MAT, c);
            lms *= balance;
            return mul(LMS_2_LIN_MAT, lms);
        }

        float3 ChannelMixer(float3 c, float3 red, float3 green, float3 blue)
        {
            return float3(
                dot(c, red),
                dot(c, green),
                dot(c, blue)
                );
        }

        float3 LiftGammaGainHDR(float3 c, float3 lift, float3 invgamma, float3 gain)
        {
            c = c * gain + lift;

            // ACEScg will output negative values, as clamping to 0 will lose precious information we'll
            // mirror the gamma function instead
            return FastSign(c) * pow(abs(c), invgamma);
        }

        float3 RgbToHsv(float3 c)
        {
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
            float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
            float d = q.x - min(q.w, q.y);
            float e = EPSILON;
            return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
        }

        float3 HsvToRgb(float3 c)
        {
            float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
            return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
        }

        float3 Saturation(float3 c, float sat)
        {
            float luma = LinearRgbToLuminance(c);
            return luma.xxx + sat.xxx * (c - luma.xxx);
        }

        float RotateHue(float value, float low, float hi)
        {
            return (value < low)
                ? value + hi
                : (value > hi)
                ? value - hi
                : value;
        }

        float3 ApplyCommonGradingSteps(float3 colorLinear)
        {
            colorLinear = WhiteBalance(colorLinear, _ColorBalance);
            colorLinear *= _ColorFilter;
            colorLinear = ChannelMixer(colorLinear, _ChannelMixerRed, _ChannelMixerGreen, _ChannelMixerBlue);
            colorLinear = LiftGammaGainHDR(colorLinear, _Lift, _InvGamma, _Gain);

            // Do NOT feed negative values to RgbToHsv or they'll wrap around
            colorLinear = max((float3)0.0, colorLinear);

            float3 hsv = RgbToHsv(colorLinear);

            // Hue Vs Sat
            float satMult;
            satMult = saturate(tex2Dlod(_Curves, float4(hsv.x, 0.25, 0, 0)).y) * 2.0;

            // Sat Vs Sat
            satMult *= saturate(tex2Dlod(_Curves, float4(hsv.y, 0.25, 0, 0)).z) * 2.0;

            // Lum Vs Sat
            satMult *= saturate(tex2Dlod(_Curves, float4(LinearRgbToLuminance(colorLinear), 0.25, 0, 0)).w) * 2.0;

            // Hue Vs Hue
            float hue = hsv.x + _HueSatCon.x;
            float offset = saturate(tex2Dlod(_Curves, float4(hue, 0.25, 0, 0)).x) - 0.5;
            hue += offset;
            hsv.x = RotateHue(hue, 0.0, 1.0);

            colorLinear = HsvToRgb(hsv);
            colorLinear = Saturation(colorLinear, _HueSatCon.y * satMult);

            return colorLinear;
        }

        float3 YrgbCurve(float3 c, sampler2D curveTex)
        {
            const float kHalfPixel = (1.0 / 128.0) / 2.0;

            // Y (master)
            c += kHalfPixel.xxx;
            float mr = tex2Dlod(curveTex, float4(c.r, 0.75, 0, 0)).a;
            float mg = tex2Dlod(curveTex, float4(c.g, 0.75, 0, 0)).a;
            float mb = tex2Dlod(curveTex, float4(c.b, 0.75, 0, 0)).a;
            c = saturate(float3(mr, mg, mb));

            // RGB
            c += kHalfPixel.xxx;
            float r = tex2Dlod(curveTex, float4(c.r, 0.75, 0, 0)).r;
            float g = tex2Dlod(curveTex, float4(c.g, 0.75, 0, 0)).g;
            float b = tex2Dlod(curveTex, float4(c.b, 0.75, 0, 0)).b;
            return saturate(float3(r, g, b));
        }

        float3 ColorGradeLDR(float3 colorLinear)
        {
            // Brightness is a simple linear multiplier. Works better in LDR than using e.v.
            colorLinear *= _Brightness;

            // Contrast is done in linear, switching to log for that in LDR is pointless and doesn't
            // feel as good to tweak
            const float kMidGrey = pow(0.5, 2.2);
            colorLinear = Contrast(colorLinear, kMidGrey, _HueSatCon.z);

            colorLinear = ApplyCommonGradingSteps(colorLinear);

            // YRGB only works in LDR for now as we don't do any curve range remapping
            colorLinear = YrgbCurve(saturate(colorLinear), _Curves);

            return saturate(colorLinear);
        }

        #define LUT_SPACE_DECODE(x) LogCToLinear(x)


        struct ParamsLogC
        {
            float cut;
            float a, b, c, d, e, f;
        };

        static const ParamsLogC LogC =
        {
            0.011361, // cut
            5.555556, // a
            0.047996, // b
            0.244161, // c
            0.386036, // d
            5.301883, // e
            0.092819  // f
        };

        float3 LogCToLinear(float3 x)
        {
            return (pow(10.0, (x - LogC.d) / LogC.c) - LogC.b) / LogC.a;
        }

        float3 LinearToLogC(float3 x)
        {
            return LogC.c * log10(LogC.a * x + LogC.b) + LogC.d;
        }

        #define ACEScc_MIDGRAY  0.4135884
        float3 LogGradeHDR(float3 colorLog)
        {
            // HDR contrast feels a lot more natural when done in log rather than doing it in linear
            colorLog = Contrast(colorLog, ACEScc_MIDGRAY, _HueSatCon.z);
            return colorLog;
        }

        float3 LinearGradeHDR(float3 colorLinear)
        {
            colorLinear = ApplyCommonGradingSteps(colorLinear);
            return colorLinear;
        }

        float3 AcesTonemap(float3 aces)
        {
            // --- Glow module --- //
            float saturation = rgb_2_saturation(aces);
            float ycIn = rgb_2_yc(aces);
            float s = sigmoid_shaper((saturation - 0.4) / 0.2);
            float addedGlow = 1.0 + glow_fwd(ycIn, RRT_GLOW_GAIN * s, RRT_GLOW_MID);
            aces *= addedGlow;

            // --- Red modifier --- //
            float hue = rgb_2_hue(aces);
            float centeredHue = center_hue(hue, RRT_RED_HUE);
            float hueWeight;
            {
                //hueWeight = cubic_basis_shaper(centeredHue, RRT_RED_WIDTH);
                hueWeight = smoothstep(0.0, 1.0, 1.0 - abs(2.0 * centeredHue / RRT_RED_WIDTH));
                hueWeight *= hueWeight;
            }

            aces.r += hueWeight * saturation * (RRT_RED_PIVOT - aces.r) * (1.0 - RRT_RED_SCALE);

            // --- ACES to RGB rendering space --- //
            float3 acescg = max(0.0, ACES_to_ACEScg(aces));

            // --- Global desaturation --- //
            //acescg = mul(RRT_SAT_MAT, acescg);
            acescg = lerp(dot(acescg, AP1_RGB2Y).xxx, acescg, RRT_SAT_FACTOR.xxx);

            // Luminance fitting of *RRT.a1.0.3 + ODT.Academy.RGBmonitor_100nits_dim.a1.0.3*.
            // https://github.com/colour-science/colour-unity/blob/master/Assets/Colour/Notebooks/CIECAM02_Unity.ipynb
            // RMSE: 0.0012846272106
            const float a = 278.5085;
            const float b = 10.7772;
            const float c = 293.6045;
            const float d = 88.7122;
            const float e = 80.6889;
            float3 x = acescg;
            float3 rgbPost = (x * (a * x + b)) / (x * (c * x + d) + e);

            // Scale luminance to linear code value
            // float3 linearCV = Y_2_linCV(rgbPost, CINEMA_WHITE, CINEMA_BLACK);

            // Apply gamma adjustment to compensate for dim surround
            float3 linearCV = darkSurround_to_dimSurround(rgbPost);

            // Apply desaturation to compensate for luminance difference
            //linearCV = mul(ODT_SAT_MAT, color);
            linearCV = lerp(dot(linearCV, AP1_RGB2Y).xxx, linearCV, ODT_SAT_FACTOR.xxx);

            // Convert to display primary encoding
            // Rendering space RGB to XYZ
            float3 XYZ = mul(AP1_2_XYZ_MAT, linearCV);

            // Apply CAT from ACES white point to assumed observer adapted white point
            XYZ = mul(D60_2_D65_CAT, XYZ);

            // CIE XYZ to display primaries
            linearCV = mul(XYZ_2_REC709_MAT, XYZ);

            return linearCV;
        }

        float3 NeutralCurve(float3 x, float a, float b, float c, float d, float e, float f)
        {
            return ((x * (a * x + c * b) + d * e) / (x * (a * x + b) + d * f)) - e / f;
        }

        float3 NeutralTonemap(float3 x)
        {
            // Tonemap
            float a = 0.2;
            float b = 0.29;
            float c = 0.24;
            float d = 0.272;
            float e = 0.02;
            float f = 0.3;
            float whiteLevel = 5.3;
            float whiteClip = 1.0;

            float3 whiteScale = (1.0).xxx / NeutralCurve(whiteLevel, a, b, c, d, e, f);
            x = NeutralCurve(x * whiteScale, a, b, c, d, e, f);
            x *= whiteScale;

            // Post-curve white point adjustment
            x /= whiteClip.xxx;

            return x;
        }

        float EvalCustomSegment(float x, float4 segmentA, float2 segmentB)
        {
            const float kOffsetX = segmentA.x;
            const float kOffsetY = segmentA.y;
            const float kScaleX = segmentA.z;
            const float kScaleY = segmentA.w;
            const float kLnA = segmentB.x;
            const float kB = segmentB.y;

            float x0 = (x - kOffsetX) * kScaleX;
            float y0 = (x0 > 0.0) ? exp(kLnA + kB * log(x0)) : 0.0;
            return y0 * kScaleY + kOffsetY;
        }

        float EvalCustomCurve(float x, float3 curve, float4 toeSegmentA, float2 toeSegmentB, float4 midSegmentA, float2 midSegmentB, float4 shoSegmentA, float2 shoSegmentB)
        {
            float4 segmentA;
            float2 segmentB;

            if (x < curve.y)
            {
                segmentA = toeSegmentA;
                segmentB = toeSegmentB;
            }
            else if (x < curve.z)
            {
                segmentA = midSegmentA;
                segmentB = midSegmentB;
            }
            else
            {
                segmentA = shoSegmentA;
                segmentB = shoSegmentB;
            }

            return EvalCustomSegment(x, segmentA, segmentB);
        }

        // curve: x: inverseWhitePoint, y: x0, z: x1
        float3 CustomTonemap(float3 x, float3 curve, float4 toeSegmentA, float2 toeSegmentB, float4 midSegmentA, float2 midSegmentB, float4 shoSegmentA, float2 shoSegmentB)
        {
            float3 normX = x * curve.x;
            float3 ret;
            ret.x = EvalCustomCurve(normX.x, curve, toeSegmentA, toeSegmentB, midSegmentA, midSegmentB, shoSegmentA, shoSegmentB);
            ret.y = EvalCustomCurve(normX.y, curve, toeSegmentA, toeSegmentB, midSegmentA, midSegmentB, shoSegmentA, shoSegmentB);
            ret.z = EvalCustomCurve(normX.z, curve, toeSegmentA, toeSegmentB, midSegmentA, midSegmentB, shoSegmentA, shoSegmentB);
            return ret;
        }

        float3 ColorGradeHDR(float3 colorLutSpace)
        {
            #if TONEMAPPING_ACES
            {
                float3 colorLinear = LUT_SPACE_DECODE(colorLutSpace);
                float3 aces = unity_to_ACES(colorLinear);

                // ACEScc (log) space
                float3 acescc = ACES_to_ACEScc(aces);
                acescc = LogGradeHDR(acescc);
                aces = ACEScc_to_ACES(acescc);

                // ACEScg (linear) space
                float3 acescg = ACES_to_ACEScg(aces);
                acescg = LinearGradeHDR(acescg);

                // Tonemap ODT(RRT(aces))
                aces = ACEScg_to_ACES(acescg);
                colorLinear = AcesTonemap(aces);

                return colorLinear;
            }
            #else
            {
                // colorLutSpace is already in log space
                colorLutSpace = LogGradeHDR(colorLutSpace);

                // Switch back to linear
                float3 colorLinear = LUT_SPACE_DECODE(colorLutSpace);
                colorLinear = LinearGradeHDR(colorLinear);
                colorLinear = max(0.0, colorLinear);

                // Tonemap
                #if TONEMAPPING_NEUTRAL
                {
                    colorLinear = NeutralTonemap(colorLinear);
                }
                #elif TONEMAPPING_CUSTOM
                {
                    colorLinear = CustomTonemap(
                        colorLinear, _CustomToneCurve.xyz,
                        _ToeSegmentA, _ToeSegmentB.xy,
                        _MidSegmentA, _MidSegmentB.xy,
                        _ShoSegmentA, _ShoSegmentB.xy
                    );
                }
                #endif

                return colorLinear;
            }
            #endif
        }

        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag(v2f i) : SV_Target
            {
                float3 colorLinear = GetLutStripValue(i.uv, _Lut2D_Params);
                float3 graded = ColorGradeLDR(colorLinear);
                return float4(graded, 1.0);
            }

            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float3 ApplyLut2D(sampler2D tex, float3 uvw, float3 scaleOffset)
            {
                // Strip format where `height = sqrt(width)`
                uvw.z *= scaleOffset.z;
                float shift = floor(uvw.z);
                uvw.xy = uvw.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
                uvw.x += shift * scaleOffset.y;
                uvw.xyz = lerp(
                    tex2Dlod(tex, float4(uvw.xy, 0, 0)).rgb,
                    tex2Dlod(tex, float4(uvw.xy + float2(scaleOffset.y, 0.0), 0, 0)).rgb,
                    uvw.z - shift
                );
                return uvw;
            }

            sampler2D _MainTex;

            float4 frag(v2f i) : SV_Target
            {
                float3 neutralColorLinear = GetLutStripValue(i.uv, _Lut2D_Params);
                float3 lookup = ApplyLut2D(_MainTex, neutralColorLinear, _UserLut2D_Params.xyz);
                float3 colorLinear = lerp(neutralColorLinear, lookup, _UserLut2D_Params.w);
                float3 graded = ColorGradeLDR(colorLinear);
                return float4(graded, 1.0);
            }

            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ TONEMAPPING_ACES TONEMAPPING_NEUTRAL TONEMAPPING_CUSTOM

            float4 frag(v2f i) : SV_Target
            {
                float3 colorLinear = GetLutStripValue(i.uv, _Lut2D_Params);
                float3 graded = ColorGradeHDR(colorLinear);
                return float4(max(graded, 0.0), 1.0);
            }

            ENDCG
        }
    }
}
