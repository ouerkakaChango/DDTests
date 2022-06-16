using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CenturyGame.PostProcess
{
    public partial class PostProcessHandle
    {
        FPHBAO m_hbao;
        FPDepthOfField m_dof;
        FPBloom m_bloom;
        FPColorGrading m_colorGrading;
        FPGaussianBlur m_gaussianBlur;
        FPRadialBlur m_radialBlur;
        FPSSSS m_ssss;
        FPVignette m_vignette;

        Available m_hbaoAvailable;
        Available m_dofAvailable;
        Available m_bloomAvailable;
        Available m_colorGradingAvailable;
        Available m_gaussianBlurAvailable;
        Available m_radialBlurAvailable;
        Available m_ssssAvailable;
        Available m_vignetteAvailable;

        const int maxBloomPyramidSize = 16;
        int[] bloomUp = new int[maxBloomPyramidSize];
        int[] bloomDown = new int[maxBloomPyramidSize];

        void Bloom(CommandBuffer cmd, RenderTexture source, RenderTexture destination)
        {
            var mat = m_materials.bloomMat;

            float linearThreshold = m_bloom.threshold;
            float knee = linearThreshold * m_bloom.softKnee + 1e-5f;
            Vector4 curve = new Vector4(linearThreshold - knee, knee * 2f, 0.25f / knee, linearThreshold);
            mat.SetVector("_Curve", curve);

            float tempIntensity = Mathf.Exp(m_bloom.intensity * 0.69314718055994530941723212145818f) - 1f;
            mat.SetFloat("_Intensity", tempIntensity);

            int width = source.width / 2;
            int height = source.height / 2;
            int s = Mathf.Max(width, height);
            float logh = Mathf.Log(s, 2) + Mathf.Min(m_bloom.radius, 10f) - 10f;
            int nLogh = Mathf.FloorToInt(logh);
            int iterations = Mathf.Clamp(nLogh, 1, maxBloomPyramidSize);
            mat.SetFloat("_SampleScale", 0.5f + logh - nLogh);

            mat.SetColor("_BloomColor", m_bloom.bloomColor);

            for (int i = 0; i < iterations; ++i)
            {
                cmd.GetTemporaryRT(bloomDown[i], width, height, 0, FilterMode.Bilinear, HDRFormat);
                cmd.GetTemporaryRT(bloomUp[i], width, height, 0, FilterMode.Bilinear, HDRFormat);
                width = Mathf.Max(width / 2, 1);
                height = Mathf.Max(height / 2, 1);
            }

            RenderTargetIdentifier _last = source;
            for (int i = 0; i < iterations; ++i)
            {
                var down = bloomDown[i];
                int pass = i == 0 ? 0 : 1;
                cmd.Blit(_last, down, mat, pass);
                _last = down;
            }

            _last = bloomDown[iterations - 1];
            for (int i = iterations - 2; i >= 0; --i)
            {
                var down = bloomDown[i];
                var up = bloomUp[i];
                cmd.SetGlobalTexture("_BaseTex", down);
                cmd.Blit(_last, up, mat, 2);
                _last = up;
            }

            cmd.SetGlobalTexture("_BaseTex", source);
            cmd.Blit(_last, destination, mat, 3);

            for (int i = 0; i < iterations; ++i)
            {
                cmd.ReleaseTemporaryRT(bloomDown[i]);
                cmd.ReleaseTemporaryRT(bloomUp[i]);
            }
        }

        const int k_Lut2DSize = 32;

        private RenderTexture m_internalLDRLut = null;
        readonly HableCurve m_HableCurve = new HableCurve();

        void CreateLUT()
        {
            var format = GetLutFormat();
            m_internalLDRLut = new RenderTexture(k_Lut2DSize * k_Lut2DSize, k_Lut2DSize, 0, format, RenderTextureReadWrite.Linear)
            {
                name = "FP Color Grading Lut",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
                autoGenerateMips = false,
                useMipMap = false,
            };
            m_internalLDRLut.Create();
        }

        static RenderTextureFormat GetLutFormat()
        {
            // Use ARGBHalf if possible, fallback on ARGB2101010 and ARGB32 otherwise
            var format = RenderTextureFormat.ARGBHalf;

            if (!SystemInfo.SupportsRenderTextureFormat(format))
            {
                format = RenderTextureFormat.ARGB2101010;

                // Note that using a log lut in ARGB32 is a *very* bad idea but we need it for
                // compatibility reasons (else if a platform doesn't support one of the previous
                // format it'll output a black screen, or worse will segfault on the user).
                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGB32;
            }

            return format;
        }

        void BakeColorGradingLDRLUT(CommandBuffer cmd)
        {
            var mat = m_materials.colorGradingBakerMat;

            var lut2d_params = new Vector4(k_Lut2DSize, 0.5f / (k_Lut2DSize * k_Lut2DSize), 0.5f / k_Lut2DSize, k_Lut2DSize / (k_Lut2DSize - 1f));
            var colorBalance = ColorGradingUtility.ComputeColorBalance(m_colorGrading.temprature, m_colorGrading.tint);
            var hsc = new Vector4(m_colorGrading.hue / 360, m_colorGrading.saturation + 1, m_colorGrading.contrast + 1, 0);
            var channelMixerR = new Vector4(m_colorGrading.mixerRedOutRedIn, m_colorGrading.mixerRedOutGreenIn, m_colorGrading.mixerRedOutBlueIn, 0) / 100;
            var channelMixerG = new Vector4(m_colorGrading.mixerGreenOutRedIn, m_colorGrading.mixerGreenOutGreenIn, m_colorGrading.mixerGreenOutBlueIn, 0) / 100;
            var channelMixerB = new Vector4(m_colorGrading.mixerBlueOutRedIn, m_colorGrading.mixerBlueOutGreenIn, m_colorGrading.mixerBlueOutBlueIn, 0) / 100;
            var _lift = ColorGradingUtility.ColorToLift(m_colorGrading.lift);
            var _gain = ColorGradingUtility.ColorToGain(m_colorGrading.gain);
            var invgamma = ColorGradingUtility.ColorToInverseGamma(m_colorGrading.gamma);
            var _brightness = m_colorGrading.brightness + 1;
            var curveTexture = GetCurveTexture(false);

            mat.SetVector(ShaderIDs.Lut2D_Params, lut2d_params);
            mat.SetVector(ShaderIDs.ColorBalance, colorBalance);
            mat.SetColor(ShaderIDs.ColorFilter, m_colorGrading.colorFilter);
            mat.SetVector(ShaderIDs.HueSatCon, hsc);
            mat.SetFloat(ShaderIDs.Brightness, _brightness);
            mat.SetVector(ShaderIDs.ChannelMixerRed, channelMixerR);
            mat.SetVector(ShaderIDs.ChannelMixerGreen, channelMixerG);
            mat.SetVector(ShaderIDs.ChannelMixerBlue, channelMixerB);
            mat.SetVector(ShaderIDs.Lift, _lift);
            mat.SetVector(ShaderIDs.Gain, _gain);
            mat.SetVector(ShaderIDs.InvGamma, invgamma);
            mat.SetTexture(ShaderIDs.Curves, curveTexture);

            if (m_colorGrading.ldrLut == null)
                cmd.Blit(null, m_internalLDRLut, mat, 0);
            else
            {
                var userLut_params = new Vector4(1f / m_colorGrading.ldrLut.width, 1f / m_colorGrading.ldrLut.height, m_colorGrading.ldrLut.height - 1f, m_colorGrading.ldrLutContribution);
                mat.SetVector(ShaderIDs.UserLut2D_Params, userLut_params);
                cmd.Blit(m_colorGrading.ldrLut, m_internalLDRLut, mat, 1);
            }
        }

        void BakeColorGradingHDRLUT(CommandBuffer cmd)
        {
            var mat = m_materials.colorGradingBakerMat;

            var lut2d_params = new Vector4(k_Lut2DSize, 0.5f / (k_Lut2DSize * k_Lut2DSize), 0.5f / k_Lut2DSize, k_Lut2DSize / (k_Lut2DSize - 1f));
            var colorBalance = ColorGradingUtility.ComputeColorBalance(m_colorGrading.temprature, m_colorGrading.tint);
            var hsc = new Vector4(m_colorGrading.hue / 360, m_colorGrading.saturation + 1, m_colorGrading.contrast + 1, 0);
            var channelMixerR = new Vector4(m_colorGrading.mixerRedOutRedIn, m_colorGrading.mixerRedOutGreenIn, m_colorGrading.mixerRedOutBlueIn, 0) / 100;
            var channelMixerG = new Vector4(m_colorGrading.mixerGreenOutRedIn, m_colorGrading.mixerGreenOutGreenIn, m_colorGrading.mixerGreenOutBlueIn, 0) / 100;
            var channelMixerB = new Vector4(m_colorGrading.mixerBlueOutRedIn, m_colorGrading.mixerBlueOutGreenIn, m_colorGrading.mixerBlueOutBlueIn, 0) / 100;
            var _lift = ColorGradingUtility.ColorToLift(m_colorGrading.lift * 0.2f);
            var _gain = ColorGradingUtility.ColorToGain(m_colorGrading.gain * 0.8f);
            var invgamma = ColorGradingUtility.ColorToInverseGamma(m_colorGrading.gamma * 0.8f);
            var curveTexture = GetCurveTexture(true);
            if (m_colorGrading.tonemapper == FPColorGrading.Tonemapper.Custom)
            {
                m_HableCurve.Init(
                    m_colorGrading.toneCurveToeStrength,
                    m_colorGrading.toneCurveToeLength,
                    m_colorGrading.toneCurveShoulderStrength,
                    m_colorGrading.toneCurveShoulderLength,
                    m_colorGrading.toneCurveShoulderAngle,
                    m_colorGrading.toneCurveGamma
                );

                mat.SetVector(ShaderIDs.CustomToneCurve, m_HableCurve.uniforms.curve);
                mat.SetVector(ShaderIDs.ToeSegmentA, m_HableCurve.uniforms.toeSegmentA);
                mat.SetVector(ShaderIDs.ToeSegmentB, m_HableCurve.uniforms.toeSegmentB);
                mat.SetVector(ShaderIDs.MidSegmentA, m_HableCurve.uniforms.midSegmentA);
                mat.SetVector(ShaderIDs.MidSegmentB, m_HableCurve.uniforms.midSegmentB);
                mat.SetVector(ShaderIDs.ShoSegmentA, m_HableCurve.uniforms.shoSegmentA);
                mat.SetVector(ShaderIDs.ShoSegmentB, m_HableCurve.uniforms.shoSegmentB);
            }

            mat.SetVector(ShaderIDs.Lut2D_Params, lut2d_params);
            mat.SetVector(ShaderIDs.ColorBalance, colorBalance);
            mat.SetColor(ShaderIDs.ColorFilter, m_colorGrading.colorFilter);
            mat.SetVector(ShaderIDs.HueSatCon, hsc);
            mat.SetVector(ShaderIDs.ChannelMixerRed, channelMixerR);
            mat.SetVector(ShaderIDs.ChannelMixerGreen, channelMixerG);
            mat.SetVector(ShaderIDs.ChannelMixerBlue, channelMixerB);
            mat.SetVector(ShaderIDs.Lift, _lift);
            mat.SetVector(ShaderIDs.Gain, _gain);
            mat.SetVector(ShaderIDs.InvGamma, invgamma);
            mat.SetTexture(ShaderIDs.Curves, curveTexture);

            if (m_colorGrading.tonemapper == FPColorGrading.Tonemapper.ACES)
            {
                mat.EnableKeyword("TONEMAPPING_ACES");
                mat.DisableKeyword("TONEMAPPING_NEUTRAL");
                mat.DisableKeyword("TONEMAPPING_CUSTOM");
            }
            else if (m_colorGrading.tonemapper == FPColorGrading.Tonemapper.Neutral)
            {
                mat.DisableKeyword("TONEMAPPING_ACES");
                mat.EnableKeyword("TONEMAPPING_NEUTRAL");
                mat.DisableKeyword("TONEMAPPING_CUSTOM");
            }
            else if (m_colorGrading.tonemapper == FPColorGrading.Tonemapper.Custom)
            {
                mat.DisableKeyword("TONEMAPPING_ACES");
                mat.DisableKeyword("TONEMAPPING_NEUTRAL");
                mat.EnableKeyword("TONEMAPPING_CUSTOM");
            }
            else
            {
                mat.DisableKeyword("TONEMAPPING_ACES");
                mat.DisableKeyword("TONEMAPPING_NEUTRAL");
                mat.DisableKeyword("TONEMAPPING_CUSTOM");
            }

            cmd.Blit(null, m_internalLDRLut, mat, 2);
        }

        static class ShaderIDs
        {
            internal static readonly int MainTex = Shader.PropertyToID("_MainTex");
            internal static readonly int Lut2D = Shader.PropertyToID("_Lut2D");
            internal static readonly int Lut2D_Params = Shader.PropertyToID("_Lut2D_Params");
            internal static readonly int UserLut2D_Params = Shader.PropertyToID("_UserLut2D_Params");
            internal static readonly int PostExposure = Shader.PropertyToID("_PostExposure");
            internal static readonly int ColorBalance = Shader.PropertyToID("_ColorBalance");
            internal static readonly int ColorFilter = Shader.PropertyToID("_ColorFilter");
            internal static readonly int HueSatCon = Shader.PropertyToID("_HueSatCon");
            internal static readonly int Brightness = Shader.PropertyToID("_Brightness");
            internal static readonly int ChannelMixerRed = Shader.PropertyToID("_ChannelMixerRed");
            internal static readonly int ChannelMixerGreen = Shader.PropertyToID("_ChannelMixerGreen");
            internal static readonly int ChannelMixerBlue = Shader.PropertyToID("_ChannelMixerBlue");
            internal static readonly int Lift = Shader.PropertyToID("_Lift");
            internal static readonly int InvGamma = Shader.PropertyToID("_InvGamma");
            internal static readonly int Gain = Shader.PropertyToID("_Gain");
            internal static readonly int Curves = Shader.PropertyToID("_Curves");
            internal static readonly int CustomToneCurve = Shader.PropertyToID("_CustomToneCurve");
            internal static readonly int ToeSegmentA = Shader.PropertyToID("_ToeSegmentA");
            internal static readonly int ToeSegmentB = Shader.PropertyToID("_ToeSegmentB");
            internal static readonly int MidSegmentA = Shader.PropertyToID("_MidSegmentA");
            internal static readonly int MidSegmentB = Shader.PropertyToID("_MidSegmentB");
            internal static readonly int ShoSegmentA = Shader.PropertyToID("_ShoSegmentA");
            internal static readonly int ShoSegmentB = Shader.PropertyToID("_ShoSegmentB");
        }

        Spline masterSpline = new Spline(0f, false, new Vector2(0f, 1f));
        Spline redSpline = new Spline(0f, false, new Vector2(0f, 1f));
        Spline greenSpline = new Spline(0f, false, new Vector2(0f, 1f));
        Spline blueSpline = new Spline(0f, false, new Vector2(0f, 1f));

        Spline hueVsHueSpline = new Spline(0.5f, true, new Vector2(0f, 1f));
        Spline hueVsSatSpline = new Spline(0.5f, true, new Vector2(0f, 1f));
        Spline satVsSatSpline = new Spline(0.5f, false, new Vector2(0f, 1f));
        Spline lumVsSatSpline = new Spline(0.5f, false, new Vector2(0f, 1f));

        Texture2D m_gradingCurve;
        readonly Color[] m_Pixels = new Color[Spline.k_Precision * 2];

        Texture2D GetCurveTexture(bool hdr)
        {
            if (m_gradingCurve == null)
            {
                var format = GetCurveFormat();
                m_gradingCurve = new Texture2D(Spline.k_Precision, 2, format, false, true)
                {
                    name = "Internal Curves Texture",
                    hideFlags = HideFlags.DontSave,
                    anisoLevel = 0,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
            }

            var pixels = m_Pixels;

            masterSpline.SetCurve(m_colorGrading.masterCurve);
            redSpline.SetCurve(m_colorGrading.redCurve);
            greenSpline.SetCurve(m_colorGrading.greenCurve);
            blueSpline.SetCurve(m_colorGrading.blueCurve);

            hueVsHueSpline.SetCurve(m_colorGrading.hueVsHueCurve);
            hueVsSatSpline.SetCurve(m_colorGrading.hueVsSatCurve);
            satVsSatSpline.SetCurve(m_colorGrading.satVsSatCurve);
            lumVsSatSpline.SetCurve(m_colorGrading.lumVsSatCurve);

            for (int i = 0; i < Spline.k_Precision; ++i)
            {
                float x = hueVsHueSpline.cachedData[i];
                float y = hueVsSatSpline.cachedData[i];
                float z = satVsSatSpline.cachedData[i];
                float w = lumVsSatSpline.cachedData[i];
                pixels[i] = new Color(x, y, z, w);

                if (!hdr)
                {
                    float m = masterSpline.cachedData[i];
                    float r = redSpline.cachedData[i];
                    float g = greenSpline.cachedData[i];
                    float b = blueSpline.cachedData[i];
                    pixels[i + Spline.k_Precision] = new Color(r, g, b, m);
                }
            }

            m_gradingCurve.SetPixels(pixels);
            m_gradingCurve.Apply(false, false);

            return m_gradingCurve;
        }

        static TextureFormat GetCurveFormat()
        {
            // Use RGBAHalf if possible, fallback on ARGB32 otherwise
            var format = TextureFormat.RGBAHalf;

            if (!SystemInfo.SupportsTextureFormat(format))
                format = TextureFormat.ARGB32;

            return format;
        }

        void ColorGrading(CommandBuffer cmd, RenderTexture source, RenderTexture destination)
        {
            var mat = m_materials.colorGradingMat;

            var lut2d_params = new Vector4(1f / m_internalLDRLut.width, 1f / m_internalLDRLut.height, m_internalLDRLut.height - 1f);
            mat.SetVector(ShaderIDs.Lut2D_Params, lut2d_params);
            mat.SetTexture(ShaderIDs.Lut2D, m_internalLDRLut);
            int pass = m_colorGrading.gradingMode == FPColorGrading.GradingMode.HDR
                ? 2
                : QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? 1
                    : 0;

            cmd.Blit(source, destination, mat, pass);
        }

        int[] m_blurBuffers = new int[FPGaussianBlur.MaxIterationCount];

        void GaussianBlur(CommandBuffer cmd, RenderTexture source, RenderTexture destination)
        {
            var mat = m_materials.gaussianBlurMat;

            mat.SetFloat("_SampleScale", Mathf.Max(m_gaussianBlur.sampleScale, 0));

            int iteration = Mathf.Clamp(m_gaussianBlur.iterationCount, 1, FPGaussianBlur.MaxIterationCount);
            int width = source.width / 2;
            int height = source.height / 2;

            for (int i = 0; i < iteration; ++i)
            {
                cmd.GetTemporaryRT(m_blurBuffers[i], width, height, 0, FilterMode.Bilinear, HDRFormat);
                width /= 2;
                height /= 2;
            }

            RenderTargetIdentifier _last = source;
            for (int i = 0; i < iteration; ++i)
            {
                cmd.Blit(_last, m_blurBuffers[i], mat, 0);
                _last = m_blurBuffers[i];
            }

            for (int i = iteration - 2; i >= 0; --i)
            {
                cmd.Blit(m_blurBuffers[i + 1], m_blurBuffers[i], mat, 1);
            }

            cmd.Blit(m_blurBuffers[0], destination, mat, 1);

            for (int i = 0; i < iteration; ++i)
            {
                cmd.ReleaseTemporaryRT(m_blurBuffers[i]);
            }
        }

        void RadialBlur(CommandBuffer cmd, RenderTexture source, RenderTexture destination)
        {
            var mat = m_materials.radialBlurMat;

            mat.SetFloat("_BlurStrength", m_radialBlur.blurStrength);
            mat.SetFloat("_SampleStrength", m_radialBlur.sampleStrength);

            cmd.Blit(source, destination, mat);
        }

        int lastHBAONoiseTexParam = -1;
        Texture2D hbaoNoiseTex;

        void CreateRandomTexture(int RayMarchingDirectionCount)
        {
            int size = 4;
            if (hbaoNoiseTex == null)
            {
                hbaoNoiseTex = new Texture2D(size, size, TextureFormat.RGB24, false, true);
                hbaoNoiseTex.filterMode = FilterMode.Point;
                hbaoNoiseTex.wrapMode = TextureWrapMode.Repeat;
            }

            if (lastHBAONoiseTexParam == RayMarchingDirectionCount)
                return;

            float[] MersenneTwisterNumbers = new float[] {
            0.463937f,0.340042f,0.223035f,0.468465f,0.322224f,0.979269f,0.031798f,0.973392f,0.778313f,0.456168f,0.258593f,0.330083f,0.387332f,0.380117f,0.179842f,0.910755f,
            0.511623f,0.092933f,0.180794f,0.620153f,0.101348f,0.556342f,0.642479f,0.442008f,0.215115f,0.475218f,0.157357f,0.568868f,0.501241f,0.629229f,0.699218f,0.707733f
        };

            int z = 0;
            for (int x = 0; x < size; ++x)
            {
                for (int y = 0; y < size; ++y)
                {
                    float r1 = MersenneTwisterNumbers[z++];
                    float r2 = MersenneTwisterNumbers[z++];
                    float angle = 2.0f * Mathf.PI * r1 / RayMarchingDirectionCount;
                    Color color = new Color(Mathf.Cos(angle), Mathf.Sin(angle), r2);
                    hbaoNoiseTex.SetPixel(x, y, color);
                }
            }

            hbaoNoiseTex.Apply();

            lastHBAONoiseTexParam = RayMarchingDirectionCount;
        }

        void HBAO(CommandBuffer cmd, RenderTexture source)
        {
            CreateRandomTexture(m_hbao.RayMarchingDirectionCount);

            var mat = m_materials.hbaoMat;

            int sourceWidth = ColorTex.width;
            int sourceHeight = ColorTex.height;

            mat.SetFloat("_Intensity", Mathf.Max(m_hbao.Strength, 0.001f));
            mat.SetFloat("_MaxPixelRadius", m_hbao.MaxPixelRadius);
            mat.SetInt("_RayMarchingStepCount", m_hbao.RayMarchingStepCount);
            mat.SetInt("_RayMarchingDirectionCount", m_hbao.RayMarchingDirectionCount);
            mat.SetFloat("_AngleBiasValue", m_hbao.AngleBiasValue);
            mat.SetFloat("_AOmultiplier", 2.0f * (1.0f / (1.0f - m_hbao.AngleBiasValue)));
            mat.SetFloat("_MaxDistance", Mathf.Max(m_hbao.MaxDistance, 0.01f));
            mat.SetFloat("_DistanceFalloff", Mathf.Max(m_hbao.DistanceFalloff, 0.01f));

            float tanHalfFovY = Mathf.Tan(0.5f * mainCamera.fieldOfView * Mathf.Deg2Rad);
            float invFocalLenX = 1.0f / (1.0f / tanHalfFovY * (sourceHeight / (float)sourceWidth));
            float invFocalLenY = 1.0f / (1.0f / tanHalfFovY);
            mat.SetVector("_UVToView", new Vector4(2.0f * invFocalLenX, -2.0f * invFocalLenY, -1.0f * invFocalLenX, 1.0f * invFocalLenY));

            mat.SetFloat("_Radius", m_hbao.RayMarchingRadius * 0.5f * (sourceHeight / (tanHalfFovY * 2.0f)));
            mat.SetFloat("_NegInvRadius2", -1 / (m_hbao.RayMarchingRadius * m_hbao.RayMarchingRadius));
            mat.SetTexture("_NoiseTex", hbaoNoiseTex);
            mat.SetVector("_TargetScale", new Vector4((sourceWidth + 0.5f) / sourceWidth, (sourceHeight + 0.5f) / sourceHeight, 1f, 1f));
            mat.SetVector("_Offset_X", new Vector2(1, 0) * m_hbao.BlurRadius);
            mat.SetVector("_Offset_Y", new Vector2(0, 1) * m_hbao.BlurRadius * 0.5f);

            int rtWidth = sourceWidth >> m_hbao.DownSample;
            int rtHeight = sourceHeight >> m_hbao.DownSample;

            int aoRT = Shader.PropertyToID("HBAO RT");
            int blurRT = Shader.PropertyToID("HBAO BLUR RT");

            cmd.GetTemporaryRT(aoRT, rtWidth, rtHeight, 0, FilterMode.Bilinear);
            cmd.GetTemporaryRT(blurRT, rtWidth >> 1, rtHeight >> 1, 0, FilterMode.Bilinear);

            cmd.Blit(ColorTex, aoRT, mat, 0);

            cmd.Blit(aoRT, blurRT, mat, 4);

            cmd.Blit(blurRT, aoRT, mat, 5);

            cmd.SetGlobalTexture("_AOTex", aoRT);
            cmd.Blit(null, ColorTex, mat, 1);

            cmd.ReleaseTemporaryRT(aoRT);
            cmd.ReleaseTemporaryRT(blurRT);
        }

        static int dof_temp1 = Shader.PropertyToID("Dof_temp1");
        static int dof_temp2 = Shader.PropertyToID("Dof_temp2");

        void DepthOfField(CommandBuffer cmd, RenderTexture source, RenderTexture destination)
        {
            var mat = m_materials.depthOfFieldMat;

            float far = FocalDistance01(m_dof.farDistance);
            float near = FocalDistance01(m_dof.nearDistance);
            mat.SetVector("_parameter", new Vector4(far, near, m_dof.blurScale, 1));

            cmd.GetTemporaryRT(dof_temp1, source.width, source.height, 0, FilterMode.Bilinear, ARGBHalfFormat, RenderTextureReadWrite.Default);
            cmd.GetTemporaryRT(dof_temp2, source.width, source.height, 0, FilterMode.Bilinear, ARGBHalfFormat, RenderTextureReadWrite.Default);

            cmd.Blit(source, dof_temp1, mat, 2);
            cmd.SetGlobalVector("_offsets", new Vector4(0, m_dof.offset, 0, 0));
            cmd.Blit(dof_temp1, dof_temp2, mat, 0);
            cmd.SetGlobalVector("_offsets", new Vector4(m_dof.offset, 0, 0, 0));
            cmd.Blit(dof_temp2, dof_temp1, mat, 0);

            cmd.SetGlobalTexture("_BlurTex", dof_temp1);
            cmd.Blit(source, destination, mat, 1);

            cmd.ReleaseTemporaryRT(dof_temp1);
            cmd.ReleaseTemporaryRT(dof_temp2);
        }

        private float FocalDistance01(float distance)
        {
            return MainCamera.WorldToViewportPoint((distance - MainCamera.nearClipPlane) * MainCamera.transform.forward + MainCamera.transform.position).z / (MainCamera.farClipPlane - MainCamera.nearClipPlane);
        }

        static readonly int MarkTexID = Shader.PropertyToID("_SSSSMarkTex");
        static readonly int StencilTexID = Shader.PropertyToID("_StencilTex");
        static readonly int Kernel = Shader.PropertyToID("_Kernel");
        static readonly int StencilID = Shader.PropertyToID("_Stencil");
        static readonly int SSSScaler = Shader.PropertyToID("_SSSScale");
        static readonly int PickSample = 5;
        private Vector4[] KernelArray = null;

        private Color lastSubsurfaceColor, lastSubsurfaceFalloff;

        private void InitSSSS(Color SubsurfaceColor, Color SubsurfaceFalloff)
        {
            if (KernelArray != null && lastSubsurfaceColor == SubsurfaceColor && lastSubsurfaceFalloff == SubsurfaceFalloff)
                return;

            KernelArray = new Vector4[PickSample];
            lastSubsurfaceColor = SubsurfaceColor;
            lastSubsurfaceFalloff = SubsurfaceFalloff;

            Vector3 SSSC = Vector3.Normalize(new Vector3(SubsurfaceColor.r, SubsurfaceColor.g, SubsurfaceColor.b));
            Vector3 SSSFC = Vector3.Normalize(new Vector3(SubsurfaceFalloff.r, SubsurfaceFalloff.g, SubsurfaceFalloff.b));

            CalculateKernel(KernelArray, PickSample, SSSC, SSSFC);
        }

        void SSSS(CommandBuffer cmd, RenderTexture source)
        {
            var mat = m_materials.ssssMat;

            int sourceWidth = ColorTex.width;
            int sourceHeight = ColorTex.height;

            InitSSSS(m_ssss.SubsurfaceColor, m_ssss.SubsurfaceFalloff);

            mat.SetVectorArray(Kernel, KernelArray);
            mat.SetInt(StencilID, m_ssss.Stencil);
            mat.SetFloat(SSSScaler, m_ssss.SubsurfaceScaler);

            cmd.GetTemporaryRT(MarkTexID, sourceWidth, sourceHeight, 0, FilterMode.Point, R8Format, RenderTextureReadWrite.Linear);
            SSSS_BlitMask(cmd, ColorTex.colorBuffer, MarkTexID, DepthTex.depthBuffer, mat, 2);
            SSSS_BlitStencil(cmd, ColorTex.colorBuffer, DesTex.colorBuffer, DepthTex.depthBuffer, mat, 0);
            SSSS_BlitStencil(cmd, DesTex.colorBuffer, ColorTex.colorBuffer, DepthTex.depthBuffer, mat, 1);
            cmd.ReleaseTemporaryRT(MarkTexID);
        }

        public void CalculateKernel(Vector4[] kernel, int nSamples, Vector3 strength, Vector3 falloff)
        {
            float RANGE = nSamples > 20 ? 3.0f : 2.0f;
            float EXPONENT = 2.0f;

            // Calculate the SSS_Offset_UV:
            float step = 2.0f * RANGE / (nSamples - 1);
            for (int i = 0; i < nSamples; i++)
            {
                float o = -RANGE + i * step;
                float sign = o < 0.0f ? -1.0f : 1.0f;
                float w = RANGE * sign * Mathf.Abs(Mathf.Pow(o, EXPONENT)) / Mathf.Pow(RANGE, EXPONENT);
                kernel[i] = new Vector4(0, 0, 0, w);
            }
            // Calculate the SSS_Scale:
            for (int i = 0; i < nSamples; i++)
            {
                float w0 = i > 0 ? Mathf.Abs(kernel[i].w - kernel[i - 1].w) : 0.0f;
                float w1 = i < nSamples - 1 ? Mathf.Abs(kernel[i].w - kernel[i + 1].w) : 0.0f;
                float area = (w0 + w1) / 2.0f;
                Vector3 temp = profile(kernel[i].w, falloff);
                Vector4 tt = new Vector4(area * temp.x, area * temp.y, area * temp.z, kernel[i].w);
                kernel[i] = tt;
            }
            Vector4 t = kernel[nSamples / 2];
            for (int i = nSamples / 2; i > 0; i--)
                kernel[i] = kernel[i - 1];
            kernel[0] = t;
            Vector4 sum = Vector4.zero;

            for (int i = 0; i < nSamples; i++)
            {
                sum.x += kernel[i].x;
                sum.y += kernel[i].y;
                sum.z += kernel[i].z;
            }

            for (int i = 0; i < nSamples; i++)
            {
                Vector4 vecx = kernel[i];
                vecx.x /= sum.x;
                vecx.y /= sum.y;
                vecx.z /= sum.z;
                kernel[i] = vecx;
            }

            Vector4 vec = kernel[0];
            vec.x = (1.0f - strength.x) * 1.0f + strength.x * vec.x;
            vec.y = (1.0f - strength.y) * 1.0f + strength.y * vec.y;
            vec.z = (1.0f - strength.z) * 1.0f + strength.z * vec.z;
            kernel[0] = vec;

            for (int i = 1; i < nSamples; i++)
            {
                var vect = kernel[i];
                vect.x *= strength.x;
                vect.y *= strength.y;
                vect.z *= strength.z;
                kernel[i] = vect;
            }
        }

        private Vector3 gaussian(float variance, float r, Vector3 falloff)
        {
            Vector3 g;

            float rr1 = r / (0.001f + falloff.x);
            g.x = Mathf.Exp((-(rr1 * rr1)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

            float rr2 = r / (0.001f + falloff.y);
            g.y = Mathf.Exp((-(rr2 * rr2)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

            float rr3 = r / (0.001f + falloff.z);
            g.z = Mathf.Exp((-(rr3 * rr3)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

            return g;
        }
        private Vector3 profile(float r, Vector3 falloff)
        {
            return 0.100f * gaussian(0.0484f, r, falloff) +
                    0.118f * gaussian(0.187f, r, falloff) +
                    0.113f * gaussian(0.567f, r, falloff) +
                    0.358f * gaussian(1.99f, r, falloff) +
                    0.078f * gaussian(7.41f, r, falloff);
        }

        void SSSS_BlitMask(CommandBuffer buffer, RenderTargetIdentifier colorSrc, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthStencilBuffer, Material mat, int pass)
        {
            buffer.SetGlobalTexture(StencilTexID, colorSrc);
            buffer.SetRenderTarget(colorBuffer, depthStencilBuffer);
            buffer.ClearRenderTarget(false, true, Color.black);
            buffer.DrawMesh(m_mesh, Matrix4x4.identity, mat, 0, pass);
            buffer.SetGlobalTexture(MarkTexID, colorBuffer);
        }

        void SSSS_BlitStencil(CommandBuffer buffer, RenderTargetIdentifier colorSrc, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthStencilBuffer, Material mat, int pass)
        {
            buffer.SetGlobalTexture(StencilTexID, colorSrc);
            buffer.SetRenderTarget(colorBuffer, depthStencilBuffer);
            buffer.DrawMesh(m_mesh, Matrix4x4.identity, mat, 0, pass);
        }

        [Range(0.0312f, 0.0833f)]
        public float contrastThreshold = 0.0312f;
        [Range(0.063f, 0.333f)]
        public float relativeThreshold = 0.063f;
        [Range(0.1f, 3.0f)]
        public float pointScale = 1.0f;
        [Range(1.0f, 2.0f)]
        public float sharpness = 1.1f;

        void FXAA3(CommandBuffer cmd, RenderTexture source, RenderTexture destination)
        {
            var mat = m_materials.fxaaMat;

            mat.SetFloat("_ContrastThreshold", contrastThreshold);
            mat.SetFloat("_RelativeThreshold", relativeThreshold);
            mat.SetFloat("_PointScale", pointScale);
            mat.SetFloat("_Sharpness", sharpness);

            mat.EnableKeyword("ISMOBILE");
            mat.EnableKeyword("LUMINANCE_GREEN");

            cmd.Blit(source, destination, mat, 1);
        }

        static int forceSwitchTempRT = Shader.PropertyToID("_PP_FS_RT");

        void ForceSwitch(CommandBuffer cmd)
        {
            cmd.GetTemporaryRT(forceSwitchTempRT, 1, 1, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.SetRenderTarget(forceSwitchTempRT);
            cmd.ClearRenderTarget(false, true, Color.clear);
            cmd.ReleaseTemporaryRT(forceSwitchTempRT);
        }

        void CopyDepth(CommandBuffer cmd)
        {
            cmd.Blit(DepthTex.depthBuffer, DepthBuffer.colorBuffer);
            cmd.SetGlobalTexture(depthTexName, DepthBuffer);
        }

        void Vignette(CommandBuffer cmd, RenderTexture source, RenderTexture destination)
        {
            var mat = m_materials.vignetteMat;
            int pass;

            cmd.SetGlobalColor("_Vignette_Color", m_vignette.color.linear);

            if (m_vignette.mode == VignetteMode.Classic)
            {
                cmd.SetGlobalVector("_Vignette_Center", m_vignette.center);
                float roundness = (1 - m_vignette.roundness) * 6 + m_vignette.roundness;
                cmd.SetGlobalVector("_Vignette_Settings", new Vector4(m_vignette.intensity * 3, m_vignette.smoothness * 5, roundness, m_vignette.rounded ? 1 : 0));
                pass = 0;
            }
            else
            {
                cmd.SetGlobalTexture("_Vignette_Mask", m_vignette.mask);
                cmd.SetGlobalFloat("_Vignette_Opacity", m_vignette.opacity);
                pass = 1;
            }

            cmd.Blit(source, destination, mat, pass);
        }

        private bool doFinalBlit = true;

        public override void ChangeDoBlit(bool doBlit)
        {
            doFinalBlit = doBlit;
        }

        void FinalBlit(RenderTexture source)
        {
            if (doFinalBlit)
            {
                m_cmdPostProcess.Blit(source, null as RenderTexture);
            }
        }
    }
}